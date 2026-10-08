#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.SkyBeast;
using CampusRift.Combat;
using CampusRift.Skills;
namespace CampusRift.Validation
{
    public sealed class P12VisualCapture : P12PlayTest
    {
        const string Root="task/p12/screens/";readonly List<Renderer> hidden=new List<Renderer>();
        void TrackDragon(SkyBeastController c){var rs=c.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=rs[0].bounds;foreach(var r in rs)if(r.enabled)bounds.Encapsulate(r.bounds);var delta=bounds.center-world.player.followCamera.transform.position;world.Look(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);}
        IEnumerator Track(SkyBeastController c,float seconds){float until=Time.time+seconds;while(Time.time<until){TrackDragon(c);yield return null;}}
        IEnumerator Shot(string path){Directory.CreateDirectory(Path.GetDirectoryName(Root+path));yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Root+path);yield return new WaitForEndOfFrame();yield return null;Measure("capture "+path);}
        protected override IEnumerator Run(){Begin();UIValidation.SetResolution(1920,1080);world.Look(90,14);world.Lighting(false);
            foreach(var r in world.player.GetComponentsInChildren<Renderer>())if(!r.forceRenderingOff){hidden.Add(r);r.forceRenderingOff=true;}
            foreach(var sword in FindObjectsByType<FlyingSword>())foreach(var r in sword.GetComponentsInChildren<Renderer>())if(!r.forceRenderingOff){hidden.Add(r);r.forceRenderingOff=true;}
            foreach(var row in LevelCatalog.Instance.Get(10).spawnTable.roster){var e=EnemyPool.Ensure().Spawn(row.archetype,world.origin+new Vector3(6,0,0),EnemyScaling.Default);e.Brain.enabled=false;e.Animation.enabled=false;e.GetComponent<EnemyFootPlant>().enabled=false;e.Motor.Stop();
                e.transform.rotation=Quaternion.LookRotation(Vector3.left);var a=e.Animation.Animator;a.speed=0;string id=e.Animation.profile.modelId;
                string[] states={e.Animation.profile.idle,"Walk_Forward","Run_Forward",e.Animation.profile.attack,e.Animation.profile.attack,"Hit_Front","Stun","Stun","Death_Forward","Spawn"};
                float[] frames={.2f,.3f,.25f,.22f,.5f,.4f,.3f,.3f,.65f,.45f};
                for(int i=0;i<states.Length;i++){if(i==8){world.player.GetComponent<IceSealRuntime>().enabled=false;world.player.GetComponent<IceSealRuntime>().enabled=true;world.player.GetComponent<SkillVfxPool>().Clear();}e.Status.Clear();a.Play(states[i],0,frames[i]);a.Update(0);
                    if(i==7){e.Vitality.SetMaxHealth(10000,true);var ice=world.player.GetComponent<IceSealRuntime>();ice.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();ice.CastAt(e.transform.position);yield return new WaitForSeconds(.55f);a.speed=0;}
                    if(i==9)RiftPortal.Open(e.transform.position,Vector3.left);
                    yield return null;yield return Shot("models/frames/"+id+"-"+i+".png");}
                RiftPortal.CloseAll();e.Status.Clear();a.Play("Death_Forward",0,.95f);a.Update(0);e.Animation.Dissolve(.5f);yield return Shot("models/frames/"+id+"-10.png");
                EnemyPool.Instance.ReleaseAll();world.player.GetComponent<IceSealRuntime>().enabled=false;world.player.GetComponent<IceSealRuntime>().enabled=true;world.player.GetComponent<SkillVfxPool>().Clear();yield return null;
            }
            foreach(int level in Enumerable.Range(1,10)){var d=LevelCatalog.Instance.Get(level);LevelSession.Select(d);world.Lighting(level>=3&&level<=7);UI.SettingsManager.Instance.Sky.SetPreset(d.sky);world.PlacePlayer(world.origin);world.Look(90,14);
                int n=0;foreach(var row in d.spawnTable.roster){for(int k=0;k<(level<=2?1:2);k++){float angle=(-32+n*64f/Mathf.Max(1,d.spawnTable.roster.Count*(level<=2?1:2)-1))*Mathf.Deg2Rad;Vector3 pos=world.origin+new Vector3(Mathf.Cos(angle)*(7+k*3),0,Mathf.Sin(angle)*(7+k*3));var e=EnemyPool.Instance.Spawn(row.archetype,pos,d.Scaling);if(e!=null){e.Brain.enabled=false;e.Motor.Stop();e.Animation.Play(e.Animation.profile.idle,1,10);}n++;}}
                SkyBeastPresence.Begin(level);yield return new WaitForSeconds(.4f);yield return Shot("rosters/level-"+level.ToString("00")+".png");EnemyPool.Instance.ReleaseAll();SkyBeastPresence.StopAll();yield return null;}
            foreach(var id in new[]{"020","023","026"}){var c=SkyBeastPresence.Spawn(id,.2f);foreach(bool dark in new[]{false,true}){world.Lighting(dark);c.SeekFlightForValidation(c.definition.period*.12f);yield return Track(c,.4f);yield return Shot("dragons/frames/"+id+"-fly-"+(dark?"dark":"light")+".png");c.Roar();yield return Track(c,.5f);yield return Shot("dragons/frames/"+id+"-roar-"+(dark?"dark":"light")+".png");c.SeekFlightForValidation(0);yield return Track(c,.5f);yield return Shot("dragons/frames/"+id+"-lowpass-"+(dark?"dark":"light")+".png");}SkyBeastPresence.StopAll();yield return null;}
            world.PlacePlayer(world.origin);world.Look(90,14);world.Lighting(true);
            foreach(bool dark in new[]{false,true})foreach(int level in new[]{5,7}){world.Lighting(dark);var e=EnemyPool.Instance.Spawn(LevelCatalog.Instance.Get(level).bosses[0],world.origin+Vector3.right*6,LevelCatalog.Instance.Get(level).Scaling);var b=e.GetComponent<BossController>();
                foreach(var attack in new[]{BossAttack.Roar,BossAttack.LeapSlam,BossAttack.ShadowDash}){if(level==5&&attack==BossAttack.ShadowDash)continue;e.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(world.origin+Vector3.right*6);world.PlacePlayer(world.origin);world.Look(90,14);b.Force(attack);yield return new WaitForSeconds(attack==BossAttack.Roar?.5f:attack==BossAttack.LeapSlam?.3f:.2f);yield return Shot("boss/level-"+level+"-"+attack+"-telegraph-"+(dark?"dark":"light")+".png");b.Cancel();yield return null;}
                EnemyPool.Instance.ReleaseAll();yield return null;}
            world.Look(90,14);world.Lighting(true);var ending=StartCoroutine(SkyBeastEnding.Play());yield return new WaitForSecondsRealtime(1.8f);yield return Shot("ending7.png");yield return new WaitForSecondsRealtime(2.5f);
            Check(!SkyBeastEnding.Playing,"ending camera and HUD restored after four seconds");
            Check(true,"captured seven animation series, ten roster fixtures, three flight/roar light/dark series, boss warnings and ending7");
        }
        protected override void Cleanup(){foreach(var r in hidden)if(r!=null)r.forceRenderingOff=false;base.Cleanup();}
    }
}
#endif
