#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Learning;
using CampusRift.UI;
using CampusRift.Skills;
using TMPro;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CampusRift.Localization
{
    public sealed class LocalizationPlayTest : MonoBehaviour
    {
        [Serializable] sealed class Report {public List<string> passed=new List<string>(),failed=new List<string>();}
        public bool CurriculumOnly;
        readonly Report report=new Report();LearningEngine original,engine;GameSettings settingsBefore;string originalPrefs;bool hadPrefs;
        LocalizationService Loc=>LocalizationService.Instance;
        UIManager UI=>Object.FindAnyObjectByType<UIManager>();
        LearningUI View=>Object.FindAnyObjectByType<LearningUI>();
        UIStateManager State=>UIStateManager.Instance;
        const string Output="Artifacts/Localization/";
        void Check(bool ok,string label){(ok?report.passed:report.failed).Add(label);File.WriteAllText(Output+"PlayMode.json",JsonUtility.ToJson(report,true));}
        public static void Begin(){var go=new GameObject("Localization acceptance");DontDestroyOnLoad(go);go.AddComponent<LocalizationPlayTest>();}
        void Awake(){DontDestroyOnLoad(gameObject);} // Acceptance crosses actual Hub/gameplay scenes.
        IEnumerator Start()
        {
            Directory.CreateDirectory(Output);Application.runInBackground=true;hadPrefs=PlayerPrefs.HasKey("CampusRift.Settings.v1");originalPrefs=PlayerPrefs.GetString("CampusRift.Settings.v1","");
            original=LearningService.Instance.Engine;CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());settingsBefore=SettingsManager.Instance.Current.Copy();
            engine=new LearningEngine(original.Catalog,new JsonLearningStore(Path.GetFullPath(Output+"qa-"+Guid.NewGuid().ToString("N")+".json")),947);
            LearningService.Instance.EditorUseEngine(engine);
            var run=Run();while(true)
            {
                bool moved=false;object item=null;try{moved=run.MoveNext();if(moved)item=run.Current;}catch(Exception e){Check(false,e.ToString());break;}
                if(!moved)break;yield return item;
            }
            // Finish in Vietnamese, preserving every other user setting and the production learning store.
            SettingsManager.Instance.Apply(settingsBefore,false);if(hadPrefs)PlayerPrefs.SetString("CampusRift.Settings.v1",originalPrefs);else PlayerPrefs.DeleteKey("CampusRift.Settings.v1");PlayerPrefs.Save();
            CampusRift.Progression.ProfileService.Instance.EndTransient();LearningService.Instance.EditorUseEngine(original);
            File.WriteAllText(Output+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
            Debug.Log("LOCALIZATION QA: "+report.passed.Count+" passed / "+report.failed.Count+" failed");Destroy(gameObject);
        }
        IEnumerator Frames(){yield return null;yield return new WaitForSecondsRealtime(.15f);Loc.Discover();Canvas.ForceUpdateCanvases();yield return null;}
        IEnumerator Loaded()
        {
            float until=Time.realtimeSinceStartup+60;while(GameSceneManager.Instance.IsLoading && Time.realtimeSinceStartup<until)yield return null;
            if(GameSceneManager.Instance.IsLoading)throw new Exception("Scene timeout");yield return Frames();
        }
        void Click(string english)
        {
            var b=View.content.GetComponentsInChildren<RiftButton>().First(x=>LocalizedText.Canonical(x.Label)==english);
            if(!b.IsInteractable())throw new Exception("Disabled choice: "+english);b.onClick.Invoke();
        }
        QuizSession Session=>(QuizSession)typeof(LearningUI).GetField("quiz",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(View);
        // P09 Hub and P17 full curriculum replace P06 title-button/5-lesson breakthrough assumptions.
        void CheckCurriculum(bool includeOptions)
        {
            bool content=true,options=true;int lessons=0,questions=0;var contentDetails=new List<string>();Func<string,string,string,bool> map=(en,vn,label)=>{bool ok=Loc.Translate(en??"")== (vn??"");if(!ok)contentDetails.Add(label+": EN="+en+"; VN="+vn+"; ACTUAL="+Loc.Translate(en??""));return ok;};
            foreach(var c in engine.Catalog.courses)foreach(var l in c.lessons)
            {
                lessons++;content &= !string.IsNullOrEmpty(l.titleVN) && map(l.title,l.titleVN,l.id+" title");
                foreach(var page in l.pages)content &= map(page.content,page.contentVN,l.id+" content") && map(page.example,page.exampleVN,l.id+" example") && map(page.takeaway,page.takeawayVN,l.id+" takeaway");
                foreach(var q in l.questionBank.questions){questions++;content &= map(q.prompt,q.promptVN,q.id+" prompt") && map(q.explanation,q.explanationVN,q.id+" explanation");foreach(var o in q.options)options &= Loc.Translate(o.text)==o.textVN;}
            }
            foreach(var c in engine.Catalog.courses)if(c.examBank!=null)foreach(var q in c.examBank.questions)
            {questions++;content &= map(q.prompt,q.promptVN,q.id+" exam prompt") && map(q.explanation,q.explanationVN,q.id+" exam explanation");foreach(var o in q.options)options &= Loc.Translate(o.text??"")==(o.textVN??"");}
            File.WriteAllText(Output+"content-details.txt",$"lessons={lessons}; questions={questions}\n"+string.Join("\n",contentDetails));
            Check(content && lessons==21 && questions==449,"Current curriculum21 lessons/449 questions and explanations resolve to Vietnamese");
            if(includeOptions)Check(options,"All answer options retain their ids and localized/math text");
        }
        IEnumerator Run()
        {
            if(CurriculumOnly)
            {
                var language=SettingsManager.Instance.Current.Copy();language.Language=GameLanguage.Vietnamese;SettingsManager.Instance.Apply(language,false);
                yield return Frames();CheckCurriculum(false);yield break;
            }
            GameSceneManager.Instance.LoadMainMenu();yield return Loaded();
            if(State.State==UIState.Hub)State.Back();yield return Frames();
            var start=SettingsManager.Instance.Current.Copy();start.Language=GameLanguage.Vietnamese;SettingsManager.Instance.Apply(start,false);yield return Frames();
            var play=UI.MainMenu.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="PLAY").GetComponentInChildren<TMP_Text>(true);
            Check(play!=null && play.text==Loc.Translate("PLAY"),"Current comic title menu displays translated PLAY label");
            Check(Loc.VietnameseFont.HasCharacters("TRI THỨC = SỨC MẠNH; Thiên Thủ Trấn Áp; ĐỘT PHÁ; Nguyễn",out uint[] missing,true,true),"Vietnamese font contains representative accented glyphs");
            State.OpenSettings();yield return Frames();var settings=Object.FindAnyObjectByType<SettingsUI>();
            Check(settings.Language!=null && settings.Language.options.Count==2,"Settings has EN/VN selector");
            settings.Language.value=0;yield return Frames();
            Check(Loc.Language==GameLanguage.English && play.text=="PLAY","English preview updates hidden and visible current comic text");
            Check(SettingsManager.Instance.Current.Language==GameLanguage.Vietnamese,"Preview does not commit saved language");
            settings.Cancel();yield return Frames();Check(Loc.Language==GameLanguage.Vietnamese && play.text==Loc.Translate("PLAY"),"Cancel restores Vietnamese language");
            State.OpenSettings();yield return Frames();settings.Language.value=0;settings.Apply();yield return Frames();
            Check(SettingsManager.Instance.ReadSaved().Language==GameLanguage.English,"Apply persists English");
            settings.Language.value=1;settings.Apply();yield return Frames();
            Check(SettingsManager.Instance.ReadSaved().Language==GameLanguage.Vietnamese,"Apply persists Vietnamese");settings.Cancel();
            CheckCurriculum(true);
            State.OpenHub();yield return Frames();Check(HubUI.Instance!=null&&HubUI.Instance.Visible,"P09 title enters actual Hub");
            LearningUI.PendingScreen="Lessons:0";State.OpenCourse();yield return Frames(); // P09 Hub Library opens a selected chapter directly.
            var course=engine.Catalog.courses[0];var lesson=course.lessons[0];Check(View!=null&&View.heading.text==course.titleVN,"Hub Library opens the selected chapter in Vietnamese");View.OpenLesson(lesson);yield return Frames();
            Check(View.content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==lesson.pages[0].contentVN),"Actual lesson body is Vietnamese");
            for(int i=0;i<lesson.pages.Count;i++){Click(i<lesson.pages.Count-1?"READ & NEXT PAGE":"COMPLETE READING");yield return Frames();}
            Click("START QUIZ");yield return Frames();var first=Session.Questions[0];string signature=Session.Signature;
            Check(View.content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==first.Data.promptVN),"Actual quiz question displays Vietnamese");
            Loc.Preview(GameLanguage.English);yield return Frames();
            Check(Session.Signature==signature && Session.Answered==0 && View.content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==first.Data.prompt),"Mid-quiz preview preserves order/session");Loc.Restore();yield return Frames();
            View.Answer(first.Data.correctOptionIds);yield return Frames();
            Check(View.content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==first.Data.explanationVN),"Actual answer explanation displays Vietnamese");View.NextQuestion();
            while(!Session.Complete){var q=Session.Questions[Session.Answered].Data;View.Answer(q.correctOptionIds);View.NextQuestion();}
            yield return Frames();Check(engine.Progress.Lesson(lesson.id).rewarded,"Localized quiz submits actual first-mastery reward");
            string earned=JsonUtility.ToJson(engine.Progress);Loc.Preview(GameLanguage.English);yield return Frames();
            Check(JsonUtility.ToJson(engine.Progress)==earned,"Language preview preserves all earned progress");Loc.Restore();View.Close();
            GameSceneManager.Instance.StartSandbox();yield return Loaded();State.EnterScene(true);yield return Frames();
            var player=Object.FindAnyObjectByType<CampusExplorer>();float hp=player.GetComponent<Monsters.PlayerMonsterHealth>().maxHealth;
            State.Pause();State.OpenSettings();yield return Frames();settings=Object.FindAnyObjectByType<SettingsUI>();settings.Language.value=0;settings.Apply();yield return Frames();
            Check(Loc.Language==GameLanguage.English && player.GetComponent<Monsters.PlayerMonsterHealth>().maxHealth==hp,"Gameplay language Apply preserves player stats");settings.Cancel();State.Resume();
            var vn=SettingsManager.Instance.Current.Copy();vn.Language=GameLanguage.Vietnamese;SettingsManager.Instance.Apply(vn,false);
            GameSceneManager.Instance.LoadMainMenu();yield return Loaded();
            Check(State.State==UIState.Hub && Loc.Language==GameLanguage.Vietnamese,"P09 return to Hub preserves Vietnamese");
        }

    }
}
#endif

