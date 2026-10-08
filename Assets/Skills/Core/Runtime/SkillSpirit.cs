using UnityEngine;
using CampusRift.Combat;

namespace CampusRift.Skills
{
    // Charges the Linh Lực cost of a skill at the moment it is actually cast (P03-T02).
    public static class SkillSpirit
    {
        public static bool CanPay(Component owner, string skillId)
        {
            var spirit = owner.GetComponent<SpiritPower>(); var runtime = Find(owner, skillId);
            return spirit == null || runtime == null || spirit.CanAfford(runtime.SpiritCost);
        }
        public static bool TryPay(Component owner, string skillId)
        {
            var spirit = owner.GetComponent<SpiritPower>(); var runtime = Find(owner, skillId);
            bool paid=spirit == null || runtime == null || spirit.TrySpend(runtime.SpiritCost);
            if(paid&&runtime!=null)runtime.CommitCast();
            return paid;
        }
        static SkillRuntime Find(Component owner, string skillId)
        {
            var loadout = owner.GetComponent<SkillLoadout>();
            if(owner.GetComponent<AR.ARCombatContext>()!=null)foreach(var skill in owner.GetComponents<SkillRuntime>())if(skill.Id==skillId)return skill;
            return loadout != null ? loadout.Find(skillId) : null;
        }
    }
}
