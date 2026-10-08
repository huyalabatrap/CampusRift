using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusRift.UI
{
    // One palette and asset registry for generated UI, authored scenes and prefabs.
    public static class ComicTheme
    {
        public static readonly Color Navy = new Color32(13, 27, 42, 255);
        public static readonly Color Ink = new Color32(2, 5, 10, 255);
        public static readonly Color PanelColor = new Color32(27, 45, 69, 255);
        public static readonly Color Gold = new Color32(245, 179, 1, 255);
        public static readonly Color Orange = new Color32(255, 138, 0, 255);
        public static readonly Color Green = new Color32(123, 224, 58, 255);
        public static readonly Color Purple = new Color32(161, 104, 217, 255);
        public static readonly Color Red = new Color32(199, 67, 50, 255);
        public static readonly Color Paper = new Color32(255, 243, 212, 255);
        public static readonly Color Muted = new Color32(177, 201, 213, 255);
        public const float MinimumButtonHeight = 68;
        public const float BorderPixels = 6;
        public const float CornerRadius = 14;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static TMP_FontAsset font;
        static Material headingMaterial;
        public static TMP_FontAsset Font { get { return font != null ? font : font = Resources.Load<TMP_FontAsset>("Comic/ComicVietnamese"); } }
        public static Material HeadingMaterial { get { return headingMaterial != null ? headingMaterial : headingMaterial = Resources.Load<Material>("Comic/ComicHeading"); } }
        public static Sprite Sprite(string id)
        {
            if (id == "panel" || id == "paper" || id == "selected" || id == "currency") id = "round-" + id;
            else if (id == "tab-selected") id = "selected";
            Sprite value;
            if (!sprites.TryGetValue(id, out value)) { value = Resources.Load<Sprite>("Comic/" + id); sprites[id] = value; }
            return value;
        }
        // A real rounded alpha stencil: RectMask2D alone cannot cut rounded corners.
        public static void ClipThumbnail(Image image)
        {
            if (image == null || image.transform.parent.name.EndsWith("Rounded Clip")) return;
            var old = image.rectTransform;
            var frame = new GameObject(image.name + " Frame", typeof(RectTransform));
            frame.transform.SetParent(old.parent, false); frame.transform.SetSiblingIndex(old.GetSiblingIndex());
            var r = (RectTransform)frame.transform; r.anchorMin=old.anchorMin;r.anchorMax=old.anchorMax;r.pivot=old.pivot;
            r.anchoredPosition=old.anchoredPosition;r.sizeDelta=old.sizeDelta;Frame(frame);
            var clip = new GameObject(image.name + " Rounded Clip",typeof(RectTransform),typeof(Image),typeof(Mask));
            clip.transform.SetParent(frame.transform,false);
            var cr=(RectTransform)clip.transform;cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=new Vector2(8,14);cr.offsetMax=new Vector2(-14,-8);
            var stencil=clip.GetComponent<Image>();stencil.sprite=Sprite("round-mask");stencil.type=Image.Type.Sliced;stencil.raycastTarget=false;
            clip.GetComponent<Mask>().showMaskGraphic=false;
            old.SetParent(cr,false);old.anchorMin=Vector2.zero;old.anchorMax=Vector2.one;old.offsetMin=old.offsetMax=Vector2.zero;
        }
        public static void ReadabilityPlate(TMP_Text text)
        {
            if(text==null || text.GetComponent<ComicTextPlate>()!=null)return;
            var plate=new GameObject(text.name+" Dark Plate",typeof(RectTransform),typeof(Image));plate.transform.SetParent(text.transform.parent,false);
            plate.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
            var image=plate.GetComponent<Image>();image.sprite=Sprite("round-mask");image.type=Image.Type.Sliced;image.color=new Color(.035f,.065f,.10f,.91f);image.raycastTarget=false;
            var sync=text.gameObject.AddComponent<ComicTextPlate>();sync.Plate=image;sync.Refresh();
        }
        public static void ClipBar(Image fill)
        {
            if(fill==null||fill.transform.parent.name.EndsWith("Bar Clip"))return;
            var old=fill.rectTransform;
            var go=new GameObject(fill.name+" Bar Clip",typeof(RectTransform),typeof(Image),typeof(Mask));
            go.transform.SetParent(old.parent,false);go.transform.SetSiblingIndex(old.GetSiblingIndex());
            var r=(RectTransform)go.transform;r.anchorMin=old.anchorMin;r.anchorMax=old.anchorMax;r.pivot=old.pivot;r.anchoredPosition=old.anchoredPosition;r.sizeDelta=old.sizeDelta;
            var stencil=go.GetComponent<Image>();stencil.sprite=Sprite("round-mask");stencil.type=Image.Type.Sliced;stencil.raycastTarget=false;
            go.GetComponent<Mask>().showMaskGraphic=false;
            old.SetParent(r,false);old.anchorMin=Vector2.zero;old.anchorMax=Vector2.one;old.offsetMin=old.offsetMax=Vector2.zero;
        }
        public static void Palette(CampusRiftUITheme theme)
        {
            if (theme == null) return;
            theme.Font = Font; theme.PrimaryBackground = Navy; theme.SecondaryBackground = PanelColor;
            theme.PanelBackground = PanelColor; theme.PrimaryAccent = Gold; theme.SecondaryAccent = Orange;
            theme.SuccessColor = Green; theme.DangerColor = Red; theme.BlueAccent = Muted;
            theme.TextPrimary = Paper; theme.TextSecondary = Muted;
        }
        public static Image Frame(GameObject go, string style = "panel", bool raycast = false)
        {
            var image = go.GetComponent<Image>();
            if (image == null && go.GetComponent<Graphic>() != null)
            {
                go.GetComponent<Graphic>().color = Color.clear;
                var child = go.transform.Find("Comic Frame");
                if (child == null)
                {
                    var frame = new GameObject("Comic Frame", typeof(RectTransform), typeof(Image)); frame.transform.SetParent(go.transform, false); frame.transform.SetAsFirstSibling();
                    var r = (RectTransform)frame.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                    child = frame.transform;
                }
                image = child.GetComponent<Image>();
            }
            if (image == null) image = go.AddComponent<Image>();
            image.sprite = Sprite(style); image.type = Image.Type.Sliced; image.preserveAspect = false;
            image.color = Color.white; image.raycastTarget = raycast;
            if (style == "panel" && image.transform.Find("Comic Texture") == null)
            {
                var decoration = new GameObject("Comic Texture",typeof(RectTransform),typeof(Image)); decoration.transform.SetParent(image.transform,false);
                var r = (RectTransform)decoration.transform; r.anchorMin = r.anchorMax = r.pivot = Vector2.one; r.anchoredPosition = new Vector2(-14,-14);
                r.sizeDelta = new Vector2(Mathf.Min(220, image.rectTransform.rect.width*.35f),Mathf.Min(110,image.rectTransform.rect.height*.32f));
                var texture=decoration.GetComponent<Image>();texture.sprite=Sprite("panel-texture");texture.raycastTarget=false;
                decoration.transform.SetAsFirstSibling();
            }
            return image;
        }
        public static void Text(TMP_Text text, bool heading = false)
        {
            if (text == null || Font == null) return;
            text.font = Font; text.fontSharedMaterial = heading && HeadingMaterial != null ? HeadingMaterial : Font.material;
            text.enableVertexGradient = false; text.characterSpacing = 0;
            text.overflowMode = TextOverflowModes.Truncate;
            text.margin = new Vector4(5,4,7,4);
            text.color = Paper;
            if (heading) text.fontStyle = FontStyles.Bold | FontStyles.Italic | FontStyles.UpperCase;
            text.extraPadding = true;
        }
        public static bool IsBack(string value)
        {
            return value.Contains("BACK") || value.Contains("QUIT") || value.Contains("CANCEL") || value.Contains("QUAY LẠI") || value.Contains("HỦY") || value == "MAIN MENU" || value == "MENU CHÍNH";
        }
        public static bool IsPrimary(string value)
        {
            return value == "PLAY" || value == "APPLY" || value == "EXCHANGE" || value == "UPGRADE" || value.Contains("START") || value.Contains("RESUME") || value.Contains("CONFIRM") || value.Contains("SUBMIT") || value.Contains("TRAO ĐỔI") || value.Contains("BẮT ĐẦU") || value.Contains("XÁC NHẬN") || value.Contains("NỘP BÀI") || value.Contains("TIẾP TỤC") || value.Contains("ÁP DỤNG");
        }
        public static void Button(RiftButton button)
        {
            if (button == null) return;
            string label = button.Label != null ? button.Label.text.ToUpperInvariant() : button.name.ToUpperInvariant();
            bool primary = button.Gold || IsPrimary(label) || IsPrimary(button.name.ToUpperInvariant());
            bool back = button.Danger || IsBack(label) || IsBack(button.name.ToUpperInvariant());
            button.targetGraphic = Frame(button.gameObject, back ? "button-red" : primary ? "button-green" : "button-blue", true);
            button.Edge = null; button.Glow = null; button.Gems = null;
            foreach (Transform child in button.transform)
                if (child.name != "Label" && child.name != "Comic Frame" && child.GetComponent<TMP_Text>() == null) child.gameObject.SetActive(false);
            Text(button.Label, true);
            if (button.Label != null)
            {
                button.Label.color = primary ? Ink : Paper;
                button.Label.fontSharedMaterial = Font.material;
                button.Label.enableAutoSizing = true; button.Label.fontSizeMax = Mathf.Max(24, button.Label.fontSize); button.Label.fontSizeMin = 15;
                var labelRect=button.Label.rectTransform;labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;labelRect.offsetMin=new Vector2(24,8);labelRect.offsetMax=new Vector2(-52,-8);
            }
            foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))
                if(text!=button.Label&&text.name=="Arrow")
                {
                    var arrow=text.rectTransform;arrow.anchorMin=arrow.anchorMax=new Vector2(1,.5f);arrow.pivot=new Vector2(.5f,.5f);arrow.anchoredPosition=new Vector2(-27,0);arrow.sizeDelta=new Vector2(30,38);
                    Text(text);text.fontSize=22;text.fontSizeMax=22;text.fontSizeMin=18;text.enableAutoSizing=true;text.alignment=TextAlignmentOptions.Center;
                }
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null) layout.preferredHeight = Mathf.Max(MinimumButtonHeight, layout.preferredHeight);
            var r = button.transform as RectTransform;
            if (r != null && r.anchorMin.y == r.anchorMax.y) r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(MinimumButtonHeight, r.rect.height));
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.pressedColor = new Color(.78f,.78f,.78f); colors.disabledColor = new Color(.48f,.48f,.48f); button.colors = colors;
        }
        public static void Backdrop(Transform parent, bool first = true)
        {
            var existing = parent.Find("Comic Backdrop"); if (existing != null) return;
            var go = new GameObject("Comic Backdrop", typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>(); image.sprite = Sprite("academy-background"); image.color = Color.white; image.raycastTarget = false;
            if (first) go.transform.SetAsFirstSibling();
        }
        public static void LegacyPanel(Rect rect)
        {
            var old = GUI.color;
            GUI.color = Color.black; GUI.DrawTexture(new Rect(rect.x+6,rect.y+6,rect.width,rect.height),Texture2D.whiteTexture);
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color = Muted; GUI.DrawTexture(new Rect(rect.x+4,rect.y+4,rect.width-8,rect.height-8),Texture2D.whiteTexture);
            GUI.color = Navy; GUI.DrawTexture(new Rect(rect.x+6,rect.y+6,rect.width-12,rect.height-12),Texture2D.whiteTexture);
            GUI.color = old;
        }
        public static GUIStyle LegacyButton(string style = "button-blue")
        {
            var value = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.BoldAndItalic };
            if (Font != null) value.font = Font.sourceFontFile;
            var sprite = Sprite(style); if (sprite != null) value.normal.background = sprite.texture;
            value.normal.textColor = Paper; value.border = new RectOffset(28,28,28,28);
            value.hover.background = Sprite("selected").texture; value.hover.textColor = Navy;
            value.active.background = Sprite("button-green").texture; value.active.textColor = Navy;
            return value;
        }
    }
}
