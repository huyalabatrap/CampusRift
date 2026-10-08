using System;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;

namespace CampusRift.Enemies
{
    // One live minion: its archetype plus the multipliers of the current level.
    [DisallowMultipleComponent, RequireComponent(typeof(MonsterVitality))]
    public sealed class EnemyInstance : MonoBehaviour
    {
        public EnemyArchetype archetype;
        public EnemyScaling scaling = EnemyScaling.Default;
        // Summoned or dropped monsters do not fill the Thiên Kiếm meter (plan §5.1).
        public bool countsForSwordIntent = true;
        public MonsterVitality Vitality { get; private set; }
        public StatusEffectHost Status { get; private set; }
        public MinionMotor Motor { get; private set; }
        public MinionBrain Brain { get; private set; }
        public EnemyPool Pool { get; internal set; }
        public bool ARSession { get; internal set; }
        public bool Alive => Vitality != null && !Vitality.Defeated && isActiveAndEnabled;
        public float Damage => archetype.baseDamage * scaling.damage;
        public EliteAffix Elite {get;private set;}
        public float MaxHealth => archetype.baseHealth * scaling.health*(scaling.level>=6?archetype.lateHealthMultiplier:1)*(Elite!=null&&Elite.IsElite?3:1);
        public float Speed => archetype.baseSpeed * scaling.speed * (scaling.level==6&&shelter!=null&&shelter.Current==SkyBeast.Shelter.Outdoor?Levels.BloodMoonEvent.SpeedBoost:1) * (fireState!=null?fireState.SpeedMultiplier:1)*(Elite!=null?Elite.SpeedMultiplier:1);
        SkyBeast.ShelterDetector shelter;
        SkyBeast.FireEnemyState fireState;
        public int SwordIntentWeight => Elite!=null&&Elite.IsElite?4:archetype.swordIntentWeight;
        public bool IsMelee => !archetype.ranged;
        public event Action<EnemyInstance> Died;
        public DamageInfo LastDamage {get;private set;}
        public EnemyAnimationDriver Animation => GetComponent<EnemyAnimationDriver>();

        void Awake() { Cache(); Vitality.Damaged+=RememberDamage; }
        void RememberDamage(DamageInfo info){LastDamage=info;Animation?.Hit(info);}
        void OnDestroy(){if(Vitality!=null)Vitality.Damaged-=RememberDamage;}
        void Cache()
        {
            if (Vitality != null) return;
            Vitality = GetComponent<MonsterVitality>(); Status = GetComponent<StatusEffectHost>();
            Motor = GetComponent<MinionMotor>(); Brain = GetComponent<MinionBrain>();
        }

        // Called by the pool every time the monster (re)enters play.
        public void Configure(EnemyArchetype data, EnemyScaling levelScaling, bool countsForIntent = true)
        {
            Cache();
            archetype = data; scaling = levelScaling; countsForSwordIntent = countsForIntent;
            Elite=GetComponent<EliteAffix>()??gameObject.AddComponent<EliteAffix>();Elite.ResetLife();GetComponent<EnemyWard>()?.Clear();
            shelter=GetComponent<SkyBeast.ShelterDetector>();if(shelter==null)shelter=gameObject.AddComponent<SkyBeast.ShelterDetector>();shelter.isMonster=true;shelter.Sample();
            fireState=GetComponent<SkyBeast.FireEnemyState>();if(fireState==null)fireState=gameObject.AddComponent<SkyBeast.FireEnemyState>();fireState.ResetLife();
            Vitality.Element = data.element; Vitality.defense = data.defense; Vitality.resistHardControl = data.resistHardControl;
            Vitality.SetMaxHealth(MaxHealth, true); Vitality.ResetVitality();
            if (Status != null) Status.Clear();
            var outline = GetComponent<TelegraphOutline>(); if (outline != null) outline.Hide();
            if (Motor != null) {Motor.enabled=true;Motor.Configure(this);}
            if (Brain != null) {Brain.enabled=true;Brain.Configure(this);}
            LastDamage=default;if(Animation!=null){Animation.enabled=true;Animation.ResetLife();}
            var plant=GetComponent<EnemyFootPlant>();if(plant!=null){plant.enabled=true;plant.ResetMetrics();}
            GetComponent<EnemyAbilityRunner>()?.ResetLife();
            GetComponent<ShabanEnemyBridge>()?.Configure();
            GetComponent<EnemyConcealment>()?.ResetLife();GetComponent<ExpandedEnemyRuntime>()?.ResetLife();
            var flying=GetComponent<FlyingMotor>();if(flying!=null){flying.enabled=true;flying.ResetLife();}
            PlayVoice();
        }

        AudioSource voice;
        // Explicit AR pool path: health/status/animation only. No campus sampling or abilities.
        internal void ConfigureAR(EnemyArchetype data, float worldScale)
        {
            Cache();archetype=data;scaling=EnemyScaling.Default;countsForSwordIntent=false;
            Vitality.Element=data.element;Vitality.defense=data.defense;Vitality.resistHardControl=data.resistHardControl;
            Vitality.SetMaxHealth(data.baseHealth,true);Vitality.ResetVitality();Status?.Clear();LastDamage=default;
            foreach(var c in GetComponentsInChildren<Collider>(true))c.enabled=true;
            Animation?.ResetLife();PlayVoice();if(voice!=null){voice.minDistance=4*worldScale;voice.maxDistance=45*worldScale;}
        }
        // Every minion, including summoned ones, uses the run's growl and a phase-aligned spatial loop.
        void PlayVoice()
        {
            if (!Audio.GameSfx.Enabled || archetype == null || archetype.isBoss || GetComponent<ShabanEnemyBridge>() != null) return;
            var director = EnemyDirector.Ensure();
            var clip = director.VoiceForSpawn(scaling.level); if (clip == null) return;
            if (voice == null)
            {
                voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 1; voice.minDistance = 4; voice.maxDistance = 45;
                voice.rolloffMode = AudioRolloffMode.Logarithmic; voice.dopplerLevel = 0; voice.volume = 0.6f; voice.loop = true;
            }
            voice.Stop(); voice.mute = true; voice.clip = clip; voice.pitch = 1;
            if (clip.length > 0) voice.time = (float)((AudioSettings.dspTime - director.SmallMonsterVoiceStartDsp) % clip.length);
            voice.Play();
            Audio.GameSfx.NotePlayed(clip);
        }
        public AudioSource Voice => voice;
        // Only the nearest minion is audible (EnemyDirector decides); other loops keep their phase while muted.
        public AudioClip VoiceClip => voice != null ? voice.clip : null;
        public void SetVoiceAudible(bool audible) { if (voice != null && voice.isPlaying) voice.mute = !audible; }

        internal void RaiseDied() { if (voice != null) voice.Stop(); Died?.Invoke(this); }
    }
}
