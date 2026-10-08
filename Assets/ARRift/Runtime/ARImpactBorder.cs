using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.AR
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ARImpactBorder:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float w=Mathf.Min(r.width,r.height)*.018f;
            Quad(vh,new Rect(r.xMin,r.yMin,r.width,w));Quad(vh,new Rect(r.xMin,r.yMax-w,r.width,w));Quad(vh,new Rect(r.xMin,r.yMin+w,w,r.height-2*w));Quad(vh,new Rect(r.xMax-w,r.yMin+w,w,r.height-2*w));
            // Jagged shards stay at the edges so the camera centre remains readable.
            for(int side=0;side<2;side++)for(int n=0;n<7;n++){float x=side==0?r.xMin:r.xMax,sign=side==0?1:-1,y=r.yMin+(n+.4f)*r.height/7;int i=vh.currentVertCount;vh.AddVert(new Vector3(x,y),color,Vector2.zero);vh.AddVert(new Vector3(x+sign*w*(2+n%3),y+w*3),color,Vector2.zero);vh.AddVert(new Vector3(x+sign*w*.7f,y+w*7),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);}
        }
        void Quad(VertexHelper vh,Rect r){int i=vh.currentVertCount;vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    }
}
