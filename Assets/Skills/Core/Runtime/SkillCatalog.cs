using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Skills
{
    // Every skill of the game, in book order, for screens outside a level (the Hub's skill book and the loadout screen), where
    // there is no player object to ask. New skills only need to be listed here (menu Campus Rift/V2/Install Skill Catalog).
    [CreateAssetMenu(menuName = "Campus Rift/Skill Catalog")]
    public sealed class SkillCatalog : ScriptableObject
    {
        public List<SkillDefinition> skills = new List<SkillDefinition>();
        static SkillCatalog cached;
        public static SkillCatalog Instance
        {
            get
            {
                if (cached == null) cached = Resources.Load<SkillCatalog>("SkillCatalog");
                if (cached == null) cached = CreateInstance<SkillCatalog>();
                return cached;
            }
        }
        public static void Use(SkillCatalog catalog) { cached = catalog; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cached = null; }
        public SkillDefinition Find(string id) { foreach (var s in skills) if (s != null && s.id == id) return s; return null; }
    }
}
