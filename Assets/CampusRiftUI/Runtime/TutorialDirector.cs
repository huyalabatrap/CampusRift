using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using CampusRift.Controls;
using CampusRift.Progression;
using CampusRift.Levels;
using CampusRift.Combat;

namespace CampusRift.UI
{
    [Serializable] public sealed class TutorialStep { public string vn, en, mobileVN, mobileEN; }
    [Serializable] public sealed class TutorialProgress { public int hub, combat, fire; public bool skipHub, skipCombat, skipFire; }
    // Non-modal context cards: normal gameplay keeps running. H advances and Backspace skips on PC.
    public sealed class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }
#if UNITY_EDITOR
        public static bool Suppress;
#endif
        public string ActiveGroup { get; private set; }
        public int ActiveStep { get; private set; } = -1;
        public bool Visible => card != null && card.gameObject.activeSelf;
        public readonly TutorialStep[] Hub = {
            Step("THƯ VIỆN", "LIBRARY", "Mở Thư Viện trong Sảnh để học bài 1.", "Open Library in the Hub to study lesson 1."),
            Step("HỌC BÀI 1", "STUDY LESSON 1", "Đọc bài, làm trắc nghiệm và xem giải thích. Đạt 80% để nhận Tu Vi và Linh Thạch; có thể thử lại.", "Read, answer the quiz and review explanations. Pass at 80% to earn cultivation and stones; retry is available."),
            Step("ĐAN CÁC", "ALCHEMY PAVILION", "Mở Đan Các, mua 1 Hồi Khí Đan bằng Linh Thạch vừa học được.", "Open Alchemy Pavilion and buy one Recovery Pill with your study rewards."),
            Step("BẢN ĐỒ", "LEVEL MAP", "Mở Bản Đồ, chọn màn 1 rồi Chuẩn Bị.", "Open Map, select level 1, then Prepare."),
            Step("CHUẨN BỊ", "PREPARE", "Chọn Đại Thủ Ấn và mang Hồi Khí Đan vào túi dùng nhanh. Nhấn Vào Màn khi sẵn sàng.", "Equip Giant Hand and bring a Recovery Pill in a quick-use slot. Enter the level when ready.")
        };
        public readonly TutorialStep[] Combat = {
            Step("DI CHUYỂN", "MOVE", "WASD di chuyển; chuột xoay góc nhìn.", "WASD moves; mouse rotates the camera.", "Kéo cần trái để đi; kéo vùng phải để nhìn.", "Drag the left stick to move; drag the right side to look."),
            Step("ĐÁNH THƯỜNG", "ATTACK", "Nhấp chuột trái để phi kiếm; giữ 0,8 giây để đâm xuyên.", "Left click launches swords; hold 0.8 seconds for a piercing strike.", "Chạm nút Tấn công tròn; giữ để đâm xuyên.", "Tap the round Attack button; hold for a piercing strike."),
            Step("NÉ ĐÒN", "DODGE", "Khi thấy vòng báo đỏ, nhấn Ctrl để né ngang. Né tốn thể lực.", "When a red warning appears, press Ctrl to dodge sideways. Dodging costs stamina.", "Rời vòng đỏ bằng nút Né tròn. Né tốn thể lực.", "Leave red warnings with the round Dodge button. Dodging costs stamina."),
            Step("ĐẠI THỦ ẤN", "GIANT HAND", "Dùng phím ghi trên ô Đại Thủ Ấn. Ngắm xuống đất rồi xác nhận; cần Linh Lực.", "Use the key shown on Giant Hand's slot. Aim at the ground, then confirm; it costs spirit.", "Giữ và kéo ô Đại Thủ Ấn để ngắm; thả để dùng. Kéo tới Hủy để bỏ ngắm.", "Drag Giant Hand's button to aim, release to cast; drag to Cancel to stop aiming."),
            Step("VẬT PHẨM", "ITEMS", "Phím 1–3 dùng vật phẩm đã mang. Hồi Khí Đan phục hồi HP; chỉ dùng khi cần.", "Keys 1–3 use carried items. Recovery Pills restore HP; use one when needed.", "Chạm ô vật phẩm tròn đã mang; Hồi Khí Đan phục hồi HP.", "Tap a carried round item slot; Recovery Pills restore HP."),
            Step("TẦM YÊU", "FIND ENEMIES", "Nếu lạc quái, dùng Tầm Yêu Phù để tìm. Mua thêm ở Đan Các sau màn; bạn đã biết các điều khiển chính.", "Use an Enemy Finder charm if enemies are hard to find. Buy one at Alchemy after the level; you know the main controls now.")
        };
        public readonly TutorialStep[] Fire = {
            Step("THIÊN HỎA: VÀO NHÀ!", "SKY FIRE: GET INSIDE!", "Khi có cảnh báo Thiên Hỏa, theo mũi tên đến cửa và vào nhà. Ngoài trời nhận sát thương rất lớn; trong nhà vẫn nhận một phần.", "During Sky Fire warning, follow the arrow through a door into shelter. Outdoors is dangerous; indoors still takes some damage."),
            Step("THIÊN KIẾM", "HEAVEN SWORD", "Diệt hết đợt để đầy Kiếm Ý, ra ngoài trời và giữ V để triệu hồi. Đứng an toàn; không thể niệm trong Long Nộ.", "Clear a wave to fill Sword Intent, step outdoors and hold V to summon. Choose a safe moment; channeling is blocked during Fury.", "Diệt hết đợt, ra ngoài trời và giữ nút Thiên Kiếm tròn. Không thể niệm trong Long Nộ.", "Clear the wave, step outdoors and hold the round Heaven Sword button. Channeling is blocked during Fury.")
        };
        static TutorialStep Step(string vn, string en, string bodyVN, string bodyEN, string mobileVN = null, string mobileEN = null)
            => new TutorialStep { vn = vn + "\n" + bodyVN, en = en + "\n" + bodyEN, mobileVN = vn + "\n" + (mobileVN ?? bodyVN), mobileEN = en + "\n" + (mobileEN ?? bodyEN) };
        TelemetryConsentUI consent; UiKit kit; RectTransform root, card; TMP_Text text, counter; Button next, skip;
        Vector3 moveStart; CampusExplorer player; int swings, dodges; bool skillDone, itemDone; string rendered;
        TutorialProgress Progress => ProfileService.Instance.Data.tutorial ?? (ProfileService.Instance.Data.tutorial = new TutorialProgress());
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null;
#if UNITY_EDITOR
            Suppress = false;
