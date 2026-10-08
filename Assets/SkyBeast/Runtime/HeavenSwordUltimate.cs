using System;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.Skills;
namespace CampusRift.SkyBeast
{
    [DisallowMultipleComponent,DefaultExecutionOrder(150)]
    public sealed class HeavenSwordUltimate : MonoBehaviour
    {
        public enum ButtonState { Charging, Ready, NeedOutdoor, Blocked, Channeling }
        public static HeavenSwordUltimate Instance {get;private set;}
        public SwordIntent Intent {get;private set;}
        public LevelDirector Director {get;private set;}
        public HeavenSwordConfig Config {get;private set;}
        public bool Channeling {get;private set;}
        public float ChannelProgress=>Channeling?Mathf.Clamp01(channelAge/channelDuration):0;
        public float ChannelDuration=>channelDuration;
        public bool Visible=>Director!=null&&Director.Level!=null&&Director.Level.index>=8&&Director.Level.index<=10&&Realm>=Realm.HoaThan&&Director.State!=LevelDirector.Phase.Won&&Director.State!=LevelDirector.Phase.Lost&&Director.State!=LevelDirector.Phase.Idle;
        public bool Busy=>Channeling||(cinematic!=null&&cinematic.Playing);
        public Realm Realm=>ProfileService.Instance!=null?ProfileService.Instance.Cultivation.Realm:Realm.LuyenKhi;
        public bool FireWarning=>FireBreathCycle.Instance!=null&&FireBreathCycle.Instance.IsBreathing;
        bool IsBlocked
        {
            get { var s=SkyBeastScheduler.Instance;return FireBreathCycle.Instance!=null&&FireBreathCycle.Instance.IsFury || s!=null&&(s.Completed||(s.Fury!=null&&(s.Fury.Warning||s.Fury.Active))||(s.Level==10&&s.Phase==3&&(s.Fury==null||!s.Fury.Completed))); }
        }
        public ButtonState State
        {
            get
            {
                if(Busy)return ButtonState.Channeling;
                if(IsBlocked)return ButtonState.Blocked;
                if(Intent==null||!Intent.Full)return ButtonState.Charging;
                return ShelterDetector.AtFeet(transform.position)==Shelter.Outdoor?ButtonState.Ready:ButtonState.NeedOutdoor;
            }
        }
        public event Action Changed;
        public static event Action<float> Summoned;
        float summonDelay;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetEvents(){Summoned=null;}
        CampusInput input;CampusExplorer explorer;PlayerMonsterHealth health;BuffSystem buffs;GoldenBellRuntime bell;
        HeavenSwordCinematic cinematic;SwordChannelVisual circle;
        float channelAge,channelDuration;bool explorerWasEnabled;Vector3 channelPoint;
        public static HeavenSwordUltimate Ensure(GameObject player)=>player.GetComponent<HeavenSwordUltimate>()??player.AddComponent<HeavenSwordUltimate>();
        void Awake()
        {
            Instance=this;Config=HeavenSwordConfig.Load();input=GetComponent<CampusInput>();explorer=GetComponent<CampusExplorer>();health=GetComponent<PlayerMonsterHealth>();buffs=GetComponent<BuffSystem>();bell=GetComponent<GoldenBellRuntime>();
            health.DamageReceived+=Damage;health.Defeated.AddListener(StopUltimate);
            circle=new GameObject("Golden sword channel array").AddComponent<SwordChannelVisual>();circle.transform.SetParent(transform,false);circle.Build(Config.gold);circle.gameObject.SetActive(false);
            cinematic=gameObject.AddComponent<HeavenSwordCinematic>();cinematic.Initialize(this);
            HeavenSwordAudio.Attach(gameObject,Config);
            UI.SwordIntentUI.Attach(this);
        }
        public void Begin(LevelDirector director,SwordIntent intent){StopUltimate();Director=director;Intent=intent;Changed?.Invoke();}
        public float DurationForRealm(Realm realm)=>realm>=Realm.DoKiep?Config.doKiepChannel:realm>=Realm.LuyenHu?Config.luyenHuChannel:Config.hoaThanChannel;
        public bool TryChannel()
        {
            if(!Visible||Busy||State!=ButtonState.Ready||input==null||!input.Allowed)return false;
            input.ResetAll();Channeling=true;channelAge=0;channelDuration=DurationForRealm(Realm)*(1-Mathf.Clamp(buffs!=null?buffs.SwordChannelSpeed:0,0,.4f));channelPoint=transform.position;
            explorerWasEnabled=explorer.enabled;explorer.CancelDash();explorer.enabled=false;input.UltimateLocked=true;
            circle.gameObject.SetActive(true);HeavenSwordAudio.Instance?.Step(0);Changed?.Invoke();return true;
        }
        void Damage(DamageInfo info)
        {
            if(!Channeling||info.source!=DamageSource.Environment||info.skillId!="thien-hoa")return;
            if(bell!=null&&bell.ShieldActive)return;
            if(buffs!=null&&buffs.ConsumeUninterrupted())return;
            CancelChannel(); // Meter is consumed only after a successful sword strike.
        }
        public void CancelChannel()
        {
            if(!Channeling)return;Channeling=false;circle.Fade();input.UltimateLocked=false;input.ResetAll();explorer.enabled=explorerWasEnabled;Changed?.Invoke();
        }
        void Update()
        {
            if(Director==null)return;
            if(Director.State==LevelDirector.Phase.Lost||Director.State==LevelDirector.Phase.Idle){StopUltimate();return;}
            if(!Busy&&input.Pressed(CampusAction.Ultimate))TryChannel();
            if(!Channeling)return;
            if(Time.timeScale<=0)return;
            if(ShelterDetector.AtFeet(transform.position)!=Shelter.Outdoor||Vector3.Distance(transform.position,channelPoint)>.3f||IsBlocked){CancelChannel();return;}
            channelAge+=Time.deltaTime;circle.Show(ChannelProgress);
            if(channelAge<channelDuration)return;
            summonDelay=Time.time-Intent.ReadyAt;
            Channeling=false;circle.Fade();Progression.LocalTelemetry.Skill("thien-kiem");cinematic.Play();Changed?.Invoke();
        }
        internal void CinematicFinished(bool hit)
        {
            if(input!=null){input.UltimateLocked=false;input.ResetAll();}if(explorer!=null)explorer.enabled=explorerWasEnabled;
            if(hit)
            {
                Summoned?.Invoke(summonDelay);
                Intent.Consume();
                if(Realm>=Realm.LuyenHu)buffs?.SetSkillBuff("kiem-y-ho-the",StatType.FireResistance,Config.protectionFireResistance,Config.protectionSeconds);
                Director.CompleteSkySword();
            }
            Changed?.Invoke();
        }
        public void StopUltimate(){bool interrupted=Busy;CancelChannel();if(cinematic!=null)cinematic.Cancel();if(interrupted&&explorer!=null)explorer.enabled=explorerWasEnabled;if(input!=null)input.UltimateLocked=false;Director=null;Intent=null;Changed?.Invoke();}
        void OnDestroy(){StopUltimate();if(health!=null){health.DamageReceived-=Damage;health.Defeated.RemoveListener(StopUltimate);}if(Instance==this)Instance=null;}
    }
}
