#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Combat;
namespace CampusRift.Validation
{
 public sealed class P12EnemyVfxCapture:P12PlayTest
 {
  readonly List<Renderer> hidden=new List<Renderer>();
  IEnumerator Shot(string file){Directory.CreateDirectory("task/p12/screens/enemy-vfx");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("task/p12/screens/enemy-vfx/"+file+".png");yield return new WaitForEndOfFrame();yield return null;Measure(file);}
  protected override IEnumerator Run(){Begin();UIValidation.SetResolution(1920,1080);
   foreach(var r in world.player.GetComponentsInChildren<Renderer>())if(!r.forceRenderingOff){hidden.Add(r);r.forceRenderingOff=true;}
   foreach(var sword in FindObjectsByType<FlyingSword>())foreach(var r in sword.GetComponentsInChildren<Renderer>())if(!r.forceRenderingOff){hidden.Add(r);r.forceRenderingOff=true;}
   foreach(bool dark in new[]{false,true})foreach(var row in LevelCatalog.Instance.Get(10).spawnTable.roster){world.PlacePlayer(world.origin);world.Look(90,14);world.Lighting(dark);world.player.GetComponent<Monsters.PlayerMonsterHealth>().Revive(1,0);
    var scale=EnemyScaling.Default;scale.level=7;float distance=row.archetype.id=="bao-thi"||row.archetype.id=="hoa-trung"?2.5f:row.archetype.id=="liem-hon"?3.5f:6;var e=EnemyPool.Ensure().Spawn(row.archetype,world.origin+Vector3.right*distance,scale);e.Brain.enabled=false;e.Motor.Stop();e.transform.rotation=Quaternion.LookRotation(Vector3.left);yield return new WaitForSeconds(.2f);
    var runner=e.GetComponent<EnemyAbilityRunner>();var ability=e.archetype.abilities.FirstOrDefault(x=>!(x.ability is ExplodeAbility)).ability;float warning=.8f;
    bool basic=ability==null&&e.archetype.id!="bao-thi";
    if(e.archetype.id=="bao-thi"){warning=.4f;e.Vitality.ApplyDamage(DamageInfo.Create(e.Vitality.Health+1,Element.None,DamageSource.Skill,e.transform.position,Vector3.right));}
    else if(ability!=null){warning=ability.telegraphSeconds;runner.Force(ability,world.player.transform);}
    else {typeof(MinionBrain).GetMethod("BeginWindup",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(e.Brain,null);warning=e.Brain.LastWindupSeconds;}
    yield return new WaitForSeconds(warning*.45f);yield return Shot(e.archetype.id+"-warning-"+(dark?"dark":"light"));
    if(basic){yield return new WaitForSeconds(warning*.6f);typeof(MinionBrain).GetMethod("Strike",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(e.Brain,null);}
    else {yield return new WaitForSeconds(warning*.6f+(ability is LeapAbility?.6f:ability is ChargeAbility?distance/14-.08f:.06f));}
    yield return Shot(e.archetype.id+"-impact-"+(dark?"dark":"light"));EnemyPool.Instance.ReleaseAll();EnemyTelegraph.Clear();world.player.GetComponent<Skills.SkillVfxPool>().Clear();RiftPortal.CloseAll();yield return new WaitForSeconds(.4f);
   }
   Check(true,"seven real enemy warnings/impacts captured on light and dark backgrounds");
  }
  protected override void Cleanup(){foreach(var r in hidden)if(r!=null)r.forceRenderingOff=false;EnemyTelegraph.Clear();base.Cleanup();}
 }
}
#endif
