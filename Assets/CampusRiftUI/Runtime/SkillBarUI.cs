using UnityEngine;
namespace CampusRift.UI
{
    public sealed class SkillBarUI : MonoBehaviour
    {
        public SkillSlotUI[] Slots;
        // Clean slot used for skills without an authored slot and for empty loadout slots (SkillBarBinder).
        public SkillSlotUI slotTemplate;
        public SkillSlotUI GetSlot(int index) { return index>=0 && index<Slots.Length?Slots[index]:null; }
        void Awake(){if(GetComponent<SkillBarBinder>()==null)gameObject.AddComponent<SkillBarBinder>();}
    }
}
