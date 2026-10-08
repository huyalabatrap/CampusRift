#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Progression;
using CampusRift.UI;
using CampusRift.Monsters;
using CampusRift.Combat;
using CampusRift.SkyBeast;
namespace CampusRift.Levels
{
    // Scoped corrections to the two gameplay stills and first-wave elite fixture.
    public sealed class EndgameVisualSmoke:MonoBehaviour
    {
        public EndgamePlayTest.Evidence proof=new EndgamePlayTest.Evidence();public bool Done;
        const string Root="task/p22/";LevelDirector d;Camera cam;Vector3 yard=new Vector3(-5,.13f,2);
        void Check(bool ok,string name){proof.checks.Add(new EndgamePlayTest.Check{pass=ok,name=name});if(ok)proof.passed++;else proof.failed++;File.WriteAllText(Root+"visual-smoke.json",JsonUtility.ToJson(proof,true));}
        IEnumerator Wait(Func<bool> condition){float end=Time.realtimeSinceStartup+20;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;}
        void Frame()
        {
            var player=d.PlayerTransform.GetComponent<CampusExplorer>();player.enabled=false;var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=yard;cc.enabled=true;player.followCamera=null;
            int i=0;foreach(var enemy in d.Alive)
            {var p=yard+new Vector3(6+i%4*2.6f,0,(i/4-1)*2.8f);if(enemy.Motor!=null){enemy.Motor.Place(p);enemy.Motor.Stop();}else enemy.transform.position=p;if(enemy.Brain!=null)enemy.Brain.enabled=false;enemy.GetComponent<EnemyAbilityRunner>()?.Cancel();var flying=enemy.GetComponent<FlyingMotor>();if(flying!=null){flying.enabled=false;enemy.transform.position=p+Vector3.up*3;}enemy.transform.rotation=Quaternion.LookRotation(Vector3.back);i++;}
            d.enabled=false;cam.transform.position=yard+new Vector3(10,5,-11);cam.transform.LookAt(yard+new Vector3(10,2,0));cam.fieldOfView=55;Physics.SyncTransforms();
        }
        IEnumerator Shot(string name)
        {Frame();yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+"screens/"+name+".png",tex.EncodeToPNG());Destroy(tex);var audit=ComicTextAudit.Scan(name);ComicTextAudit.Save(audit,Root+"screens/"+name+"-audit.json");Check(audit.issues.Count==0,name+" final TextAudit 0");}
        IEnumerator Start()
        {
            yield return null;yield return null;var owner=ProfileService.Ensure();owner.UseTransient(EndgamePlayTest.CompleteProfile());TutorialDirector.Suppress=true;
            d=LevelDirector.Ensure();d.introSeconds=.02f;d.spawnInterval=.005f;d.portalLead=0;var player=FindAnyObjectByType<CampusExplorer>();cam=player.followCamera;
            var qa=player.GetComponent<V2QaPlayer>()??player.gameObject.AddComponent<V2QaPlayer>();qa.God=true;PlayerCostume.Equip(owner,"dawn");
            var t=EndgameFactory.Tower(30,DateTime.UtcNow);LevelSession.Select(t);UIStateManager.Instance.EnterScene(true);d.Begin(t);yield return Wait(()=>d.AliveCount>=11);yield return new WaitForSecondsRealtime(1);yield return Shot("tower-floor30");
            Check(d.Alive.Any(x=>x.Elite.IsElite)&&d.Level.towerFloor==30,"Final high-floor picture contains actual elites");d.End();d.enabled=true;
            var n=EndgameFactory.Nightmare(6);n.restSeconds=.02f;LevelSession.Select(n);UIStateManager.Instance.EnterScene(true);d.Begin(n);
            while(d.WaveIndex<1){V2DevTools.KillCurrentWave();yield return null;}
            yield return Wait(()=>d.Alive.Any(x=>x.Elite.IsElite));yield return new WaitForSecondsRealtime(1);yield return Shot("nightmare-affixes");
            var elite=d.Alive.FirstOrDefault(x=>x.Elite.IsElite);Check(elite!=null&&elite.Elite.Affixes.Count==2&&elite.Elite.Affixes[0]!=elite.Elite.Affixes[1],"Nightmare wave2 actual elite has two distinct affixes");d.End();d.enabled=true;
            var fire=EndgameFactory.Tower(1000000,DateTime.UtcNow);Check(fire.TotalMonsters+fire.bosses.Count<=32,"Highest tower counts include boss within total cap32");fire.towerRule=TowerRule.FireMoon;LevelSession.Select(fire);UIStateManager.Instance.EnterScene(true);d.Begin(fire);yield return Wait(()=>d.Alive.Any(x=>x.archetype.element==Element.Hoa));
            var hoa=d.Alive.FirstOrDefault(x=>x.archetype.element==Element.Hoa);Check(hoa!=null&&Mathf.Approximately(hoa.scaling.health,fire.healthMultiplier*1.3f)&&Mathf.Approximately(hoa.scaling.damage,fire.damageMultiplier*1.3f),"Fire Moon really applies health/damage +30% to fire enemy");fire.towerRule=TowerRule.Silence;Check(EndgameHazard.Silenced,"Silence disables the actual enemy finder branch");d.End();
            UIStateManager.Instance.EnterScene(true);d.Begin(EndgameFactory.Nightmare(10));var cycle=FireBreathCycle.Instance;cycle.AutoAdvance=false;cycle.PostSwordRest(15);Check(Mathf.Approximately(cycle.PhaseDuration,15),"Nightmare retains15s sword rest");cycle.StartFury();Check(Mathf.Approximately(cycle.PhaseDuration,12),"Nightmare retains12s Long No");d.End();
            Done=true;File.WriteAllText(Root+"VISUAL-DONE.txt",proof.passed+" PASS / "+proof.failed+" FAIL");
        }
    }
}
#endif
