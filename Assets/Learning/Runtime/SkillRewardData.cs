using UnityEngine;
namespace CampusRift.Learning
{
    [CreateAssetMenu(menuName="Campus Rift/Learning/Skill Reward")]
    public sealed class SkillRewardData : ScriptableObject
    {
        public string id, displayName;
        public string displayNameVN;
        [TextArea] public string descriptionVN;
        [TextArea] public string description;
        public Sprite icon;
    }
}
