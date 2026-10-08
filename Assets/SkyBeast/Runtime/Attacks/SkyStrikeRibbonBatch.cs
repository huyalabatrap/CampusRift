using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.SkyBeast
{
    // Six gameplay trails, using the approved PERF-FIRE mesh layout and shader.
    // Separate from the 72/48/36 cosmetic meteor budget owned by FireBreathVisuals.
    public sealed class SkyStrikeRibbonBatch:MonoBehaviour
    {
        const int Capacity=6,Stride=14;
        readonly Vector3[] vertices=new Vector3[Capacity*Stride];
        Mesh mesh;MeshRenderer draw;
        void Awake()
        {
            mesh=new Mesh{name="Six tapered sky strike ribbons"};mesh.MarkDynamic();
            var uv=new Vector2[vertices.Length];var seeds=new Vector2[vertices.Length];var indices=new int[Capacity*30];
            for(int i=0;i<Capacity;i++)
            {
                int v=i*Stride,t=i*30;
                for(int row=0;row<5;row++)
                {
                    float y=Distance(row);uv[v+row*2]=new Vector2(0,y);uv[v+row*2+1]=new Vector2(1,y);
                    if(row==4)continue;int a=v+row*2,k=t+row*6;
                    indices[k]=a;indices[k+1]=a+2;indices[k+2]=a+1;indices[k+3]=a+1;indices[k+4]=a+2;indices[k+5]=a+3;
                }
                uv[v+10]=new Vector2(0,-1);uv[v+11]=new Vector2(1,-1);uv[v+12]=new Vector2(0,-2);uv[v+13]=new Vector2(1,-2);
                indices[t+24]=v+10;indices[t+25]=v+12;indices[t+26]=v+11;indices[t+27]=v+11;indices[t+28]=v+12;indices[t+29]=v+13;
                for(int j=0;j<Stride;j++)seeds[v+j]=new Vector2(i*2.399963f,0);
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.uv2=seeds;mesh.triangles=indices;
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;draw=gameObject.AddComponent<MeshRenderer>();draw.sharedMaterial=Resources.Load<Material>("P13/meteor-batch");
            draw.shadowCastingMode=ShadowCastingMode.Off;draw.receiveShadows=false;draw.lightProbeUsage=LightProbeUsage.Off;draw.reflectionProbeUsage=ReflectionProbeUsage.Off;draw.enabled=false;
        }
        static float Distance(int row)=>row==0?0:row==1?.14f:row==2?.38f:row==3?.7f:1;
        static float Width(int row)=>row==0?1:row==1?.82f:row==2?.57f:row==3?.26f:0;
        public void Set(int slot,Vector3 head,Vector3 tail,Vector3 camera)
        {
            var trail=tail-head;var side=Vector3.Cross(trail,camera-head).normalized;if(side.sqrMagnitude<.01f)side=Vector3.right;
            float nearest=Mathf.Clamp01(Vector3.Dot(camera-head,trail)/Mathf.Max(.001f,trail.sqrMagnitude));
            float scale=Mathf.Clamp(Vector3.Distance(camera,head+trail*nearest)/8,.16f,1);tail=head+trail*scale;int v=slot*Stride;
            for(int row=0;row<5;row++){var p=Vector3.Lerp(head,tail,Distance(row));var w=side*(.43f*scale*Width(row));vertices[v+row*2]=p-w;vertices[v+row*2+1]=p+w;}
            var along=trail.normalized*(.55f*scale);var across=side*(.55f*scale);
            vertices[v+10]=head-across-along;vertices[v+11]=head+across-along;vertices[v+12]=head-across+along;vertices[v+13]=head+across+along;
        }
        public void Hide(int slot){for(int j=0;j<Stride;j++)vertices[slot*Stride+j]=Vector3.zero;}
        public void Upload(bool active){draw.enabled=active;if(!active)return;mesh.vertices=vertices;mesh.RecalculateBounds();}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
