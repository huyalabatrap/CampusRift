using UnityEngine;

namespace CampusRift.Skills
{
    // Single permission check for every skill (P02-T08): starter skills are always open, the rest open with the
    // player's realm and tier.
    public static class SkillUnlockService
    {
        public static bool IsUnlocked(SkillRuntime runtime)
        {
            if (runtime == null || !runtime.isActiveAndEnabled) return false;
            var d = runtime.Definition;
            if (Progression.DevMode.Active || d == null || d.starter) return true;
            // Everything else opens with the cultivation tier named on the skill (plan §8.5).
            var cultivation = Progression.ProfileService.CultivationOrNull;
            return cultivation != null && cultivation.AtLeast(d.unlockRealm, d.unlockTier);
        }

        static Behaviour LegacyAbility(SkillRuntime runtime) => runtime is ILegacySkill legacy ? legacy.Ability : null;
    }

    // Adapters around the pre-V2 skill components expose the wrapped component for gating and HUD reuse.
    public interface ILegacySkill { Behaviour Ability { get; } }
}
