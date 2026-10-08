using UnityEngine;

namespace CampusRift.Combat
{
    public enum DamageSource { Melee, Projectile, Skill, Environment, Reaction }

    // One hit, from any source. Defense is applied by the receiver, not here.
    public struct DamageInfo
    {
        public float amount;
        public Element element;
        public DamageSource source;
        public Vector3 point, direction;
        // Environment ticks (Thiên Hỏa, Dư Hỏa) and reactions must not be swallowed by hit invulnerability.
        public bool ignoreInvulnerability;
        public bool critical;
        public GameObject attacker;
        public string skillId;
        public float attackPower;
        public bool isArea;
        // Authored impact weight, independent of damage amount or critical rolls.
        public bool isHeavy;

        public static DamageInfo Create(float amount, Element element, DamageSource source, Vector3 point, Vector3 direction, GameObject attacker = null)
        {
            return new DamageInfo
            {
                amount = amount, element = element, source = source, point = point, direction = direction, attacker = attacker,
                ignoreInvulnerability = source == DamageSource.Environment || source == DamageSource.Reaction
            };
        }
    }

    public interface IDamageable
    {
        bool ApplyDamage(DamageInfo info);
        Element Element { get; }
        bool IsDead { get; }
        Transform Anchor { get; }
    }

    public static class DamageCalculator
    {
        // Damage = attack × percent × element multiplier × (critical ? critDamage : 1).
        public static DamageInfo Compute(float attackPower, float percent, Element element, IDamageable target,
            float critChance, float critDamage, System.Random rng, DamageSource source = DamageSource.Skill)
        {
            float amount = Mathf.Max(0, attackPower) * Mathf.Max(0, percent);
            if (target != null) amount *= ElementChart.Multiplier(element, target.Element);
            bool critical = critChance > 0 && rng != null && rng.NextDouble() < critChance;
            if (critical) amount *= Mathf.Max(1, critDamage);
            Vector3 point = target != null && target.Anchor != null ? target.Anchor.position : Vector3.zero;
            var info = DamageInfo.Create(amount, element, source, point, Vector3.zero);
            info.attackPower = Mathf.Max(0, attackPower);
            info.critical = critical;
            return info;
        }

        // Receiver-side defense: fraction 0..0.8 of the incoming amount is removed.
        public static float AfterDefense(float amount, float defense) => amount * (1f - Mathf.Clamp(defense, 0f, 0.8f));
    }
}
