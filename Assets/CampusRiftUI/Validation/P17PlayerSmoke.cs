#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using CampusRift.Learning;
using UnityEngine;
using CampusRift.UI;
using CampusRift.Progression;
using CampusRift.Levels;
namespace CampusRift.Validation
{
    // Only an explicit command-line flag in a development player activates this QA helper.
    public sealed class P17PlayerSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p17-smoke")<0&&Array.IndexOf(Environment.GetCommandLineArgs(),"-p17-persistence")<0)return;
            var go=new GameObject("P17 development player smoke");DontDestroyOnLoad(go);go.AddComponent<P17PlayerSmoke>();
        }
        IEnumerator Shot(string path)
        {
            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,texture.EncodeToPNG());Destroy(texture);
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;string folder=Path.Combine(Application.dataPath,"../P17-Smoke");Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(2);
            var s=SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.Language=CampusRift.Localization.GameLanguage.Vietnamese;s.Fullscreen=false;s.ResolutionWidth=1280;s.ResolutionHeight=720;SettingsManager.Instance.Apply(s,false);
            var profile=ProfileService.Instance;string qaSave=Path.Combine(folder,"player-profile.json");profile.Initialize(new JsonProfileStore(qaSave),null);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p17-persistence")>=0)
            {
                bool retained=profile.Data.learning.lessons.Any(x=>x.rewarded)&&profile.Inventory.Count("hoi-khi-dan")==1;
                File.WriteAllText(Path.Combine(folder,"PERSISTENCE.txt"),(retained?"PASS":"FAIL")+" reopened player; mastered lesson="+profile.Data.learning.lessons.Any(x=>x.rewarded)+"; Recovery Pill="+profile.Inventory.Count("hoi-khi-dan"));Application.Quit();yield break;
            }
            // All automated answers are confined to this build-folder QA save, never the user's profile.
            var engine=new LearningEngine(Resources.Load<LearningCatalog>("LearningCatalog"),new ProfileLearningStore(profile),17,profile.Cultivation);engine.Economy=new StudyEconomy(profile.Wallet);
            typeof(LearningService).GetProperty("Engine").SetValue(LearningService.Instance,engine,null);
            UIStateManager.Instance.OpenHub();HubUI.Instance.Select(HubUI.Tab.Library);yield return new WaitForSecondsRealtime(.5f);
            HubUI.Instance.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Chapter 0").onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);
            var course=engine.Catalog.courses[0];var lesson=course.lessons[0];for(int i=0;i<lesson.pages.Count;i++)engine.ReadPage(course,lesson,i);
            var quiz=engine.StartQuiz(course,lesson);while(!quiz.Complete)quiz.Answer(quiz.Questions[quiz.Answered].Data.correctOptionIds);engine.Submit(quiz);
            UIStateManager.Instance.Back();HubUI.Instance.Select(HubUI.Tab.Shop);yield return new WaitForSecondsRealtime(.5f);
            HubUI.Instance.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Button TRAO ĐỔI").onClick.Invoke();profile.Flush();
            bool studyBuy=profile.Data.learning.Lesson(lesson.id).rewarded&&profile.Inventory.Count("hoi-khi-dan")==1;
            File.WriteAllText(Path.Combine(folder,"study-buy.txt"),(studyBuy?"PASS":"FAIL")+" automated study/quiz/buy through runtime rules; no human usability evidence; QA save="+qaSave+"; stones="+profile.Wallet.Balance);
            HubUI.Instance.Select(HubUI.Tab.Map);yield return new WaitForSecondsRealtime(2);
            yield return Shot(Path.Combine(folder,"windows-hub.png"));
            File.WriteAllText(Path.Combine(folder,"progress.txt"),"Hub visible="+(HubUI.Instance!=null&&HubUI.Instance.Visible));
            GameSceneManager.Instance.StartLevel(1);
            float until=Time.realtimeSinceStartup+45;while((GameSceneManager.Instance.IsLoading||LevelDirector.Instance==null)&&Time.realtimeSinceStartup<until)yield return null;
            yield return new WaitForSecondsRealtime(3);yield return Shot(Path.Combine(folder,"windows-level1.png"));
            var d=LevelDirector.Instance;File.WriteAllText(Path.Combine(folder,"DONE.txt"),"Hub + level1: "+(d!=null&&d.Level!=null&&d.Level.index==1?"PASS":"FAIL")+"\nLevel="+(d?.Level?.index??0)+"\nSpawned="+(d?.SpawnedTotal??0)+"\nTutorial="+(TutorialDirector.Instance?.Visible??false));
            profile.Flush();Application.Quit();
        }
    }
}
#endif
