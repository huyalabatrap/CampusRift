using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.UI
{
    // Light menu motion on unscaled time: floating medallions, pulsing halos and drifting motes.
    public sealed class MenuAmbience : MonoBehaviour
    {
        public RectTransform[] Floaters;
        public float FloatAmplitude = 9, FloatSpeed = .55f;
        public Graphic[] Halos;
        public RectTransform MoteArea;
        public Sprite MoteSprite;
        public int MoteCount = 26;
        public Color MoteColor = new Color(.8f,.7f,1,.55f);
        Vector2[] origins;
        float[] haloAlpha;
        RectTransform[] motes;
        Image[] moteImages;
        Vector4[] moteData;
        void Awake()
        {
            origins = new Vector2[Floaters.Length];
            for (int i = 0; i < Floaters.Length; i++) if (Floaters[i] != null) origins[i] = Floaters[i].anchoredPosition;
            haloAlpha = new float[Halos.Length];
            for (int i = 0; i < Halos.Length; i++) if (Halos[i] != null) haloAlpha[i] = Halos[i].color.a;
            if (MoteArea == null || MoteSprite == null) return;
            motes = new RectTransform[MoteCount]; moteImages = new Image[MoteCount]; moteData = new Vector4[MoteCount];
            var random = new System.Random(7);
            for (int i = 0; i < MoteCount; i++)
            {
                var go = new GameObject("Mote", typeof(RectTransform), typeof(Image)); go.layer = gameObject.layer;
                var r = (RectTransform)go.transform; r.SetParent(MoteArea, false);
                r.anchorMin = r.anchorMax = Vector2.zero;
                float size = 6 + (float)random.NextDouble() * 16;
                r.sizeDelta = new Vector2(size, size);
                var image = go.GetComponent<Image>(); image.sprite = MoteSprite; image.raycastTarget = false; image.color = MoteColor;
                // x (0-1), rise speed, sway phase, start offset (0-1)
                moteData[i] = new Vector4((float)random.NextDouble(), 14 + (float)random.NextDouble() * 30, (float)random.NextDouble() * 6.3f, (float)random.NextDouble());
                motes[i] = r; moteImages[i] = image;
            }
        }
        void Update()
        {
            float t = Time.unscaledTime;
            for (int i = 0; i < Floaters.Length; i++)
                if (Floaters[i] != null) Floaters[i].anchoredPosition = origins[i] + new Vector2(0, Mathf.Sin(t * FloatSpeed + i * 1.9f) * FloatAmplitude);
            for (int i = 0; i < Halos.Length; i++)
                if (Halos[i] != null) { var c = Halos[i].color; c.a = haloAlpha[i] * (.78f + .22f * Mathf.Sin(t * 1.1f + i)); Halos[i].color = c; }
            if (motes == null) return;
            var area = MoteArea.rect;
            for (int i = 0; i < motes.Length; i++)
            {
                var d = moteData[i];
                float travel = area.height + 60;
                float y = Mathf.Repeat(d.w * travel + t * d.y, travel) - 30;
                float x = d.x * area.width + Mathf.Sin(t * .4f + d.z) * 26;
                motes[i].anchoredPosition = new Vector2(x, y);
                float fade = Mathf.Clamp01(y / 160) * Mathf.Clamp01((area.height - y) / 220);
                var c = MoteColor; c.a *= fade * (.55f + .45f * Mathf.Sin(t * 2.3f + d.z * 3)); moteImages[i].color = c;
            }
        }
    }
}
