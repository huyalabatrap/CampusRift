#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CampusRift.Learning;
using CampusRift.Progression;
namespace CampusRift.UI
{
    // Text audits only: every authored reading page, every question layout, credits, and actual UI timers.
    public sealed class P23ReadingVariantsSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public List<string> passed=new List<string>(),failed=new List<string>();public int audits;public float ordinaryPractice,slowPractice,examSeconds;}
        readonly Report report=new Report();GameSettings original;LearningEngine oldEngine;LearningUI view;const string Root="task/p23/reading-variants/";
        readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        void Check(bool ok,string name){(ok?report.passed:report.failed).Add(name);Save();}
        void Save(){File.WriteAllText(Root+"result.json",JsonUtility.ToJson(report,true));}
        IEnumerator Audit(string name)
        {
            yield return new WaitForSecondsRealtime(.32f);yield return new WaitForEndOfFrame();
            var audit=ComicTextAudit.Scan(name);report.audits++;ComicTextAudit.Save(audit,Root+name+".json");Check(audit.issues.Count==0,name+" ComicTextAudit0");
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Root);DontDestroyOnLoad(gameObject);Application.runInBackground=true;TutorialDirector.Suppress=true;
            original=SettingsManager.Instance.Current.Copy();oldEngine=LearningService.Instance.Engine;ProfileService.Instance.UseTransient(new ProfileData());
            var engine=new LearningEngine(oldEngine.Catalog,new ProfileLearningStore(ProfileService.Instance),23,ProfileService.Instance.Cultivation);LearningService.Instance.EditorUseEngine(engine);
            foreach(var lesson in engine.Catalog.courses.SelectMany(c=>c.lessons)){var p=engine.Progress.Lesson(lesson.id);p.completed=p.rewarded=true;p.pagesRead=lesson.pages.Count;}
            GameSceneManager.Instance.LoadMainMenu();while(GameSceneManager.Instance.IsLoading)yield return null;yield return new WaitForSecondsRealtime(.3f);
            var run=Run(engine);var stack=new Stack<IEnumerator>();stack.Push(run);
            while(stack.Count>0){var e=stack.Peek();bool more=false;object next=null;try{more=e.MoveNext();if(more)next=e.Current;}catch(Exception ex){Check(false,ex.ToString());break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
            SettingsManager.Instance.Apply(original,false);LearningService.Instance.EditorUseEngine(oldEngine);ProfileService.Instance.EndTransient();Save();File.WriteAllText(Root+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
        IEnumerator Run(LearningEngine engine)
        {
            var state=UIStateManager.Instance;var questions=engine.Catalog.courses.SelectMany(c=>c.lessons).SelectMany(l=>l.questionBank.questions).GroupBy(q=>q.type).Select(g=>g.OrderByDescending(q=>q.prompt.Length+q.options.Sum(o=>o.text.Length)).First()).ToArray();
            for(int size=0;size<3;size++)
            {
                var setting=original.Copy();setting.TelemetryConsentAsked=true;setting.LocalTelemetryEnabled=false;setting.TextSize=size;setting.AccessibleColors=AccessiblePalette.ColorBlind;setting.Language=Localization.GameLanguage.Vietnamese;setting.ResolutionWidth=1920;setting.ResolutionHeight=1080;setting.Fullscreen=false;SettingsManager.Instance.Apply(setting,false);
                state.EnterScene(false);state.OpenCredits();yield return null;var credits=FindAnyObjectByType<P21CreditsMenu>();
                var pages=typeof(P21CreditsMenu).GetField("pages",flags).GetValue(credits) as string[];
                for(int i=0;i<pages.Length;i++){credits.Show(i);yield return Audit("size"+size+"-credits"+i);}
                state.Back();yield return new WaitForSecondsRealtime(.3f);state.OpenHub();yield return new WaitForSecondsRealtime(.3f);LearningUI.PendingScreen="Lessons:0";state.OpenCourse();yield return new WaitForSecondsRealtime(.3f);view=FindAnyObjectByType<LearningUI>();
                foreach(var course in engine.Catalog.courses)
                {
                    view.ShowLessons(course);
                    foreach(var lesson in course.lessons)for(int page=0;page<lesson.pages.Count;page++)
                    {engine.Progress.Lesson(lesson.id).pagesRead=page;view.OpenLesson(lesson);yield return Audit("size"+size+"-read-"+lesson.id+"-"+page);}
                }
                foreach(var q in questions)
                {engine.Progress.notebook.Clear();engine.Progress.notebook.Add(new WrongQuestionProgress{id=q.id,lastWrongUtc=engine.Now.ToString("o")});view.StartNotebook();yield return Audit("size"+size+"-question-"+q.type);}
                view.Close();yield return new WaitForSecondsRealtime(.3f);
            }
            LearningUI.PendingScreen="Practice";state.OpenCourse();yield return null;view=FindAnyObjectByType<LearningUI>();
            var s=SettingsManager.Instance.Current.Copy();s.SlowReading=false;SettingsManager.Instance.Apply(s,false);view.StartPractice();report.ordinaryPractice=(float)typeof(LearningUI).GetField("practiceEndsAt",flags).GetValue(view)-Time.unscaledTime;
            s.SlowReading=true;SettingsManager.Instance.Apply(s,false);view.StartPractice();report.slowPractice=(float)typeof(LearningUI).GetField("practiceEndsAt",flags).GetValue(view)-Time.unscaledTime;
            Check(Mathf.Abs(report.ordinaryPractice-300)<1&&Mathf.Abs(report.slowPractice-450)<1,"Actual practice UI deadline300s to450s");
            ProfileService.Instance.Cultivation.SetState(Realm.LuyenKhi,5,100);view.ShowExamIntro(engine.Catalog.courses[0]);typeof(LearningUI).GetMethod("StartExam",flags).Invoke(view,null);yield return null;
            var exam=(BreakthroughExam)typeof(LearningUI).GetField("exam",flags).GetValue(view);report.examSeconds=exam.TotalSeconds;
            Check(view.Screen=="Quiz"&&Mathf.Approximately(report.examSeconds,engine.Catalog.courses[0].examMinutes*60f),"Real exam UI ignores slow reading multiplier");yield return Audit("exam-slow-reading");
        }
    }
}
#endif
