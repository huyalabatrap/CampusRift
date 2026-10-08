using System.Collections.Generic;
using UnityEngine;
using CampusRift.Controls;
using CampusRift.Skills;

namespace CampusRift.UI
{
    // Lays the PC skill bar out in loadout order (P02-T06). Legacy skills keep the slot objects their setup
    // built (glyphs, charge crystals…) and those slots move with the skill; other skills and empty slots
    // use generic slots cloned from SkillBarUI.slotTemplate.
    [DefaultExecutionOrder(-40)]
    public sealed class SkillBarBinder : MonoBehaviour
    {
        SkillBarUI bar;
        SkillLoadout loadout;
        CampusInput input;
        readonly Dictionary<SkillRuntime, SkillSlotUI> legacySlots = new Dictionary<SkillRuntime, SkillSlotUI>();
        readonly SkillSlotUI[] generic = new SkillSlotUI[SkillLoadout.SlotCount];
        readonly SkillSlotUI[] shown = new SkillSlotUI[SkillLoadout.SlotCount];
        Vector2 origin, step;
        bool ready;

        public SkillSlotUI SlotAt(int index) => index >= 0 && index < shown.Length ? shown[index] : null;

        void Awake()
        {
            bar = GetComponent<SkillBarUI>();
            loadout = FindAnyObjectByType<SkillLoadout>();
            input = loadout != null ? loadout.GetComponent<CampusInput>() : FindAnyObjectByType<CampusInput>();
            if (bar == null || loadout == null || bar.Slots == null || bar.Slots.Length == 0) { enabled = false; return; }
            var rect = (RectTransform)transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);
            rect.anchoredPosition=new Vector2(-38,66);rect.sizeDelta=new Vector2(580,140);
            origin=new Vector2(76,0);step=new Vector2(140,0);
            var heading=transform.Find("Label")?.GetComponent<TMPro.TMP_Text>();if(heading!=null){var h=heading.rectTransform;h.anchorMin=h.anchorMax=new Vector2(.5f,1);h.pivot=new Vector2(.5f,0);h.anchoredPosition=new Vector2(0,12);h.sizeDelta=new Vector2(580,46);}
            var hud = GetComponentInParent<GameplayHUD>();
            if (hud != null)
            {
                var hand = hud.GetComponent<GiantHandHUD>(); if (hand != null) Map<GiantHandRuntime>(hand.slot);
                var wall = hud.GetComponent<VoidWallHUD>(); if (wall != null) Map<VoidWallRuntime>(wall.slot);
                var phantom = hud.GetComponent<PhantomDecoyHUD>(); if (phantom != null) Map<PhantomRuntime>(phantom.slot);
            }
            loadout.LoadoutChanged += Layout;
            for(int i=0;i<generic.Length;i++)Generic(i);
            ready = true; Layout();
        }
        void OnDestroy() { if (loadout != null) loadout.LoadoutChanged -= Layout; }

        void Map<T>(SkillSlotUI slot) where T : SkillRuntime
        {
            var runtime = loadout.GetComponent<T>();
            if (runtime != null && slot != null) legacySlots[runtime] = slot;
        }

        public void Layout()
        {
            if (!ready) return;
            foreach (var slot in legacySlots.Values) slot.gameObject.SetActive(false);
            for (int i = 0; i < SkillLoadout.SlotCount; i++)
            {
                var runtime = loadout.Get(i);
                SkillSlotUI slot;
                if (runtime != null && legacySlots.TryGetValue(runtime, out var owned)) { slot = owned; if (generic[i] != null) generic[i].gameObject.SetActive(false); }
                else
                {
                    slot = Generic(i);
                    if (slot == null) { shown[i] = null; continue; }
                    string key = input != null ? input.KeyLabel(SkillLoadout.SlotAction(i)) : "";
                    if (runtime == null) { slot.Configure(null, key, "EMPTY SLOT"); slot.SetLocked(false); slot.SetCooldown(0, 1); }
                    else slot.Configure(runtime.Definition != null ? runtime.Definition.icon : null, key, Tooltip(runtime), runtime.rank);
                }
                slot.gameObject.SetActive(true);
                var r=(RectTransform)slot.transform;r.anchorMin=r.anchorMax=new Vector2(0,.5f);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(116,116);r.anchoredPosition=origin+step*i;
                ComicSkillLayout.Apply(slot);
                slot.SetRank(runtime!=null?runtime.rank:0);
                slot.SetElement(runtime!=null&&runtime.Definition!=null?runtime.Definition.element:Combat.Element.None);
                shown[i] = slot;
            }
            bar.Slots = (SkillSlotUI[])shown.Clone();
        }

        SkillSlotUI Generic(int index)
        {
            if (generic[index] != null) return generic[index];
            if (bar.slotTemplate == null) return null;
            var slot = Instantiate(bar.slotTemplate, transform);
            slot.name = "Skill Slot " + (index + 1);
            generic[index] = slot; return slot;
        }

        static string Tooltip(SkillRuntime r)
        {
            var d = r.Definition;
            if (d == null) return r.ShortName;
            return d.displayName.ToUpperInvariant() + "\n" + d.description;
        }

        // Generic slots only: legacy HUD scripts drive their own slots.
        void Update()
        {
            for (int i = 0; i < SkillLoadout.SlotCount; i++)
            {
                var runtime = loadout.Get(i);
                if(shown[i]!=null)shown[i].SetRank(runtime!=null?runtime.rank:0);
                if (runtime == null || legacySlots.ContainsKey(runtime) || shown[i] == null) continue;
                shown[i].SetLocked(!runtime.IsUnlocked);
                shown[i].SetCooldown(runtime.CooldownRemaining, runtime.CooldownDuration);
            }
        }
    }
}
