using System.Collections;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Levels;
namespace CampusRift.SkyBeast
{
    public sealed class FireSpiritDrops:MonoBehaviour
    {
        SkyBeastScheduler scheduler;float next=30;public int Dropped{get;private set;}
        public void Initialize(SkyBeastScheduler s){scheduler=s;next=Time.time+30;}
        void Update(){if(scheduler==null||scheduler.Completed||scheduler.CinematicPaused||scheduler.Phase!=3||SwordIntent.Instance!=null&&SwordIntent.Instance.Full)return;if(Time.time>=next){next=Time.time+30;StartCoroutine(Drop());}}
        public IEnumerator Drop()
        {
            var level=LevelDirector.Instance;var player=EnemyDirector.Ensure().FindPlayer();var arch=Resources.Load<EnemyArchetype>("P19/HoaLinh");if(player==null||arch==null)yield break;
            for(int i=0;i<4;i++)
            {
                if(SwordIntent.Instance!=null&&SwordIntent.Instance.Full)yield break;
                if(!FlyingMotor.OutdoorPoint(player.position+Quaternion.Euler(0,i*90,0)*Vector3.forward*10,out var point))continue;
                var warning=EnemyTelegraph.Show(point,Vector3.forward,1.6f,.8f);yield return new WaitForSeconds(.8f);warning?.Hide();
                if(SwordIntent.Instance!=null&&SwordIntent.Instance.Full)yield break;
                if(EnemyDirector.Instance.Active.Count>=(level?.ConcurrentCap??14))continue;
                var e=EnemyPool.Ensure().Spawn(arch,point,level!=null?level.Level.Scaling:EnemyScaling.Default,false);
                if(e!=null){Dropped++;level?.TrackSummoned(e);EnemyAbilityRunner.Feedback(point,1.6f);}
            }
        }
    }
}
