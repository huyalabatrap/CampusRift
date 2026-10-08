using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.UI
{
    // Native UGUI shapes: share the existing comic palette, no generated bitmap/icon dependencies.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ComboGraphic : Graphic
    {
        public enum Shape { Burst, Ring, Shield, Disc }
        public Shape shape;
        public Color tint = ComicTheme.Paper, ink = ComicTheme.Ink;
        public float progress = 1;
        static readonly Vector2[] ShieldPoints={new Vector2(-.9f,.9f),new Vector2(.9f,.9f),new Vector2(.8f,-.2f),new Vector2(0,-1),new Vector2(-.8f,-.2f)};
        public void Refresh() { SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; Vector2 center = r.center;
            if (shape == Shape.Shield)
            {
                Polygon(vh, center, r.size * .5f, ink, true);
                Polygon(vh, center, r.size * .37f, tint, true);
                Quad(vh, center + new Vector2(-2, 8), center + new Vector2(2, -4), ink);
                Quad(vh, center + new Vector2(-2, -4), center + new Vector2(7, -9), ink);
                return;
            }
            int count = shape == Shape.Burst ? 40 : 64;
            if (shape == Shape.Ring)
            {
                int steps = Mathf.CeilToInt(count * Mathf.Clamp01(progress));
                for (int i = 0; i < steps; i++)
                {
                    float a = (90 - i * 360f / count) * Mathf.Deg2Rad, b = (90 - Mathf.Min(i + 1f, count * progress) * 360f / count) * Mathf.Deg2Rad;
                    Vector2 outerA = center + Vector2.Scale(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), r.size * .5f);
                    Vector2 outerB = center + Vector2.Scale(new Vector2(Mathf.Cos(b), Mathf.Sin(b)), r.size * .5f);
                    Vector2 innerA = center + (outerA - center) * .79f, innerB = center + (outerB - center) * .79f;
                    int start = vh.currentVertCount; Vertex(vh, outerA, tint); Vertex(vh, outerB, tint); Vertex(vh, innerB, tint); Vertex(vh, innerA, tint);
                    vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
                }
                return;
            }
            Fan(vh, r, count, ink, 1);
            Fan(vh, r, count, tint, shape == Shape.Burst ? .90f : .77f);
        }
        static void Vertex(VertexHelper vh, Vector2 p, Color c) { var v = UIVertex.simpleVert; v.position = p; v.color = c; vh.AddVert(v); }
        void Fan(VertexHelper vh, Rect r, int count, Color c, float scale)
        {
            int start = vh.currentVertCount; Vertex(vh, r.center, c);
            for (int i = 0; i <= count; i++) { float a = i * Mathf.PI * 2 / count; float spike = shape == Shape.Burst && i % 2 == 1 ? .78f : 1; Vertex(vh, r.center + Vector2.Scale(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), r.size * (.5f * scale * spike)), c); }
            for (int i = 0; i < count; i++) vh.AddTriangle(start, start + i + 1, start + i + 2);
        }
        static void Polygon(VertexHelper vh, Vector2 center, Vector2 size, Color c, bool shield)
        {
            int start = vh.currentVertCount; Vertex(vh, center, c);
            foreach (var p in ShieldPoints) Vertex(vh, center + Vector2.Scale(p, size), c);
            for (int i = 0; i < 5; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 5);
        }
        static void Quad(VertexHelper vh, Vector2 from, Vector2 to, Color c)
        {
            int s = vh.currentVertCount; Vector2 side = new Vector2(-(to - from).y, (to - from).x).normalized * 2;
            Vertex(vh, from - side, c); Vertex(vh, from + side, c); Vertex(vh, to + side, c); Vertex(vh, to - side, c); vh.AddTriangle(s,s+1,s+2); vh.AddTriangle(s,s+2,s+3);
        }
    }
}
