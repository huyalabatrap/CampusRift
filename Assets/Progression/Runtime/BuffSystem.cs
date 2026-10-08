using System;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;

namespace CampusRift.Progression
{
    // Timed effects of items on the player (P08-T03). A buff is a modifier of source Buff in PlayerStats, so the caps of
    // PlayerStats (fire resistance 80%, damage taken −80%, move speed) hold and expiry restores the exact previous stats.
    // Using the same item again refreshes its timer, it never stacks. Time runs on scaled time: studying or pausing stops it.
    [DisallowMultipleComponent, DefaultExecutionOrder(60)]
    public sealed class BuffSystem : MonoBehaviour
    {
        public sealed class ActiveBuff
        {
            public ItemDefinition item; public ItemEffect effect; public int index;
            public float remaining, total;
            public string Key => item.id + "#" + index;
            public float Fraction => total > 0 ? Mathf.Clamp01(remaining / total) : 0;
        }
        readonly List<ActiveBuff> active = new List<ActiveBuff>();
        struct SkillBuff {public string key;public StatType stat;public float expires;}
        readonly SkillBuff[] skillBuffs=new SkillBuff[16];
        public bool SetSkillBuff(string key,StatType stat,float value,float seconds)
        {
            if(stats==null)stats=GetComponent<PlayerStats>();if(stats==null||seconds<=0)return false;
            int free=-1;
            for(int i=0;i<skillBuffs.Length;i++){if(skillBuffs[i].key==key&&skillBuffs[i].stat==stat){free=i;break;}if(skillBuffs[i].key==null)free=i;}
            if(free<0)return false;skillBuffs[free]=new SkillBuff{key=key,stat=stat,expires=Time.time+seconds};stats.SetModifier(StatSource.Skill,key,stat,value,0);return true;
        }
        public void RemoveSkillBuff(string key)
        {for(int i=0;i<skillBuffs.Length;i++)if(skillBuffs[i].key==key){if(stats!=null)stats.SetModifier(StatSource.Skill,key,skillBuffs[i].stat,0,0);skillBuffs[i]=default(SkillBuff);}}
        PlayerStats stats;
        PlayerMonsterHealth health;
        // Counted down by whoever interrupts the sword channelling (P15); Kiếm Tâm Đan grants one.
        int uninterruptedCharges;
        public event Action Changed;
        public IReadOnlyList<ActiveBuff> Active => active;

        void Awake() { stats = GetComponent<PlayerStats>(); health = GetComponent<PlayerMonsterHealth>(); }
        void Update() { Tick(Time.deltaTime); }

        public bool Has(string itemId) { foreach (var b in active) if (b.item.id == itemId) return true; return false; }
        public float Remaining(string itemId) { float best = 0; foreach (var b in active) if (b.item.id == itemId) best = Mathf.Max(best, b.remaining); return best; }

        // Starts (or refreshes) the timed effects of an item: stat modifiers, flags and heal over time.
        public void Apply(ItemDefinition item)
        {
            if (item == null) return;
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (health == null) health = GetComponent<PlayerMonsterHealth>();
            for (int i = 0; i < item.effects.Count; i++)
            {
                var e = item.effects[i];
                if (e.type != ItemEffectType.Stat && e.type != ItemEffectType.Flag && e.type != ItemEffectType.HealOverTime && e.type != ItemEffectType.ElementBoost) continue;
                var existing = active.Find(b => b.item == item && b.index == i);
                if (existing != null) { existing.remaining = existing.total = e.duration; }
                else active.Add(new ActiveBuff { item = item, effect = e, index = i, remaining = e.duration, total = e.duration });
                if(e.type==ItemEffectType.Flag&&e.flag==ItemFlag.ControlGuard)controlCharges=Mathf.Max(1,Mathf.RoundToInt(e.value));
                if (e.type == ItemEffectType.Stat) PushStat(active.Find(b => b.item == item && b.index == i));
                if (e.type == ItemEffectType.Flag && e.flag == ItemFlag.SwordUninterrupted) uninterruptedCharges = Mathf.Max(uninterruptedCharges, Mathf.Max(1, Mathf.RoundToInt(e.value)));
            }
            Changed?.Invoke();
        }

        void PushStat(ActiveBuff b)
        {
            if (stats == null) return;
            var e = b.effect;
            // Speed is a percentage of the base; every other stat here is a plain fraction added to the total.
            if (e.stat == StatType.MoveSpeed) stats.SetModifier(StatSource.Buff, b.Key, e.stat, 0, e.value);
            else stats.SetModifier(StatSource.Buff, b.Key, e.stat, e.value, 0);
        }

        public void Tick(float dt)
        {
            for(int i=0;i<skillBuffs.Length;i++)if(skillBuffs[i].key!=null&&Time.time>=skillBuffs[i].expires)
            {if(stats!=null)stats.SetModifier(StatSource.Skill,skillBuffs[i].key,skillBuffs[i].stat,0,0);skillBuffs[i]=default(SkillBuff);}
            if (active.Count == 0 || dt <= 0) return;
            bool changed = false;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var b = active[i];
                float step = Mathf.Min(dt, b.remaining);
                if (b.effect.type == ItemEffectType.HealOverTime && health != null && b.total > 0)
                    health.Heal(b.effect.value * health.maxHealth * step / b.total);
                b.remaining -= dt;
                if (b.remaining > 0) continue;
                if (b.effect.type == ItemEffectType.Stat && stats != null) stats.SetModifier(StatSource.Buff, b.Key, b.effect.stat, 0, 0);
                active.RemoveAt(i); changed = true;
            }
            if (changed) Changed?.Invoke();
        }

        public void ClearAll()
        {
            for(int i=0;i<skillBuffs.Length;i++)if(skillBuffs[i].key!=null){if(stats!=null)stats.SetModifier(StatSource.Skill,skillBuffs[i].key,skillBuffs[i].stat,0,0);skillBuffs[i]=default(SkillBuff);}
            if (stats != null) stats.RemoveSource(StatSource.Buff);
            active.Clear(); uninterruptedCharges = controlCharges = 0; Changed?.Invoke();
        }

        // ---- flags for other systems
        public bool HasFlag(ItemFlag flag) { foreach (var b in active) if (b.effect.type == ItemEffectType.Flag && b.effect.flag == flag) return true; return false; }
        int controlCharges;
        public bool ControlImmune => HasFlag(ItemFlag.ControlImmune);
        public bool ConsumeControlGuard(){if(controlCharges<=0||!HasFlag(ItemFlag.ControlGuard))return false;controlCharges--;return true;}
        public float ElementBonus(Element element){float best=0;foreach(var b in active)if(b.effect.type==ItemEffectType.ElementBoost&&b.effect.element==element)best=Mathf.Max(best,b.effect.value);return best;}
        public bool RevealEnemies=>HasFlag(ItemFlag.RevealEnemies);
        public bool EmberImmune => HasFlag(ItemFlag.EmberImmune);
        // 0.4 = channel 40% faster while Kiếm Tâm Đan lasts.
        public float SwordChannelSpeed { get { float v = 0; foreach (var b in active) if (b.effect.type == ItemEffectType.Flag && b.effect.flag == ItemFlag.SwordChannelSpeed) v += b.effect.value; return v; } }
        public bool ConsumeUninterrupted() { if (uninterruptedCharges <= 0 || !HasFlag(ItemFlag.SwordUninterrupted)) return false; uninterruptedCharges--; return true; }
        public int UninterruptedCharges => uninterruptedCharges;
    }
}
