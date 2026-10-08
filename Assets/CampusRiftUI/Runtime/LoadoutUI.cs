using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Progression;
using CampusRift.Skills;

namespace CampusRift.UI
{
    // The preparation screen of a level (P09-T04): the four skill slots, level intelligence, the suggested set, three saved sets
    // and the items to carry. START writes the choice into LevelSession and loads the level.
    public sealed class LoadoutUI : MonoBehaviour
    {
        public static LevelDefinition Level;
        public static LoadoutUI Instance { get; private set; }
        public const int PresetCount = 3;
        UiKit kit;
        RectTransform root, body;
        readonly string[] slots = new string[SkillLoadout.SlotCount];
        int selectedSlot;
        Element elementFilter = Element.None; bool filterElement;
        int roleFilter = -1;
        static bool Vn => LevelHUD.Vietnamese;
        static string L(string en, string vn) => Vn ? vn : en;
        public string[] Slots => slots;
        public bool Visible => root != null && root.gameObject.activeSelf;
        static readonly string[] SlotKeys = { "Q", "E", "R", "F" };

        // Suggested sets of plan §7.6, by level; ids that do not exist yet or are still locked are simply left out.
        static readonly string[][] Suggested =
        {
            new[] { "hu-khong-ket-gioi", "anh-phan-than", "", "dai-thu-an" },
            new[] { "tich-lich-nhat-thiem", "dai-thu-an", "hu-khong-ket-gioi", "anh-phan-than" },
            new[] { "phat-no-hoa-lien", "han-bang-phong-an", "than-kiem-ngu-loi", "tich-lich-nhat-thiem" },
            new[] { "han-bang-phong-an", "than-kiem-ngu-loi", "hu-khong-ket-gioi", "phat-no-hoa-lien" },
            new[] { "than-kiem-ngu-loi", "kim-chung-trao", "hang-long-thap-bat-chuong", "han-bang-phong-an" },
            new[] { "than-kiem-ngu-loi", "tam-muoi-chan-hoa", "kim-chung-trao", "hang-long-thap-bat-chuong" },
            new[] { "hac-dong-than-la", "tam-muoi-chan-hoa", "than-kiem-ngu-loi", "bac-minh-than-cong" },
            new[] { "han-bang-phong-an", "kim-chung-trao", "van-kiem-quyet", "hac-dong-than-la" },
            new[] { "tru-tien-kiem-tran", "hac-dong-than-la", "han-bang-phong-an", "kim-chung-trao" },
            new[] { "banh-truong-lanh-dia", "tru-tien-kiem-tran", "hac-dong-than-la", "kim-chung-trao" },
        };
        public static string[] SuggestedIds(int level) => Suggested[Mathf.Clamp(level, 1, Suggested.Length) - 1];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; Level = null; }

        void Start()
        {
            Instance = this;
            kit = UiKit.Create();
            if (kit == null) { enabled = false; return; }
            var rt = (RectTransform)transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            root = kit.Stretch(transform, "Loadout Root");
            var back = kit.Stretch(root, "Background").gameObject.AddComponent<Image>();
            back.sprite = ContentImages.Get("Scenes", "hub-background"); back.color = back.sprite != null ? new Color(.6f, .6f, .7f, 1) : new Color(.02f, .03f, .07f, 1);
            var shade = kit.Stretch(root, "Shade").gameObject.AddComponent<Image>(); shade.color = new Color(.01f, .015f, .04f, .7f); shade.raycastTarget = false;
            body = kit.Fit(root, "Body");
            root.gameObject.SetActive(false);
            UIStateManager.Instance.Changed += OnState;
            if (SettingsManager.Instance != null) SettingsManager.Instance.Changed += _ => { if (Visible) Rebuild(); };
            OnState(UIStateManager.Instance.State);
        }
        void OnDestroy() { if (Instance == this) Instance = null; if (UIStateManager.Instance != null) UIStateManager.Instance.Changed -= OnState; }

