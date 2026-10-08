using UnityEngine;
using UnityEngine.UI;

namespace CampusRift.Skills
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GiantHandGlyph : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI*2/40,b=(i+0.78f)*Mathf.PI*2/40;
                Quad(vh,P(a,.47f),P(b,.47f),P(b,.43f),P(a,.43f),new Color(.65f,.3f,1,color.a));
            }
            Box(vh,.3f,.2f,.68f,.57f);
            Box(vh,.29f,.53f,.37f,.77f);Box(vh,.4f,.53f,.48f,.88f);
            Box(vh,.51f,.53f,.59f,.84f);Box(vh,.62f,.52f,.7f,.74f);
            Quad(vh,new Vector2(.3f,.31f),new Vector2(.17f,.53f),new Vector2(.22f,.59f),new Vector2(.39f,.43f),color);
            Quad(vh,new Vector2(.39f,.12f),new Vector2(.61f,.12f),new Vector2(.62f,.22f),new Vector2(.38f,.22f),color);
            Quad(vh,new Vector2(.42f,.4f),new Vector2(.5f,.49f),new Vector2(.58f,.4f),new Vector2(.5f,.29f),new Color(.1f,.15f,.3f,color.a));
        }
        static Vector2 P(float angle,float radius)=>new Vector2(.5f+Mathf.Cos(angle)*radius,.5f+Mathf.Sin(angle)*radius);
        void Box(VertexHelper vh,float x,float y,float right,float top)
        {Quad(vh,new Vector2(x,y),new Vector2(right,y),new Vector2(right,top),new Vector2(x,top),color);}
        void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
        {
            var r=rectTransform.rect;int start=vh.currentVertCount;
            Add(vh,a,r,tint);Add(vh,b,r,tint);Add(vh,c,r,tint);Add(vh,d,r,tint);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        static void Add(VertexHelper vh,Vector2 p,Rect r,Color tint)
        {var v=UIVertex.simpleVert;v.position=new Vector3(r.xMin+p.x*r.width,r.yMin+p.y*r.height);v.color=tint;vh.AddVert(v);}
    }
}
