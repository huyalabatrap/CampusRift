using UnityEngine;
using CampusRift.Skills;
namespace CampusRift.AR
{
    [CreateAssetMenu(menuName="Campus Rift/AR/Gesture Skill Map")]
    public sealed class GestureSkillMapper:ScriptableObject
    {
        [System.Serializable]public struct Entry {public string label;public SkillDefinition skill;}
        public Entry[] entries;
        public SkillDefinition Find(string label){if(!GestureStateMachine.Mapped(label)||entries==null)return null;foreach(var e in entries)if(e.label==label)return e.skill;return null;}
        public static readonly string[] Labels={"Open_Palm","Closed_Fist","Pointing_Up","Victory","Thumb_Down","Thumb_Up"};
        public static readonly string[] Ids={"dai-thu-an","hac-dong-than-la","than-kiem-ngu-loi","van-kiem-quyet","han-bang-phong-an"};
    }
}
