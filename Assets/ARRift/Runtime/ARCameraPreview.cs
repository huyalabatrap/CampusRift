using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.AR
{
    // Reuses the sampled CPU image in memory, using the same display mapping as landmarks.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ARCameraPreview:MaskableGraphic
    {
        public FrameSampler sampler;
        public override Texture mainTexture=>sampler!=null&&sampler.Preview!=null?sampler.Preview:Texture2D.blackTexture;
        void Update(){if(sampler!=null){sampler.PreviewRequested=isActiveAndEnabled;SetVerticesDirty();SetMaterialDirty();}}
        protected override void OnDisable(){base.OnDisable();if(sampler!=null)sampler.PreviewRequested=false;}
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();var r=rectTransform.rect;var f=sampler!=null?sampler.PreviewFrame:default;
            var a=GestureCoordinates.Viewport(f,Vector2.zero);var b=GestureCoordinates.Viewport(f,Vector2.right)-a;var c=GestureCoordinates.Viewport(f,Vector2.up)-a;float det=b.x*c.y-b.y*c.x;
            foreach(var p in new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right}){var d=p-a;var uv=Mathf.Abs(det)>.00001f?new Vector2((d.x*c.y-d.y*c.x)/det,(b.x*d.y-b.y*d.x)/det):p;v.AddVert(new Vector2(r.xMin+p.x*r.width,r.yMin+p.y*r.height),Color.white,uv);}v.AddTriangle(0,1,2);v.AddTriangle(0,2,3);
        }
    }
}
