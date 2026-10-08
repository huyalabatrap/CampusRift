using UnityEngine;
using UnityEngine.UI;

namespace CampusRift.Skills
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PhantomGlyph : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Silhouette(vh, -.17f, .78f);
            Silhouette(vh, .18f, 1f);
        }
        void Silhouette(VertexHelper vh, float x, float alpha)
        {
            var c = color; c.a *= alpha;
            var r = rectTransform.rect; float scale = Mathf.Min(r.width, r.height);
            Vector2 center = r.center + new Vector2(x * scale, .22f * scale);
            Disc(vh, center, .105f * scale, c);
            Quad(vh, r.center + new Vector2((x-.115f)*scale,-.23f*scale),
                r.center + new Vector2((x+.115f)*scale,.07f*scale), c);
        }
        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Color c)
        {
            int n=vh.currentVertCount;
            vh.AddVert(new Vector2(a.x,a.y),c,Vector2.zero);vh.AddVert(new Vector2(b.x,a.y),c,Vector2.zero);
            vh.AddVert(new Vector2(b.x,b.y),c,Vector2.zero);vh.AddVert(new Vector2(a.x,b.y),c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        static void Disc(VertexHelper vh, Vector2 center, float radius, Color c)
        {
            int n=vh.currentVertCount; vh.AddVert(center,c,Vector2.zero);
            for(int i=0;i<=12;i++)
            {
                float a=i*Mathf.PI*2/12;vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);
                if(i>0)vh.AddTriangle(n,n+i,n+i+1);
            }
        }
    }
}
