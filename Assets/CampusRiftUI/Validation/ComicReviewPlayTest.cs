#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Learning;
using CampusRift.Levels;
using CampusRift.Localization;
using CampusRift.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace CampusRift.UI
{
    // Review-only fixtures. Runs the real UI, saves visible TMP bounds, captures scenes and restores settings/profile.
    public sealed class ComicReviewPlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report { public string started,finished,error;public int issues,visibleTexts;public List<ComicTextAudit.ScreenReport> screens=new List<ComicTextAudit.ScreenReport>(); }
        const string Folder="task/ui-comic/";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        readonly Report report=new Report();
        GameSettings original;
        public static void Begin()
        {if(!Application.isPlaying)throw new InvalidOperationException("Play mode required");new GameObject("Comic round 3 review").AddComponent<ComicReviewPlayTest>();}
        UIStateManager State=>UIStateManager.Instance;
        IEnumerator Wait(Func<bool> condition,float seconds=30)
        {float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;if(!condition())throw new TimeoutException("Comic review scene flow timed out");}
        IEnumerator Frames(int n=5){for(int i=0;i<n;i++)yield return null;}
        void Save(){Directory.CreateDirectory(Folder+"tests/round3");File.WriteAllText(Folder+"tests/round3/TextAudit.json",JsonUtility.ToJson(report,true));}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;
            report.started=DateTime.Now.ToString("s");original=SettingsManager.Instance.Current.Copy();
            var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0)
            {
                var run=stack.Peek();bool more=false;object next=null;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){report.error=e.ToString();break;}
                if(!more){stack.Pop();continue;}var nested=next as IEnumerator;if(nested!=null){stack.Push(nested);continue;}yield return next;
            }
            report.finished=DateTime.Now.ToString("s");Save();
            File.WriteAllText(Folder+"tests/round3/Review-DONE.txt",report.issues+" text issues; "+report.screens.Count+" screen audits; "+report.error);
            ComicRendering.PreviewEnabled=null;LevelSession.Clear();ProfileService.Instance.EndTransient();SettingsManager.Instance.Apply(original,false);
            UIValidation.SetResolution(1920,1080);Time.timeScale=1;
            Destroy(gameObject);
        }
        IEnumerator Capture(string name,bool image=true)
        {
            File.WriteAllText(Folder+"tests/round3/Review-PROGRESS.txt",name);
            yield return new WaitForSecondsRealtime(.22f);yield return Frames();
            var screen=ComicTextAudit.Scan(name);report.screens.Add(screen);report.issues+=screen.issues.Count;report.visibleTexts+=screen.visibleTexts;Save();
            if(image&&LevelHUD.Vietnamese)
            {
                string dir=Folder+"screens/round3/";Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(dir+name+(Screen.width==2340?"-wide":"")+".png");yield return Frames(3);
            }
        }
        IEnumerator MenuScene()
        {
            GameSceneManager.Instance.LoadMainMenu();yield return Wait(()=>!GameSceneManager.Instance.IsLoading&&HubUI.Instance!=null&&State.State==UIState.Hub);
            yield return Frames();State.Back();yield return Frames();
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Folder+"screens/round3/levels");Directory.CreateDirectory(Folder+"tests/round3");
            var data=new ProfileData();data.wallet.linhThach=1234;data.Level(1,true).bestTime=114;data.Level(1,true).cleared=true;
            ProfileService.Instance.UseTransient(data);ProfileService.Instance.Inventory.Add("hoi-khi-dan",3);ProfileService.Instance.Inventory.Add("tu-linh-dan",2);
            var settings=original.Copy();settings.Quality=1;settings.ControlMode=Controls.ControlMode.PC;settings.ComicEffects=true;settings.VSync=false;settings.SkyBrightness=1;settings.Language=GameLanguage.Vietnamese;
            SettingsManager.Instance.Apply(settings,false);yield return MenuScene();SettingsManager.Instance.Apply(settings,false);
            foreach(var language in new[]{GameLanguage.Vietnamese,GameLanguage.English})
            {
                settings.Language=language;SettingsManager.Instance.Apply(settings,false);
                foreach(int width in new[]{1920,2340})
                {
                    UIValidation.SetResolution(width,1080);yield return new WaitForSecondsRealtime(.4f);
                    if(State.State==UIState.Hub)State.Back();
                    yield return Capture("menu");State.OpenCredits();yield return Capture("credits");State.Back();
                    State.OpenSettings();yield return Frames();
                    var ui=UnityEngine.Object.FindAnyObjectByType<UIManager>();var controls=ui.Settings.GetComponent<SettingsUI>();
                    for(int tab=0;tab<3;tab++){controls.SelectTab(tab);yield return Capture("settings-"+new[]{"video","audio","gameplay"}[tab]);}
                    controls.ChooseMobile();yield return Capture("settings-mobile");controls.Cancel();yield return Frames();
                    State.OpenHub();yield return Frames();var hub=HubUI.Instance;
                    foreach(var tab in new[]{HubUI.Tab.Map,HubUI.Tab.Library,HubUI.Tab.Skills,HubUI.Tab.Realm,HubUI.Tab.Shop})
                    {
                        hub.Select(tab);yield return Capture(tab==HubUI.Tab.Map?"hub":tab==HubUI.Tab.Shop?"dan-cac":"hub-"+tab.ToString().ToLowerInvariant());
                        if(tab==HubUI.Tab.Shop)
                            for(int cat=1;cat<4;cat++)
                            {hub.ContentRect.GetComponentsInChildren<Button>().First(b=>b.name=="Category "+cat).onClick.Invoke();yield return Capture("dan-cac-"+cat);}
                    }
                    for(int level=1;level<=10;level++)
                    {
                        LoadoutUI.Level=LevelCatalog.Instance.Get(level);State.OpenLoadout();yield return Capture(level==1?"loadout":"loadout-"+level,level==1);State.Back();yield return Frames(2);
                    }
                    LearningUI.PendingScreen="Lessons:0";State.OpenCourse();yield return Frames();
                    var learning=UnityEngine.Object.FindAnyObjectByType<LearningUI>();var course=LearningService.Instance.Engine.Catalog.courses[0];
                    yield return Capture("lessons");learning.OpenLesson(course.lessons[0]);yield return Capture("lesson");learning.StartQuiz();yield return Capture("quiz");
                    var quiz=(QuizSession)typeof(LearningUI).GetField("quiz",Private).GetValue(learning);
                    while(!quiz.Complete)
                    {
                        var q=quiz.Questions[quiz.Answered];if(quiz.Answered>0)yield return Capture("quiz-question-"+(quiz.Answered+1),false);
                        learning.Answer(q.Data.type=="ordering"?q.Options.Select(o=>o.id).ToArray():new[]{q.Options[0].id});yield return Capture("quiz-explanation-"+quiz.Answered,false);learning.NextQuestion();
                    }
                    yield return Capture("quiz-result");learning.ShowExamIntro(course);yield return Capture("exam-intro");learning.Close();yield return Frames();
                    State.Back();yield return Frames();
                }
            }
            settings.Language=GameLanguage.Vietnamese;SettingsManager.Instance.Apply(settings,false);
            State.OpenHub();HubUI.Instance.Select(HubUI.Tab.Shop);yield return Frames();
            HubUI.Instance.ContentRect.GetComponentsInChildren<Button>().First(b=>b.name=="Category 0").onClick.Invoke();
            UIValidation.SetResolution(1080,1920);yield return Capture("dan-cac-phone-portrait");
            UIValidation.SetResolution(2340,1080);yield return Capture("dan-cac-phone-landscape");
            UIValidation.SetResolution(1920,1080);
            for(int index=1;index<=10;index++)
            {
                LevelSession.Select(index);
                if(index==1)GameSceneManager.Instance.StartLevel(1);else GameSceneManager.Instance.ReloadCurrentScene();
                yield return Wait(()=>!GameSceneManager.Instance.IsLoading&&LevelDirector.Instance!=null&&LevelDirector.Instance.Level!=null&&LevelDirector.Instance.Level.index==index);
                yield return Frames();
                SettingsManager.Instance.Apply(settings,false);var director=LevelDirector.Ensure();director.Begin(LevelCatalog.Instance.Get(index));State.EnterScene(true);Time.timeScale=0;yield return Frames(12);
                if(index==1)
                {
                    yield return Capture("hud-level01");
                    State.Pause();yield return Capture("pause");State.OpenSettings();yield return Capture("settings");State.Back();State.Resume();Time.timeScale=0;
                    foreach(int width in new[]{2340})
                    {UIValidation.SetResolution(width,1080);yield return Capture("hud-level01");State.Pause();yield return Capture("pause");State.Resume();Time.timeScale=0;}
                    UIValidation.SetResolution(1920,1080);
                }
                State.Pause();yield return Frames();var ui=UnityEngine.Object.FindAnyObjectByType<UIManager>();
                foreach(var panel in new[]{ui.PauseMenu,ui.GameOver,ui.Victory,ui.Settings,ui.Course})if(panel!=null)panel.gameObject.SetActive(false);
                var player=UnityEngine.Object.FindAnyObjectByType<CampusExplorer>();var camera=Camera.main;
                // A review-only open courtyard pose; authored spawn points and geometry stay intact.
                player.enabled=false;var controller=player.GetComponent<CharacterController>();controller.enabled=false;
                player.transform.position=new Vector3(10,.1f,-5);player.transform.rotation=Quaternion.identity;
                foreach(var renderer in player.GetComponentsInChildren<Renderer>())renderer.forceRenderingOff=false;
                camera.transform.position=player.transform.position+new Vector3(0,3.2f,-8);camera.transform.LookAt(player.transform.position+Vector3.up*2.8f);
                yield return Frames(8);ScreenCapture.CaptureScreenshot(Folder+"screens/round3/levels/level-"+index.ToString("00")+".png");yield return Frames(4);
            }
            // Complete level 1 through the existing enemy damage path, then audit both outcome panels.
            LevelSession.Select(1);GameSceneManager.Instance.ReloadCurrentScene();yield return Wait(()=>!GameSceneManager.Instance.IsLoading&&LevelDirector.Instance!=null&&LevelDirector.Instance.Level!=null&&LevelDirector.Instance.Level.index==1);
            SettingsManager.Instance.Apply(settings,false);var first=LevelDirector.Instance;first.introSeconds=.1f;first.winDelay=.1f;State.EnterScene(true);Time.timeScale=1;
            int guard=0;while(first.State!=LevelDirector.Phase.Won&&guard++<180)
            {
                foreach(var enemy in first.Alive.ToArray())if(enemy!=null&&enemy.Alive)enemy.Vitality.ApplyDamage(DamageInfo.Create(1e7f,Element.None,DamageSource.Melee,enemy.transform.position+Vector3.up,Vector3.forward));
                yield return new WaitForSecondsRealtime(.12f);
            }
            yield return Wait(()=>State.State==UIState.Victory,10);yield return new WaitForSecondsRealtime(1.3f);
            foreach(int width in new[]{1920,2340}){UIValidation.SetResolution(width,1080);yield return Capture("result");}
            State.EnterScene(true);State.Defeat();foreach(int width in new[]{1920,2340}){UIValidation.SetResolution(width,1080);yield return Capture("game-over");}
            yield return MenuScene();
        }
    }
}
#endif
