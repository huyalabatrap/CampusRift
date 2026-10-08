using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Learning;

namespace CampusRift.UI
{
    // Small helpers to build the Hub, level map, loadout and skill book in code (P09), styled like the rest of the UI:
    // the font and colours come from the theme, buttons are clones of the Library's RiftButton. Positions are in the
    // 1920×1080 reference canvas, measured from the top-left corner of the parent.
    public sealed class UiKit
    {
        public readonly CampusRiftUITheme Theme;
        public readonly TMP_FontAsset Font;
        public readonly Material FontMaterial;
        public static readonly Color Gold = ComicTheme.Gold, Muted = ComicTheme.Muted, Danger = ComicTheme.Red;

        UiKit(LearningUI source)
        {
            Theme = source.buttonTemplate.Theme;
            ComicTheme.Palette(Theme);
            Font = ComicTheme.Font != null ? ComicTheme.Font : source.textTemplate.font; FontMaterial = Font.material;
        }
        // Templates live in the Library panel of the menu scene; null when that panel is missing.
        public static UiKit Create()
        {
            var source = UnityEngine.Object.FindAnyObjectByType<LearningUI>(FindObjectsInactive.Include);
            return source != null && source.buttonTemplate != null && source.textTemplate != null ? new UiKit(source) : null;
        }
        public Color Text1 => Theme != null ? Theme.TextPrimary : Color.white;

        public RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        // A 1920×1080 design frame that is scaled to fit the parent (letterboxed) and kept inside the device safe area,
        // so the same layout works at 16:9, 19.5:9 and 4:3 (P09-T08).
        public RectTransform Fit(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.pivot = new Vector2(.5f, .5f); r.sizeDelta = new Vector2(1920, 1080);
            go.AddComponent<FitFrame>(); return r;
        }
        // ---- ornamental pieces (P09 restyle): framed panels, gold titles, gems, progress bars
        public static readonly Color GoldHi = ComicTheme.Gold, GoldLo = ComicTheme.Orange, Border = Color.black;