        void OnState(UIState state)
        {
            bool show = state == UIState.Loadout && Level != null;
            if (root == null) return;
            root.gameObject.SetActive(show);
            if (show) { transform.SetAsLastSibling(); Begin(); }
        }

        // ---------------------------------------------------------------- state
        CultivationService Cultivation => ProfileService.Instance.Cultivation;
        bool Unlocked(SkillDefinition d) => HubUI.SkillUnlocked(d, Cultivation);

        void Begin()
        {
            var saved = LevelSession.Loadout;
            var start = saved != null && saved.Length == SkillLoadout.SlotCount ? saved : new[] { "hu-khong-ket-gioi", "anh-phan-than", "", "dai-thu-an" };
            Apply(start); selectedSlot = 0; filterElement = false; roleFilter = -1;
            Rebuild();
        }

        // Puts ids into the slots: unknown, locked and duplicate skills are dropped.
        public void Apply(string[] ids)
        {
            var catalog = SkillCatalog.Instance; var used = new HashSet<string>();
            for (int i = 0; i < slots.Length; i++)
            {
                string id = ids != null && i < ids.Length ? ids[i] : "";
                var def = string.IsNullOrEmpty(id) ? null : catalog.Find(id);
                slots[i] = def != null && Unlocked(def) && used.Add(id) ? id : "";
            }
        }
        public void SetSlot(int slot, string id)
        {
            if (slot < 0 || slot >= slots.Length) return;
            for (int i = 0; i < slots.Length; i++) if (!string.IsNullOrEmpty(id) && slots[i] == id) slots[i] = "";
            slots[slot] = id ?? "";
        }
        public void UseSuggested() { Apply(SuggestedIds(Level.index)); Rebuild(); }

        List<LoadoutEntry> Presets => ProfileService.Instance.Data.loadouts;
        public void SavePreset(int index)
        {
            var list = Presets; while (list.Count < PresetCount) list.Add(new LoadoutEntry());
            list[index].ids = new List<string>(slots); ProfileService.Instance.MarkDirty(); Rebuild();
        }
        public bool HasPreset(int index) { var list = Presets; return index < list.Count && list[index].ids != null && list[index].ids.Any(s => !string.IsNullOrEmpty(s)); }
        public void LoadPreset(int index) { if (!HasPreset(index)) return; Apply(Presets[index].ids.ToArray()); Rebuild(); }

        // ---------------------------------------------------------------- drawing
        void Rebuild()
        {
            if (kit == null || Level == null) return;
            kit.Clear(body);
            DrawLevel(); DrawSkills(); DrawItems(); DrawFooter();
        }

        static string WeaknessText(Element e)
        {
            if (e == Element.None) return "—";
            var beats = new List<string>();
            foreach (Element x in Enum.GetValues(typeof(Element))) if (x != Element.None && ElementChart.Multiplier(x, e) > 1.01f) beats.Add(HubUI.ElementName(x, Vn));
            return beats.Count == 0 ? "—" : string.Join(", ", beats);
        }

