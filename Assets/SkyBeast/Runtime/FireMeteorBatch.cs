using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.SkyBeast
{
    // One dynamic draw: five tapered ribbon rows and a soft fireball quad per pooled slot.
    public sealed class FireMeteorBatch:MonoBehaviour
    {
        const int Capacity=72,VerticesPerMeteor=14;
        const float HalfWidth=.43f,GlowRadius=.55f,NearDistance=8;
        readonly Vector3[] vertices=new Vector3[Capacity*VerticesPerMeteor];
        Mesh mesh;Material material;MeshRenderer rendererComponent;bool shown;
        void Awake()
        {
            mesh=new Mesh{name="Pooled fire meteor ribbons",hideFlags=HideFlags.DontSave};mesh.MarkDynamic();
            var uv=new Vector2[vertices.Length];var seeds=new Vector2[vertices.Length];var indices=new int[Capacity*30];
            for(int i=0;i<Capacity;i++)
            {
                int v=i*VerticesPerMeteor,t=i*30;
                for(int row=0;row<5;row++)
                {
                    float y=RowDistance(row);uv[v+row*2]=new Vector2(0,y);uv[v+row*2+1]=new Vector2(1,y);
                    if(row==4)continue;
                    int a=v+row*2,k=t+row*6;
                    indices[k]=a;indices[k+1]=a+2;indices[k+2]=a+1;
                    indices[k+3]=a+1;indices[k+4]=a+2;indices[k+5]=a+3;
                }
                // Negative UV.y selects the radial glow, rather than an opaque semicircle cap.
                uv[v+10]=new Vector2(0,-1);uv[v+11]=new Vector2(1,-1);
                uv[v+12]=new Vector2(0,-2);uv[v+13]=new Vector2(1,-2);
                indices[t+24]=v+10;indices[t+25]=v+12;indices[t+26]=v+11;
                indices[t+27]=v+11;indices[t+28]=v+12;indices[t+29]=v+13;
                for(int j=0;j<VerticesPerMeteor;j++)seeds[v+j]=new Vector2(i*2.399963f,0);
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.uv2=seeds;mesh.triangles=indices;
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;rendererComponent=gameObject.AddComponent<MeshRenderer>();
            // A Resources material keeps the shader referenced in player builds.
            material=new Material(Resources.Load<Material>("P13/meteor-batch")){name="Fire meteor batch (runtime)",hideFlags=HideFlags.DontSave};
            rendererComponent.sharedMaterial=material;
            rendererComponent.shadowCastingMode=ShadowCastingMode.Off;rendererComponent.receiveShadows=false;rendererComponent.enabled=false;
            rendererComponent.lightProbeUsage=LightProbeUsage.Off;rendererComponent.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
        static float RowDistance(int row)=>row==0?0:row==1?.14f:row==2?.38f:row==3?.7f:1;
        static float RowWidth(int row)=>row==0?1:row==1?.82f:row==2?.57f:row==3?.26f:0;
        public void Set(int slot,Vector3 head,Vector3 tail,Vector3 camera)
        {
            Vector3 trail=tail-head;
            Vector3 side=Vector3.Cross(trail,camera-head).normalized;
            if(side.sqrMagnitude<.01f)side=Vector3.right;
            // Constrain the whole streak, including a tail that passes close to the camera.
            float nearest=Mathf.Clamp01(Vector3.Dot(camera-head,trail)/Mathf.Max(.001f,trail.sqrMagnitude));
            float distance=Vector3.Distance(camera,head+trail*nearest);
            float scale=Mathf.Clamp(distance/NearDistance,.16f,1);
            tail=head+trail*scale;
            int v=slot*VerticesPerMeteor;
            for(int row=0;row<5;row++)
            {
                Vector3 point=Vector3.Lerp(head,tail,RowDistance(row));
                Vector3 width=side*(HalfWidth*scale*RowWidth(row));
                vertices[v+row*2]=point-width;vertices[v+row*2+1]=point+width;
            }
            Vector3 along=trail.normalized*(GlowRadius*scale),across=side*(GlowRadius*scale);
            vertices[v+10]=head-across-along;vertices[v+11]=head+across-along;
            vertices[v+12]=head-across+along;vertices[v+13]=head+across+along;
        }
        public void Hide(int slot){int v=slot*VerticesPerMeteor;for(int i=0;i<VerticesPerMeteor;i++)vertices[v+i]=Vector3.zero;}
        public void Upload(bool visible)
        {
            if(shown!=visible){shown=visible;rendererComponent.enabled=visible;}
            if(!visible)return;
            mesh.vertices=vertices;
            mesh.bounds=new Bounds(transform.InverseTransformPoint(Vector3.zero),new Vector3(400,400,400));
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
