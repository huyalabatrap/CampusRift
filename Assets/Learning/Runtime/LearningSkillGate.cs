using System;
using UnityEngine;
namespace CampusRift.Learning
{
    [Serializable] public sealed class SkillBinding
    {
        public SkillRewardData reward;
        public Behaviour ability;
    }
    [DisallowMultipleComponent]
    public sealed class LearningSkillGate : MonoBehaviour
    {
        public SkillBinding[] bindings = new SkillBinding[0];
        // True when a learning reward controls this ability (SkillUnlockService defers to it until P06).
        public static bool HasBinding(Behaviour ability)
        {
            if(ability==null)return false;
            var gate=ability.GetComponent<LearningSkillGate>();
            if(gate==null)return false;
            foreach(var binding in gate.bindings)if(binding.ability==ability && binding.reward!=null)return true;
            return false;
        }
        public static bool Allows(Behaviour ability)
        {
            if(ability==null || !ability.isActiveAndEnabled)return false;
            if (Progression.DevMode.Active) return true;
            var gate=ability.GetComponent<LearningSkillGate>();
            if(gate==null)return true;
            foreach(var binding in gate.bindings)
                if(binding.ability==ability && binding.reward!=null)
                    return LearningService.Instance?.Engine.Progress.unlockedSkills.Contains(binding.reward.id)==true;
            return true;
        }
    }
}
