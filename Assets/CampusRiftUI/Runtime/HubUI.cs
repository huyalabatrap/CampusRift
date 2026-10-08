using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CampusRift.Combat;
using CampusRift.Learning;
using CampusRift.Levels;
using CampusRift.Progression;
using CampusRift.Skills;

namespace CampusRift.UI
{
    // The Tu Luyện Hub (P09-T02): everything outside a level. Header with realm, Tu Vi, Linh Thạch and reviews due, five tabs.
    // Library, Alchemy Pavilion and Realm are screens of the study panel (they open it directly); Skill Book and Level Map are
    // drawn here. Built in code under the menu canvas, so the menu scene needs no new objects.
    public sealed partial class HubUI : MonoBehaviour
    {
        public enum Tab { Library, Shop, Skills, Realm, Map, Endgame }
        public static HubUI Instance { get; private set; }
        UiKit kit;
        RectTransform root, header, tabs, content;
        Tab tab = Tab.Map;
        SkillDefinition selectedSkill;
        static bool Vn => LevelHUD.Vietnamese;
        static string L(string en, string vn) => Vn ? vn : en;
        public Tab Current => tab;
        public bool Visible => root != null && root.gameObject.activeSelf;
        public RectTransform ContentRect => content;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneLoaded += OnSceneLoaded; TryCreate(); }
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { TryCreate(); }
        static void TryCreate()
        {
            var manager = FindAnyObjectByType<UIManager>();
            if (manager == null || manager.IsGameplay || manager.MainMenu == null || FindAnyObjectByType<HubUI>() != null) return;
            var go = new GameObject("Hub UI", typeof(RectTransform)); go.transform.SetParent(manager.MainMenu.transform.parent, false);
            go.AddComponent<HubUI>();
            var loadout = new GameObject("Loadout UI", typeof(RectTransform)); loadout.transform.SetParent(manager.MainMenu.transform.parent, false);
            loadout.AddComponent<LoadoutUI>();
        }

        void Start()
        {
            Instance = this;
            kit = UiKit.Create();
            if (kit == null) { enabled = false; return; }
            TidyTitleMenu();
            Build();
            UIStateManager.Instance.Changed += OnState;
            if (SettingsManager.Instance != null) SettingsManager.Instance.Changed += OnSettings;
            if (ProfileService.Instance != null) ProfileService.Instance.Changed += OnProfile;
            DevMode.Changed += OnDevMode;
            OnState(UIStateManager.Instance.State);
        }
        void OnDestroy()
        {
            ClearEndgamePreview();
            if (Instance == this) Instance = null;
            if (UIStateManager.Instance != null) UIStateManager.Instance.Changed -= OnState;
            if (SettingsManager.Instance != null) SettingsManager.Instance.Changed -= OnSettings;
            if (ProfileService.Instance != null) ProfileService.Instance.Changed -= OnProfile;
            DevMode.Changed -= OnDevMode;
        }

