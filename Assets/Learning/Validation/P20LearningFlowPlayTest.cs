#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CampusRift.Localization;
using CampusRift.Progression;
using CampusRift.UI;
using UnityEngine;

namespace CampusRift.Learning
{
    // Small current-UI regression. Legacy LearningPlayTest still assumes the removed HUD ring.
    // Starts in SampleScene, uses a transient profile and enters study through the real Hub.
    public sealed class P20LearningFlowPlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report { public List<string> passed=new List<string>(), failed=new List<string>(); }
        readonly Report report=new Report();
        ProfileService profile; LearningEngine engine,originalEngine; GameSettings settings; LearningUI view;
        void Check(bool ok,string message)
        {
            (ok?report.passed:report.failed).Add(message);
            File.WriteAllText("task/p20/learning-flow.json",JsonUtility.ToJson(report,true));
        }
        IEnumerator Settle() { yield return null; yield return new WaitForSecondsRealtime(.15f); }
        void Click(string label)
        {
            var button=view.content.GetComponentsInChildren<RiftButton>().First(b=>b.Label.text.StartsWith(label));
            if(!button.IsInteractable())throw new InvalidOperationException("Unavailable button: "+label);
            button.onClick.Invoke();
        }
        QuizSession Session => (QuizSession)typeof(LearningUI).GetField("quiz",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
        IEnumerator AnswerSession()
        {
            var session=Session;
            while(!session.Complete)
            {
                view.Answer(session.Questions[session.Answered].Data.correctOptionIds);
                yield return null;
                if(session.Kind!=QuizKind.Exam)Click(session.Complete?"VIEW RESULT":"NEXT QUESTION");
                yield return null;
            }
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            profile=ProfileService.Instance;settings=SettingsManager.Instance.Current.Copy();originalEngine=LearningService.Instance.Engine;
            var english=settings.Copy();english.Language=GameLanguage.English;SettingsManager.Instance.Apply(english,false);
            profile.UseTransient(new ProfileData{tutorial=new TutorialProgress{skipHub=true,skipCombat=true,skipFire=true}});
            engine=new LearningEngine(Resources.Load<LearningCatalog>("LearningCatalog"),new ProfileLearningStore(profile),7,profile.Cultivation){Economy=new StudyEconomy(profile.Wallet)};
            LearningService.Instance.EditorUseEngine(engine);
            var run=Run();
            while(true)
            {
                bool more=false;object current=null;
                try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception e){Check(false,e.ToString());break;}
                if(!more)break;yield return current;
            }
            profile.EndTransient();LearningService.Instance.EditorUseEngine(originalEngine);SettingsManager.Instance.Apply(settings,false);
            File.WriteAllText("task/p20/LEARNING-FLOW-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
            Destroy(gameObject);
        }
        IEnumerator Run()
        {
            GameSceneManager.Instance.LoadMainMenu();
            float until=Time.realtimeSinceStartup+15;
            while((GameSceneManager.Instance.IsLoading||HubUI.Instance==null)&&Time.realtimeSinceStartup<until)yield return null;
            UIStateManager.Instance.OpenHub();yield return Settle();
            HubUI.Instance.Select(HubUI.Tab.Library);yield return Settle();
            HubUI.Instance.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name.StartsWith("Chapter ")).onClick.Invoke();
            yield return new WaitForSecondsRealtime(.6f);
            view=FindAnyObjectByType<LearningUI>();var course=engine.Catalog.courses[0];var lesson=course.lessons[0];
            Check(UIStateManager.Instance.State==UIState.Course&&view.Screen=="Lessons","Hub Library opens the current study panel");
            Click(lesson.title);yield return Settle();
            Check(view.Screen=="Lesson","Lesson opens from the real lesson list");
            while(view.Screen=="Lesson")
            {
                var button=view.content.GetComponentsInChildren<RiftButton>().First(b=>b.Label.text.StartsWith("READ & NEXT")||b.Label.text.StartsWith("COMPLETE READING"));
                button.onClick.Invoke();yield return null;
            }
            yield return Settle();
            Check(engine.Progress.Lesson(lesson.id).completed&&profile.Cultivation.Data.totalEarned>0,"Reading completes and pays Tu Vi");
            float afterRead=profile.Cultivation.Data.totalEarned;
            Click("START QUIZ");yield return Settle();
            Check(Session.Questions.Count==10&&view.Screen=="Quiz","Lesson quiz opens with ten real catalog questions");
            var answers=AnswerSession();while(answers.MoveNext())yield return answers.Current;
            yield return Settle();
            Check(view.Screen=="Result"&&engine.Progress.Lesson(lesson.id).rewarded,"Correct answers finish the quiz and master the lesson");
            Check(profile.Cultivation.Data.totalEarned>afterRead&&profile.Wallet.Balance>0,"First quiz pays Tu Vi and Linh Thach");
            float earned=profile.Cultivation.Data.totalEarned;
            Click("RETRY QUIZ");yield return Settle();
            answers=AnswerSession();while(answers.MoveNext())yield return answers.Current;yield return Settle();
            Check(Mathf.Approximately(earned,profile.Cultivation.Data.totalEarned),"Repeating a mastered quiz grants no first-pass Tu Vi");
            view.ShowCards();yield return Settle();
            Check(view.Screen=="Cards"&&engine.Cards(lesson).Count>0,"Read lesson becomes available in flashcards");
            view.ShowNotebook();yield return Settle();Check(view.Screen=="Notebook","Mistake notebook opens in the current UI");
            view.ShowDaily();yield return Settle();Check(view.Screen=="Daily"&&engine.Daily.State.lessons>0,"Daily progress reflects the completed reading");
            profile.Cultivation.SetState(Realm.LuyenKhi,5,100);view.ShowRealm();yield return Settle();
            Click("BREAKTHROUGH EXAM");yield return Settle();
            Check(view.Screen=="ExamIntro","Bottleneck opens the exam introduction");
            Click("START EXAM");yield return Settle();
            Check(Session.Kind==QuizKind.Exam&&Session.Questions.Count==20,"Exam starts with twenty real questions");
            answers=AnswerSession();while(answers.MoveNext())yield return answers.Current;
            yield return new WaitForSecondsRealtime(1.5f);
            Check(profile.Cultivation.Realm==Realm.TrucCo&&engine.Progress.Exam(course.id).passed,"Passing through the UI advances to Truc Co");
            view.ShowShop();yield return Settle();Click("RECOVERY");yield return Settle();Click("Qi Recovery Pill");yield return Settle();
            int wallet=profile.Wallet.Balance,owned=profile.Inventory.Count("hoi-khi-dan");
            Click("CONFIRM PURCHASE");yield return Settle();
            Check(profile.Wallet.Balance==wallet-25&&profile.Inventory.Count("hoi-khi-dan")==owned+1,"Study earnings buy an item through the real shop");
            var persisted=JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(profile.Data));
            Check(persisted.learning.lessons.Any(l=>l.id==lesson.id&&l.rewarded)&&persisted.learning.exams.Any(e=>e.course==course.id&&e.passed),"Learning and exam records survive profile serialization");
            view.Close();yield return new WaitForSecondsRealtime(.7f);
            Check(UIStateManager.Instance.State==UIState.Hub,"Leaving study returns to Hub");
        }
    }
}
#endif
