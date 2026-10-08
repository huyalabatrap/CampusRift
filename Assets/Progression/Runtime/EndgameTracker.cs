using UnityEngine;
using CampusRift.Levels;
using CampusRift.Enemies;
namespace CampusRift.Progression
{
    public sealed class EndgameTracker:MonoBehaviour
    {
        static EndgameTracker instance;ProfileService profile;bool evaluating;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){Ensure();}
        public static void Ensure(){if(instance==null)new GameObject("Endgame Achievements").AddComponent<EndgameTracker>();}
        void Awake(){instance=this;DontDestroyOnLoad(gameObject);profile=ProfileService.Ensure();profile.Changed+=Changed;LevelEvents.EnemyKilled+=Killed;Changed();}
        void Changed(){if(evaluating)return;evaluating=true;try{EndgameService.Evaluate(profile);}finally{evaluating=false;}}
        void Killed(EnemyInstance enemy)
        {if(enemy==null||!enemy.countsForSwordIntent)return;var e=EndgameService.State(profile.Data);e.kills++;if(enemy.Elite!=null&&enemy.Elite.IsElite)e.eliteKills++;if(enemy.archetype.isBoss)e.bossKills++;profile.MarkDirty();}
        void OnDestroy(){if(profile!=null)profile.Changed-=Changed;LevelEvents.EnemyKilled-=Killed;if(instance==this)instance=null;}
    }
}
