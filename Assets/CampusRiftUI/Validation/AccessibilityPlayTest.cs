#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TMPro;
using UnityEngine;
using CampusRift.Learning;
using CampusRift.Progression;
using CampusRift.Levels;
using CampusRift.Combat;
namespace CampusRift.UI
{
    public sealed class AccessibilityPlayTest:MonoBehaviour
    {
        [Serializable] public sealed class Report {public List<string> passed=new List<string>(),failed=new List<string>();public List<string> audits=new List<string>();}
        readonly Report report=new Report();const string Root="task/p23/";
        GameSettings original; string saved;bool hadSaved;
        public bool FailedScreensOnly;
        void Check(bool ok,string name){report.passed.Remove(name);report.failed.Remove(name);(ok?report.passed:report.failed).Add(name);Write();}
        void Write(){File.WriteAllText(Root+"accessibility.json",JsonUtility.ToJson(report,true));}
        IEnumerator Audit(string name,bool photo=false)
        {
            if(FailedScreensOnly&&!report.failed.Contains(name+" ComicTextAudit0"))yield break;
            yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();
            var a=ComicTextAudit.Scan(name);ComicTextAudit.Save(a,Root+"audits/"+name+".json");report.audits.Add(name);Check(a.issues.Count==0,name+" ComicTextAudit0");
            if(photo){var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+"screens/"+name+".png",frame.EncodeToPNG());Destroy(frame);}
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Root+"audits");Directory.CreateDirectory(Root+"screens");Application.runInBackground=true;TutorialDirector.Suppress=true;
            original=SettingsManager.Instance.Current.Copy();saved=PlayerPrefs.GetString("CampusRift.Settings.v1","");hadSaved=PlayerPrefs.HasKey("CampusRift.Settings.v1");
            if(FailedScreensOnly){var previous=JsonUtility.FromJson<Report>(File.ReadAllText(Root+"accessibility.json"));report.passed.AddRange(previous.passed);report.failed.AddRange(previous.failed);report.audits.AddRange(previous.audits);}
            var run=Run();while(true){bool more=false;object current=null;try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception e){Check(false,e.ToString());break;}if(!more)break;yield return current;}
            if(hadSaved)PlayerPrefs.SetString("CampusRift.Settings.v1",saved);else PlayerPrefs.DeleteKey("CampusRift.Settings.v1");PlayerPrefs.Save();
            SettingsManager.Instance.Apply(original,false);ProfileService.Instance.EndTransient();File.WriteAllText(Root+"accessibility-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
        IEnumerator Run()
        {
            ProfileService.Instance.UseTransient(new ProfileData());var p=ProfileService.Instance;p.Cultivation.SetState(Realm.DoKiep,1,0);foreach(int level in Enumerable.Range(1,10)){var d=p.Data.Level(level,true);d.cleared=true;d.stars=7;}
            var legacy=SettingsManager.Defaults();JsonUtility.FromJsonOverwrite("{\"Quality\":0}",legacy);Check(legacy.Subtitles&&legacy.TextSize==0,"Old Settings defaults preserve normal size and enabled captions");
            Check(Enum.GetValues(typeof(Element)).Cast<Element>().Select(Accessibility.Symbol).Distinct().Count()==9,"Nine distinct monochrome element symbols");
            Check(Enum.GetValues(typeof(Element)).Cast<Element>().All(e=>Accessibility.SymbolSprite(e)!=null),"Nine imported geometric sprites");
            var state=UIStateManager.Instance;var settings=SettingsManager.Instance;
            for(int size=0;size<3;size++)
            {
                var s=original.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.Language=Localization.GameLanguage.Vietnamese;s.TextSize=size;s.Subtitles=true;s.AccessibleColors=AccessiblePalette.ColorBlind;s.SlowReading=true;s.Fullscreen=false;s.ResolutionWidth=1920;s.ResolutionHeight=1080;s.ControlMode=Controls.ControlMode.PC;settings.Apply(s,false);
                state.EnterScene(false);yield return Audit("size"+size+"-menu");state.OpenSettings();yield return new WaitForSecondsRealtime(.3f);var view=FindAnyObjectByType<SettingsUI>();
                for(int tab=0;tab<4;tab++){view.SelectTab(tab);yield return Audit("size"+size+"-settings"+tab,size==2&&tab==3);}
                if(size==2){view.TextSizeDropdown.SetValueWithoutNotify(2);view.PaletteDropdown.SetValueWithoutNotify(1);view.SlowReadingToggle.SetIsOnWithoutNotify(true);view.SubtitlesToggle.SetIsOnWithoutNotify(true);view.Apply();var disk=settings.ReadSaved();Check(disk.TextSize==2&&disk.AccessibleColors==AccessiblePalette.ColorBlind&&disk.SlowReading&&disk.Subtitles,"Real Settings Apply and ReadSaved persist accessibility");}
                state.Back();state.OpenHub();yield return new WaitForSecondsRealtime(.3f);
                foreach(HubUI.Tab tab in Enum.GetValues(typeof(HubUI.Tab))){HubUI.Instance.Select(tab);yield return Audit("size"+size+"-hub"+tab);}
                foreach(HubUI.EndgamePage page in Enum.GetValues(typeof(HubUI.EndgamePage))){HubUI.Instance.OpenEndgame(page);yield return Audit("size"+size+"-endgame"+page);}
                LoadoutUI.Level=LevelCatalog.Instance.Get(10);state.OpenLoadout();yield return Audit("size"+size+"-loadout");state.Back();
                LearningUI.PendingScreen="Lessons:0";state.OpenCourse();yield return new WaitForSecondsRealtime(.3f);var study=FindAnyObjectByType<LearningUI>();var c=LearningService.Instance.Engine.Catalog.courses[0];study.ShowLessons(c);yield return Audit("size"+size+"-lessons");study.OpenLesson(c.lessons[0]);yield return Audit("size"+size+"-reading",size==2);
                study.StartQuiz();yield return Audit("size"+size+"-quiz");study.ShowExamIntro(c);yield return Audit("size"+size+"-exam-intro");study.Close();yield return new WaitForSecondsRealtime(.3f);state.OpenHub();
            }
            Check(Mathf.Approximately(Accessibility.ReadingTime(QuizKindProxy.Study),1.5f)&&Mathf.Approximately(Accessibility.ReadingTime(QuizKindProxy.Exam),1),"Slow reading multiplies practice time1.5; exam1.0; untimed lesson/stele unchanged");
        }
    }
}
#endif
