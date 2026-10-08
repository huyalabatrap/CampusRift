using System;
using UnityEngine;
using CampusRift.Controls;

namespace CampusRift.Skills
{
    // The four skill slots on the player (plan §7.1). Only equipped skills receive input.
    [DisallowMultipleComponent, DefaultExecutionOrder(-160)]
    public sealed class SkillLoadout : MonoBehaviour
    {
        public const int SlotCount = 4;
        [SerializeField] SkillRuntime[] slots = new SkillRuntime[SlotCount];
        // Used when a slot is left empty in the prefab: skill ids in slot order (Q, E, R, F).
        public string[] defaultIds = { "hu-khong-ket-gioi", "anh-phan-than", "", "dai-thu-an" };
        public event Action LoadoutChanged;

        void Awake()
        {
            if(GetComponent<Combat.GenerationChainTracker>()==null)gameObject.AddComponent<Combat.GenerationChainTracker>();
            if(GetComponent<Combat.ReactionFeedback>()==null)gameObject.AddComponent<Combat.ReactionFeedback>();
            if(GetComponent<UI.GenerationChainUI>()==null)gameObject.AddComponent<UI.GenerationChainUI>();
            if (slots == null || slots.Length != SlotCount) Array.Resize(ref slots, SlotCount);
            bool empty = true;
            foreach (var s in slots) if (s != null) empty = false;
            if (empty) for (int i = 0; i < SlotCount && i < defaultIds.Length; i++) slots[i] = Find(defaultIds[i]);
        }

        public SkillRuntime Get(int slot) => slot >= 0 && slot < SlotCount ? slots[slot] : null;
        public int IndexOf(SkillRuntime runtime) { for (int i = 0; i < SlotCount; i++) if (runtime != null && slots[i] == runtime) return i; return -1; }
        public int IndexOf(string skillId) { for (int i = 0; i < SlotCount; i++) if (slots[i] != null && slots[i].Id == skillId) return i; return -1; }
        public int SlotOfAction(CampusAction identity)
        { for (int i = 0; i < SlotCount; i++) if (slots[i] != null && slots[i].IdentityAction == identity) return i; return -1; }
        public bool IsEquipped(SkillRuntime runtime) => IndexOf(runtime) >= 0;

        public SkillRuntime Find(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            foreach (var r in GetComponents<SkillRuntime>()) if (r.Id == skillId) return r;
            return null;
        }

        // Equipping a skill already in another slot moves it; an empty id clears the slot.
        public bool Equip(int slot, string skillId)
        {
            if (slot < 0 || slot >= SlotCount) return false;
            var runtime = Find(skillId);
            if (!string.IsNullOrEmpty(skillId) && runtime == null) return false;
            int previous = runtime != null ? IndexOf(runtime) : -1;
            if (previous >= 0) slots[previous] = null;
            if (slots[slot] != null && slots[slot] != runtime) slots[slot].Cancel();
            slots[slot] = runtime;
            LoadoutChanged?.Invoke();
            return true;
        }

        public void CancelAll() { foreach (var s in GetComponents<SkillRuntime>()) s.Cancel(); }
        public bool EquipDuringRest(int slot,string id)
        {
            var level=Levels.LevelDirector.Instance;
            if(level==null||!level.CanChangeSkills)return false;
            var skill=Find(id);
            if(skill==null||!skill.IsUnlocked)return false;
            bool newlyEquipped=!IsEquipped(skill);
            if(!Equip(slot,id))return false;
            if(newlyEquipped)skill.ReadyOnRestEquip();
            return true;
        }

        public static CampusAction SlotAction(int slot) => (CampusAction)((int)CampusAction.Skill1 + slot);
        public static int SlotOf(CampusAction action)
        {
            int i = (int)action - (int)CampusAction.Skill1;
            return i >= 0 && i < SlotCount ? i : -1;
        }
    }
}
