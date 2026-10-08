using UnityEngine;

namespace CampusRift.UI
{
    // Scales its 1920×1080 rect to the largest size that fits the safe area of the parent canvas.
    public sealed class FitFrame : MonoBehaviour
    {
        Vector2 lastParent; Rect lastSafe; Vector2Int lastScreen;
        void OnEnable() { Apply(); }
        void LateUpdate() { Apply(); }
        public void Apply()
        {
            var self = (RectTransform)transform; var parent = self.parent as RectTransform; if (parent == null) return;
            var size = parent.rect.size; var safe = Screen.safeArea; var screen = new Vector2Int(Screen.width, Screen.height);
            if (size == lastParent && safe == lastSafe && screen == lastScreen) return;
            lastParent = size; lastSafe = safe; lastScreen = screen;
            if (size.x <= 0 || size.y <= 0 || screen.x <= 0 || screen.y <= 0) return;
            // Safe area as fractions of the screen, applied to the canvas rect.
            float left = safe.xMin / screen.x, right = 1f - safe.xMax / screen.x, bottom = safe.yMin / screen.y, top = 1f - safe.yMax / screen.y;
            float availW = size.x * (1f - left - right), availH = size.y * (1f - bottom - top);
            float scale = Mathf.Min(availW / 1920f, availH / 1080f);
            self.localScale = new Vector3(scale, scale, 1f);
            self.anchoredPosition = new Vector2(size.x * (left - right) * .5f, size.y * (bottom - top) * .5f);
        }
    }
}
