using UnityEngine;
using UnityEngine.UI;

namespace CampusRift.Skills
{
    // Original vector icon: split spatial slab, central lightning seam, three fragments.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VoidWallGlyph : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Polygon(vh,new[]{new Vector2(.15f,.15f),new Vector2(.12f,.78f),new Vector2(.44f,.98f),new Vector2(.38f,.58f),new Vector2(.52f,.43f),new Vector2(.36f,.05f)});
            Polygon(vh,new[]{new Vector2(.58f,.15f),new Vector2(.55f,.43f),new Vector2(.46f,.59f),new Vector2(.59f,.91f),new Vector2(.85f,.72f),new Vector2(.86f,.2f)});
            Polygon(vh,new[]{new Vector2(.88f,.82f),new Vector2(.97f,.9f),new Vector2(.93f,.63f)});
        }
        void Polygon(VertexHelper vh,Vector2[] points)
        {
            int start=vh.currentVertCount;var r=rectTransform.rect;
            foreach(var p in points){var v=UIVertex.simpleVert;v.position=new Vector3(r.xMin+p.x*r.width,r.yMin+p.y*r.height);v.color=color;vh.AddVert(v);}
            for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);
        }
    }
}
