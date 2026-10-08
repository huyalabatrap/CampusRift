using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.UI
{
    // Original, texture-free geometry. No postprocessing, particles, or per-frame mesh rebuilds.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RiftGraphic : MaskableGraphic
    {
        public enum Shape { Diamond, Ring, Slash, Panel, Lock, Disc }
        public Shape Form;
        public float Thickness = 2;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=rectTransform.rect;
            if (Form == Shape.Disc)
            {
                const int n=80;
                for(int i=0;i<n;i++)
                {
                    float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;int start=vh.currentVertCount;
                    Add(vh,r.center);Add(vh,r.center+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),r.size*.5f));
                    Add(vh,r.center+Vector2.Scale(new Vector2(Mathf.Cos(b),Mathf.Sin(b)),r.size*.5f));vh.AddTriangle(start,start+1,start+2);
                }
            }
            else if (Form == Shape.Ring)
            {
                const int n=80;
                for(int i=0;i<n;i++)
                {
                    float a=i*Mathf.PI*2/n, b=(i+1)*Mathf.PI*2/n;
                    Vector2 outer=new Vector2(r.width/2,r.height/2), inner=outer-Vector2.one*Thickness;
                    Quad(vh,r.center+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),outer),r.center+Vector2.Scale(new Vector2(Mathf.Cos(b),Mathf.Sin(b)),outer),r.center+Vector2.Scale(new Vector2(Mathf.Cos(b),Mathf.Sin(b)),inner),r.center+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),inner));
                }
            }
            else if (Form == Shape.Diamond) Quad(vh,new Vector2(r.center.x,r.yMax),new Vector2(r.xMax,r.center.y),new Vector2(r.center.x,r.yMin),new Vector2(r.xMin,r.center.y));
            else if (Form == Shape.Slash) Quad(vh,new Vector2(r.xMin+r.width*.7f,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMin+r.width*.3f,r.yMin),new Vector2(r.xMin,r.yMin));
            else if (Form == Shape.Lock)
            {
                Quad(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.center.y),new Vector2(r.xMin,r.center.y));
                for(int i=0;i<20;i++)
                {
                    float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20; Vector2 c=r.center;
                    Quad(vh,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r.width*.38f,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r.width*.38f,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r.width*.24f,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r.width*.24f);
                }
            }
            else
            {
                float cut=Mathf.Min(16,r.height*.22f);
                Vector2[] p={new Vector2(r.xMin+cut,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin+cut),new Vector2(r.xMax-cut,r.yMin),new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax-cut)};
                for(int i=0;i<p.Length;i++) { Add(vh,r.center); Add(vh,p[i]); Add(vh,p[(i+1)%p.Length]); vh.AddTriangle(i*3,i*3+1,i*3+2); }
            }
        }
        void Add(VertexHelper vh,Vector2 p) { var v=UIVertex.simpleVert; v.position=p;v.color=color;vh.AddVert(v); }
        void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        { int i=vh.currentVertCount;Add(vh,a);Add(vh,b);Add(vh,c);Add(vh,d);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3); }
    }
}
