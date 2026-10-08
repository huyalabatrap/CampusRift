using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Progression;

namespace CampusRift.UI
{
    // An independent, non-interactive ComicTheme label survives UI panel rebuilds and AR scenes.
    public sealed class DevModeBadge : MonoBehaviour
    {
        public static DevModeBadge Instance { get; private set; }
        public bool Visible => label != null && label.gameObject.activeSelf;
        TMP_Text label;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("Dev Mode badge", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(go); go.AddComponent<DevModeBadge>();
        }
        void Awake()
        {
            Instance = this;
            var canvas = GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 900;
            var scaler = GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            var badge = new GameObject("DEV MODE", typeof(RectTransform), typeof(Image)); badge.transform.SetParent(transform, false);
            var rect = (RectTransform)badge.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1); rect.anchoredPosition = new Vector2(-12, -12); rect.sizeDelta = new Vector2(206, 42);
            var bg = badge.GetComponent<Image>(); bg.color = ComicTheme.Gold; bg.raycastTarget = false;
            var text = new GameObject("Dev label", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(badge.transform, false);
            label = text.GetComponent<TMP_Text>(); label.text = "DEV MODE"; label.fontSize = 26; label.alignment = TextAlignmentOptions.Center; ComicTheme.Text(label); label.color = ComicTheme.Ink; label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            DevMode.Changed += Refresh; Refresh();
        }
        void Refresh() { label.gameObject.SetActive(DevMode.Active); label.transform.parent.gameObject.SetActive(DevMode.Active); }
        void OnDestroy() { DevMode.Changed -= Refresh; if (Instance == this) Instance = null; }
    }
}
