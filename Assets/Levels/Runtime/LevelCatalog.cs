using UnityEngine;

namespace CampusRift.Levels
{
    // The ten levels in order; loaded from Resources so a build always contains them.
    [CreateAssetMenu(menuName = "Campus Rift/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelDefinition[] levels = new LevelDefinition[0];
        static LevelCatalog cached;
        public static LevelCatalog Instance { get { if (cached == null) cached = Resources.Load<LevelCatalog>("LevelCatalog"); return cached; } }
        public static void ResetCache() { cached = null; }
        public int Count => levels != null ? levels.Length : 0;
        public LevelDefinition Get(int index)
        {
            if (levels == null) return null;
            foreach (var l in levels) if (l != null && l.index == index) return l;
            return null;
        }
    }
}
