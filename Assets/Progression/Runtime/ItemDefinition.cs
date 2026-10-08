using System;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Combat;

namespace CampusRift.Progression
{
    // Append only: values are stored in assets.
    public enum ItemKind { Heal = 0, Buff = 1, FireWard = 2, Revive = 3, Utility = 4 }
    public enum ItemShape { Pill = 0, Talisman = 1, Pearl = 2 }
    public enum ItemEffectType { HealInstant = 0, HealOverTime = 1, RestoreSpirit = 2, Revive = 3, Stat = 4, Flag = 5, RestoreEnergy = 6, ClearNegative = 7, ElementBoost = 8 }
    // Flags other phases read: P13 (ember immunity), P15 (sword channelling).
    public enum ItemFlag { None = 0, EmberImmune = 1, SwordChannelSpeed = 2, SwordUninterrupted = 3, ControlImmune = 4, ControlGuard = 5, RevealEnemies = 6 }

    [Serializable] public sealed class ItemEffect
    {
        public ItemEffectType type;
        // HealInstant / HealOverTime / RestoreSpirit / Revive: fraction of the maximum. Stat: the modifier. Flag: strength (0.4 = 40%) or count.
        public float value;
        public float duration;          // seconds (HealOverTime, Stat, Flag); Revive: seconds of invulnerability afterwards
        public StatType stat;
        public ItemFlag flag;
        public Element element;
    }

    // One consumable of plan §9.3.
    [CreateAssetMenu(menuName = "Campus Rift/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string id;
        public Element chosenElement;
        public bool IsElementTalisman => id!=null && (id=="ngu-hanh-phu" || id.StartsWith("ngu-hanh-phu-"));
        public string nameEN, nameVN, descriptionEN, descriptionVN;
        public ItemKind kind;
        [Min(0)] public int price = 50;
        [Min(1)] public int maxPerLevel = 2;
        [Tooltip("Realm from which the shop sells it.")] public Realm availableFrom;
        public List<ItemEffect> effects = new List<ItemEffect>();
        public ItemShape shape;
        public Color tint = Color.white;

        public string Name(bool vietnamese) => vietnamese && !string.IsNullOrEmpty(nameVN) ? nameVN : nameEN;
        public string Description(bool vietnamese) => vietnamese && !string.IsNullOrEmpty(descriptionVN) ? descriptionVN : descriptionEN;
        // Used by the player automatically when about to die; cannot be used by hand.
        public bool IsPassive { get { foreach (var e in effects) if (e.type == ItemEffectType.Revive) return true; return false; } }
        // Heals the player's health (used to refuse a pointless use at full health).
        public bool HealsHealth { get { foreach (var e in effects) if (e.type == ItemEffectType.HealInstant || e.type == ItemEffectType.HealOverTime) return true; return false; } }
        public bool RestoresSpirit { get { foreach (var e in effects) if (e.type == ItemEffectType.RestoreSpirit) return true; return false; } }
    }
}
