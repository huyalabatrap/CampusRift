using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Localization;
using CampusRift.UI;

namespace CampusRift.Progression
{
    // One-time notice on the main menu after the old learning save was archived (P06-T02). It is a small card at the
    // bottom of the screen, so it never covers the menu buttons.
    public sealed class MigrationNotice : MonoBehaviour
    {
        GameObject root; TMP_Text text; Button button; TMP_Text buttonLabel;
        ProfileService profile;

        void Awake() { profile = GetComponent<ProfileService>(); }

        void Update()
        {
            if (profile == null || !profile.Data.migration.noticePending) { Destroy(this); return; }
            var ui = UIStateManager.Instance;
            bool inMenu = ui != null && ui.State == UIState.Menu;
            if (root == null && inMenu) Build();
            if (root != null)
            {
                root.SetActive(inMenu);
                bool vn = LevelHUD.Vietnamese;
                string message = vn ? "Đã chuyển sang nội dung mới. Tiến độ cũ được lưu trữ." : "Switched to the new content. Your old progress has been archived.";
                if (text.text != message)
                {
                    text.text = message; buttonLabel.text = vn ? "ĐÃ HIỂU" : "GOT IT";
                    var font = CampusRift.UI.ComicTheme.Font;
                    if (font != null) { text.font = font; buttonLabel.font = font; }
                }
            }
        }

        void Build()
        {
            root = new GameObject("Migration Notice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 900;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var card = Rect("Card", root.transform, new Vector2(0.5f, 0), new Vector2(920, 120), new Vector2(0, 70));
            var bg = CampusRift.UI.ComicTheme.Frame(card.gameObject);
            text = Label("Message", card, new Vector2(-110, 0), new Vector2(660, 100), 26, TextAlignmentOptions.Left);
            var buttonRect = Rect("Button", card, new Vector2(0.5f, 0.5f), new Vector2(190, 68), new Vector2(340, 0));
            var buttonBg = CampusRift.UI.ComicTheme.Frame(buttonRect.gameObject,"button-green",true);
            button = buttonRect.gameObject.AddComponent<Button>(); button.targetGraphic = buttonBg;
            buttonLabel = Label("Label", buttonRect, Vector2.zero, new Vector2(190, 64), 24, TextAlignmentOptions.Center);
            button.onClick.AddListener(Dismiss);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f); r.sizeDelta = size; r.anchoredPosition = position; return r;
        }
        static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions align)
        {
            var r = Rect(name, parent, new Vector2(0.5f, 0.5f), size, position);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.fontSize = fontSize; t.alignment = align; t.color = Color.white; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; CampusRift.UI.ComicTheme.Text(t,name=="Label"); return t;
        }

        void Dismiss()
        {
            if (profile != null) { profile.Data.migration.noticePending = false; profile.MarkDirty(); profile.Flush(); }
            if (root != null) Destroy(root);
            Destroy(this);
        }
    }
}
