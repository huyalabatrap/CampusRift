using System;
using UnityEngine;
using CampusRift.Combat;

namespace CampusRift.Skills
{
    public enum CastType { Instant = 0, Aimed = 1, Channel = 2, Toggle = 3 }
    public enum SkillRole { Burst = 0, Control = 1, Defense = 2, Mobility = 3, Summon = 4, Support = 5, Utility = 6 }

    // One of the five ranks Nhập Môn → Hóa Cảnh (plan §7.4).
    [Serializable] public sealed class SkillRank
    {
        public float effectMultiplier = 1f;
        public float cooldownMultiplier = 1f;
        [Min(0)] public int upgradeCost;
        // Realm index (0 = Luyện Khí … 6 = Độ Kiếp); the realm enum arrives with cultivation in P06.
        [Range(0, 6)] public int requiredRealm;
    }

    // Data for one skill. Behaviour lives in a SkillRuntime component on the player (plan §14.3).
    [CreateAssetMenu(menuName = "Campus Rift/Skill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        public string id;
        public string displayName, displayNameVN;
        [TextArea] public string description, descriptionVN;
        // Short label on the mobile button, e.g. "VOID WALL".
        public string shortName;
        public Element element;
        public SkillRole role;
        public CastType castType;
        public Sprite icon;
        // Available from the first session (plan §8.5: the Luyện Khí starter set).
        public bool starter;
        [Range(0, 6)] public int unlockRealm;
        [Range(1, 5)] public int unlockTier = 1;
        [Min(0)] public float cooldown;
        [Min(0)] public float spiritCost;
        public SkillRank[] ranks = new SkillRank[5];

        public string LocalizedName(bool vietnamese) => vietnamese && !string.IsNullOrEmpty(displayNameVN) ? displayNameVN : displayName;
    }
}
