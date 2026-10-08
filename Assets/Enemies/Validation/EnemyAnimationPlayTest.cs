#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Combat;
using CampusRift.Levels;
namespace CampusRift.Validation
{
    public sealed class EnemyAnimationPlayTest : P12PlayTest
    {
        public bool FailedItemsOnly;
        protected override IEnumerator Run(){Begin();
            foreach(var arch in LevelCatalog.Instance.Get(10).spawnTable.roster.Select(x=>x.archetype)){
                if(FailedItemsOnly && arch.id!="anh-yeu" && arch.id!="trieu-hon-su" && arch.id!="duc-yeu" && arch.id!="hoa-linh")continue;
                bool unfinished=!FailedItemsOnly || arch.id=="hoa-linh";
                var e=EnemyPool.Ensure().Spawn(arch,world.origin+new Vector3(6,0,6),EnemyScaling.Default);if(e==null){Check(false,arch.id+" spawn");continue;}
                var driver=e.Animation;e.Brain.enabled=false;var runner=e.GetComponent<EnemyAbilityRunner>();runner.Cancel();
                string[] states={driver.profile.idle,"Walk_Forward","Run_Forward",driver.profile.attack,"Hit_Front","Hit_Back","Hit_Left","Hit_Right","Stagger","Stun","Death_Forward","Death_Backward","Spawn","Taunt","Idle_Alert","Turn_Left","Turn_Right"};
                if(unfinished)Check(states.All(driver.HasState)&&driver.profile.clips.Length==28,arch.id+" all required states / 28 clips");
                driver.Play("Run_Forward",1);yield return null;e.Status.Apply(StatusType.Freeze,.5f);yield return null;if(unfinished)Check(driver.Animator.speed==0,arch.id+" Freeze stops animator");
                e.Status.Clear();driver.BeginAttack(.6f);yield return new WaitForSeconds(.6f);
                var state=driver.Animator.GetCurrentAnimatorStateInfo(0);float normalized=Mathf.Repeat(state.normalizedTime,1);
                Check(Mathf.Abs(normalized-driver.profile.attackImpactSeconds/driver.profile.Clip(driver.profile.attack).length)<.14f,arch.id+" authored strike frame aligns with .6s clock ("+normalized.ToString("F3")+")");
                if(FailedItemsOnly && (arch.id=="anh-yeu" || arch.id=="trieu-hon-su")){EnemyPool.Instance.Release(e);yield return null;continue;}
                yield return new WaitForSeconds(1.4f);driver.Play("Run_Forward",1);var plant=e.GetComponent<EnemyFootPlant>();if(plant!=null)plant.ResetMetrics();e.Motor.MoveTo(world.origin+new Vector3(6,0,18));
                yield return new WaitForSeconds(2.8f);e.Motor.Stop();
                Check(arch.isFlying||plant!=null&&plant.PlantSamples>20&&plant.MaxPlantSlip<=.08f&&plant.MaxWorldSlide<=.04f,arch.id+" stance IK residual <=8cm and world slide <=4cm/frame");if(plant!=null)Measure(arch.id+" measured source walk/run "+driver.profile.walkMetersPerSecond.ToString("F3")+"/"+driver.profile.runMetersPerSecond.ToString("F3")+" m/s; IK "+plant.PlantSamples+" samples, max "+plant.MaxPlantSlip.ToString("F4")+"m, world slide "+plant.MaxWorldSlide.ToString("F4")+"m");
                e.Status.Apply(StatusType.Stun,3);driver.Dissolve(.9f);EnemyPool.Instance.Release(e);
                var again=EnemyPool.Instance.Spawn(arch,world.origin+new Vector3(6,0,6),EnemyScaling.Default);
                Check(again==e&&e.Alive&&!e.Status.Has(StatusType.Stun)&&!driver.Dead&&driver.CurrentState=="Spawn"&&!runner.Busy,arch.id+" clean pool reuse");EnemyPool.Instance.Release(e);yield return null;
            }
        }
    }
}
#endif
