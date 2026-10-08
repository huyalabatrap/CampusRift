using UnityEngine;

namespace CampusRift.Skills
{
    // Skills are silent: pressing a skill key no longer plays any sound (their sources are muted).
    public static class SkillAudio
    {
        public static bool Enabled = false;
        public static void Apply(params AudioSource[] sources)
        {
            foreach (var s in sources) if (s != null) { s.mute = !Enabled; if (!Enabled) s.Stop(); }
        }
    }
}
