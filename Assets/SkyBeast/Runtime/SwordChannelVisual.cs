using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.SkyBeast
{
    // One mesh: two ink-edged rings, twelve sword/rune spokes. No colliders or lights.
    public sealed class SwordChannelVisual : MonoBehaviour
    {
        Mesh mesh;Material material;MeshRenderer draw;float fade=-1;
        public void Build(Material gold)
        {
            var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var indices=new System.Collections.Generic.List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int i=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
            for(int ring=0;ring<2;ring++)for(int i=0;i<96;i++){float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96,r=ring==0?2.5f:1.95f,w=ring==0?.13f:.07f;
                Vector3 P(float angle,float radius)=>new Vector3(Mathf.Cos(angle)*radius,.025f,Mathf.Sin(angle)*radius);Quad(P(a,r-w),P(a,r+w),P(b,r+w),P(b,r-w));}
            for(int i=0;i<12;i++){float a=i*30*Mathf.Deg2Rad;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,dir)*.11f;Quad(dir*.9f-side,dir*.9f+side,dir*1.7f+side*.2f,dir*1.7f-side*.2f);
                var mid=dir*2.2f;Quad(mid-side-dir*.09f,mid+side-dir*.09f,mid+side+dir*.09f,mid-side+dir*.09f);}
            mesh=new Mesh{name="Sword channel rune array"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);var colors=new Color[vertices.Count];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;mesh.colors=colors;mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;draw=gameObject.AddComponent<MeshRenderer>();material=new Material(gold);draw.sharedMaterial=material;draw.shadowCastingMode=ShadowCastingMode.Off;draw.receiveShadows=false;
        }
        public void Show(float progress){fade=-1;material.SetFloat("_Alpha",1);transform.localScale=Vector3.one*Mathf.Lerp(.8f,1.2f,progress);}
        public void Fade(){fade=0;}
        public void Tint(Color color){material.SetColor("_Tint",color);}
        void Update(){transform.localRotation=Quaternion.Euler(0,Time.unscaledTime*32,0);material.SetFloat("_Clock",Time.unscaledTime);if(fade>=0){fade+=Time.unscaledDeltaTime;material.SetFloat("_Alpha",1-Mathf.Clamp01(fade/.45f));if(fade>=.45f)gameObject.SetActive(false);}}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
