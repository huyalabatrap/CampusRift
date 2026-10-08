using System;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Levels;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    [DefaultExecutionOrder(80)]
    public sealed class FireBreathCycle:MonoBehaviour
    {
        public enum Phase { Disabled, Warning, Breath, Afterfire, Rest }
        public static FireBreathCycle Instance{get;private set;}
        public FireBreathProfile Profile{get;private set;}
        public Phase State{get;private set;}
        public bool IsBreathing=>State==Phase.Breath;
        public bool IsFury{get;private set;}
        public bool AutoAdvance=true;
        public bool CinematicPaused {get;set;}
        float swordRest;
        public void PostSwordRest(float seconds){Ground.Clear();IsFury=false;swordRest=seconds;Enter(Phase.Rest);}
        public bool DevMode{get;private set;}
        public float Remaining=>Mathf.Max(0,Duration-elapsed);
        public float WarningProgress=>State==Phase.Warning?Mathf.Clamp01(elapsed/Duration):0;
        public int TickCount{get;private set;}
        public event Action WarningStarted,BreathStarted,BreathEnded;
        public event Action<int> BreathTick;
        public PlayerMonsterHealth Player{get;private set;}
        public BurningGround Ground{get;private set;}
        float elapsed,nextTick;
        bool furyUsed;
        readonly List<MonsterVitality> enemies=new List<MonsterVitality>(32);
        public static bool IsFireHazard(DamageInfo info)=>info.source==DamageSource.Environment&&(info.skillId=="thien-hoa"||info.skillId=="long-no"||info.skillId=="du-hoa"||info.skillId=="long-vu-hoa"||info.skillId=="mua-thien-thach");
        public bool IsBreathSource(SkyBeastController dragon)=>SkyBeastScheduler.Instance==null||SkyBeastScheduler.Instance.BreathSource==dragon;
        public void RestartWarning(){Ground.Clear();IsFury=false;Enter(Phase.Warning);}
        public static FireBreathCycle Ensure()
        {return Instance!=null?Instance:new GameObject("Fire Breath Cycle").AddComponent<FireBreathCycle>();}
        void Awake()
        {
            Instance=this;Player=FindAnyObjectByType<PlayerMonsterHealth>();
            if(Player!=null&&Player.GetComponent<ShelterDetector>()==null)Player.gameObject.AddComponent<ShelterDetector>();
            Ground=gameObject.AddComponent<BurningGround>();gameObject.AddComponent<FireBreathVisuals>();FireWarningHUD.Attach(this);
        }
        void Update()
        {
            if(CinematicPaused||!AutoAdvance||State==Phase.Disabled)return;
            if(!DevMode&&LevelDirector.Instance!=null&&(LevelDirector.Instance.State==LevelDirector.Phase.Won||LevelDirector.Instance.State==LevelDirector.Phase.Lost)){StopCycle();return;}
            Advance(Time.deltaTime);
        }
        public static void BeginLevel(int level)
        {if(Instance!=null)Instance.StopCycle();if(level>=8)Ensure().StartCycle(FireBreathProfile.Load(level));}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void StartDev(int level=8){StartCycle(FireBreathProfile.Load(level));DevMode=true;}
#endif
        public void StartCycle(FireBreathProfile profile)
        {
            StopCycle();Profile=profile;DevMode=false;furyUsed=false;TickCount=0;
            if(Profile!=null)Enter(Phase.Warning);
        }
        public void SetPhase(int phase)
        {if(Profile!=null){var p=FireBreathProfile.Load(Profile.level,phase);if(p!=null)Profile=p;}}
        public float TimingMultiplier=>LevelDirector.Instance!=null&&LevelDirector.Instance.Level!=null&&LevelDirector.Instance.Level.runMode==EndgameMode.Nightmare?.8f:1;
        public float PhaseDuration=>Duration;
        float Duration=>State==Phase.Rest&&swordRest>0?swordRest:State==Phase.Breath&&IsFury?12:TimingMultiplier*(State==Phase.Warning?Profile.warningSeconds:State==Phase.Breath?(IsFury?12:Profile.breathSeconds):State==Phase.Afterfire?Profile.afterfireSeconds:(swordRest>0?swordRest:Mathf.Max(0,Profile.cycleSeconds-Profile.warningSeconds-Profile.breathSeconds-Profile.afterfireSeconds)));
        public bool StartFury()
        {
            if(furyUsed||Profile==null||Profile.level!=10)return false;
            furyUsed=true;Ground.Clear();IsFury=true;TickCount=0;Enter(Phase.Breath);return true;
        }
        public void Advance(float dt)
        {
            if(LevelDirector.Instance!=null && LevelDirector.Instance.RestLoadoutOpen)return;
            if(CinematicPaused)return;
            int guard=0;
            while(State!=Phase.Disabled&&guard++<32&&(dt>0||Duration<=.00001f))
            {
                float step=Mathf.Min(dt,Mathf.Max(0,Duration-elapsed));elapsed+=step;dt-=step;
                if(State==Phase.Breath)
                    while(elapsed+.0001f>=nextTick&&nextTick<=Duration+.001f){DealTick();nextTick+=.5f;}
                if(elapsed+.0001f<Duration)break;
                if(State==Phase.Warning)Enter(Phase.Breath);
                else if(State==Phase.Breath)
                {
                    BreathEnded?.Invoke();BoostFireEnemies();IsFury=false;Ground.SpawnPatches(Profile);Enter(Phase.Afterfire);
                }
                else if(State==Phase.Afterfire)Enter(Phase.Rest);
                else Enter(Phase.Warning);
            }
        }
        void Enter(Phase phase)
        {
            if(phase==Phase.Warning)swordRest=0;
            State=phase;elapsed=0;nextTick=.5f;
            if(phase==Phase.Warning){UI.ImportantCaptions.Show("[Còi báo Thiên Hỏa] Tìm mái che!","[Fire storm alarm] Find shelter!",4);TickCount=0;WarningStarted?.Invoke();if(SkyBeastScheduler.Instance==null)foreach(var dragon in FindObjectsByType<SkyBeastController>(FindObjectsSortMode.None))dragon.BeginWarning(Duration);}
            if(phase==Phase.Breath){BreathStarted?.Invoke();if(SkyBeastScheduler.Instance==null)foreach(var dragon in FindObjectsByType<SkyBeastController>(FindObjectsSortMode.None))dragon.RequestBreath(Duration);}
        }
        public void StopCycle()
        {
            if(IsBreathing)BreathEnded?.Invoke();State=Phase.Disabled;IsFury=false;elapsed=0;swordRest=0;CinematicPaused=false;Ground?.Clear();
        }
        void DealTick()
        {
            TickCount++;
            if(Player!=null&&!Player.IsDead)
            {
                var shelter=ShelterDetector.ForFire(Player.transform.position);
                float damage=IsFury?Profile.recommendedHealth*(shelter==Shelter.Outdoor?.10f:shelter==Shelter.Partial?.04f:.01f)*.5f:Profile.Total(shelter)/8;
                DamagePlayer(Player,damage,IsFury?"long-no":"thien-hoa");
            }
            enemies.Clear();enemies.AddRange(MonsterVitality.Active);
            foreach(var v in enemies)
            {
                if(v==null||v.IsDead||ShelterDetector.AtFeet(v.transform.position)!=Shelter.Outdoor)continue;
                var fire=v.GetComponent<FireEnemyState>();if(fire==null)fire=v.gameObject.AddComponent<FireEnemyState>();
                if(fire.Immune)continue;
                float damage=IsFury?Profile.recommendedHealth*.1f*.5f*.5f:Profile.outdoorDamage/8*.5f;
                var info=DamageInfo.Create(damage,Element.Hoa,DamageSource.Environment,v.transform.position,Vector3.down,gameObject);info.skillId=IsFury?"long-no":"thien-hoa";info.isArea=true;v.ApplyDamage(info);
            }
            BreathTick?.Invoke(TickCount);
        }
        void BoostFireEnemies()
        {foreach(var v in MonsterVitality.Active)if(v!=null&&!v.IsDead){var state=v.GetComponent<FireEnemyState>();if(state==null)state=v.gameObject.AddComponent<FireEnemyState>();state.AfterBreath();}}
        public static void DamagePlayer(PlayerMonsterHealth player,float amount,string id)
        {
            var info=DamageInfo.Create(amount,Element.Hoa,DamageSource.Environment,player.transform.position+Vector3.up,Vector3.down);info.skillId=id;info.isArea=true;player.ApplyDamage(info);
        }
        void OnDestroy(){StopCycle();if(Instance==this)Instance=null;}
    }
}
