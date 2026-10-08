#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.UI;
using CampusRift.Progression;
using CampusRift.Levels;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Skills;

namespace CampusRift.Validation
{
    public sealed class P17Smoke : MonoBehaviour
    {
        [Serializable] sealed class Report { public List<string> passed=new List<string>(),failed=new List<string>(); }
        public bool FailedItemsOnly;
        Report report=new Report(); GameSettings original; string prefs; LevelDirector director;
        void Check(bool ok,string label){(ok?report.passed:report.failed).Add(label);Debug.Log("P17 "+(ok?"PASS ":"FAIL ")+label);}
        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("task/p17/screens/"+name+".png",texture.EncodeToPNG());Destroy(texture);
            var audit=ComicTextAudit.Scan(name);File.WriteAllText("task/p17/screens/"+name+"-text-audit.json",JsonUtility.ToJson(audit,true));Check(audit.issues.Count==0,name+" text fits");
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Directory.CreateDirectory("task/p17/screens");original=SettingsManager.Instance.Current.Copy();prefs=PlayerPrefs.GetString("CampusRift.Settings.v1","");
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){var routine=stack.Peek();bool more=false;object value=null;try{more=routine.MoveNext();if(more)value=routine.Current;}catch(Exception e){Check(false,e.ToString());break;}if(!more){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;}
            if(director!=null)director.End();ProfileService.Instance.EndTransient();TutorialDirector.Suppress=true;LocalTelemetry.TestFolder=null;
            SettingsManager.Instance.Apply(original,false);PlayerPrefs.SetString("CampusRift.Settings.v1",prefs);PlayerPrefs.Save();
            File.WriteAllText("task/p17/smoke.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/p17/smoke-DONE.txt",report.passed.Count+" PASS / "+report.failed.Count+" FAIL");Destroy(gameObject);
        }
        IEnumerator Run()
        {
            Application.runInBackground=true;UIValidation.SetResolution(1920,1080);TutorialDirector.Suppress=false;
            var s=original.Copy();s.Language=Localization.GameLanguage.Vietnamese;s.ControlMode=Controls.ControlMode.PC;s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;SettingsManager.Instance.Apply(s,false);
            ProfileService.Instance.UseTransient(new ProfileData());
            UIStateManager.Instance.EnterScene(true);director=LevelDirector.Ensure();director.Begin(LevelCatalog.Instance.Get(1));director.enabled=false;
            var player=FindAnyObjectByType<CampusExplorer>();player.enabled=false;var health=player.GetComponent<PlayerMonsterHealth>();health.respawnOnDefeat=false;health.Revive(1,0);
            if(FailedItemsOnly){
                s.ControlMode=Controls.ControlMode.Mobile;SettingsManager.Instance.Apply(s,false);yield return null;yield return null;
                yield return Shot("tutorial-mobile");
                string focusedTelemetry="task/p17/telemetry-smoke-"+Guid.NewGuid().ToString("N");LocalTelemetry.TestFolder=focusedTelemetry;
                LevelEvents.RaiseStarted(LevelCatalog.Instance.Get(1));LocalTelemetry.Skill("giant-hand-seal");LocalTelemetry.Instance.Finish("won",1,4);
                Check(!Directory.Exists(focusedTelemetry),"opt-out writes no file or directory");yield break;
            }
            yield return null;yield return null;Check(TutorialDirector.Instance.Visible&&TutorialDirector.Instance.ActiveStep==0,"fresh level1 tutorial visible");
            yield return Shot("tutorial-pc");
            s.ControlMode=Controls.ControlMode.Mobile;SettingsManager.Instance.Apply(s,false);yield return null;yield return Shot("tutorial-mobile");
            Check(FindObjectsByType<TutorialCircleHit>(FindObjectsSortMode.None).Length==2,"mobile tutorial round hit filters");
            TutorialDirector.Instance.Skip();yield return null;Check(!TutorialDirector.Instance.Visible&&ProfileService.Instance.Data.tutorial.skipCombat,"skip persists in profile");
            var disk=new JsonProfileStore("task/p17/tutorial-save.json");disk.Save(ProfileService.Instance.Data);Check(disk.Load().tutorial.skipCombat,"tutorial survives disk load");
            string telemetry="task/p17/telemetry-smoke-"+Guid.NewGuid().ToString("N");LocalTelemetry.TestFolder=telemetry;
            LevelEvents.RaiseStarted(LevelCatalog.Instance.Get(1));LocalTelemetry.Skill("giant-hand-seal");LocalTelemetry.Instance.Finish("won",1,4);
            Check(!Directory.Exists(telemetry),"opt-out writes no file or directory");
            s.LocalTelemetryEnabled=true;SettingsManager.Instance.Apply(s,false);LevelEvents.RaiseStarted(LevelCatalog.Instance.Get(1));
            player.GetComponent<GiantHandRuntime>().CommitCast();
            ProfileService.Instance.Inventory.Add("hoi-khi-dan",1);ProfileService.Instance.Data.carry.Add(new KeyCount{key="hoi-khi-dan",count=1});
            var items=player.GetComponent<PlayerItems>();items.BeginLevel();
            var injury=DamageInfo.Create(30,Element.None,DamageSource.Environment,player.transform.position,Vector3.up);injury.ignoreInvulnerability=true;health.ApplyDamage(injury);
            Check(items.Use(0)==ItemUseResult.Used,"real carried healing item use accepted");
            var fire=DamageInfo.Create(1,Element.Hoa,DamageSource.Environment,player.transform.position,Vector3.up);fire.skillId="thien-hoa";fire.ignoreInvulnerability=true;health.ApplyDamage(fire);
            var kill=DamageInfo.Create(100000,Element.None,DamageSource.Melee,player.transform.position,Vector3.up);kill.ignoreInvulnerability=true;kill.skillId="smoke-hit";health.ApplyDamage(kill);
            yield return null;
            var files=Directory.GetFiles(telemetry,"*.jsonl");var row=JsonUtility.FromJson<LocalTelemetry.Row>(File.ReadLines(files[0]).First());
            Check(row.level==1&&row.deaths==1&&row.deathCause=="smoke-hit"&&row.fireHits==1,"real damage/death result telemetry");
            Check(row.skills.Any(x=>x.id=="dai-thu-an"&&x.count==1)&&row.items.Any(x=>x.id=="hoi-khi-dan"&&x.count==1),"skill commit and successful item telemetry");
            var quiz=new Learning.QuizResult{answers=new List<Learning.AnswerRecord>{new Learning.AnswerRecord{questionId="ch1-b1-01",correct=true}}};LocalTelemetry.Quiz(quiz);
            var summaryType=Type.GetType("CampusRift.Progression.TelemetrySummary, Assembly-CSharp-Editor");string summary=(string)summaryType.GetMethod("Build").Invoke(null,new object[]{telemetry});File.WriteAllText("task/p17/telemetry-summary.csv",summary);Check(summary.Contains("question,ch1-b1-01,1,1"),"question accuracy summary readable");
            s.LocalTelemetryEnabled=false;SettingsManager.Instance.Apply(s,false);int before=File.ReadLines(files[0]).Count();LocalTelemetry.Quiz(quiz);Check(File.ReadLines(files[0]).Count()==before,"turning off stops all writes");
            File.Copy(files[0],"task/p17/telemetry-smoke-evidence.jsonl",true);
            File.WriteAllText(Path.Combine(telemetry,"keep.txt"),"unrelated file");Check(LocalTelemetry.Instance.DeleteLocalFiles()&&Directory.GetFiles(telemetry,"*.jsonl").Length==0&&File.Exists(Path.Combine(telemetry,"keep.txt")),"delete removes only telemetry JSONL");
            director.End();health.Revive(1,0);UIStateManager.Instance.EnterScene(true);
            s.ReduceCameraShake=false;s.ReduceSkillFlashes=false;SettingsManager.Instance.Apply(s,false);var view=player.followCamera;var rotation=view.transform.rotation;
            var impulse=player.GetComponent<GiantHandCameraImpulse>();impulse.ApplyCinematic(view,.06f,1);float normal=Quaternion.Angle(rotation,view.transform.rotation);view.transform.rotation=rotation;
            s.ReduceCameraShake=true;SettingsManager.Instance.Apply(s,false);impulse.ApplyCinematic(view,.06f,1);float reduced=Quaternion.Angle(rotation,view.transform.rotation);view.transform.rotation=rotation;
            Check(normal>0&&reduced<normal*.4f,"camera reduction works independently of flash");
            s.ReduceSkillFlashes=true;SettingsManager.Instance.Apply(s,false);yield return null;Check(Shader.GetGlobalFloat("_CampusReduceFlashes")==1,"sky meteor flicker reduction enabled");
            UIStateManager.Instance.Pause();UIStateManager.Instance.OpenSettings();yield return null;
            var settingsUI=FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);settingsUI.SelectTab(3);Check(settingsUI.ReduceCameraShake.isOn&&settingsUI.ReduceSkillFlashes.isOn,"comfort tab reflects settings");yield return Shot("settings-comfort-data");
            ProfileService.Instance.EndTransient();GameSceneManager.Instance.LoadMainMenu();float until=Time.realtimeSinceStartup+30;while(GameSceneManager.Instance.IsLoading&&Time.realtimeSinceStartup<until)yield return null;
            s.TelemetryConsentAsked=false;s.LocalTelemetryEnabled=false;SettingsManager.Instance.Apply(s,false);yield return null;yield return Shot("telemetry-consent");
            var consent=FindAnyObjectByType<TelemetryConsentUI>();Check(consent.Visible,"first visit consent shown");consent.Choose(false);Check(!SettingsManager.Instance.Current.LocalTelemetryEnabled&&SettingsManager.Instance.Current.TelemetryConsentAsked,"decline stays off");
            ProfileService.Instance.UseTransient(new ProfileData());UIStateManager.Instance.OpenHub();yield return null;yield return Shot("tutorial-hub");
            Check(TutorialDirector.Instance.Visible&&TutorialDirector.Instance.ActiveGroup=="hub","fresh hub tutorial");TutorialDirector.Instance.Advance();yield return null;Check(ProfileService.Instance.Data.tutorial.hub==1,"hub next stores progress");
        }
    }
}
#endif
