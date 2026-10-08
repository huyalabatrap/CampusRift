using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.UI
{
    public static class ComboUIFactory
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = (RectTransform)go.transform; r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f,.5f); r.sizeDelta = size; r.anchoredPosition = position; return r;
        }
        public static TextMeshProUGUI Text(string name, Transform parent, Vector2 size, Vector2 position, float fontSize)
        {
            var r = Rect(name, parent, size, position); var text = r.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = ComicTheme.Font; text.fontSharedMaterial = ComicTheme.HeadingMaterial; text.fontStyle = FontStyles.Bold | FontStyles.Italic;
            text.fontSize = fontSize; text.enableAutoSizing = true; text.fontSizeMax = fontSize; text.fontSizeMin = fontSize * .75f;
            text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false; text.color = ComicTheme.Paper; return text;
        }
        public static ComboGraphic Graphic(string name, Transform parent, Vector2 size, Vector2 position, ComboGraphic.Shape shape, Color tint)
        {
            var r=Rect(name,parent,size,position); var graphic=r.gameObject.AddComponent<ComboGraphic>(); graphic.shape=shape; graphic.tint=tint; graphic.raycastTarget=false; return graphic;
        }
        public static Canvas Canvas(string name, Transform parent, int order)
        {
            var r=Rect(name,parent,new Vector2(1920,1080),Vector2.zero); var canvas=r.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=order;
            var scaler=r.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f; return canvas;
        }
    }
}
