using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.SkyBeast
{
    // Temporary cinematic skirt at ground level. Covers map edges without changing terrain/NavMesh.
    public sealed class HeavenSwordEdgeMist : MonoBehaviour
    {
        Mesh mesh;Material material;
        public void Build(Vector3 center)
        {
            transform.SetPositionAndRotation(new Vector3(center.x,.3f,center.z),Quaternion.identity);
            var v=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var t=new List<int>();
            float[] radii={55,75,92,112,320,1200};float[] alpha={0,.18f,.7f,.98f,1,1};
            for(int j=0;j<radii.Length-1;j++)for(int i=0;i<128;i++)
            {float a=i*Mathf.PI*2/128,b=(i+1)*Mathf.PI*2/128;int n=v.Count;Vector3 P(float angle,float r)=>new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);
                v.AddRange(new[]{P(a,radii[j]),P(a,radii[j+1]),P(b,radii[j+1]),P(b,radii[j])});uv.AddRange(new[]{new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(.5f,1),new Vector2(.5f,1)});foreach(float al in new[]{alpha[j],alpha[j+1],alpha[j+1],alpha[j]})colors.Add(new Color(.42f,.16f,.065f,al));t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            mesh=new Mesh{name="Cinematic ground mist skirt"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(t,0);mesh.RecalculateBounds();gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=gameObject.AddComponent<MeshRenderer>();material=new Material(Resources.Load<Material>("P21/CinematicMist"));r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
