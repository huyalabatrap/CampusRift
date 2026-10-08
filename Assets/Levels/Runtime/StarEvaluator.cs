using System;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Progression;
namespace CampusRift.Levels
{
    [Serializable] public sealed class StarRun
    {public int giantHandKills,poisonHits,iceLightning,outdoorFireHits,swordReadyCount,swordsInTime,chains;public bool revived,healUsed,eliteKilled;public float eliteSeconds=float.PositiveInfinity;public int summonersSeen,summonersKilled;public bool secondSummon;}
    public sealed class StarEvaluator : MonoBehaviour
    {
        public StarRun Run {get;private set;}=new StarRun();
        PlayerMonsterHealth health;PlayerItems items;bool tracking;
        GenerationChainTracker chain;
        public void BeginRun(GameObject player){Unhook();Run=new StarRun();tracking=true;if(player!=null){health=player.GetComponent<PlayerMonsterHealth>();items=player.GetComponent<PlayerItems>();}
            LevelEvents.EnemyKilled+=Killed;EnemyDirector.EnemySpawned+=Spawned;ExpandedEnemyRuntime.Summoned+=Summon;ReactionResolver.Feedback+=Reacted;
            SkyBeast.SwordIntent.Ready+=Ready;SkyBeast.HeavenSwordUltimate.Summoned+=Summoned;
            chain=player!=null?player.GetComponent<GenerationChainTracker>():null;if(chain!=null)chain.ChainCompleted+=Chained;
            if(health!=null)health.DamageReceived+=Hit;if(items!=null){items.ItemUsed+=Used;items.Revived+=Revived;}}
        void Spawned(EnemyInstance e){if(tracking&&e.archetype.id=="trieu-hon-su"&&e.countsForSwordIntent)Run.summonersSeen++;}
        void Summon(EnemyInstance e,int count){if(tracking&&e.countsForSwordIntent&&count>=2)Run.secondSummon=true;}
        void Killed(EnemyInstance e){if(!tracking)return;if(e.archetype.id=="trieu-hon-su"&&e.countsForSwordIntent)Run.summonersKilled++;if(e.LastDamage.skillId=="dai-thu-an"&&e.LastDamage.attacker==health?.gameObject)Run.giantHandKills++;
            var shaban=e.GetComponent<ShabanEnemyBridge>();if(shaban!=null&&!e.archetype.isBoss){Run.eliteKilled=true;Run.eliteSeconds=shaban.EncounterAt>=0?Time.time-shaban.EncounterAt:float.PositiveInfinity;}}
        void Hit(DamageInfo hit){if(tracking&&hit.source==DamageSource.Projectile&&hit.skillId=="doc-nhan-poison")Run.poisonHits++;
            if(tracking&&hit.source==DamageSource.Environment&&(hit.skillId=="thien-hoa"||hit.skillId=="long-no")&&health!=null&&SkyBeast.ShelterDetector.ForFire(health.transform.position)==SkyBeast.Shelter.Outdoor)Run.outdoorFireHits++;}
        void Ready(){if(tracking)Run.swordReadyCount++;}
        void Summoned(float seconds){if(tracking&&seconds<=10)Run.swordsInTime++;}
        void Chained(){if(tracking)Run.chains++;}
        void Reacted(ReactionEvent e){if(tracking&&e.type==ReactionType.IceLightning&&e.attacker==health?.gameObject)Run.iceLightning++;}
        void Used(ItemDefinition item,ItemUseResult result){if(tracking&&result==ItemUseResult.Used&&item!=null&&item.HealsHealth)Run.healUsed=true;}
        void Revived(ItemDefinition item){if(tracking)Run.revived=true;}
        public static int Evaluate(int level,bool won,float seconds,float par,StarRun run)
        {
            if(!won)return 0;int mask=1;if(!run.revived&&seconds<=par)mask|=2;
            bool special=level==1?run.giantHandKills>=3:level==2?run.poisonHits<=3:level==3?run.eliteKilled&&run.eliteSeconds<=60:
                level==4?run.iceLightning>=5:level==5?!run.healUsed:level==6?run.summonersSeen>0&&run.summonersKilled==run.summonersSeen&&!run.secondSummon:level==7?seconds<780:
                level==8?run.outdoorFireHits==0:level==9?run.swordReadyCount==2&&run.swordsInTime==2:level==10?run.chains>=3:false;
            return mask|(special?4:0);
        }
        public int Finish(LevelDefinition level,bool won,float seconds){tracking=false;int mask=Evaluate(level.index,won,seconds,level.parTimeSeconds,Run);Unhook();return mask;}
        public void Unhook(){LevelEvents.EnemyKilled-=Killed;EnemyDirector.EnemySpawned-=Spawned;ExpandedEnemyRuntime.Summoned-=Summon;ReactionResolver.Feedback-=Reacted;SkyBeast.SwordIntent.Ready-=Ready;SkyBeast.HeavenSwordUltimate.Summoned-=Summoned;if(chain!=null)chain.ChainCompleted-=Chained;chain=null;if(health!=null)health.DamageReceived-=Hit;if(items!=null){items.ItemUsed-=Used;items.Revived-=Revived;}health=null;items=null;tracking=false;}
        void OnDisable(){Unhook();}
    }
}
