using System;
using UnityEngine;

namespace CampusRift.Progression
{
    // Seven realms of cultivation, five tiers each (plan §8.1). Values are stored in saves: append only.
    public enum Realm { LuyenKhi = 0, TrucCo = 1, KetDan = 2, NguyenAnh = 3, HoaThan = 4, LuyenHu = 5, DoKiep = 6 }

    // Per-realm numbers and texts (plan §8.1, §8.4, §8.5).
    [Serializable] public sealed class RealmRow
    {
        public Realm realm;
        public string nameEN, nameVN;
        [Min(1)] public int tuViPerTier = 100;
        [Header("Stats at tier 1 → tier 5")]
        public float health1 = 100, health5 = 140, attack1 = 20, attack5 = 28, spirit1 = 100, spirit5 = 120;
        [Range(0, 0.8f)] public float defense;
        [Header("Unlock text (plan §8.5)")]
        public string skillsEN, skillsVN;
    }
}
