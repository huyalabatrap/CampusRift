using UnityEngine;
namespace CampusRift.AR
{
    public sealed class ARRune:MonoBehaviour
    {
        Mesh mesh;Material material;
        public void SetPreview(bool preview){if(material!=null)material.SetFloat("_Silhouette",preview?1:0);}
        public void SetValid(bool valid){if(material!=null)material.SetFloat("_Valid",valid?1:0);}
        public static ARRune Create(Transform parent,float radius)
        {
            var go=new GameObject("Animated gold and violet rune",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);var r=go.AddComponent<ARRune>();
            r.mesh=new Mesh{name="AR runic disc",vertices=new[]{new Vector3(-radius,.008f,-radius),new Vector3(-radius,.008f,radius),new Vector3(radius,.008f,radius),new Vector3(radius,.008f,-radius)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};r.mesh.RecalculateNormals();
            go.GetComponent<MeshFilter>().sharedMesh=r.mesh;r.material=new Material(Shader.Find("Campus Rift/AR/Runic Circle"));go.GetComponent<MeshRenderer>().sharedMaterial=r.material;go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return r;
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
