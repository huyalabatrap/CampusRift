#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Skills;

namespace CampusRift.Validation
{
    public sealed class EnemyReactionAnimationPlayTest : P12PlayTest
    {
        protected override IEnumerator Run()
        {
            Begin();world.Mode(true);
            var combat=world.player.GetComponent<PlayerCombat>();
            Vector3[] incoming={Vector3.back,Vector3.forward,Vector3.right,Vector3.left};
            string[] sides={"Front","Back","Left","Right"};
            foreach(var row in LevelCatalog.Instance.Get(10).spawnTable.roster)
            {
                var e=EnemyPool.Ensure().Spawn(row.archetype,world.origin+Vector3.right*6,EnemyScaling.Default);
                e.Brain.enabled=false;e.Motor.Stop();e.GetComponent<EnemyAbilityRunner>().Cancel();e.Vitality.SetMaxHealth(10000,true);
                var driver=e.Animation;yield return new WaitForSeconds(driver.SpawnSeconds+.1f);
                for(int i=0;i<incoming.Length;i++)
                {
                    driver.Play(driver.profile.idle,1,0);
                    var hit=DamageInfo.Create(1,Element.None,DamageSource.Projectile,e.transform.position,e.transform.TransformDirection(incoming[i]),world.player.gameObject);
                    bool accepted=e.Vitality.ApplyDamage(hit);yield return new WaitForSeconds(.15f);
                    Check(accepted&&driver.CurrentState=="Hit_"+sides[i],row.archetype.id+" real incoming "+sides[i]+" hit selects directional recoil");
                    yield return new WaitForSeconds(.22f);
                }
                driver.Play(driver.profile.idle,1,0);combat.ResolveHit(e.Vitality,combat.config.chainPercent[combat.config.chainPercent.Length-1],true);yield return null;
                Check(e.LastDamage.isHeavy&&driver.CurrentState=="Stagger",row.archetype.id+" actual combo finisher staggers");
                yield return new WaitForSeconds(.5f);combat.ResolveHit(e.Vitality,combat.config.piercePercent,false);yield return null;
                Check(e.LastDamage.isHeavy&&driver.CurrentState=="Stagger",row.archetype.id+" actual piercing sword staggers");
                yield return new WaitForSeconds(.5f);
                e.Vitality.ApplyDamage(DamageInfo.Create(1,Element.None,DamageSource.Reaction,e.transform.position,Vector3.forward));yield return null;
                Check(driver.CurrentState=="Stagger",row.archetype.id+" actual reaction damage staggers");
                e.Status.Apply(StatusType.Shock,.5f);yield return null;
                Check(driver.CurrentState=="Stun",row.archetype.id+" Shock uses Stun pose");
                e.Status.Clear();driver.Play(driver.profile.idle,1,.5f);e.Status.Apply(StatusType.Chill,1,.5f);yield return null;
                Check(Mathf.Abs(driver.Animator.speed-.5f)<.01f,row.archetype.id+" Chill50 slows the actual Animator");
                e.Status.Clear();EnemyPool.Instance.Release(e);yield return null;
            }
            var target=EnemyPool.Ensure().Spawn(LevelCatalog.Instance.Get(1).spawnTable.roster[0].archetype,world.origin+Vector3.right*6,EnemyScaling.Default);
            target.Brain.enabled=false;target.Motor.Stop();target.Vitality.SetMaxHealth(10000,true);yield return new WaitForSeconds(target.Animation.SpawnSeconds+.1f);
            var hole=world.player.GetComponent<BlackHoleRuntime>();hole.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();
            Check(hole.CastAt(world.origin+Vector3.right*8),"Black Hole real cast accepted");yield return new WaitForSeconds(1.4f);
            var skin=target.GetComponentInChildren<SkinnedMeshRenderer>();var model=skin.rootBone;while(model.parent!=null&&model.parent!=target.transform)model=model.parent; // P12 root Animator uses a child visual wrapper.
            Vector3 toward=Vector3.ProjectOnPlane(world.origin+Vector3.right*8-model.position,Vector3.up).normalized;
            Check(target.Status.Has(StatusType.Pulled)&&target.Animation.CurrentState=="Stagger"&&Vector3.Dot(model.up,toward)>.15f,"Black Hole Pulled loops stagger and leans toward the actual center");
            yield return new WaitForSeconds(1.68f);
            Check(target.LastDamage.isHeavy&&!target.Status.Has(StatusType.Pulled)&&target.Animation.CurrentState=="Stagger","Black Hole expulsion preserves heavy recoil after leaving Pulled");
            yield return new WaitForSeconds(.5f);
            Check(!hole.IsCasting&&target.Motor.enabled&&target.Motor.OnMesh,"Black Hole release restores navigation after recoil");
        }
    }
}
#endif