        void DrawLevel()
        {
            var lv = Level;
            kit.Panel(body, "Left Back", 40, 40, 660, 940);
            kit.Image(body, "Picture", 52, 52, 636, 358, Color.white, ContentImages.Level(lv.index)).preserveAspect = false;
            kit.Text(body, L("LEVEL ", "MÀN ") + lv.index + "  /  " + lv.LocalizedName(Vn), 60, 420, 620, 44, 30, UiKit.Gold, TextAlignmentOptions.TopLeft, false);
            kit.Text(body, Vn && !string.IsNullOrEmpty(lv.briefVN) ? lv.briefVN : lv.briefEN, 60, 468, 620, 100, 20, null, TextAlignmentOptions.TopLeft, true);
            kit.Text(body, L("INTELLIGENCE", "TÌNH BÁO"), 60, 576, 620, 28, 20, UiKit.Gold, TextAlignmentOptions.TopLeft, false);
            float y = 608;
            var seen = new HashSet<EnemyArchetype>();
            foreach (var wave in lv.waves) foreach (var entry in wave.entries) if (entry.archetype != null && seen.Add(entry.archetype))
            {
                var a = entry.archetype;
                kit.Image(body, "El", 60, y - 2, 34, 34, Color.white, ContentImages.Get("Elements", HubElementFile(a.element)));
                kit.Text(body, $"{a.LocalizedName(Vn)}  —  {HubUI.ElementName(a.element, Vn)}  —  {L("weak to", "yếu")} {WeaknessText(a.element)}", 102, y, 590, 34, 19, null, TextAlignmentOptions.TopLeft, false);
                y += 38; if (y > 900) break;
            }
            foreach (var boss in lv.bosses) if (boss != null && y < 930)
            { kit.Text(body, $"{L("BOSS", "BOSS")}: {boss.LocalizedName(Vn)}  —  {HubUI.ElementName(boss.element, Vn)}  —  {L("weak to", "yếu")} {WeaknessText(boss.element)}", 60, y, 630, 34, 19, new Color(1f, .6f, .5f), TextAlignmentOptions.TopLeft, false); y += 38; }
            if (seen.Count == 0 && lv.bosses.Count == 0) kit.Text(body, L("Monster data arrives with the level.", "Thông tin quái sẽ có cùng màn chơi."), 60, y, 620, 30, 19, UiKit.Muted, TextAlignmentOptions.TopLeft, false);
        }
        static string HubElementFile(Element e) => e == Element.KhongGian ? "khong-gian" : e == Element.None ? "vo-he" : e.ToString().ToLowerInvariant();