        // The title menu no longer needs Continue and Courses: the profile is always loaded and studying lives in the Hub.
        void TidyTitleMenu()
        {
            var manager = FindAnyObjectByType<UIManager>();
            if (manager == null || manager.MainMenu == null) return;
            var buttons = manager.MainMenu.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var b in buttons) if (b.name == "CONTINUE" || b.name == "COURSES") b.gameObject.SetActive(false);
            float y = -80;
            foreach (var name in new[] { "SETTINGS", "CREDITS", "QUIT" })
            {
                var b = buttons.FirstOrDefault(x => x.name == name); if (b == null) continue;
                ((RectTransform)b.transform).anchoredPosition = new Vector2(((RectTransform)b.transform).anchoredPosition.x, y); y -= 68;
            }
        }

        void OnState(UIState state)
        {
            bool show = state == UIState.Hub;
            if (root == null) return;
            root.gameObject.SetActive(show);
            if(!show)ClearEndgamePreview();
            if (show) { transform.SetAsLastSibling(); Refresh(); }
        }
        void OnSettings(GameSettings settings) { if (Visible) Refresh(); }
        void OnProfile() { if (Visible) RefreshHeader(); }
        void OnDevMode() { if (Visible) Refresh(); }

        // ---------------------------------------------------------------- frame
        void Build()
        {
            var rt = (RectTransform)transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            root = kit.Stretch(transform, "Hub Root");
            var back = kit.Stretch(root, "Background"); var image = back.gameObject.AddComponent<Image>();
            image.sprite = ContentImages.Get("Scenes", "hub-background"); image.color = image.sprite != null ? new Color(.85f, .85f, .9f, 1) : new Color(.02f, .03f, .07f, 1); image.raycastTarget = true;
            var shade = kit.Stretch(root, "Shade"); var s = shade.gameObject.AddComponent<Image>(); s.color = new Color(.01f, .015f, .04f, .55f); s.raycastTarget = false;
            var frame = kit.Fit(root, "Frame");
            header = kit.Rect(frame, "Header", 0, 0, 1920, 124); tabs = kit.Rect(frame, "Tabs", 0, 128, 1920, 84);
            content = kit.Rect(frame, "Content", 60, 232, 1800, 750);
            var nav = kit.Rect(frame, "Footer", 0, 996, 1920, 84);
            kit.Button(nav, L("MAIN MENU", "MENU CHÍNH"), 60, 2, 320, 78, () => UIStateManager.Instance.Back());
            var hint = kit.Text(nav, "ESC", 402, 30, 80, 32, 16, UiKit.Muted, TextAlignmentOptions.MidlineLeft);
            hint.name = "Hint";
            var arButton=kit.Button(nav,"AR RIFT",700,2,340,78,()=>CampusRift.AR.ARSceneNavigation.Enter());
            kit.Button(nav, L("SETTINGS", "CÀI ĐẶT"), 1060, 2, 156, 78, () => UIStateManager.Instance.OpenSettings(), true, false, 18);
            var arEntry=gameObject.AddComponent<CampusRift.AR.ARHubEntry>();arEntry.button=arButton;
            arEntry.unavailable=kit.Text(nav,"",540,22,620,48,19,UiKit.Muted,TextAlignmentOptions.Center);
            kit.Button(nav, L("TO COURSES", "ĐẾN KHÓA HỌC"), 1230, 2, 340, 78, () => Select(Tab.Library));
            kit.Button(nav, L("TO HUB", "VỀ HUB"), 1590, 2, 270, 78, () => Select(Tab.Map));
            root.gameObject.SetActive(false);
        }

        void Refresh() { RefreshHeader(); BuildTabs(); ShowTab(tab); }

        void RefreshHeader()
        {
            if (header == null) return;
            kit.Clear(header);
            var profile = ProfileService.Instance; if (profile == null) return;
            var cultivation = profile.Cultivation; var info = CultivationUI.Describe(cultivation, Vn);
            kit.Panel(header, "Panel", 40, 12, 1840, 108);
            kit.Image(header, "Realm Badge", 56, 20, 92, 92, Color.white, ContentImages.Realm(cultivation.RealmIndex));
            var cosmeticTitle=EndgameService.Title(profile.Data);
            kit.Text(header, info.title+(cosmeticTitle!=null?" · "+(Vn?cosmeticTitle.nameVN:cosmeticTitle.nameEN):""), 164, 22, 700, 40, 34, UiKit.Gold, TextAlignmentOptions.MidlineLeft, false);
            kit.Image(header, "Tu Vi Back", 164, 72, 520, 14, new Color(.12f, .13f, .2f, 1));
            kit.Image(header, "Tu Vi Fill", 164, 72, 520 * Mathf.Clamp01(info.fraction), 14, info.bottleneck ? UiKit.Gold : new Color(.55f, .45f, 1f, 1));
            kit.Text(header, info.bar, 700, 62, 520, 34, 20, UiKit.Muted, TextAlignmentOptions.MidlineLeft, false);
            kit.Text(header, "LINH THẠCH", 1210, 26, 290, 34, 18, UiKit.Muted, TextAlignmentOptions.TopRight, false);
            kit.Text(header, profile.Wallet.BalanceText, 1210, 60, 290, 48, 34, UiKit.Gold, TextAlignmentOptions.TopRight, false);
            int due = LearningService.Instance != null ? LearningService.Instance.Engine.DueReviewCountReadOnly : 0;
            kit.Text(header, L("REVIEWS DUE", "BÀI ĐẾN HẠN ÔN"), 1530, 26, 290, 34, 18, UiKit.Muted, TextAlignmentOptions.TopRight, false);
            kit.Text(header, due.ToString(), 1530, 60, 290, 48, 34, due > 0 ? new Color(.5f, 1f, .8f) : UiKit.Muted, TextAlignmentOptions.TopRight, false);
        }

        static readonly Tab[] Order = { Tab.Library, Tab.Shop, Tab.Skills, Tab.Realm, Tab.Map, Tab.Endgame };
        string TabName(Tab t)
        {
            switch (t)
            {
                case Tab.Library: return L("LIBRARY", "THƯ VIỆN");
                case Tab.Shop: return L("ALCHEMY", "ĐAN CÁC");
                case Tab.Skills: return L("SKILLS", "CÔNG PHÁP");
                case Tab.Realm: return L("REALM", "CẢNH GIỚI");
                case Tab.Endgame: return L("ENDGAME", "HẬU KẾT");
                default: return L("MAP", "BẢN ĐỒ");
            }
        }
        void BuildTabs()
        {
            kit.Clear(tabs);
            float x = 60;
            foreach (var t in Order)
            {
                var captured = t;
                var b = kit.Button(tabs, TabName(t), x, 0, 286, 80, () => Select(captured), true, tab == t);
                x += 300;
            }
        }

        public void Select(Tab target) { tab = target; BuildTabs(); ShowTab(target); }

        void ShowTab(Tab t)
        {
            ClearEndgamePreview();
            kit.Clear(content);
            switch (t)
            {
                case Tab.Endgame: BuildEndgame(); break;
                case Tab.Skills: BuildSkillBook(); break;
                case Tab.Shop: BuildShop(); break;
                case Tab.Library: BuildLibrary(); break;
                case Tab.Realm: BuildRealm(); break;
                default: BuildMap(); break;
            }
        }

        // ---------------------------------------------------------------- level map (P09-T03)
        void BuildMap()
        {
            var catalog = LevelCatalog.Instance; var progress = ProfileService.Instance.Levels;
            kit.Panel(content,"Map Navy Back",0,0,1840,750);
            const float cardW = 336, cardH = 360, gap = 30;
            for (int i = 1; i <= 10; i++)
            {
                int index = i; int col = (i - 1) % 5, row = (i - 1) / 5;
                float x = col * (cardW + gap) + 12, y = row * (cardH + 16) + 4;
                var level = catalog != null ? catalog.Get(i) : null;
                bool open = progress.IsUnlocked(i, out string reason);
                var card = kit.Card(content, "Level " + i, x, y, cardW, cardH, new Color(.04f, .05f, .11f, .9f), () => OpenLoadout(index), open && level != null);
                var rect = (RectTransform)card.transform;
                var sprite = open ? ContentImages.Level(i) : ContentImages.Get("Scenes","level-"+i.ToString("00")+"-locked");
                var pic = kit.Image(rect, "Picture", 8, 8, cardW - 16, 180, Color.white, sprite);
                pic.preserveAspect = false;
                if(!open)kit.Image(rect,"Locked",138,60,60,60,Color.white,ComicTheme.Sprite("lock"));
                kit.Text(rect, L("LEVEL ", "MÀN ") + i, 24, 194, cardW - 48, 32, 20, UiKit.Gold, TextAlignmentOptions.TopLeft, false);
                kit.Text(rect, level != null ? level.LocalizedName(Vn) : "—", 24, 230, cardW - 48, 38, 23, null, TextAlignmentOptions.TopLeft, false);
                int stars = progress.StarCount(i);
                for (int s = 0; s < 3; s++) kit.Image(rect, "Star", 24 + s * 36, 274, 30, 30, Color.white, ContentImages.Star(s < stars));
                float best = progress.BestTime(i);
                kit.Text(rect, best > 0 ? L("BEST ", "TỐT NHẤT ") + LevelResultUI.FormatTime(best) : L("NOT CLEARED", "CHƯA QUA"), 142, 272, 170, 34, 17, UiKit.Muted, TextAlignmentOptions.TopRight, false);
                if (!open) kit.Text(rect, string.IsNullOrEmpty(reason) ? L("Locked", "Đã khóa") : reason, 24, 310, cardW - 48, 40, 18, UiKit.Danger, TextAlignmentOptions.TopLeft, true);
                else kit.Text(rect, progress.IsCleared(i) ? L("CLEARED — PLAY AGAIN", "ĐÃ QUA — CHƠI LẠI") : L("READY", "SẴN SÀNG"), 24, 310, cardW - 48, 34, 18, new Color(.5f, 1f, .8f), TextAlignmentOptions.TopLeft, false);
            }
        }

        void OpenLoadout(int index)
        {
            LoadoutUI.Level = LevelCatalog.Instance.Get(index);
            UIStateManager.Instance.OpenLoadout();
        }

        // ---------------------------------------------------------------- skill book (P09-T05)
        static string ElementFile(Element e)
        {
            switch (e)
            {
                case Element.Kim: return "kim"; case Element.Moc: return "moc"; case Element.Thuy: return "thuy"; case Element.Hoa: return "hoa";
                case Element.Tho: return "tho"; case Element.Loi: return "loi"; case Element.Am: return "am"; case Element.KhongGian: return "khong-gian";
                default: return "vo-he";
            }
        }
        public static string ElementName(Element e, bool vn)
        {
            switch (e)
            {
                case Element.Kim: return vn ? "Kim" : "Metal"; case Element.Moc: return vn ? "Mộc" : "Wood"; case Element.Thuy: return vn ? "Thủy" : "Water";
                case Element.Hoa: return vn ? "Hỏa" : "Fire"; case Element.Tho: return vn ? "Thổ" : "Earth"; case Element.Loi: return vn ? "Lôi" : "Lightning";
                case Element.Am: return vn ? "Âm" : "Yin"; case Element.KhongGian: return vn ? "Không Gian" : "Space"; default: return vn ? "Vô hệ" : "Neutral";
            }
        }
        public static string RoleName(SkillRole r, bool vn)
        {
            switch (r)
            {
                case SkillRole.Burst: return vn ? "Bạo phát" : "Burst"; case SkillRole.Control: return vn ? "Khống chế" : "Control"; case SkillRole.Defense: return vn ? "Phòng thủ" : "Defense";
                case SkillRole.Mobility: return vn ? "Cơ động" : "Mobility"; case SkillRole.Summon: return vn ? "Triệu hồi" : "Summon"; case SkillRole.Support: return vn ? "Hỗ trợ" : "Support";
                default: return vn ? "Tiện ích" : "Utility";
            }
        }
        public static Sprite SkillIcon(SkillDefinition d) => d.icon != null ? d.icon : ContentImages.Get("Skills", d.id);
        public static bool SkillUnlocked(SkillDefinition d, CultivationService c) => d != null && (d.starter || (c != null && c.AtLeast(d.unlockRealm, d.unlockTier)));
        public static string UnlockText(SkillDefinition d, CultivationService c, bool vn)
        {
            var name = c.Table.TierName((Realm)Mathf.Clamp(d.unlockRealm, 0, c.Table.RealmCount - 1), d.unlockTier, vn);
            return (vn ? "Mở ở " : "Unlocks at ") + name;
        }

        void BuildSkillBook()
        {
            var catalog = SkillCatalog.Instance; var cultivation = ProfileService.Instance.Cultivation;
            var skills = catalog.skills.Where(s => s != null).ToList();
            if (selectedSkill == null || !skills.Contains(selectedSkill)) selectedSkill = skills.FirstOrDefault();
            kit.Panel(content, "List Back", 0, 0, 640, 770);
            kit.Text(content, L("SKILLS", "CÔNG PHÁP") + "  /  " + skills.Count, 24, 14, 590, 34, 22, UiKit.Gold, TextAlignmentOptions.TopLeft, false);
            float rowH = 96; var list = kit.Scroll(content, "List", 8, 56, 624, 706, Mathf.Max(706, skills.Count * rowH));
            for (int i = 0; i < skills.Count; i++)
            {
                var d = skills[i]; bool open = SkillUnlocked(d, cultivation); bool sel = d == selectedSkill;
                var card = kit.Card(list, "Skill " + d.id, 0, i * rowH, 616, rowH - 8, sel ? new Color(.2f, .16f, .38f, 1) : new Color(.06f, .07f, .14f, .95f), () => { selectedSkill = d; ShowTab(Tab.Skills); });
                var r = (RectTransform)card.transform;
                kit.Image(r, "Icon", 10, 8, 76, 76, open ? Color.white : new Color(.4f, .4f, .45f, 1), SkillIcon(d));
                kit.Text(r, d.LocalizedName(Vn), 100, 10, 420, 34, 26, open ? (Color?)null : UiKit.Muted, TextAlignmentOptions.TopLeft, false);
                kit.Text(r, ElementName(d.element, Vn) + "  /  " + RoleName(d.role, Vn), 100, 40, 400, 32, 19, UiKit.Muted, TextAlignmentOptions.TopLeft, false);
                kit.Image(r, "Element", 540, 22, 56, 56, Color.white, ContentImages.Get("Elements", ElementFile(d.element)));
                if (!open) kit.Image(r, "Lock", 566, 4, 4, 4, Color.clear);
            }
            // detail
            var detail=kit.Panel(content, "Detail Back", 660, 0, 1140, 770);
            if (selectedSkill == null) { kit.Text(detail, L("No skills yet.", "Chưa có kỹ năng."), 40, 40, 900, 40, 26); return; }
            var s = selectedSkill; bool unlocked = SkillUnlocked(s, cultivation);
            kit.Image(detail, "Big Icon", 30, 24, 240, 240, unlocked ? Color.white : new Color(.45f, .45f, .5f, 1), SkillIcon(s));
            kit.Text(detail, s.LocalizedName(Vn), 300, 26, 800, 54, 42, UiKit.Gold, TextAlignmentOptions.TopLeft, false);
            kit.Image(detail, "Element Big", 300, 92, 52, 52, Color.white, ContentImages.Get("Elements", ElementFile(s.element)));
            kit.Text(detail, ElementName(s.element, Vn) + "  /  " + RoleName(s.role, Vn) + "  /  " + CastName(s.castType), 364, 100, 740, 40, 24, UiKit.Muted, TextAlignmentOptions.TopLeft, false);
            kit.Text(detail, unlocked ? L("Unlocked", "Đã mở") : UnlockText(s, cultivation, Vn), 300, 156, 800, 34, 24, unlocked ? new Color(.5f, 1f, .8f) : UiKit.Danger, TextAlignmentOptions.TopLeft, false);
            var progress=ProfileService.Instance.Skills;int currentRank=progress.GetRank(s.id);
            kit.Text(detail, (Vn && !string.IsNullOrEmpty(s.descriptionVN) ? s.descriptionVN : s.description), 40, 280, 1070, 106, 24, null, TextAlignmentOptions.TopLeft, true);
            kit.Text(detail, $"{L("Rank", "Tầng")} {currentRank}/5   •   {L("Cooldown", "Hồi chiêu")}: {s.cooldown*(1-.05f*(currentRank-1)):0.#}s    {L("Spirit cost", "Linh Lực")}: {s.spiritCost:0}", 40, 394, 1070, 34, 22, UiKit.Muted, TextAlignmentOptions.TopLeft, false);
            kit.Text(detail, L("RANKS", "CÁC TẦNG"), 40, 434, 400, 30, 20, UiKit.Gold, TextAlignmentOptions.TopLeft, false);
            string[] rankVN = { "Nhập Môn", "Tiểu Thành", "Đại Thành", "Viên Mãn", "Hóa Cảnh" }, rankEN = { "Initiate", "Minor", "Major", "Perfect", "Transcendent" };
            for (int r = 0; r < s.ranks.Length && r < 5; r++)
            {
                var rank = s.ranks[r]; if (rank == null) continue;
                string realm = cultivation.Table.RealmName((Realm)Mathf.Clamp(rank.requiredRealm, 0, cultivation.Table.RealmCount - 1), Vn);
                kit.Text(detail, $"{r + 1}. {(Vn ? rankVN[r] : rankEN[r])}    {L("power", "sức mạnh")} ×{rank.effectMultiplier:0.##}    {L("cooldown", "hồi chiêu")} ×{rank.cooldownMultiplier:0.##}    {L("cost", "giá")} {rank.upgradeCost}    {L("realm", "cảnh giới")} {realm}", 40, 468 + r * 31, 1070, 30, 20, r+1==currentRank?(Color?)UiKit.Gold:null, TextAlignmentOptions.TopLeft, false);
            }
            var upgrade=kit.Button(detail,currentRank==5?L("MAX RANK","ĐÃ HÓA CẢNH"):L("UPGRADE / ","NÂNG TẦNG / ")+progress.NextCost(s)+L(" STONES"," LINH THẠCH"),40,638,470,68,()=>{if(progress.TryUpgrade(s))ShowTab(Tab.Skills);},true);
            upgrade.interactable=progress.CanUpgrade(s);
            string requirement=currentRank==5?L("Mastered","Đã đạt tầng tối đa"):L("Required realm: ","Cảnh giới cần: ")+cultivation.Table.RealmName((Realm)progress.RequiredRealm(s),Vn);
            kit.Text(detail,requirement,534,642,568,60,22,UiKit.Muted,TextAlignmentOptions.MidlineLeft,true);
            kit.Text(detail, ComboHint(s), 40, 714, 1070, 40, 20, UiKit.Muted, TextAlignmentOptions.TopLeft, false);
        }

        static string CastName(CastType c)
        {
            switch (c)
            {
                case CastType.Instant: return Vn ? "Tức thì" : "Instant"; case CastType.Aimed: return Vn ? "Ngắm" : "Aimed";
                case CastType.Channel: return Vn ? "Niệm" : "Channel"; default: return Vn ? "Bật/tắt" : "Toggle";
            }
        }
        // The reaction table arrives with P11; until then the element chart is the only combo rule.
        static string ComboHint(SkillDefinition s)
        {
            if (s.element == Element.None) return L("A neutral skill: pairs with any element.", "Kỹ năng vô hệ: kết hợp được với mọi hệ.");
            var strong = new List<string>(); var weak = new List<string>();
            foreach (Element x in Enum.GetValues(typeof(Element)))
            {
                if (x == Element.None) continue;
                if (ElementChart.Multiplier(s.element, x) > 1.01f) strong.Add(ElementName(x, Vn));
                if (ElementChart.Multiplier(x, s.element) > 1.01f) weak.Add(ElementName(x, Vn));
            }
            string a = strong.Count > 0 ? string.Join(", ", strong) : "—", b = weak.Count > 0 ? string.Join(", ", weak) : "—";
            return L($"Strong against: {a}.  Weak to: {b}.  Combine skills to trigger elemental reactions.", $"Khắc: {a}.  Bị khắc bởi: {b}.  Kết hợp kỹ năng để kích hoạt phản ứng nguyên tố.");
        }
    }
}
