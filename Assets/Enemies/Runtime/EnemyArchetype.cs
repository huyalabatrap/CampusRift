using System;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Combat;

namespace CampusRift.Enemies
{
    // A monster skill that unlocks from a given level on (charge, leap, fan shot…). Concrete abilities arrive in P12.
    public abstract class EnemyAbility : ScriptableObject
    {
        public string id;
        [Min(0)] public float cooldown = 6f;
        [Min(0)] public float telegraphSeconds = 0.8f;
        public float range=8, radius=3, travelSpeed=14, impactSeconds=.8f;
        public string clip="Attack_Heavy";
    }

    [Serializable] public struct AbilityUnlock { [Min(1)] public int minLevel; public EnemyAbility ability; }

    // Data for one kind of monster (plan §3.2). Level multipliers are applied at spawn time.
    [CreateAssetMenu(menuName = "Campus Rift/Enemy")]
    public sealed class EnemyArchetype : ScriptableObject
    {
        public string id;
        public string displayName, displayNameVN;
        public Element element;
        public GameObject prefab;
        // The prefab does not exist yet (Thiết Giáp Ngưu, Bạo Thi: P12); validation tolerates it.
        public bool prefabPending;
        [Header("Base stats (level 1)")]
        [Min(1)] public float baseHealth = 60;
        [Min(0)] public float baseDamage = 8;
        [Min(0.1f)] public float baseSpeed = 5.5f;
        [Range(0, 0.8f)] public float defense;
        [Range(.5f,1f)] public float lateHealthMultiplier=1;
        [Header("Attack")]
        [Min(0.3f)] public float attackRange = 1.6f;
        [Min(0.1f)] public float attackCooldown = 1.4f;
        // Ranged monsters keep a band of distance and shoot instead of striking.
        public bool ranged;
        [Min(1)] public float preferredMin = 8f;
        [Min(1)] public float preferredMax = 12f;
        [Min(1)] public float projectileSpeed = 14f;
        [Header("Animation")]
        [Min(0.1f)] public float attackClipSeconds = 0.5f;
        [Range(0.1f, 0.95f)] public float impactFraction = 0.5f;
        [Header("Rules")]
        // Weight towards the Thiên Kiếm meter: 1 normal, 2 heavy, 4 elite.
        [Min(0)] public int swordIntentWeight = 1;
        public bool isFlying, isBoss, resistHardControl;
        public List<AbilityUnlock> abilities = new List<AbilityUnlock>();

        public string LocalizedName(bool vietnamese) => vietnamese && !string.IsNullOrEmpty(displayNameVN) ? displayNameVN : displayName;
    }

    // Multipliers a level applies to a monster.
    [Serializable] public struct EnemyScaling
    {
        public float health, damage, speed;
        public int aiTier;
        public int level;
        public static EnemyScaling Default => new EnemyScaling { health = 1, damage = 1, speed = 1, aiTier = 0, level = 1 };
    }
}
