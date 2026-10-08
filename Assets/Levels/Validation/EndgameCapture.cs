#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CampusRift.UI;
using CampusRift.Progression;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.SkyBeast;
namespace CampusRift.Levels
{
    public sealed class EndgameCapture:MonoBehaviour
    {
        [Serializable]public sealed class Proof {public int passed,failed;public string method="Unity framebuffer; transient profile, fixture record values, real rig/prefabs/AI/UI. Camera/AI held for stills; no image postprocessing.";public List<EndgamePlayTest.Check> checks=new List<EndgamePlayTest.Check>();public List<string> images=new List<string>();}
        const string Root="task/p22/";public Proof proof=new Proof();public bool Done;GameSettings settings;
        void Check(bool pass,string name){proof.checks.Add(new EndgamePlayTest.Check{pass=pass,name=name});if(pass)proof.passed++;else proof.failed++;Save();}
        void Save(){File.WriteAllText(Root+"capture.json",JsonUtility.ToJson(proof,true));}
        IEnumerator Wait(Func<bool> condition){float until=Time.realtimeSinceStartup+30;while(!condition()&&Time.realtimeSinceStartup<until)yield return null;}
        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();
            var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+"screens/"+name+".png",tex.EncodeToPNG());Destroy(tex);
            var audit=ComicTextAudit.Scan("P22 "+name);ComicTextAudit.Save(audit,Root+"screens/"+name+"-audit.json");Check(audit.issues.Count==0,name+" TextAudit 0 issues");proof.images.Add(name);Save();
        }
        void Settings(ControlMode mode){var s=SettingsManager.Instance.Current.Copy();s.ControlMode=mode;s.LocalTelemetryEnabled=false;s.TelemetryConsentAsked=true;SettingsManager.Instance.Apply(s,false);}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Directory.CreateDirectory(Root+"screens");yield return null;yield return null;
            settings=SettingsManager.Instance.Current.Copy();var owner=ProfileService.Ensure();owner.UseTransient(EndgamePlayTest.CompleteProfile());TutorialDirector.Suppress=true;
            var p=owner.Data;p.learning.Lesson(EndgameService.LessonIds()[0]).mastery=3;
            var e=EndgameService.State(p);e.highestFloor=30;var n=e.Nightmare(1,true);n.cleared=true;n.stars=7;n.bestTime=125;EndgameService.Evaluate(owner);PlayerCostume.Equip(owner,"dream");EndgameService.EquipTitle(owner,"tower30");
            LevelSession.Clear();GameSceneManager.Instance.LoadMainMenu();yield return Wait(()=>SceneManager.GetActiveScene().name=="MainMenu"&&HubUI.Instance!=null&&HubUI.Instance.Visible);
            Settings(ControlMode.PC);UIStateManager.Instance.OpenHub();HubUI.Instance.OpenEndgame(HubUI.EndgamePage.Tower);yield return Shot("tower-hub");
            HubUI.Instance.OpenEndgame(HubUI.EndgamePage.Nightmare);yield return Shot("nightmare-map");
            HubUI.Instance.OpenEndgame(HubUI.EndgamePage.Achievements);yield return Shot("achievements");
            // Real click changes the cosmetic title in the header.
            var title=HubUI.Instance.ContentRect.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name.Contains("MANG DANH HIỆU"));if(title!=null)title.onClick.Invoke();Check(title!=null&&EndgameService.Title(owner.Data)!=null,"Achievement title click updates header");
            HubUI.Instance.OpenEndgame(HubUI.EndgamePage.Records);yield return Shot("records");
            HubUI.Instance.OpenEndgame(HubUI.EndgamePage.Costumes);yield return Shot("costume-pc");Check(HubUI.Instance.PreviewCostume!=null&&HubUI.Instance.PreviewCostume.Applied=="dream","PC preview uses saved equipped costume");
            Settings(ControlMode.Mobile);HubUI.Instance.OpenEndgame(HubUI.EndgamePage.Costumes);yield return Shot("costume-mobile");
            var round=HubUI.Instance.ContentRect.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name.Contains("MẶC")&&b.interactable);Check(round!=null&&round.GetComponent<RestRoundHit>()!=null,"Mobile equip uses circular hit filter");
            if(round!=null)round.onClick.Invoke();yield return null;Check(HubUI.Instance.PreviewCostume!=null&&HubUI.Instance.PreviewCostume.Applied==owner.Data.endgame.costume,"Mobile click updates preview and save");
            // First-time learning achievement emits its normal comic notification.
            e.achievements.Remove("first-read");p.learning.Lesson(EndgameService.LessonIds()[0]).readRewarded=true;owner.MarkDirty();yield return Shot("achievement-toast");
            yield return new WaitForSecondsRealtime(3.6f);
            Settings(ControlMode.PC);HubUI.Instance.OpenEndgame(HubUI.EndgamePage.Tower);
            var go=HubUI.Instance.ContentRect.GetComponentsInChildren<Button>().First(b=>b.name.Contains("VÀO TẦNG 1"));go.onClick.Invoke();yield return null;
            Check(UIStateManager.Instance.State==UIState.Loadout&&LoadoutUI.Level.runMode==EndgameMode.Tower,"Tower hub opens real preparation screen");
            LoadoutUI.Instance.StartLevel();yield return Wait(()=>SceneManager.GetActiveScene().name=="SampleScene"&&LevelDirector.Instance!=null&&LevelDirector.Instance.Level!=null);yield return null;
            Check(LevelDirector.Instance.Level.runMode==EndgameMode.Tower&&LevelSession.Current.runMode==EndgameMode.Tower,"Preparation starts Tower through real scene flow");
            var d=LevelDirector.Instance;d.End();d.introSeconds=.02f;d.portalLead=0;d.spawnInterval=.005f;
            var hp=FindAnyObjectByType<PlayerMonsterHealth>();var qa=hp.gameObject.GetComponent<V2QaPlayer>()??hp.gameObject.AddComponent<V2QaPlayer>();qa.God=true;
            var floor=EndgameFactory.Tower(30,DateTime.UtcNow);LevelSession.Select(floor);UIStateManager.Instance.EnterScene(true);d.Begin(floor);
            yield return Wait(()=>d.AliveCount>=Math.Min(11,d.ConcurrentCap));yield return new WaitForSecondsRealtime(1);
            Hold(d);Frame(d);yield return Shot("tower-floor30");Check(d.Alive.Any(x=>x.Elite.IsElite),"High floor real elite visible");
            d.enabled=true;UIStateManager.Instance.EnterScene(true);float until=Time.realtimeSinceStartup+25;bool boss=false;
            while(d.State!=LevelDirector.Phase.Won&&Time.realtimeSinceStartup<until){boss|=d.Alive.Any(x=>x.archetype.isBoss);V2DevTools.KillCurrentWave();yield return null;}
            Check(boss&&d.State==LevelDirector.Phase.Won,"High tower floor spawns and clears Shaban boss through normal win gate");d.End();
            var nightmare=EndgameFactory.Nightmare(6);LevelSession.Select(nightmare);UIStateManager.Instance.EnterScene(true);d.Begin(nightmare);yield return Wait(()=>d.Alive.Any(x=>x.Elite.IsElite));yield return new WaitForSecondsRealtime(1);Hold(d);Frame(d);yield return Shot("nightmare-affixes");
            var elite=d.Alive.FirstOrDefault(x=>x.Elite.IsElite);Check(elite!=null&&elite.Elite.Affixes.Count==2,"Nightmare six real two-affix elite");d.End();d.enabled=true;
            UIStateManager.Instance.EnterScene(true);var nm8=EndgameFactory.Nightmare(8);LevelSession.Select(nm8);d.Begin(nm8);FireBreathCycle.Instance.AutoAdvance=false;
            Check(Mathf.Approximately(FireBreathCycle.Instance.PhaseDuration,FireBreathCycle.Instance.Profile.warningSeconds*.8f),"Nightmare fire warning timing ×0.8");FireBreathCycle.Instance.SetPhase(1);Check(Mathf.Approximately(FireBreathCycle.Instance.TimingMultiplier,.8f),"Nightmare factor survives fire phase changes");d.End();
            UIStateManager.Instance.EnterScene(true);d.Begin(LevelCatalog.Instance.Get(8));FireBreathCycle.Instance.AutoAdvance=false;Check(Mathf.Approximately(FireBreathCycle.Instance.TimingMultiplier,1)&&Mathf.Approximately(FireBreathCycle.Instance.PhaseDuration,FireBreathCycle.Instance.Profile.warningSeconds),"Normal fire retains original timing");d.End();
            // Restore camera/AI by scene reload on exit; real profile is restored only after Play stops.
            SettingsManager.Instance.Apply(settings,false);Done=true;Save();File.WriteAllText(Root+"CAPTURE-DONE.txt",proof.passed+" PASS / "+proof.failed+" FAIL");
        }
        void Hold(LevelDirector d)
        {d.enabled=false;foreach(var enemy in d.Alive){if(enemy.Brain!=null)enemy.Brain.enabled=false;enemy.GetComponent<EnemyAbilityRunner>()?.Cancel();enemy.Motor?.Stop();var fly=enemy.GetComponent<FlyingMotor>();if(fly!=null)fly.enabled=false;}d.PlayerTransform.GetComponent<CampusExplorer>().enabled=false;}
        void Frame(LevelDirector d)
        {
            var cam=Camera.main;foreach(var behaviour in cam.GetComponents<MonoBehaviour>())behaviour.enabled=false;
            var origin=d.PlayerTransform.position;int i=0;foreach(var enemy in d.Alive){float angle=(i%6)*Mathf.PI/3;var pos=origin+new Vector3(Mathf.Sin(angle)*6,0,Mathf.Cos(angle)*6)+Vector3.right*(i/6)*3;var motor=enemy.GetComponent<MinionMotor>();if(motor!=null)motor.Place(pos);else enemy.transform.position=pos;enemy.transform.LookAt(origin+Vector3.up*(enemy.transform.position.y-origin.y));i++;}
            cam.transform.position=origin+new Vector3(0,8,-15);cam.transform.LookAt(origin+Vector3.up*1.3f);cam.fieldOfView=62;
        }
    }
}
#endif
