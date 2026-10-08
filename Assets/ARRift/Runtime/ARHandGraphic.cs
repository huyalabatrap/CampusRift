using UnityEngine;
using UnityEngine.UI;
using CampusRift.UI;
namespace CampusRift.AR
{
    // Five deliberately different comic silhouettes; no dependency on emoji fonts.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ARHandGraphic:MaskableGraphic
    {
        public int Gesture;
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();Draw(v,1.12f,new Vector2(2,-2),ComicTheme.Ink);Draw(v,1,Vector2.zero,color);
        }
        void Draw(VertexHelper v,float scale,Vector2 offset,Color tint)
        {
            Box(v,-.25f,-.25f,.26f,.15f,scale,offset,tint);Box(v,-.13f,-.43f,.18f,-.2f,scale,offset,tint);
            for(int f=0;f<4;f++){float x=-.25f+f*.14f;bool up=Gesture==0||Gesture==2&&f==0||Gesture==3&&f<2;float h=up?.47f-Mathf.Abs(f-1)*.055f:.24f;Box(v,x,.08f,x+.115f,h,scale,offset,tint);}
            if(Gesture==5)Box(v,-.42f,-.1f,-.27f,.5f,scale,offset,tint);else if(Gesture==4)Box(v,-.42f,-.42f,-.27f,.1f,scale,offset,tint);else Box(v,-.42f,-.12f,-.22f,Gesture==0?.28f:.1f,scale,offset,tint);
        }
        void Box(VertexHelper v,float x,float y,float a,float b,float scale,Vector2 offset,Color tint){float w=rectTransform.rect.width*scale,h=rectTransform.rect.height*scale;int i=v.currentVertCount;v.AddVert(new Vector2(x*w,y*h)+offset,tint,Vector2.zero);v.AddVert(new Vector2(x*w,b*h)+offset,tint,Vector2.zero);v.AddVert(new Vector2(a*w,b*h)+offset,tint,Vector2.zero);v.AddVert(new Vector2(a*w,y*h)+offset,tint,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
    }
}
