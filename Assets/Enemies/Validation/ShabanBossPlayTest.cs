#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Combat;
using CampusRift.Skills;
namespace CampusRift.Validation
{
    public sealed class ShabanBossPlayTest : P12PlayTest
    {
        public bool FailedItemsOnly;
        protected override IEnumerator Run(){Begin();var player=world.player.GetComponent<PlayerMonsterHealth>();
            if(FailedItemsOnly){
                var e=EnemyPool.Ensure().Spawn(LevelCatalog.Instance.Get(7).bosses[0],world.origin+Vector3.right*5,LevelCatalog.Instance.Get(7).Scaling);
                var boss=e.GetComponent<BossController>();float old=e.GetComponent<ShabanEnemyBridge>().RuntimeConfig.ChaseSpeed;
                e.Vitality.ApplyDamage(DamageInfo.Create(e.MaxHealth*.51f/(1-e.Vitality.defense),Element.None,DamageSource.Skill,e.transform.position,Vector3.forward,world.player.gameObject));yield return null;
                Check(boss.PhaseTwo&&boss.PhaseTransitions==1&&boss.AddsSpawned==2&&Mathf.Abs(e.GetComponent<ShabanEnemyBridge>().RuntimeConfig.ChaseSpeed-old*1.3f)<.01f,"boss7 one phase transition / speed+30% / two Shadow adds");
                EnemyPool.Instance.ReleaseAll();
                var yes=new StarRun{giantHandKills=3,poisonHits=3,eliteKilled=true,eliteSeconds=60,iceLightning=5,summonersSeen=1,summonersKilled=1};
                var no=new StarRun{giantHandKills=2,poisonHits=4,eliteKilled=true,eliteSeconds=60.1f,iceLightning=4,summonersSeen=1,summonersKilled=0,secondSummon=true,healUsed=true,revived=true};
                Check(StarEvaluator.Evaluate(6,true,60,300,yes)==7&&(StarEvaluator.Evaluate(6,true,800,300,no)&6)==0&&StarEvaluator.Evaluate(6,false,1,300,yes)==0,"stars L6 pass/fail/completion guards");yield break;
            }
            var elite=EnemyPool.Ensure().Spawn(Resources.Load<EnemyArchetype>("P12/ShabanElite"),world.origin+Vector3.right*6,LevelCatalog.Instance.Get(3).Scaling);
            Check(elite!=null&&elite.MaxHealth==900&&elite.GetComponent<ShabanEnemyBridge>().RuntimeConfig.Damage==45&&elite.SwordIntentWeight==4,"elite3 HP900 damage45 weight4 runtime config");
            var config=elite.GetComponent<ShabanEnemyBridge>().RuntimeConfig;Check(!config.enableTimeEscalation&&config!=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MonsterShaban/Monster_Shaban.prefab").GetComponent<MonsterBrain>().config,"elite escalation off / source config untouched");
            elite.Vitality.ApplyDamage(DamageInfo.Create(99999,Element.None,DamageSource.Skill,elite.transform.position,Vector3.forward,world.player.gameObject));yield return null;Check(!elite.Alive,"elite kill ends lifecycle");EnemyPool.Instance.Release(elite);
            foreach(int level in new[]{5,7}){
                var e=EnemyPool.Instance.Spawn(LevelCatalog.Instance.Get(level).bosses[0],world.origin+Vector3.right*5,LevelCatalog.Instance.Get(level).Scaling);var boss=e.GetComponent<BossController>();
                Check(Mathf.Abs(e.MaxHealth-(level==5?1500:3450))<.01f&&e.Vitality.resistHardControl,"boss"+level+" exact HP / hard-control resistance");
                boss.Force(BossAttack.Roar);float at=boss.WarningAt;yield return new WaitForSeconds(1.3f);Check(boss.Roars==1&&boss.ImpactAt-at>=1.19f&&PlayerEnemyControl.Ensure(world.player.gameObject).Stunned,"boss"+level+" roar 1.2s warning / stun1");
                yield return new WaitForSeconds(2);var bell=world.player.GetComponent<GoldenBellRuntime>();bell.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();bell.QuickCast();yield return null;
                boss.Force(BossAttack.Roar);yield return new WaitForSeconds(1.35f);Check(bell.ShieldActive&&!PlayerEnemyControl.Ensure(world.player.gameObject).Stunned,"boss"+level+" Golden Bell blocks roar");bell.enabled=false;bell.enabled=true;
                yield return new WaitForSeconds(1.3f);e.GetComponent<MonsterBrain>().enabled=false;e.GetComponent<MonsterNavigation>().Stop();
                var a=e.GetComponent<UnityEngine.AI.NavMeshAgent>();a.Warp(world.origin+Vector3.right*6);world.PlacePlayer(world.origin);player.Revive(1,0);int before=player.DamageCount;
                boss.Force(BossAttack.LeapSlam);float warning=boss.WarningAt;yield return new WaitForSeconds(2);
                Check(boss.Slams>=1&&boss.ImpactAt-warning>=.8f&&player.DamageCount>before,"boss"+level+" leap-slam warning / real damage");
                if(level==7){float old=e.GetComponent<ShabanEnemyBridge>().RuntimeConfig.ChaseSpeed;
                    e.Vitality.ApplyDamage(DamageInfo.Create(e.MaxHealth*.51f/(1-e.Vitality.defense),Element.None,DamageSource.Skill,e.transform.position,Vector3.forward,world.player.gameObject));yield return null;
                    Check(boss.PhaseTwo&&boss.PhaseTransitions==1&&boss.AddsSpawned==2&&Mathf.Abs(e.GetComponent<ShabanEnemyBridge>().RuntimeConfig.ChaseSpeed-old*1.3f)<.01f,"boss7 one phase transition / speed+30% / two Shadow adds");
                    Check(EnemyDirector.Instance.Active.FindAllForP12().TrueForAll(x=>x.archetype.id!="tieu-yeu"||!x.countsForSwordIntent),"boss7 adds do not grant Sword Intent");
                    boss.Force(BossAttack.ShadowDash);yield return new WaitForSeconds(4);Check(boss.Dashes==3&&boss.PhaseTransitions==1,"boss7 three shadow dashes / .4s warnings");
                }
                EnemyPool.Instance.ReleaseAll();yield return null;
            }
            for(int level=1;level<=7;level++){var yes=new StarRun{giantHandKills=3,poisonHits=3,eliteKilled=true,eliteSeconds=60,iceLightning=5,summonersSeen=1,summonersKilled=1};var no=new StarRun{giantHandKills=2,poisonHits=4,eliteKilled=true,eliteSeconds=60.1f,iceLightning=4,summonersSeen=1,summonersKilled=0,secondSummon=true,healUsed=true,revived=true};
                Check(StarEvaluator.Evaluate(level,true,60,300,yes)==7&&(StarEvaluator.Evaluate(level,true,800,300,no)&6)==0&&StarEvaluator.Evaluate(level,false,1,300,yes)==0,"stars L"+level+" pass/fail/completion guards");}
        }
    }
    static class P12EnemyList {public static System.Collections.Generic.List<EnemyInstance> FindAllForP12(this System.Collections.Generic.IReadOnlyList<EnemyInstance> source)=>new System.Collections.Generic.List<EnemyInstance>(source);}
}
#endif
