using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.AR
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ARLandmarkGraphic : MaskableGraphic
    {
        GestureFrame frame;static readonly int[] Edges={0,1,1,2,2,3,3,4,0,5,5,6,6,7,7,8,5,9,9,10,10,11,11,12,9,13,13,14,14,15,15,16,13,17,17,18,18,19,19,20,0,17};
        public void Set(GestureFrame value){frame=value;SetVerticesDirty();}
        Vector2 Point(int i){var p=GestureCoordinates.Viewport(frame,new Vector2(frame.landmarks[i*3],frame.landmarks[i*3+1]));return new Vector2((p.x-.5f)*rectTransform.rect.width,(p.y-.5f)*rectTransform.rect.height);}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(frame.landmarks==null||frame.landmarks.Length!=63||frame.label=="None")return;
            for(int i=0;i<Edges.Length;i+=2){var a=Point(Edges[i]);var b=Point(Edges[i+1]);var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*1.5f;Quad(vh,a-n,a+n,b+n,b-n,color);}
            for(int i=0;i<21;i++){var p=Point(i);Quad(vh,p+new Vector2(-4,-4),p+new Vector2(-4,4),p+new Vector2(4,4),p+new Vector2(4,-4),Color.white);}
            var palm=GestureCoordinates.Palm(frame);var center=new Vector2((palm.x-.5f)*rectTransform.rect.width,(palm.y-.5f)*rectTransform.rect.height);Quad(vh,center+new Vector2(-7,-7),center+new Vector2(-7,7),center+new Vector2(7,7),center+new Vector2(7,-7),CampusRift.UI.ComicTheme.Gold);
        }
        static void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint){int i=v.currentVertCount;v.AddVert(a,tint,Vector2.zero);v.AddVert(b,tint,Vector2.zero);v.AddVert(c,tint,Vector2.zero);v.AddVert(d,tint,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
    }
}
