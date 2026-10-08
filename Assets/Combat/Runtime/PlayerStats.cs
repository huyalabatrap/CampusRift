using System;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // Append only: values are referenced by assets.
    public enum StatType { MaxHealth = 0, Attack = 1, MaxSpirit = 2, Defense = 3, CritChance = 4, CritDamage = 5, MoveSpeed = 6, MaxEnergy = 7,
        SpiritRegen = 8, DamageDealt = 9, DamageTaken = 10, CooldownReduction = 11, FireResistance = 12 }

    // Where a modifier comes from. Cultivation and artifacts are permanent; buffs expire.
    public enum StatSource { Cultivation = 0, Artifact = 1, Buff = 2, Skill = 3 }

    [Serializable] public struct StatModifier
    {
        public StatSource source; public string key; public StatType stat;
        public float flat;      // added before percent
        public float percent;   // 0.1 = +10% of (base + flat)
    }

    // Combat stats of the player (plan §8.4, P03-T01). Base values are the Luyện Khí 1 numbers; cultivation,
    // artifacts and buffs push modifiers keyed by (source, key). Recomputing is idempotent, so reloading a
    // scene never stacks a bonus twice.
    [DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public sealed class PlayerStats : MonoBehaviour
    {
        public const float DefenseCap = 0.8f, PermanentMoveCap = 0.10f, TotalMoveCap = 0.6f, FireResistanceCap = 0.8f;
        [Header("Base (Luyện Khí 1)")]
        [Min(1)] public float baseAttack = 20f;
        [Min(1)] public float baseSpirit = 100f;
        public float baseSpiritRegen = 4f;
        [Range(0, 1)] public float baseDefense = 0f;
        [Range(0, 1)] public float baseCritChance = 0.05f;
        [Min(1)] public float baseCritDamage = 1.5f;
        // QA only: makes damage deterministic.
        public bool suppressCrit;

        readonly List<StatModifier> modifiers = new List<StatModifier>();
        PlayerMonsterHealth health;
        CampusExplorer explorer;
        float baseHealth, baseWalk, baseRun, baseEnergy;
        bool captured;
        public event Action Changed;

        // Health of the prefab at start; cultivation subtracts it to get its flat modifier.
        public float BaseHealth => baseHealth;
        public float MaxHealth { get; private set; }
        public float Attack { get; private set; }
        public float MaxSpirit { get; private set; }
        public float SpiritRegen { get; private set; }
        public float Defense { get; private set; }
        public float CritChance { get; private set; }
        public float CritDamage { get; private set; }
        public float MoveSpeedBonus { get; private set; }
        public float MaxEnergy { get; private set; }
        public float DamageDealt { get; private set; } = 1f;
        public float DamageTaken { get; private set; } = 1f;
        public float CooldownReduction { get; private set; }
        public float FireResistance { get; private set; }
        public IReadOnlyList<StatModifier> Modifiers => modifiers;
        public float EffectiveCritChance => suppressCrit ? 0f : CritChance;
        // Shared by every ability so buffs and reductions apply uniformly.
        public float ScaleCooldown(float seconds) => seconds * (1f - Mathf.Clamp(CooldownReduction, 0f, 0.75f));

        void Awake()
        {
            health = GetComponent<PlayerMonsterHealth>(); explorer = GetComponent<CampusExplorer>();
            // Captured before anything else runs so a scene reload never compounds a bonus into the base.
            baseHealth = health != null ? health.maxHealth : 100f;
            baseWalk = explorer != null ? explorer.walkSpeed : 6f; baseRun = explorer != null ? explorer.runSpeed : 10f;
            baseEnergy = explorer != null ? explorer.maxEnergy : 100f;
            captured = true; Recalculate();
        }

        // Replaces the modifier with the same (source, key, stat); zero flat and percent removes it.
        public void SetModifier(StatSource source, string key, StatType stat, float flat, float percent)
        {
            int index = modifiers.FindIndex(m => m.source == source && m.key == key && m.stat == stat);
            bool remove = Mathf.Approximately(flat, 0) && Mathf.Approximately(percent, 0);
            if (remove) { if (index < 0) return; modifiers.RemoveAt(index); }
            else
            {
                var m = new StatModifier { source = source, key = key, stat = stat, flat = flat, percent = percent };
                if (index >= 0) modifiers[index] = m; else modifiers.Add(m);
            }
            Recalculate();
        }

        public int RemoveSource(StatSource source, string key = null)
        {
            int removed = modifiers.RemoveAll(m => m.source == source && (key == null || m.key == key));
            if (removed > 0) Recalculate();
            return removed;
        }

        float Sum(StatType stat, out float flat, Func<StatModifier, bool> filter = null)
        {
            flat = 0; float percent = 0;
            foreach (var m in modifiers) if (m.stat == stat && (filter == null || filter(m))) { flat += m.flat; percent += m.percent; }
            return percent;
        }
        float Value(StatType stat, float baseValue)
        {
            float percent = Sum(stat, out float flat);
            return (baseValue + flat) * (1f + percent);
        }

        public void Recalculate()
        {
            if (!captured) return;
            MaxHealth = Mathf.Max(1, Value(StatType.MaxHealth, baseHealth));
            Attack = Mathf.Max(1, Value(StatType.Attack, baseAttack));
            MaxSpirit = Mathf.Max(1, Value(StatType.MaxSpirit, baseSpirit));
            SpiritRegen = Mathf.Max(0, Value(StatType.SpiritRegen, baseSpiritRegen));
            MaxEnergy = Mathf.Max(1, Value(StatType.MaxEnergy, baseEnergy));
            CritChance = Mathf.Clamp01(Value(StatType.CritChance, baseCritChance));
            CritDamage = Mathf.Max(1, Value(StatType.CritDamage, baseCritDamage));
            Defense = Mathf.Clamp(Value(StatType.Defense, baseDefense), 0, DefenseCap);
            // Damage-taken modifiers are percentages (−0.35 = 35% less); they multiply with defense.
            float takenPercent = Sum(StatType.DamageTaken, out float takenFlat);
            DamageTaken = Mathf.Clamp(1f + takenPercent + takenFlat, 1f - DefenseCap, 3f);
            float dealtPercent = Sum(StatType.DamageDealt, out float dealtFlat);
            DamageDealt = Mathf.Max(0.1f, 1f + dealtPercent + dealtFlat);
            float coolPercent = Sum(StatType.CooldownReduction, out float coolFlat);
            CooldownReduction = Mathf.Clamp(coolPercent + coolFlat, 0f, 0.75f);
            float fire = Sum(StatType.FireResistance, out float fireFlat);
            FireResistance = Mathf.Clamp(fire + fireFlat, 0f, FireResistanceCap);
            // Permanent sources are capped at +10%; buffs may push the total up to +60%.
            float permanentPercent = Sum(StatType.MoveSpeed, out float permanentFlat, m => m.source == StatSource.Cultivation || m.source == StatSource.Artifact);
            float allPercent = Sum(StatType.MoveSpeed, out float allFlat);
            float temporary = (allPercent + allFlat) - (permanentPercent + permanentFlat);
            MoveSpeedBonus = Mathf.Clamp(Mathf.Min(permanentPercent + permanentFlat, PermanentMoveCap) + temporary, 0f, TotalMoveCap);
            Apply();
            Changed?.Invoke();
        }

        void Apply()
        {
            if (health != null)
            {
                health.SetProgressionMaxHealth(MaxHealth);
                // Final reduction = 1 − (1 − defense) × damageTaken, never above the cap.
                health.DamageReduction = Mathf.Clamp(1f - (1f - Defense) * DamageTaken, 0f, DefenseCap);
            }
            if (explorer != null)
            {
                explorer.walkSpeed = baseWalk * (1f + MoveSpeedBonus); explorer.runSpeed = baseRun * (1f + MoveSpeedBonus);
                explorer.SetProgressionMaxEnergy(MaxEnergy);
            }
        }
    }
}
