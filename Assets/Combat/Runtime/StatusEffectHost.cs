using System;
using UnityEngine;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // Values are referenced by reactions and assets: append only.
    public enum StatusType { Burn = 0, Freeze = 1, Chill = 2, Shock = 3, Stun = 4, ArmorBreak = 5, Wet = 6, Pulled = 7 }

    // Timed status effects on a monster (plan §7.5 / P01-T05). One slot per type: re-applying refreshes
    // the duration and keeps the stronger magnitude.
    [DisallowMultipleComponent]
    public sealed class StatusEffectHost : MonoBehaviour
    {
        CampusRift.AR.ARCombatContext arContext;
        float SessionNow => arContext!=null?arContext.Now:Time.time;

        public const float BurnTick = 0.5f, ShockDuration = 0.5f, BossChillFromFreeze = 0.5f, BossStunCap = 1f;
        struct Slot { public float until, magnitude; public GameObject source; }
        static readonly int Count = Enum.GetValues(typeof(StatusType)).Length;
        readonly Slot[] slots = new Slot[Count];
        public event Action<StatusType, bool> Changed;
        MonsterVitality vitality;
        MonsterCombat combat;
        IMotionHold hold;
        IDamageable body;
        float nextBurn;
        float domainSlow;
        public void SetDomainSlow(float fraction){domainSlow=Mathf.Clamp01(fraction);}
        bool[] active = new bool[Count];

        void Awake()
        {
            arContext=GetComponent<CampusRift.AR.ARCombatContext>();
            vitality = GetComponent<MonsterVitality>(); combat = GetComponent<MonsterCombat>();
            hold = GetComponent<IMotionHold>(); body = GetComponent<IDamageable>();
        }

        public bool ResistsHardControl => vitality != null && vitality.resistHardControl;

        // magnitude: Burn = damage per 0.5 s tick, Chill = slow fraction (0.4 → 60% speed); unused for flags.
        public void Apply(StatusType type, float duration, float magnitude = 0, GameObject source = null)
        {
            if (duration <= 0 || (vitality != null && vitality.Defeated)) return;
            if((GetComponent<Skills.MartialAvatarRuntime>()?.ControlImmune??false)&&(type==StatusType.Freeze||type==StatusType.Chill||type==StatusType.Shock||type==StatusType.Stun||type==StatusType.Pulled))return;
            if(type==StatusType.Freeze||type==StatusType.Chill||type==StatusType.Shock||type==StatusType.Stun||type==StatusType.Pulled)
            {
                var buffs=GetComponent<Progression.BuffSystem>();
                if(buffs!=null&&(buffs.ControlImmune||buffs.ConsumeControlGuard()))return;
            }
            if (type == StatusType.ArmorBreak && magnitude <= 0) magnitude = .3f;
            if (ResistsHardControl)
            {
                if (type == StatusType.Freeze) { type = StatusType.Chill; magnitude = BossChillFromFreeze; }
                else if (type == StatusType.Stun) duration = Mathf.Min(duration, BossStunCap);
            }
            if (type == StatusType.Shock) duration = Mathf.Max(duration, ShockDuration);
            ref var slot = ref slots[(int)type];
            bool was = Has(type);
            slot.until = Mathf.Max(slot.until, SessionNow + duration);
            slot.magnitude = was ? Mathf.Max(slot.magnitude, magnitude) : magnitude;
            slot.source = source;
            if (type == StatusType.Burn && !was) nextBurn = SessionNow + BurnTick;
            if (type == StatusType.Stun) vitality?.Suppress(duration);
            if (type == StatusType.Freeze || type == StatusType.Shock || type == StatusType.Stun) combat?.Interrupt();
            if (!was) { active[(int)type] = true; Changed?.Invoke(type, true); }
            RefreshTint();
        }

        public bool Has(StatusType type) => SessionNow < slots[(int)type].until;
        public float Remaining(StatusType type) => Mathf.Max(0, slots[(int)type].until - SessionNow);
        public float Magnitude(StatusType type) => Has(type) ? slots[(int)type].magnitude : 0;

        // Reactions consume the status that triggered them (e.g. Băng Lôi Liệt shatters the freeze).
        public bool Consume(StatusType type)
        {
            if (!Has(type)) return false;
            slots[(int)type].until = 0; Expire(type);
            return true;
        }

        public void Clear()
        {
            domainSlow=0;
            vitality?.ClearSuppression();
            for (int i = 0; i < Count; i++) { slots[i] = default; if (active[i]) { active[i] = false; Changed?.Invoke((StatusType)i, false); } }
            vitality?.SetTint(null);
        }

        public float SpeedMultiplier
        {
            get
            {
                if (Has(StatusType.Freeze)) return 0f;
                return Mathf.Min(1-domainSlow,Has(StatusType.Chill) ? Mathf.Clamp01(1f - Magnitude(StatusType.Chill)) : 1f);
            }
        }

        // True while the monster may neither move nor strike.
        public bool Immobilized => Has(StatusType.Freeze) || Has(StatusType.Shock);

        void Update()
        {
            if(arContext!=null&&arContext.Paused)return;
            if (Immobilized) { hold?.HoldForSeal(); combat?.Interrupt(); }
            if (Has(StatusType.Burn) && SessionNow >= nextBurn)
            {
                nextBurn = SessionNow + BurnTick;
                var slot = slots[(int)StatusType.Burn];
                if (body != null && slot.magnitude > 0)
                {
                    var info = DamageInfo.Create(slot.magnitude, Element.Hoa, DamageSource.Reaction, transform.position + Vector3.up, Vector3.up, slot.source);
                    info.skillId = "burn";
                    body.ApplyDamage(info);
                }
            }
            for (int i = 0; i < Count; i++)
                if (active[i] && !Has((StatusType)i)) Expire((StatusType)i);
        }

        void Expire(StatusType type)
        {
            if (!active[(int)type]) return;
            active[(int)type] = false; Changed?.Invoke(type, false); RefreshTint();
        }

        void RefreshTint()
        {
            if (vitality == null) return;
            if (Has(StatusType.Freeze)) vitality.SetTint(new Color(.55f, .85f, 1.4f));
            else if (Has(StatusType.Burn)) vitality.SetTint(new Color(1.6f, .55f, .2f));
            else if (Has(StatusType.Shock)) vitality.SetTint(new Color(.28f, .11f, .45f));
            else if (Has(StatusType.Chill)) vitality.SetTint(new Color(.7f, .85f, 1.1f));
            else vitality.SetTint(null);
        }

        void OnDisable() { Clear(); }
    }
}
