using UnityEngine;

namespace CampusRift.Levels
{
    // What the next gameplay scene should play, and what is carried from one attempt to the next (plan §14.3).
    public static class LevelSession
    {
        public static LevelDefinition Current { get; private set; }
        public static int Attempt { get; private set; }
        // Skill ids in slot order that the player equipped; kept so Retry keeps the same four skills.
        public static string[] Loadout { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Current = null; Attempt = 0; Loadout = null; LevelCatalog.ResetCache(); LevelEvents.ResetAll(); }

        // Progress lives in the profile (P06-T06); these are conveniences for older call sites.
        public static int HighestCompleted => Progression.LevelProgressService.Current != null ? Mathf.Clamp(Progression.LevelProgressService.Current.HighestCleared, 0, 10) : 0;
        public static int NextToPlay => Progression.LevelProgressService.Current != null ? Progression.LevelProgressService.Current.NextPlayable() : 1;
        public static int LastIndex { get { var c = LevelCatalog.Instance; return c != null && c.Count > 0 ? c.Count : 10; } }

        public static bool Select(int index)
        {
            var catalog = LevelCatalog.Instance; var def = catalog != null ? catalog.Get(index) : null;
            if (def == null) { Debug.LogWarning("LevelSession: no level " + index); return false; }
            Select(def); return true;
        }
        public static void Select(LevelDefinition definition)
        {
            if (definition != Current) Attempt = 0;
            var previous=Current;Current = definition; Attempt++;
            if(previous!=definition)EndgameFactory.Dispose(previous);
        }
        public static void Clear() { EndgameFactory.Dispose(Current);Current = null; Attempt = 0; }
    }
}
