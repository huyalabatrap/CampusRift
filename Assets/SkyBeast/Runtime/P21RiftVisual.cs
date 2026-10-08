using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.SkyBeast
{
    public sealed class P21RiftVisual:MonoBehaviour
    {
        Mesh mesh;
        public void Build(Vector3 point,Vector3 camera)
        {
            transform.position=point;var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
            for(int i=0;i<64;i++){float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;int s=v.Count;
                foreach(float r in new[]{.94f,1f})foreach(float theta in new[]{a,b}){float torn=1+.03f*Mathf.Sin(theta*13)+.015f*Mathf.Cos(theta*31);v.Add(new Vector3(Mathf.Cos(theta)*60*r*torn,Mathf.Sin(theta)*35*r,0));c.Add(r<1?new Color(1,.24f,.03f):new Color(.3f,.015f,.09f));}
                t.AddRange(new[]{s,s+1,s+2,s+2,s+1,s+3});}
            int center=v.Count;v.Add(Vector3.zero);c.Add(new Color(.004f,.002f,.008f));
            for(int i=0;i<64;i++){int n=v.Count;v.Add(v[i*4]);v.Add(v[i*4+1]);c.Add(new Color(.004f,.002f,.008f));c.Add(new Color(.004f,.002f,.008f));t.Add(center);t.Add(n+1);t.Add(n);}
            mesh=new Mesh{name="Torn rift rim"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetColors(c);mesh.RecalculateBounds();gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=gameObject.AddComponent<MeshRenderer>();mr.sharedMaterial=Resources.Load<Material>("P21/IdentityGlow");mr.shadowCastingMode=ShadowCastingMode.Off;
            Show(0,camera);
        }
        public void Show(float amount,Vector3 camera){transform.localScale=new Vector3(Mathf.Max(.001f,amount),Mathf.Max(.001f,amount),1);transform.rotation=Quaternion.LookRotation(transform.position-camera);gameObject.SetActive(amount>.001f);}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