        void DrawSkills()
        {
            kit.Panel(body, "Center Back", 720, 40, 720, 940);
            kit.Text(body, L("SKILLS  (4 SLOTS)", "KỸ NĂNG  (4 Ô)")+"  /  "+SkillCatalog.Instance.skills.Count, 750, 64, 660, 40, 22, UiKit.Gold, TextAlignmentOptions.TopLeft, false);
            var catalog = SkillCatalog.Instance;
            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i; var def = string.IsNullOrEmpty(slots[i]) ? null : catalog.Find(slots[i]);
                var card = kit.Card(body, "Slot " + i, 748 + i * 168, 118, 158, 242, i == selectedSlot ? new Color(.27f, .2f, .5f, 1) : new Color(.07f, .08f, .16f, 1), () => { selectedSlot = slot; Rebuild(); });
                var r = (RectTransform)card.transform;
                kit.Text(r, SlotKeys[i], 18, 14, 38, 34, 22, i==selectedSlot?ComicTheme.Ink:UiKit.Gold, TextAlignmentOptions.Center, false);
                if (def != null)
                {
                    kit.Image(r, "Icon", 24, 50, 110, 110, Color.white, HubUI.SkillIcon(def));
                    kit.Text(r, def.LocalizedName(Vn), 18, 174, 122, 50, 18, null, TextAlignmentOptions.Top, true).fontStyle=FontStyles.Normal;
                }
                else kit.Text(r, L("EMPTY", "TRỐNG"), 18, 96, 122, 36, 20, UiKit.Muted, TextAlignmentOptions.Top, false);
            }
            if (!string.IsNullOrEmpty(slots[selectedSlot]))
                kit.Button(body, L("CLEAR ", "BỎ Ô ") + SlotKeys[selectedSlot], 748, 376, 156, 72, () => { slots[selectedSlot] = ""; Rebuild(); }, true, false, 20);
            // filters
            string elName = filterElement ? HubUI.ElementName(elementFilter, Vn) : L("All", "Tất cả");
            kit.Button(body, L("ELEMENT: ", "HỆ: ") + elName, 916, 376, 202, 72, CycleElement, true, false, 19);
            string roleName = roleFilter < 0 ? L("All", "Tất cả") : HubUI.RoleName((SkillRole)roleFilter, Vn);
            kit.Button(body, L("ROLE: ", "VAI TRÒ: ") + roleName, 1130, 376, 282, 72, CycleRole, true, false, 19);
            // list
            var skills = catalog.skills.Where(s => s != null && (!filterElement || s.element == elementFilter) && (roleFilter < 0 || (int)s.role == roleFilter)).ToList();
            float rowH = 112; var list = kit.Scroll(body, "Skill List", 744, 462, 680, 336, Mathf.Max(336, skills.Count * rowH));
            for (int i = 0; i < skills.Count; i++)
            {
                var d = skills[i]; bool open = Unlocked(d); bool equipped = slots.Contains(d.id);
                var card = kit.Card(list, "Skill " + d.id, 0, i * rowH, 668, rowH - 6, equipped ? new Color(.16f, .2f, .3f, 1) : new Color(.06f, .07f, .14f, .95f), () => { SetSlot(selectedSlot, d.id); Rebuild(); }, open);
                var r = (RectTransform)card.transform;
                kit.Image(r, "Icon", 12, 12, 82, 82, open ? Color.white : new Color(.4f, .4f, .45f, 1), HubUI.SkillIcon(d));
                kit.Text(r, d.LocalizedName(Vn), 110, 16, 350, 38, 23, equipped?ComicTheme.Ink:open ? (Color?)null : UiKit.Muted, TextAlignmentOptions.TopLeft, false);
                kit.Text(r, open ? HubUI.ElementName(d.element, Vn) + "  /  " + HubUI.RoleName(d.role, Vn) : HubUI.UnlockText(d, Cultivation, Vn), 110, 62, 448, 30, 17, equipped?ComicTheme.Ink:open ? UiKit.Muted : UiKit.Danger, TextAlignmentOptions.TopLeft, false);
                kit.Image(r, "El", 610, 22, 40, 40, Color.white, ContentImages.Get("Elements", HubElementFile(d.element)));
                if (equipped) kit.Text(r, L("EQUIPPED", "ĐÃ CHỌN"), 472, 12, 126, 30, 16, ComicTheme.Ink, TextAlignmentOptions.TopRight, false);
            }
            kit.Text(body,L("Select a slot, then a skill to equip.","Chọn ô, rồi chọn kỹ năng để trang bị."),748,802,660,28,14,UiKit.Muted,TextAlignmentOptions.TopLeft,false);
            // sets
            kit.Button(body, L("USE SUGGESTED SET", "DÙNG BỘ GỢI Ý"), 740, 836, 300, 68, UseSuggested, true, true);
            for (int i = 0; i < PresetCount; i++)
            {
                int p = i; float x = 1052 + i * 130;
                kit.Button(body, L("LOAD ", "NẠP ") + (i + 1), x, 836, 122, 68, () => LoadPreset(p), HasPreset(p), false, 17);
                kit.Button(body, L("SAVE ", "LƯU ") + (i + 1), x, 908, 122, 68, () => SavePreset(p), true, false, 17);
            }
        }
        void CycleElement()
        {
            var present = SkillCatalog.Instance.skills.Where(s => s != null).Select(s => s.element).Distinct().OrderBy(e => (int)e).ToList();
            if (!filterElement) { if (present.Count > 0) { filterElement = true; elementFilter = present[0]; } }
            else { int i = present.IndexOf(elementFilter) + 1; if (i >= present.Count) filterElement = false; else elementFilter = present[i]; }
            Rebuild();
        }
        void CycleRole()
        {
            var present = SkillCatalog.Instance.skills.Where(s => s != null).Select(s => (int)s.role).Distinct().OrderBy(x => x).ToList();
            int i = present.IndexOf(roleFilter) + 1; roleFilter = i >= present.Count ? -1 : present[i]; Rebuild();
        }

        void DrawItems()
        {
            var inv = ProfileService.Instance.Inventory; var catalog = ItemCatalog.Instance;
            kit.Panel(body, "Right Back", 1460, 40, 420, 940);
            kit.Text(body, L("ITEMS", "VẬT PHẨM") + $"  {inv.CarriedKinds}/{inv.SlotCount}", 1490, 64, 360, 40, 22, UiKit.Gold, TextAlignmentOptions.TopLeft, false);
            var owned = catalog.Owned(inv).ToList();
            if (owned.Count == 0)
            { kit.Text(body, L("Your inventory is empty. Buy items in the Alchemy Pavilion.", "Kho đang trống. Mua vật phẩm ở Đan Các."), 1480, 100, 380, 100, 20, UiKit.Muted, TextAlignmentOptions.TopLeft, true); return; }
            var itemList=kit.Scroll(body,"Owned Items",1476,118,388,392,Mathf.Max(392,owned.Count*148));
            float y = 0;
            foreach (var item in owned)
            {
                var captured = item; int carry = inv.CarryCount(item.id), limit = Math.Min(item.maxPerLevel, inv.Count(item.id));
                bool canAdd = carry > 0 || inv.CarriedKinds < inv.SlotCount;
                var card = kit.Card(itemList, "Item " + item.id, 0, y, 376, 140, carry > 0 ? new Color(.16f, .2f, .3f, 1) : new Color(.06f, .07f, .14f, .95f), () =>
                {
                    int next = carry >= limit ? 0 : carry + 1; inv.SetCarry(captured, next); Rebuild();
                }, canAdd);
                var r = (RectTransform)card.transform;
                kit.Image(r, "Icon", 8, 8, 76, 76, Color.white, ItemIcons.Get(item));
                kit.Text(r, item.Name(Vn), 100, 12, 256, 34, 22, carry>0?ComicTheme.Ink:(Color?)null, TextAlignmentOptions.TopLeft, false);
                kit.Text(r, $"{L("take", "mang")} {carry}/{limit}   {L("owned", "có")} {inv.CountText(item.id)}", 100, 52, 256, 30, 18, carry > 0 ? ComicTheme.Ink : UiKit.Muted, TextAlignmentOptions.TopLeft, false);
                kit.Text(r,item.Description(Vn),18,90,334,36,17,carry>0?ComicTheme.Ink:UiKit.Muted,TextAlignmentOptions.TopLeft,true);
                y += 148;
            }
            kit.Text(body,L("EQUIPMENT FOR THIS LEVEL","TRANG BỊ MANG VÀO MÀN"),1486,530,366,38,20,UiKit.Gold,TextAlignmentOptions.TopLeft,false);
            var packed=inv.Carry.ToList();
            for(int i=0;i<inv.SlotCount&&i<3;i++)
            {
                var slot=kit.Panel(body,"Carried Slot "+i,1476,580+i*102,388,92);
                if(i<packed.Count)
                {
                    var entry=packed[i];var item=catalog.Item(entry.key);
                    if(item!=null){kit.Image(slot,"Icon",16,12,64,64,Color.white,ItemIcons.Get(item));kit.Text(slot,item.Name(Vn)+" ×"+entry.count,98,20,268,44,20,null,TextAlignmentOptions.TopLeft,true);}
                }
                else kit.Text(slot,L("EMPTY EQUIPMENT SLOT","Ô TRANG BỊ TRỐNG"),22,24,336,40,18,UiKit.Muted,TextAlignmentOptions.Center,false);
            }
            kit.Button(body, L("TAKE NOTHING", "KHÔNG MANG GÌ"), 1476, 900, 388, 68, () => { inv.ClearCarry(); Rebuild(); }, inv.CarriedKinds > 0);
        }

        void DrawFooter()
        {
            kit.Button(body, L("BACK", "QUAY LẠI"), 40, 996, 300, 68, () => UIStateManager.Instance.Back());
            kit.Button(body, L("START", "BẮT ĐẦU"), 1580, 996, 300, 68, StartLevel, true, true);
            kit.Text(body, "ESC", 380, 1018, 80, 32, 16, UiKit.Muted, TextAlignmentOptions.MidlineLeft, false);
        }

        public void StartLevel()
        {
            if (Level == null || GameSceneManager.Instance == null) return;
            var ids = (string[])slots.Clone();
            if(Level.runMode==EndgameMode.Normal)GameSceneManager.Instance.StartLevel(Level.index, ids);
            else GameSceneManager.Instance.StartEndgame(Level,ids);
        }
    }
}
