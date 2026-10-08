#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.Skills;
using CampusRift.SkyBeast;
using CampusRift.UI;
namespace CampusRift.Validation
{
    // Direct smoke for the final incoming-skill cleanup and replacement screenshots only.
    public sealed class P16PolishCapture:P12PlayTest
    {
        LevelDirector d;readonly Vector3 outdoor=new Vector3(-5,.13f,2);
        IEnumerator Wait(Func<bool> predicate,float seconds=20){float until=Time.realtimeSinceStartup+seconds;while(!predicate()&&Time.realtimeSinceStartup<until)yield return null;}
        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.18f);yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("task/p16/screens/"+name+".png",frame.EncodeToPNG());Destroy(frame);
            var audit=ComicTextAudit.Scan("P16 final "+name);File.WriteAllText("task/p16/screens/"+name+"-text-audit.json",JsonUtility.ToJson(audit,true));Check(audit.issues.Count==0,name+" TextAudit0");
        }
        void StartLevel(int index)
        {
            V2DevTools.StartLevel(index);d=LevelDirector.Instance;d.introSeconds=.01f;d.spawnInterval=.005f;d.portalLead=0;
            world.PlacePlayer(outdoor);world.player.enabled=false;var hp=world.player.GetComponent<PlayerMonsterHealth>();hp.SetProgressionMaxHealth(10000);hp.Revive(1,0);hp.respawnOnDefeat=false;
            var fire=FireBreathCycle.Instance;fire.AutoAdvance=false;fire.GetComponent<FireBreathVisuals>().enabled=false;fire.Ground.Clear();
        }
        void DoorCamera(EnemyTactics.Entrance entrance)
        {
            var graph=ShelterGraphReference.Graph;var inside=graph.Nodes.FirstOrDefault(n=>n.RoomID==entrance.id.Replace("/approach","/inside"));
            Vector3 normal=inside!=null?Vector3.ProjectOnPlane(entrance.position-inside.WorldPosition,Vector3.up).normalized:Vector3.back;
            Vector3 target=entrance.position+Vector3.up*1.2f,want=target+normal*6.5f+Vector3.up*1.1f;
            if(Physics.Linecast(target,want,out var hit,1<<11,QueryTriggerInteraction.Ignore))want=hit.point-(want-target).normalized*.3f;
            world.camera.transform.position=want;world.camera.transform.LookAt(target);world.camera.fieldOfView=62;
        }
        protected override IEnumerator Run()
        {
            Begin();UIValidation.SetResolution(1920,1080);yield return new WaitForSecondsRealtime(.4f);
            StartLevel(9);ProfileService.Instance.Data.heavenSwordSeen=true;
            float until=Time.realtimeSinceStartup+20;
            while(!d.AwaitingSkySword&&Time.realtimeSinceStartup<until){foreach(var e in EnemyDirector.Instance.Active){e.Brain.enabled=false;e.GetComponent<EnemyAbilityRunner>()?.Cancel();e.Motor.Stop();}V2DevTools.KillCurrentWave();yield return null;}
            Check(d.AwaitingSkySword&&SwordIntent.Instance.Full,"Capture uses real first wave/full intent");
            var u=HeavenSwordUltimate.Instance;Check(u.TryChannel(),"Real first sword channel");yield return Wait(()=>HeavenSwordCinematic.Active!=null,4);HeavenSwordCinematic.Active?.Skip();yield return Wait(()=>!u.Busy,5);
            Check(d.CanChangeSkills&&d.State==LevelDirector.Phase.Rest,"Real phase2 rest available");
            var loadout=world.player.GetComponent<SkillLoadout>();loadout.EquipDuringRest(0,"than-kiem-ngu-loi");
            world.Mode(false);var outgoing=loadout.Find("han-bang-phong-an") as Set1SkillRuntime;var incoming=loadout.Find("phat-no-hoa-lien") as Set1SkillRuntime;
            world.player.GetComponent<SpiritPower>().Refill();incoming.ResetCooldownForValidation();incoming.CastAt(outdoor+Vector3.right*4);
            outgoing.ResetCooldownForValidation();outgoing.CastAt(outdoor+Vector3.right*3);
            Check(incoming.IsCasting&&incoming.CooldownRemaining>0&&outgoing.CooldownRemaining>0,"Incoming/outgoing start with real active casts and cooldowns");
            bool changed=loadout.EquipDuringRest(2,incoming.Id);
            Check(changed&&!incoming.IsCasting&&incoming.CooldownRemaining==0&&outgoing.CooldownRemaining>0,"Final incoming cleanup makes Ready; removed cooldown retained");
            FireBreathCycle.Instance.AutoAdvance=true;var panel=RestLoadoutUI.Instance;panel.Open();
            world.camera.transform.position=outdoor+new Vector3(-3,2,-4);world.camera.transform.LookAt(outdoor+Vector3.up);
            yield return Shot("loadout-pc");world.Mode(true);yield return new WaitForSecondsRealtime(.2f);yield return Shot("loadout-mobile");
            Check(panel.GetComponentsInChildren<Button>().All(b=>b.GetComponent<RectTransform>().rect.width>=68&&b.GetComponent<RiftGraphic>()!=null),"Final mobile buttons round68plus");panel.Close();d.End();yield return null;
            StartLevel(10);d.enabled=false;var fire=FireBreathCycle.Instance;fire.RestartWarning();var tactics=EnemyDirector.Instance.Tactics;tactics.FindEntrances();
            var template=UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");
            foreach(var entry in tactics.Entrances.Skip(1).Take(6))EnemyPool.Ensure().Spawn(template,entry.position,new EnemyScaling{health=10.5f,damage=9.6f,speed=1.35f,aiTier=4,level=10},false);
            yield return Wait(()=>EnemyDirector.Instance.Active.All(e=>e.Brain.State==MinionState.Chase),4);tactics.PlanWarning();yield return new WaitForSeconds(.35f);
            var blockers=EnemyDirector.Instance.Active.Where(e=>tactics.TryPosition(e,out var p,out bool guard)&&guard).ToArray();
            Check(blockers.Length>0&&tactics.BlockedCount*2<=tactics.Entrances.Count,"Final warning still reserves at least50percent");
            var blocked=blockers[0];tactics.TryPosition(blocked,out var point,out bool guarding);blocked.Motor.Place(point);yield return new WaitForSeconds(.3f);
            var entrance=tactics.Entrances.First(e=>Vector3.Distance(e.position,point)<1);DoorCamera(entrance);yield return Shot("warning-door-blocker");
            var free=tactics.Entrances.First(e=>Vector3.Distance(e.position,tactics.FreeEntrance)<1);DoorCamera(free);yield return Shot("warning-free-entrance");
            Check(blockers.All(e=>Vector3.Distance(e.transform.position,tactics.FreeEntrance)>3),"Free entrance has no assigned blocker");
            Measure("blocked "+entrance.id+" / free "+free.id+" / "+tactics.BlockedCount+" of "+tactics.Entrances.Count+"; camera uses paired exterior normal and collision check");
        }
        protected override void Cleanup(){RestLoadoutUI.Instance?.Close();d?.End();world.player.enabled=true;base.Cleanup();File.WriteAllText("task/p16/polish-smoke.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/p16/polish-DONE.txt",report.passed.Count+" PASS / "+report.failed.Count+" FAIL");}
    }
}
#endif
