using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.SkyBeast
{
    // One fixed mesh / renderer for three spherical shock rings and radial impact shards.
    public sealed class HeavenSwordImpactVisual : MonoBehaviour
    {
        Mesh mesh;Material material;MeshRenderer draw;
        public void Build()
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 d,Vector3 e,Color color){int n=v.Count;v.AddRange(new[]{a,b,d,e});uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});for(int j=0;j<4;j++)c.Add(color);t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            for(int ring=0;ring<5;ring++)for(int i=0;i<96;i++)
            {float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96,r=ring<3?1:.64f+(ring-3)*.15f,w=ring<3?.035f:.055f;
                Vector3 P(float angle,float radius){var p=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);return ring==1?Quaternion.Euler(65,25,0)*p:ring==2?Quaternion.Euler(-35,65,0)*p:p;}
                Quad(P(a,r-w),P(a,r+w),P(b,r+w),P(b,r-w),new Color(3.2f,1.45f,.08f,ring<3?.75f:.45f));}
            for(int i=0;i<24;i++)
            {float a=i*2.399963f;var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var s=new Vector3(-d.y,d.x,0)*(.012f+(i%3)*.006f);Quad(d*.18f-s,d*.18f+s,d*(.95f+(i%4)*.15f)+s*.1f,d*(.95f+(i%4)*.15f)-s*.1f,new Color(3.8f,1.65f,.12f,.8f));}
            mesh=new Mesh{name="Spherical shock fronts and radial golden shards"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateBounds();gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;draw=gameObject.AddComponent<MeshRenderer>();material=new Material(Shader.Find("Campus Rift/Heaven Sword Aura"));draw.sharedMaterial=material;draw.shadowCastingMode=ShadowCastingMode.Off;draw.receiveShadows=false;Hide();
        }
        public void Show(Vector3 center,Camera camera,float seconds)
        {draw.enabled=seconds>=0&&seconds<1.4f;transform.SetPositionAndRotation(center,Quaternion.LookRotation(camera.transform.position-center));transform.localScale=Vector3.one*Mathf.Lerp(19,57,Mathf.Clamp01(seconds/1.1f));material.SetFloat("_Alpha",1-Mathf.SmoothStep(0,1,Mathf.Clamp01(seconds/1.4f)));}
        public void Hide(){if(draw!=null)draw.enabled=false;}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
