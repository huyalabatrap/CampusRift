using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.SkyBeast
{
    // Prewarmed dynamic mesh, reuses P03 blade geometry. All swords and tapered trails in one draw.
    public sealed class GiantSwordVisual : MonoBehaviour
    {
        Mesh mesh,blade;Material material;MeshRenderer draw;
        Vector3[] source,vertices;Vector2[] uv;Color[] colors;int[] indices;
        Vector3[] starts;float[] phases;int per,capacity;
        public int SwordCount {get;private set;}
        public Vector3 Focus {get;private set;}
        public float Progress {get;private set;}
        public void Build(HeavenSwordConfig config)
        {
            blade=Combat.FlyingSword.BladeMesh(config.blade);source=blade.vertices;var triangles=blade.triangles;per=source.Length+4;capacity=config.highSwords;
            vertices=new Vector3[capacity*per];uv=new Vector2[vertices.Length];colors=new Color[vertices.Length];indices=new int[capacity*(triangles.Length+6)];starts=new Vector3[capacity];phases=new float[capacity];
            var rand=new System.Random(15150);
            for(int i=0;i<capacity;i++)
            {
                float angle=(float)rand.NextDouble()*Mathf.PI*2,r=12+Mathf.Sqrt((float)rand.NextDouble())*88;starts[i]=new Vector3(Mathf.Sin(angle)*r,0,Mathf.Cos(angle)*r);phases[i]=(float)rand.NextDouble();
                int v=i*per,t=i*(triangles.Length+6);
                for(int j=0;j<triangles.Length;j++)indices[t+j]=v+triangles[j];int k=t+triangles.Length;
                indices[k]=v+source.Length;indices[k+1]=v+source.Length+1;indices[k+2]=v+source.Length+2;indices[k+3]=v+source.Length;indices[k+4]=v+source.Length+2;indices[k+5]=v+source.Length+3;
                for(int j=0;j<source.Length;j++){var p=source[j];float width=p.z<-.2f?.02f:p.z<-.14f?.11f:p.z<.36f?.07f:.07f;uv[v+j]=new Vector2(Mathf.Clamp01(.5f+p.x/(width*2)),(p.z+.5f)/1.45f);colors[v+j]=Color.white;}
                uv[v+source.Length]=new Vector2(0,0);uv[v+source.Length+1]=new Vector2(1,0);uv[v+source.Length+2]=new Vector2(.6f,1);uv[v+source.Length+3]=new Vector2(.4f,1);
                for(int j=source.Length;j<per;j++)colors[v+j]=new Color(1,1,1,.55f);
            }
            mesh=new Mesh{name="Vạn Kiếm Quy Tông pooled P03 swords"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=indices;
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;draw=gameObject.AddComponent<MeshRenderer>();material=new Material(config.gold);draw.sharedMaterial=material;draw.shadowCastingMode=ShadowCastingMode.Off;draw.receiveShadows=false;draw.enabled=false;
        }
        public void Show(float p,Vector3 center,Vector3 focus,Camera camera,int count)
        {
            // Vertices use campus world positions; cancel the player's position and facing.
            transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            Progress=p;Focus=focus;SwordCount=Mathf.Min(capacity,count);draw.enabled=true;material.SetFloat("_Clock",Time.unscaledTime);
            float gather=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.23f,.58f,p));float visibility=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,.77f,p));
            for(int i=0;i<capacity;i++)
            {
                int offset=i*per;if(i>=SwordCount||visibility<=0){for(int j=0;j<per;j++)vertices[offset+j]=Vector3.zero;continue;}
                float seed=phases[i],rise=Mathf.Clamp01((p*4.3f-seed*.34f));float angle=i*2.399963f+p*8;
                Vector3 start=center+starts[i];Vector3 ascent=start+Vector3.up*(4+rise*(70+seed*70));
                float fall=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,.735f,p));
                Vector3 helix=focus+new Vector3(Mathf.Sin(angle)*(12+seed*18),60+seed*130-fall*72,Mathf.Cos(angle)*(12+seed*18));
                Vector3 position=Vector3.Lerp(ascent,helix,gather);
                // Keep a view corridor to the actual beast during the rise.
                if(p<.3f&&Vector3.ProjectOnPlane(position-focus,camera.transform.forward).sqrMagnitude<400&&Vector3.Dot(position-focus,camera.transform.forward)<15)position+=camera.transform.right*32;
                Vector3 tangent=new Vector3(Mathf.Cos(angle),.35f,-Mathf.Sin(angle));Vector3 dir=Vector3.Lerp(Vector3.up,tangent.normalized,gather*.9f).normalized;
                Vector3 side=Vector3.Cross(dir,camera.transform.position-position).normalized;if(side.sqrMagnitude<.01f)side=Vector3.right;
                float orbitVisibility=i%8==0?1:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.54f,p));
                float size=(2.5f+seed*2)*visibility*orbitVisibility*Mathf.Lerp(1,.6f,gather);
                for(int j=0;j<source.Length;j++)vertices[offset+j]=position+side*(source[j].x*size*3)+dir*(source[j].z*size);
                Vector3 tail=position-dir*(4+seed*7)*visibility,w=side*.14f*visibility;int t=offset+source.Length;
                vertices[t]=position-w;vertices[t+1]=position+w;vertices[t+2]=tail;vertices[t+3]=tail;
            }
            mesh.vertices=vertices;mesh.bounds=new Bounds(center+Vector3.up*130,new Vector3(600,550,600));
        }
        public void Hide(){if(draw!=null)draw.enabled=false;SwordCount=0;}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(blade!=null)Destroy(blade);if(material!=null)Destroy(material);}
    }
}
