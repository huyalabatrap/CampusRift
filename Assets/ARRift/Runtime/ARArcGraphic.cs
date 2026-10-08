using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.AR
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ARArcGraphic:MaskableGraphic
    {
        float amount;public float Amount {get=>amount;set{value=Mathf.Clamp01(value);if(Mathf.Abs(value-amount)<.001f)return;amount=value;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper v){v.Clear();float r=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;int n=Mathf.CeilToInt(amount*64);for(int j=0;j<n;j++){float a=Mathf.PI*.5f-j/64f*Mathf.PI*2,b=Mathf.PI*.5f-Mathf.Min((j+1)/64f,amount)*Mathf.PI*2;int i=v.currentVertCount;foreach(var p in new[]{new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(r-5),new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(r-5)})v.AddVert(p,color,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}}
    }
}