        // A dark panel with a thin border and small gold diamonds on the corners.
        public RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, bool selected = false, float fillAlpha = .88f)
        {
            var r = Rect(parent, name, x, y, w, h);
            ComicTheme.Frame(r.gameObject, name == "Currency" ? "currency" : selected ? "selected" : "panel");
            return r;
        }
        // Makes a Panel clickable: hovering brightens its border.
        public UnityEngine.UI.Button Clickable(RectTransform panel, Action click, bool enabled = true)
        {
            if (panel.rect.height < ComicTheme.MinimumButtonHeight) panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ComicTheme.MinimumButtonHeight);
            var edge = panel.GetComponent<Image>(); edge.raycastTarget = true;   // the fill is the click target
            var b = panel.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = edge; b.interactable = enabled;
            var colors = b.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f); colors.pressedColor = new Color(.7f, .7f, .8f, 1f); colors.disabledColor = new Color(.6f, .6f, .65f, 1f); b.colors = colors;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() => { UIAudioManager.Instance?.Click(); click?.Invoke(); });
            return b;
        }
        public Image Diamond(Transform parent, float x, float y, float size, Color color)
        {
            var img = Image(parent, "Diamond", x, y, size, size, color); img.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); return img;
        }
        // A four-pointed gem (Linh Thạch mark).
        public RiftGraphic Gem(Transform parent, float x, float y, float size, Color color)
        {
            var r = Rect(parent, "Gem", x, y, size, size); var g = r.gameObject.AddComponent<RiftGraphic>(); g.Form = RiftGraphic.Shape.Diamond; g.color = ComicTheme.Gold; g.raycastTarget = false;
            Image(r, "Crystal", 0, 0, size, size, Color.white, ComicTheme.Sprite("crystal")); return g;
        }
        public TMP_Text Title(Transform parent, string text, float x, float y, float w, float size = 56)
        {
            if (size >= 48) { var burst = Image(parent, "Comic Title Burst", x - 28, y - 12, Mathf.Min(w, text.Length * size * .70f + 90), size * 1.8f, Color.white, ComicTheme.Sprite("burst")); burst.preserveAspect = false; }
            var t = Text(parent, text, x + 6, y, w - 6, size * 1.3f, size, ComicTheme.Paper, TextAlignmentOptions.TopLeft, false);
            ComicTheme.Text(t, true); return t;
        }
        public void Rule(Transform parent, float x, float y, float w)
        {
            Image(parent, "Rule", x, y, w, 2, new Color(GoldLo.r, GoldLo.g, GoldLo.b, .55f)); Diamond(parent, x + w * .5f - 5, y - 4, 10, GoldHi);
        }
        public void Bar(Transform parent, float x, float y, float w, float h, float fraction, Color fill)
        {
            var back=Image(parent,"Bar Back",x,y,w,h,ComicTheme.Ink,ComicTheme.Sprite("round-mask"));back.type=UnityEngine.UI.Image.Type.Sliced;
            var bar=Image(parent,"Bar Fill",x+3,y+3,(w-6)*Mathf.Clamp01(fraction),h-6,fill,ComicTheme.Sprite("round-mask"));bar.type=UnityEngine.UI.Image.Type.Sliced;
        }

        public RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        public Image Image(Transform parent, string name, float x, float y, float w, float h, Color color, Sprite sprite = null, bool raycast = false)
        {
            var r = Rect(parent, name, x, y, w, h); var img = r.gameObject.AddComponent<Image>();
            img.color = color; img.sprite = sprite; img.preserveAspect = sprite != null; img.raycastTarget = raycast;
            if(sprite!=null&&w>=150&&h>=100&&(name=="Picture"||name.Contains("Thumbnail")))ComicTheme.ClipThumbnail(img);
            return img;
        }
        public TMP_Text Text(Transform parent, string text, float x, float y, float w, float h, float size = 24, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, bool wrap = true)
        {
            var r = Rect(parent, "Text", x, y, w, Mathf.Max(h, size * 1.4f+10)); var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font; t.fontSharedMaterial = FontMaterial; t.text = text; t.fontSize = size; t.alignment = align; t.color = color ?? Text1;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Truncate; t.raycastTarget = false;
            // Text shrinks to fit its box instead of being cut off (long Vietnamese names, small screens).
            t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = Mathf.Max(10f, size * .6f);
            t.margin = new Vector4(5,4,9,4);
            bool caps = !string.IsNullOrEmpty(text) && text.Length < 72 && text == text.ToUpperInvariant();
            if (size >= 32 || (size >= 24 && caps)) { ComicTheme.Text(t, true); t.color = color ?? Text1; }
            var parentImage = parent.GetComponent<Image>();
            if (parentImage != null && parentImage.sprite == ComicTheme.Sprite("selected")) { t.color = ComicTheme.Navy; t.fontSharedMaterial = Font.material; }
            return t;
        }
        // A framed button with centred text; `gold` makes it the primary action of the screen.
        public UnityEngine.UI.Button Button(Transform parent, string label, float x, float y, float w, float h, Action click, bool enabled = true, bool gold = false, float fontSize = 0)
        {
            h = Mathf.Max(h, 68f);   // 44 px stays reachable when the canvas is scaled down to 720p (P09-T08)
            var p = Panel(parent, "Button " + label, x, y, w, h, gold);
            bool primary = gold || ComicTheme.IsPrimary(label);
            ComicTheme.Frame(p.gameObject, parent.name == "Tabs" && gold ? "tab-selected" : ComicTheme.IsBack(label) ? "button-red" : primary ? "button-green" : "button-blue");
            Color color = primary ? ComicTheme.Ink : Text1;
            var t = Text(p, label, 18, 0, w - 36, h, fontSize > 0 ? fontSize : 26, color, TextAlignmentOptions.Center, false); t.fontStyle = FontStyles.Bold | FontStyles.Italic | FontStyles.UpperCase; t.fontSharedMaterial = Font.material;
            return Clickable(p, click, enabled);
        }
        // A clickable area (cards): a plain button on an image.
        // A clickable framed card. `color` only says whether it is the selected one (a bluish/bright colour) or a normal one.
        public UnityEngine.UI.Button Card(Transform parent, string name, float x, float y, float w, float h, Color color, Action click, bool enabled = true)
        {
            var panel = Panel(parent, name, x, y, w, h, color.b >= .28f);
            return Clickable(panel, click, enabled);
        }
        public void Clear(Transform parent) { for (int i = parent.childCount - 1; i >= 0; i--) { var c = parent.GetChild(i).gameObject; c.SetActive(false); UnityEngine.Object.Destroy(c); } }

        // A vertical scroll area; returns the content rect (its height grows with `height`, set by the caller).
        public RectTransform Scroll(Transform parent, string name, float x, float y, float w, float h, float contentHeight)
        {
            var area = Rect(parent, name, x, y, w, h); var mask = area.gameObject.AddComponent<RectMask2D>();
            var scroll = area.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            var content = Rect(area, "Content", 0, 0, w, contentHeight); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.sizeDelta = new Vector2(0, contentHeight); scroll.content = content; scroll.viewport = area;
            var back = area.gameObject.AddComponent<Image>(); back.color = new Color(0, 0, 0, .001f);
            return content;
        }
    }

}
