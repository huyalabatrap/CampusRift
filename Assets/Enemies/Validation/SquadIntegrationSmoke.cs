#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Monsters;
namespace CampusRift.Validation
{
    // A brief integration smoke for the mixed roster and escape-history hooks, not a balance bot.
    public sealed class SquadIntegrationSmoke:P12PlayTest
    {
        public bool AdaptationOnly;
        protected override IEnumerator Run()
        {
            Begin();world.player.enabled=false;var d=EnemyDirector.Ensure();d.ResetCounters();
            SkyBeast.FireBreathCycle.Instance?.StopCycle();
            var hp=world.player.GetComponent<PlayerMonsterHealth>();hp.SetProgressionMaxHealth(90000);hp.Revive(1,0);
            var field=typeof(CampusExplorer).GetField("planarVelocity",BindingFlags.Instance|BindingFlags.NonPublic);
            field.SetValue(world.player,Vector3.right*3);world.player.transform.forward=Vector3.right;
            var actors=new List<EnemyInstance>();
            var data=LevelCatalog.Instance.Get(10).spawnTable;
            for(int i=0;i<9;i++)
            {
                string id=i<6?"tieu-yeu":i==6?"doc-nhan":i==7?"trieu-hon-su":"duc-yeu";
                EnemyArchetype a=null;foreach(var row in data.roster)if(row.archetype.id==id){a=row.archetype;break;}
                actors.Add(EnemyPool.Instance.Spawn(a,world.origin+new Vector3(-8-i%3*1.5f,0,(i%4-1.5f)*1.8f),
                    new EnemyScaling{health=100,damage=1,speed=1,aiTier=4,level=8},false));
            }
            bool ranged=false,support=false,flyer=false,flank=false;
            float end=Time.time+30;
            var watched=typeof(SquadTactics).GetField("watchedEscapeDirection",BindingFlags.Instance|BindingFlags.NonPublic);
            var watching=typeof(SquadTactics).GetField("watchingEscape",BindingFlags.Instance|BindingFlags.NonPublic);
            while(Time.time<end&&(!AdaptationOnly||d.Squad.AdaptedExitCount==0))
            {
                if(actors[0]!=null&&actors[0].Brain.State!=MinionState.Spawn)
                {
                    var cc=world.player.GetComponent<CharacterController>();Vector3 direction=Vector3.right;
                    if((bool)watching.GetValue(d.Squad))direction=(Vector3)watched.GetValue(d.Squad);
                    direction.y=0;direction=direction.normalized;
                    cc.Move(direction*3*Time.deltaTime);
                    field.SetValue(world.player,direction*3);
                    foreach(var a in d.Squad.Members)
                    {
                        ranged|=a.role==SquadRole.Ranged;support|=a.role==SquadRole.Support;
                        flyer|=a.role==SquadRole.Flyer;flank|=a.role==SquadRole.FlankerLeft||a.role==SquadRole.FlankerRight;
                    }
                }
                yield return null;
            }
            if(!AdaptationOnly)Check(ranged&&support&&flyer&&flank,"Mixed roster receives ranged/support/flyer/flank roles");
            Check(d.Squad.AdaptedExitCount>0,"T4 reacts to repeated observed escape direction");
            if(!AdaptationOnly)Check(d.Squad.MaxPathsPerFrame<=3,"Mixed roster shares <=3 explicit path queries/frame");
            if(!AdaptationOnly)Check(d.MaxMeleeTokensSeen<=d.TierFor(4).meleeTokens&&d.MaxRangedTokensSeen<=d.TierFor(4).rangedTokens,"Mixed roster preserves attack token caps");
            if(!AdaptationOnly)Check(LevelCatalog.Instance.Get(1).aiTier==1&&LevelCatalog.Instance.Get(2).aiTier==1,"Levels1–2 use mild T1 squad profile");
            Measure("Adaptive orders="+d.Squad.AdaptedExitCount+", signals="+d.Squad.Signals);
            field.SetValue(world.player,Vector3.zero);world.player.enabled=true;
        }
    }
}
#endif
