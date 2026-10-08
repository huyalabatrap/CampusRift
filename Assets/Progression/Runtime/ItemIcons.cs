using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Progression
{
    // Simple generated icons for items (P08-T02): a pill, a talisman or a pearl, tinted by what the item does.
    // Drawn once into a small texture, so no art files are needed until the real icons arrive.
    public static class ItemIcons
    {
        const int Size = 96;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        // True when the drawn fallback is used (the art in Content/Images is missing); the fallback wants the item colour.
        public static bool IsGenerated(ItemDefinition item) => item != null && CampusRift.UI.ContentImages.Item(item.id) == null;

        public static Sprite Get(ItemDefinition item)
        {
            if (item == null) return null;
            var supplied = CampusRift.UI.ContentImages.Item(item.id);
            if (supplied != null) return supplied;
            string key = item.id + ":" + item.shape + ":" + ColorUtility.ToHtmlStringRGB(item.tint);
            if (cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            sprite = Build(item.shape, item.tint); cache[key] = sprite; return sprite;
        }

        static Sprite Build(ItemShape shape, Color tint)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + .5f) / Size * 2f - 1f, v = (y + .5f) / Size * 2f - 1f;
                    float alpha, shade;
                    switch (shape)
                    {
                        case ItemShape.Talisman: Talisman(u, v, out alpha, out shade); break;
                        case ItemShape.Pearl: Pearl(u, v, out alpha, out shade); break;
                        default: Pill(u, v, out alpha, out shade); break;
                    }
                    var c = Color.Lerp(tint * .45f, Color.Lerp(tint, Color.white, .55f), shade); c.a = alpha;
                    pixels[y * Size + x] = c;
                }
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "ItemIcon" };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), 100f);
        }

        static float Soft(float distance) => Mathf.Clamp01(-distance * 20f);   // 1 inside, 0 outside, ~5% wide edge

        // A capsule rotated 35 degrees, lit from the top left.
        static void Pill(float u, float v, out float alpha, out float shade)
        {
            float a = 35f * Mathf.Deg2Rad, ru = u * Mathf.Cos(a) + v * Mathf.Sin(a), rv = -u * Mathf.Sin(a) + v * Mathf.Cos(a);
            float half = .42f, radius = .3f;
            float dx = Mathf.Max(Mathf.Abs(ru) - half, 0), d = Mathf.Sqrt(dx * dx + rv * rv) - radius;
            alpha = Soft(d);
            shade = Mathf.Clamp01(.5f + (-rv / radius) * .35f + (rv > .05f && Mathf.Abs(ru) < half ? 0 : 0));
            if (Mathf.Abs(ru) < .04f) shade *= .7f;   // the seam
        }

        // A tall rounded paper strip with a bar and a dot in the middle.
        static void Talisman(float u, float v, out float alpha, out float shade)
        {
            float hx = .42f, hy = .78f, r = .12f;
            float qx = Mathf.Abs(u) - hx + r, qy = Mathf.Abs(v) - hy + r;
            float d = Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
            alpha = Soft(d); shade = .8f;
            bool bar = Mathf.Abs(u) < .06f && v > -.5f && v < .45f;
            bool cross = Mathf.Abs(v - .1f) < .05f && Mathf.Abs(u) < .24f;
            bool dot = (u * u + (v + .62f) * (v + .62f)) < .012f;
            if (bar || cross || dot) shade = .15f;
            if (Mathf.Abs(u) > hx - .06f || Mathf.Abs(v) > hy - .06f) shade = Mathf.Min(shade, .55f);
        }

        // A round bead with a highlight.
        static void Pearl(float u, float v, out float alpha, out float shade)
        {
            float d = Mathf.Sqrt(u * u + v * v) - .72f;
            alpha = Soft(d);
            float hx = u + .25f, hy = v - .28f;
            shade = Mathf.Clamp01(.35f + (1f - Mathf.Sqrt(hx * hx + hy * hy)) * .6f);
            if (hx * hx + hy * hy < .02f) shade = 1f;
        }
    }
}