#endif
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; Create(); }
        static void Loaded(Scene s, LoadSceneMode mode) { Create(); }
        static void Create() { if (FindAnyObjectByType<TutorialDirector>() == null) new GameObject("Tutorial director").AddComponent<TutorialDirector>(); }
        void Start()
        {
            Instance = this; consent=FindAnyObjectByType<TelemetryConsentUI>(); kit = UiKit.Create(); if (kit == null) return;
            var canvasGO = new GameObject("Tutorial cards", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false); var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 250;
            root = kit.Fit(canvasGO.transform, "Tutorial fit");
            card = kit.Panel(root, "Tutorial comic card", 590, 210, 740, 254); card.GetComponent<Image>().raycastTarget = false;
            text = kit.Text(card, "", 24, 16, 686, 142, 26); counter = kit.Text(card, "", 24, 182, 270, 44, 18, UiKit.Muted);
            next = kit.Button(card, "", 564, 164, 80, 80, Advance); skip = kit.Button(card, "", 650, 164, 80, 80, Skip);
            card.gameObject.SetActive(false); player = FindAnyObjectByType<CampusExplorer>(); if (player != null) moveStart = player.transform.position;
        }
        void Update()
        {
            if (card == null || UIStateManager.Instance == null || ProfileService.Instance == null) return;
#if UNITY_EDITOR
            if (Suppress) { card.gameObject.SetActive(false); return; }
#endif
            if(consent!=null&&consent.Visible){card.gameObject.SetActive(false);return;}
            string group = null; int step = -1; var p = Progress; var state = UIStateManager.Instance.State;
            if ((state == UIState.Hub || state == UIState.Course || state == UIState.Loadout) && !p.skipHub && p.hub < Hub.Length) { group = "hub"; step = p.hub; }
            else if (state == UIState.Gameplay && LevelDirector.Instance?.Level != null && !(SkyBeast.HeavenSwordCinematic.Active?.Playing ?? false))
            {
                int level = LevelDirector.Instance.Level.index;
                if (level == 1 && !p.skipCombat && p.combat < Combat.Length) { group = "combat"; step = p.combat; }
                else if (level >= 8 && !p.skipFire && p.fire < Fire.Length) { group = "fire"; step = p.fire; }
            }
            ActiveGroup = group; ActiveStep = step; card.gameObject.SetActive(group != null); if (group == null) return;
            bool mobile = CampusInput.Mobile, vn = LevelHUD.Vietnamese;
            var entries = group == "hub" ? Hub : group == "combat" ? Combat : Fire;
            string key = group + step + mobile + vn;
            if (rendered != key)
            {
                rendered = key; text.text = mobile ? (vn ? entries[step].mobileVN : entries[step].mobileEN) : (vn ? entries[step].vn : entries[step].en);
                counter.text = (step + 1) + "/" + entries.Length + (mobile ? "" : (vn ? "  H: tiếp · Backspace: bỏ qua" : "  H: next · Backspace: skip"));
                next.GetComponentInChildren<TMP_Text>().text = ">"; skip.GetComponentInChildren<TMP_Text>().text = "X";
                StyleCircle(next, mobile); StyleCircle(skip, mobile);
                card.anchoredPosition = new Vector2(group == "hub" ? 590 : 450, group == "hub" ? -744 : -210);
                if (player != null) { moveStart = player.transform.position; swings = player.GetComponent<PlayerCombat>()?.SwingCount ?? 0; dodges = player.GetComponent<DodgeAbility>()?.DodgeCount ?? 0; }
                skillDone = itemDone = false;
            }
            if (!mobile && Keyboard.current != null) { if (Keyboard.current.hKey.wasPressedThisFrame) Advance(); else if (Keyboard.current.backspaceKey.wasPressedThisFrame) Skip(); }
            if (group == "hub")
            {
                if (step == 0 && state == UIState.Course) Advance();
                else if (step == 1 && ProfileService.Instance.Data.learning.lessons.Exists(x => x.rewarded)) Advance();
                else if (step == 2 && ProfileService.Instance.Inventory.Count("hoi-khi-dan") > 0) Advance();
                else if (step == 3 && state == UIState.Loadout) Advance();
            }
            else if (group == "combat" && player != null)
            {
                if (step == 0 && Vector3.Distance(player.transform.position, moveStart) >= 2) Advance();
                else if (step == 1 && (player.GetComponent<PlayerCombat>()?.SwingCount ?? 0) > swings) Advance();
                else if (step == 2 && (player.GetComponent<DodgeAbility>()?.DodgeCount ?? 0) > dodges) Advance();
                else if (step == 3 && skillDone || step == 4 && itemDone) Advance();
            }
        }
        void StyleCircle(Button b, bool mobile)
        {
            var image = b.GetComponent<Image>();
            var circle=b.transform.Find("Tutorial circle");
            var disc=circle!=null?circle.GetComponent<RiftGraphic>():null;
            if(mobile&&disc==null)
            {
                disc=kit.Rect(b.transform,"Tutorial circle",0,0,80,80).gameObject.AddComponent<RiftGraphic>();disc.Form=RiftGraphic.Shape.Disc;disc.color=ComicTheme.Navy;disc.raycastTarget=true;disc.transform.SetAsFirstSibling();
                var ring=kit.Rect(b.transform,"Tutorial ink ring",0,0,80,80).gameObject.AddComponent<RiftGraphic>();ring.Form=RiftGraphic.Shape.Ring;ring.Thickness=5;ring.color=ComicTheme.Gold;ring.raycastTarget=false;
                b.gameObject.AddComponent<TutorialCircleHit>();
            }
            if(disc!=null)disc.enabled=mobile;
            var ink=b.transform.Find("Tutorial ink ring");if(ink!=null)ink.gameObject.SetActive(mobile);
            foreach(var img in b.GetComponentsInChildren<Image>())img.enabled=!mobile;
            var hit=b.GetComponent<TutorialCircleHit>();if(hit!=null)hit.enabled=mobile;
            b.targetGraphic=mobile?(UnityEngine.UI.Graphic)disc:image;
            foreach(var label in b.GetComponentsInChildren<TMP_Text>())label.color=ComicTheme.Paper;
        }
        public static void SkillUsed(string id) { if (Instance != null && id == "dai-thu-an") Instance.skillDone = true; }
        public static void ItemUsed() { if (Instance != null) Instance.itemDone = true; }
        public void Advance()
        {
            if (ActiveGroup == null) return; var p = Progress;
            if (ActiveGroup == "hub") p.hub++; else if (ActiveGroup == "combat") p.combat++; else p.fire++;
            ProfileService.Instance.MarkDirty(); rendered = null;
        }
        public void Skip()
        {
            if (ActiveGroup == null) return; var p = Progress;
            if (ActiveGroup == "hub") p.skipHub = true; else if (ActiveGroup == "combat") p.skipCombat = true; else p.skipFire = true;
            ProfileService.Instance.MarkDirty(); card.gameObject.SetActive(false);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
