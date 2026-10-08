using System;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Combat;
namespace CampusRift.AR
{
    public sealed class ARMonsterDirector : MonoBehaviour
    {
        public ARBattlefield field;
        public int Wave {get;private set;} public bool Finished {get;private set;} public bool Won {get;private set;}
        public int Spawned {get;private set;} public int Killed {get;private set;} public int Reactions {get;private set;}
        public int WaveCount=>field.ModeSession.Waves.Length;
        public readonly List<EnemyInstance> Actors=new List<EnemyInstance>();
        public event Action<ARBattleEventKind,bool> BattleEvent;
        public event Action BattleStarted;
        readonly List<EnemyArchetype> roster=new List<EnemyArchetype>();
        readonly Queue<Vector3> splitPending=new Queue<Vector3>();
        int remaining,spawnIndex; float nextSpawn,started,minHealthFraction,waveStarted,breakUntil=-1; System.Random random; bool checkTouched;
        public bool BetweenWaves=>breakUntil>=0;
        ARBattleEvents events;
        ARWaveDefinition CurrentWave=>field.ModeSession.Waves[Mathf.Clamp(Wave-1,0,WaveCount-1)];
        void Awake(){if(field==null)field=GetComponent<ARBattlefield>();events=GetComponent<ARBattleEvents>()??gameObject.AddComponent<ARBattleEvents>();}
        void OnEnable(){field.Built+=StartBattle;field.Removing+=Clear;ReactionResolver.Feedback+=Reaction;}
        void OnDisable(){field.Built-=StartBattle;field.Removing-=Clear;ReactionResolver.Feedback-=Reaction;Clear();}
        public void StartBattle()
        {
            Clear();field.ModeSession.BeginRun();Wave=1;Spawned=Killed=Reactions=spawnIndex=0;minHealthFraction=1;checkTouched=field.CheckLoad;
            started=waveStarted=field.Clock;random=new System.Random(field.ModeSession.Seed);breakUntil=-1;
            if(field.ModeSession.Has(ARModeFeature.Rhythm)){remaining=0;field.Shrine?.Revive(1,0);BattleStarted?.Invoke();return;}
            remaining=CurrentWave.count;nextSpawn=field.Clock+.5f;
            roster.Clear();for(int i=1;i<=4;i++){var level=LevelCatalog.Instance.Get(i);if(level?.spawnTable==null)continue;foreach(var entry in level.spawnTable.roster)if(entry.archetype!=null&&!entry.archetype.isBoss&&!roster.Contains(entry.archetype))roster.Add(entry.archetype);}
            field.Shrine?.Revive(1,0);events.BeginWave(CurrentWave);BattleStarted?.Invoke();
        }
        void Clear()
        {Finished=Won=false;breakUntil=-1;splitPending.Clear();events?.Clear();foreach(var e in Actors)if(e!=null){e.Died-=Died;if(e.gameObject.activeSelf&&e.Pool!=null)e.Pool.Release(e);}Actors.Clear();}
        void Died(EnemyInstance e)
        {
            Killed++;var elite=e.GetComponent<AREliteAffix>();
            if(elite!=null&&elite.Kind==AREliteKind.Split&&!elite.Child)
            {splitPending.Enqueue(e.transform.position-e.transform.right*.2f*field.Scale);splitPending.Enqueue(e.transform.position+e.transform.right*.2f*field.Scale);}
        }
        void Reaction(ReactionEvent r)
        {var context=r.attacker!=null?r.attacker.GetComponent<ARCombatContext>():null;if(context!=null&&context.battlefield==field&&!Finished&&!field.Paused)Reactions++;}
        public void PublishEvent(ARBattleEventKind kind,bool available)=>BattleEvent?.Invoke(kind,available);
        public EnemyInstance SpawnActor(Vector3 position,AREliteKind kind=AREliteKind.None,bool child=false,bool leader=false)
        {
            if(roster.Count==0||!CanSpawn)return null;
            int index=field.ModeSession.Has(ARModeFeature.Elites)?random.Next(roster.Count):spawnIndex%roster.Count;spawnIndex++;
            var e=EnemyPool.Ensure().SpawnAR(roster[index],field.Root,position,field);if(e==null)return null;
            if(!Actors.Contains(e)){Actors.Add(e);e.Died+=Died;}
            var elite=e.GetComponent<AREliteAffix>()??e.gameObject.AddComponent<AREliteAffix>();elite.Configure(field,kind,child,leader);
            e.GetComponent<ARMinionBrain>().SetRanged(!child&&(e.archetype.ranged||spawnIndex%3==1));
            foreach(var audio in e.GetComponentsInChildren<AudioSource>()){ARCombatAudio.Spatial(audio,field.Scale);audio.volume=.6f*ARCombatAudio.Volume*(CampusRift.UI.SettingsManager.Instance!=null?CampusRift.UI.SettingsManager.Instance.Current.MonsterVolume:1);}
            Spawned++;return e;
        }
        void Portal(EnemyInstance e)
        {
            var portal=RiftPortal.Open(e.transform.position,field.Root.forward);portal.transform.SetParent(field.Root,true);portal.transform.localScale=Vector3.one;
            var context=portal.GetComponent<ARCombatContext>()??portal.gameObject.AddComponent<ARCombatContext>();context.battlefield=field;context.scale=field.Scale;
            foreach(var audio in portal.GetComponents<AudioSource>()){ARCombatAudio.Spatial(audio,field.Scale);}
            foreach(var p in portal.GetComponentsInChildren<ParticleSystem>()){var main=p.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.simulationSpace=ParticleSystemSimulationSpace.Local;}
        }
        public int AliveCount {get{int n=0;foreach(var e in Actors)if(e!=null&&e.Alive)n++;return n;}}
        public bool CanSpawn=>AliveCount<Mathf.Min(6,field.placement.settings.maxMonsters);
        public bool GroundCleared=>remaining==0&&AliveCount==0&&splitPending.Count==0&&Killed>0&&!events.BlocksWave;
        public void RegisterSpaceReaction(){if(!Finished&&!field.Paused)Reactions++;}
        public void Finish(bool won)
        {
            if(Finished)return;Finished=true;Won=won;events.Clear();
            if(!checkTouched)field.ModeSession.Complete(won,Killed,Reactions,field.Clock-started,1-minHealthFraction);
        }
        void Update()
        {
            if(field.Root==null||field.Shrine==null)return;field.ModeSession.ObservePolicy();checkTouched|=field.CheckLoad;
            if(field.Paused)return;
            minHealthFraction=Mathf.Min(minHealthFraction,field.Shrine.CurrentHealth/field.Shrine.maxHealth);
            if(field.CheckLoad){if(field.Shrine.IsDead)field.Shrine.Revive(1,0);else field.Shrine.Heal(field.Shrine.maxHealth);remaining=6;Finished=Won=false;events.Clear();}else if(Finished)return;
            if(field.Mode.loseOnShrine&&field.Shrine.IsDead){Finish(false);return;}
            if(field.Mode.winRule==ARWinRule.CompleteRhythm)return;
            if(field.Mode.timeLimit>0&&field.Clock-started>=field.Mode.timeLimit&&field.Mode.winRule!=ARWinRule.CloseRifts){Finish(false);return;}
            if(field.Mode.winRule==ARWinRule.CloseRifts)return;
            if(breakUntil>=0)
            {
                if(field.Clock<breakUntil)return;
                GetComponent<ARKnowledgeSeal>()?.EndBreak();breakUntil=-1;Wave++;waveStarted=field.Clock;remaining=CurrentWave.count;nextSpawn=field.Clock+1;events.BeginWave(CurrentWave);return;
            }
            if(splitPending.Count>0&&CanSpawn){var child=SpawnActor(splitPending.Dequeue(),AREliteKind.None,true);if(child==null)splitPending.Clear();return;}
            if(remaining>0&&CanSpawn&&field.Clock>=nextSpawn&&roster.Count>0)
            {
                nextSpawn=field.Clock+CurrentWave.spawnInterval;var offset=field.Root.right*((spawnIndex%3-1)*.3f*field.Scale);
                var kind=field.ModeSession.Has(ARModeFeature.Elites)&&CurrentWave.eliteEvery>0&&(remaining%CurrentWave.eliteEvery==0)?(AREliteKind)random.Next(1,5):AREliteKind.None;
                var space=GetComponent<ARSpaceModes>();Vector3 origin=Vector3.zero;bool wall=false;bool secondary=space!=null&&spawnIndex%2==0&&space.SecondarySpawn(spawnIndex,out origin,out wall);
                if(!secondary){origin=field.Rift.position+offset;wall=false;}
                var e=SpawnActor(secondary?field.Root.position+Vector3.ProjectOnPlane(origin-field.Root.position,Vector3.up).normalized*field.placement.Radius*.82f:origin,kind);
                if(e!=null){remaining--;if(secondary)(e.GetComponent<ARRiftEntrance>()??e.gameObject.AddComponent<ARRiftEntrance>()).Begin(field,origin,wall);else Portal(e);}
            }
            if(remaining==0&&AliveCount==0&&splitPending.Count==0&&!events.BlocksWave&&field.Clock>=waveStarted+CurrentWave.minimumSeconds&&field.Mode.winRule==ARWinRule.ClearWaves)
            {
                if(Wave>=WaveCount){if(!(GetComponent<ARSpaceModes>()?.DragonBlocksWave??false))Finish(true);}
                else{var quiz=GetComponent<ARKnowledgeSeal>();breakUntil=field.Clock+(quiz!=null&&quiz.EnabledForRun?10:CurrentWave.breakSeconds);quiz?.BeginBreak(Wave);}
            }
        }
    }
}
