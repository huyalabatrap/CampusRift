#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Progression;
namespace CampusRift.Validation
{
 public sealed class P12StarEventPlayTest:P12PlayTest
 {
  StarEvaluator stars;
  EnemyInstance Victim(){var e=EnemyPool.Ensure().Spawn(Resources.Load<EnemyArchetype>("P12/TieuYeu"),world.origin+Vector3.right*7,EnemyScaling.Default);e.Brain.enabled=false;e.Motor.Stop();e.Vitality.SetMaxHealth(10000,true);return e;}
  protected override IEnumerator Run(){Begin();stars=gameObject.AddComponent<StarEvaluator>();stars.BeginRun(world.player.gameObject);var health=world.player.GetComponent<PlayerMonsterHealth>();
   var other=new GameObject("foreign attacker");
   for(int i=0;i<4;i++){var e=Victim();var hit=DamageInfo.Create(100001,Element.None,DamageSource.Skill,e.transform.position,Vector3.down,i==3?other:world.player.gameObject);hit.skillId="dai-thu-an";e.Vitality.ApplyDamage(hit);LevelEvents.RaiseEnemyKilled(e);EnemyPool.Instance.Release(e);}
   Check(stars.Run.giantHandKills==3,"three accepted Giant Hand kills; foreign attacker excluded");
   var poison=DamageInfo.Create(8,Element.Moc,DamageSource.Projectile,world.origin,Vector3.right,other);poison.skillId="doc-nhan-poison";
   while(health.Invulnerable)yield return null;for(int i=0;i<3;i++){health.Revive(1,0);health.ApplyDamage(poison);yield return new WaitForSeconds(.28f);}Check(stars.Run.poisonHits==3,"three accepted poison DamageReceived events");health.Revive(1,1);bool rejected=!health.ApplyDamage(poison);Check(rejected&&stars.Run.poisonHits==3,"invulnerability rejected hit does not increment poison count");yield return new WaitForSeconds(1.05f);health.Revive(1,0);health.ApplyDamage(poison);Check(stars.Run.poisonHits==4,"fourth real poison hit fails level2 special star");yield return new WaitForSeconds(.28f);
   for(int i=0;i<6;i++){var e=Victim();e.Status.Apply(StatusType.Freeze,3);e.Vitality.ApplyDamage(DamageInfo.Create(1,Element.Loi,DamageSource.Skill,e.transform.position,Vector3.right,i==5?other:world.player.gameObject));EnemyPool.Instance.Release(e);}
   Check(stars.Run.iceLightning==5,"five real IceLightning feedback events; foreign reaction excluded");
   var items=world.player.GetComponent<PlayerItems>();var inventory=ProfileService.Instance.Inventory;inventory.ClearCarry();var heal=ItemCatalog.Instance.items.First(x=>x.HealsHealth);var revive=ItemCatalog.Instance.items.First(x=>x.IsPassive);inventory.Add(heal.id,1);inventory.Add(revive.id,1);inventory.SetCarry(heal,1);inventory.SetCarry(revive,1);items.BeginLevel();health.Revive(1,0);
   Check(items.Use(0)==ItemUseResult.NothingToDo&&!stars.Run.healUsed,"pointless heal does not fail level5 star");health.ApplyDamage(DamageInfo.Create(50,Element.None,DamageSource.Environment,world.origin,Vector3.up));Check(items.Use(0)==ItemUseResult.Used&&stars.Run.healUsed,"accepted healing item flags NoHealItems");yield return new WaitForSeconds(.28f);health.Revive(1,0);health.ApplyDamage(DamageInfo.Create(100001,Element.None,DamageSource.Environment,world.origin,Vector3.up));Check(items.ReviveUsed&&stars.Run.revived&&!health.IsDead,"real Life Talisman revival fails second star");
   int mask=stars.Finish(LevelCatalog.Instance.Get(5),true,50);Check(mask==1,"event-driven result keeps clear star and loses heal/revive stars");health.Revive(1,0);stars.BeginRun(world.player.gameObject);Check(!stars.Run.healUsed&&!stars.Run.revived&&stars.Run.iceLightning==0,"new attempt clears every star counter");
   var blackout=gameObject.AddComponent<BlackoutEvent>();float ambient=RenderSettings.ambientIntensity;blackout.Apply();yield return null;Check(blackout.LightsDisabled>0&&RenderSettings.ambientIntensity<ambient*.4f&&world.player.GetComponent<PlayerFlashlight>().alwaysOn,"real D/E lights disabled, ambient reduced, flashlight enabled");blackout.Restore();Check(Mathf.Approximately(RenderSettings.ambientIntensity,ambient),"blackout restores ambient on exit");
   Destroy(other);
  }
  protected override void Cleanup(){stars?.Unhook();base.Cleanup();}
 }
}
#endif
