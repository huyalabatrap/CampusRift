using UnityEngine;

namespace CampusRift.AR
{
    // Keep the Canvas full screen; fit authored controls inside the safe area.
    public sealed class ARScreenLayout : MonoBehaviour
    {
        RectTransform canvas, content;
        public void Initialize(RectTransform target)
        {
            canvas = (RectTransform)transform;
            content = target;
            Apply();
        }
        void LateUpdate() { Apply(); }
        void Apply()
        {
            if (content == null || Screen.width <= 0 || Screen.height <= 0) return;
            var safe = Screen.safeArea;
            var bounds = canvas.rect;
            float x = bounds.width / Screen.width, y = bounds.height / Screen.height;
            content.anchoredPosition = new Vector2((safe.center.x - Screen.width * .5f) * x,
                (safe.center.y - Screen.height * .5f) * y);
            float scale = Mathf.Min(safe.width * x / 1920f, safe.height * y / 1080f);
            content.localScale = Vector3.one * scale;
        }
    }
}
