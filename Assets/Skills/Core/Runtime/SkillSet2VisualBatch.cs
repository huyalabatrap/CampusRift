using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.Skills
{
    public enum SkillShape { Dragon, Wings, Tree, Eye, SwordWheel, Domain, Avatar, Soul, Mountain, FireWall }
    // Cosmetic geometry is prebuilt once. All live shapes share one dynamic mesh/renderer.
    // Gameplay agents/colliders never move with cosmetic vertices.
    public sealed class SkillSet2VisualBatch : MonoBehaviour
    {
        public const int Capacity=32;
        const int MaxVertices=65536,MaxIndices=196608;
        public sealed class Slot
        {
            internal float born,until;internal SkillShape shape;internal bool mobile;
            public bool Live;public Vector3 Position,Scale=Vector3.one;public Quaternion Rotation=Quaternion.identity;
            public Transform Follow;public Vector3 Offset;public Color Color;public float Opacity=1;
            public void Fade(float seconds=.5f){until=Mathf.Min(until,Time.time+seconds);Follow=null;}
        }
        sealed class Geometry {public Vector3[] vertices;public Color[] colors;public int[] triangles;}
        const int ShapeCount=10;
        readonly Slot[] slots=new Slot[Capacity];readonly Geometry[] shapes=new Geometry[ShapeCount*2];
        readonly Vector3[] vertices=new Vector3[MaxVertices];readonly Color[] colors=new Color[MaxVertices];readonly int[] indices=new int[MaxIndices];
        Mesh mesh;Material material;MeshRenderer view;
        public int ActiveCount {get;private set;}public int ExhaustedCount {get;private set;}public int VertexCount {get;private set;}
        public int ShapeVertexCount(SkillShape shape,bool mobile)=>shapes[(int)shape+(mobile?ShapeCount:0)]?.vertices.Length??0;
        void Awake()
        {
            for(int i=0;i<Capacity;i++)slots[i]=new Slot();
            for(int i=0;i<ShapeCount;i++){shapes[i]=Build((SkillShape)i,false);shapes[i+ShapeCount]=Build((SkillShape)i,true);}
            mesh=new Mesh{name="P18 pooled comic geometry",indexFormat=IndexFormat.UInt32};mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;view=gameObject.AddComponent<MeshRenderer>();
            material=new Material(Resources.Load<Material>("P18/geometry"));view.sharedMaterial=material;
            view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;view.lightProbeUsage=LightProbeUsage.Off;view.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
        public Slot Spawn(SkillShape shape,Vector3 p,Color color,float duration,Vector3 scale,Quaternion rotation)
        {
            for(int i=0;i<Capacity;i++)if(!slots[i].Live){var s=slots[i];s.Live=true;s.shape=shape;s.mobile=SkillVfxPool.MobileQuality;s.born=Time.time;s.until=Time.time+duration;s.Position=p;s.Scale=scale;s.Rotation=rotation;s.Color=color;s.Opacity=1;s.Follow=null;s.Offset=Vector3.zero;return s;}
            ExhaustedCount++;return null;
        }
        public void Clear(){foreach(var s in slots)s.Live=false;ActiveCount=0;if(view!=null)view.enabled=false;}
        void LateUpdate()
        {
            int v=0,t=0;ActiveCount=0;var inverse=transform.worldToLocalMatrix;
            foreach(var s in slots)
            {
                if(!s.Live)continue;if(Time.time>=s.until){s.Live=false;continue;}ActiveCount++;
                if(s.Follow!=null)s.Position=s.Follow.position+s.Offset;
                float age=Time.time-s.born,fade=Mathf.Clamp01((s.until-Time.time)/.5f)*Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.18f))*s.Opacity;
                var g=shapes[(int)s.shape+(s.mobile?ShapeCount:0)];if(v+g.vertices.Length>vertices.Length||t+g.triangles.Length>indices.Length)continue;
                var rotation=s.Rotation;if(s.shape==SkillShape.SwordWheel)rotation*=Quaternion.Euler(0,age*24,0);
                var matrix=inverse*Matrix4x4.TRS(s.Position,rotation,s.Scale);
                for(int i=0;i<g.vertices.Length;i++)
                {
                    var p=g.vertices[i];if(s.shape==SkillShape.Dragon)p.x+=Mathf.Sin(age*14+p.z*3)*.07f;if(s.shape==SkillShape.FireWall)p.y*=.92f+.08f*Mathf.Sin(age*11+p.x*3);
                    vertices[v+i]=matrix.MultiplyPoint3x4(p);var c=g.colors[i];
                    Color tint=c.r<.1f?SkillVfxPool.Dark(s.Color):c.b>2.5f?new Color(.1f,1.4f,2.4f):c.b>c.r?Color.white:s.Color;
                    colors[v+i]=new Color(tint.r*c.g,tint.g*c.g,tint.b*c.g,c.a*fade);
                }
                for(int i=0;i<g.triangles.Length;i++)indices[t+i]=v+g.triangles[i];v+=g.vertices.Length;t+=g.triangles.Length;
            }
            VertexCount=v;view.enabled=v>0;if(v==0)return;
            // Remove indices from the previous larger shape before shrinking the vertex buffer.
            if(mesh.vertexCount!=v)mesh.Clear(false);
            mesh.SetVertices(vertices,0,v);mesh.SetColors(colors,0,v);mesh.SetIndices(indices,0,t,MeshTopology.Triangles,0,false);
            mesh.bounds=new Bounds(Vector3.zero,new Vector3(500,100,500));
        }
        static Geometry Build(SkillShape shape,bool mobile)
        {
            var b=new Builder(mobile);Color core=new Color(1,1.7f,0,.85f),white=new Color(1,2,2,.9f),dim=new Color(0,.8f,0,.82f);
            if(shape==SkillShape.Dragon)
            {
                int n=mobile?10:20;
                for(int i=0;i<n;i++){float z=-5+i*5f/(n-1),r=.06f+.38f*i/(n-1);b.Oval(new Vector3(Mathf.Sin(i*.55f)*.35f,Mathf.Sin(i*.7f)*.12f,z),new Vector3(r,r*.75f,.30f),core);if(i%2==0)b.Segment(new Vector3(0,r*.6f,z),new Vector3(0,r+ .22f,z-.25f),.025f,white);}
                b.Oval(new Vector3(0,.10f,.2f),new Vector3(.50f,.30f,.62f),core);
                b.Oval(new Vector3(0,.01f,.72f),new Vector3(.32f,.10f,.45f),core);
                b.Oval(new Vector3(0,-.26f,.66f),new Vector3(.31f,.055f,.40f),core);
                for(int side=-1;side<=1;side+=2){b.Segment(new Vector3(side*.3f,.30f,.05f),new Vector3(side*.42f,.75f,-.3f),.065f,core);b.Segment(new Vector3(side*.42f,.75f,-.3f),new Vector3(side*.24f,.85f,-.60f),.035f,white);b.Oval(new Vector3(side*.36f,.2f,.55f),new Vector3(.065f,.055f,.13f),white);b.Segment(new Vector3(side*.3f,-.03f,.9f),new Vector3(side*.95f,-.25f,1.1f),.025f,white);for(int tooth=0;tooth<3;tooth++)b.Segment(new Vector3(side*.23f,-.06f,.48f+tooth*.18f),new Vector3(side*.23f,-.19f,.50f+tooth*.18f),.03f,white);for(int leg=0;leg<2;leg++)b.Segment(new Vector3(side*.25f,-.10f,-1.1f-leg*1.4f),new Vector3(side*.9f,-.5f,-.85f-leg*1.4f),.055f,core);}
            }
            else if(shape==SkillShape.Avatar||shape==SkillShape.Soul)
            {
                bool soul=shape==SkillShape.Soul;
                b.Box(new Vector3(0,1.1f,0),new Vector3(.39f,.48f,.22f),core);
                b.Box(new Vector3(0,1.83f,0),new Vector3(.21f,.24f,.21f),core);
                b.Box(new Vector3(0,2.07f,0),new Vector3(.27f,.035f,.25f),soul?core:white);
                for(int side=-1;side<=1;side+=2){b.Segment(new Vector3(side*.23f,.75f,0),new Vector3(side*.36f,.08f,.1f),.15f,core);b.Oval(new Vector3(side*.52f,1.43f,0),new Vector3(.24f,.15f,.26f),core);b.Segment(new Vector3(side*.56f,1.35f,0),new Vector3(side*.72f,.75f,.13f),.14f,core);b.Box(new Vector3(side*.09f,1.88f,.23f),new Vector3(.055f,.03f,.025f),soul?new Color(1,1,3,1):white);}
                if(!soul){for(int i=0;i<4;i++)b.Segment(new Vector3(0,.85f+i*.23f,-.2f),new Vector3(.7f,1+i*.23f,-.2f),.03f,white);b.Sword(new Vector3(.77f,.6f,.2f),Quaternion.Euler(0,0,-25),1,core);}
            }
            else if(shape==SkillShape.SwordWheel)
            {
                int n=mobile?12:24;b.Ring(1,.015f,.012f,core,48);
                for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;var p=new Vector3(Mathf.Sin(a),.18f,Mathf.Cos(a));b.Sword(p,Quaternion.Euler(0,a*Mathf.Rad2Deg,25),.14f,core);}
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;b.Segment(new Vector3(Mathf.Sin(a)*.1f,.02f,Mathf.Cos(a)*.1f),new Vector3(Mathf.Sin(a),.02f,Mathf.Cos(a)),.009f,white);}
            }
            else if(shape==SkillShape.Domain)
            {
                int sides=mobile?32:64;for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides,k=(i+1)*Mathf.PI*2/sides;b.Rail(new Vector3(Mathf.Sin(a),.02f,Mathf.Cos(a)),new Vector3(Mathf.Sin(k),.02f,Mathf.Cos(k)),.009f,core);}
                int n=mobile?6:12;int steps=mobile?8:12;for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;Vector3 previous=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));for(int j=1;j<=steps;j++){float k=j*Mathf.PI/(2*steps);Vector3 next=new Vector3(Mathf.Sin(a)*Mathf.Cos(k),Mathf.Sin(k),Mathf.Cos(a)*Mathf.Cos(k));b.Rail(previous,next,.005f,core);previous=next;}}
                for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;b.Box(new Vector3(Mathf.Sin(a)*.9f,.04f,Mathf.Cos(a)*.9f),new Vector3(.012f,.035f,.012f),white);}
            }
            else if(shape==SkillShape.Wings)
            {for(int side=-1;side<=1;side+=2)for(int i=0;i<(mobile?4:8);i++){b.Segment(new Vector3(side*.15f,1.35f,0),new Vector3(side*(1.3f-i*.11f),1.7f-i*.1f,-.2f-i*.12f),.06f,core);}b.Ring(.8f,.05f,.025f,white,32);}
            else if(shape==SkillShape.Tree)
            {b.Segment(Vector3.zero,Vector3.up*2.6f,.035f,core);for(int i=0;i<(mobile?6:12);i++){float a=i*2.4f;Vector3 p=new Vector3(Mathf.Sin(a)*.85f,1.3f+i*.09f,Mathf.Cos(a)*.85f);b.Segment(Vector3.up*p.y,p,.017f,core);b.Segment(p,p+Vector3.down*.65f,.008f,core);for(int leaf=0;leaf<3;leaf++)b.Oval(p+Vector3.down*(leaf*.24f),new Vector3(.15f,.065f,.07f),new Color(1,1.1f,0,.55f));}b.Ring(1,.02f,.014f,core,48);}
            else if(shape==SkillShape.Eye)
            {for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40,c=(i+1)*Mathf.PI*2/40;b.Segment(new Vector3(Mathf.Cos(a),Mathf.Sin(a)*.43f,0),new Vector3(Mathf.Cos(c),Mathf.Sin(c)*.43f,0),.035f,core);}b.Box(Vector3.zero,new Vector3(.08f,.23f,.07f),white);for(int side=-1;side<=1;side+=2)b.Segment(new Vector3(side*1.15f,0,0),new Vector3(side*1.45f,0,0),.02f,white);}
            else if(shape==SkillShape.Mountain)
            {for(int i=0;i<5;i++)b.Box(new Vector3((i-2)*.52f,.8f-Mathf.Abs(i-2)*.18f,0),new Vector3(.35f,.9f-Mathf.Abs(i-2)*.18f,.65f),core);}
            else if(shape==SkillShape.FireWall)
            {int n=mobile?4:8;for(int i=0;i<n;i++){float x=-3+6f*i/(n-1),h=1.1f+.5f*Mathf.Sin(i*2.3f);b.Oval(new Vector3(x,h*.5f,0),new Vector3(.4f,h*.5f,.2f),core);b.Segment(new Vector3(x,h*.5f,0),new Vector3(x+.15f,h+.5f,0),.1f,core);b.Oval(new Vector3(x,.3f,.02f),new Vector3(.2f,.3f,.22f),white);}}
            return new Geometry{vertices=b.v.ToArray(),colors=b.c.ToArray(),triangles=b.t.ToArray()};
        }
        sealed class Builder
        {
            readonly bool mobile;public Builder(bool mobile){this.mobile=mobile;}
            public readonly List<Vector3> v=new List<Vector3>();public readonly List<Color> c=new List<Color>();public readonly List<int> t=new List<int>();
            public void Box(Vector3 p,Vector3 half,Color color){Cube(p,half*1.08f,new Color(0,.5f,0,color.a),Quaternion.identity);Cube(p,half,color,Quaternion.identity);}
            public void Oval(Vector3 p,Vector3 half,Color color){OvalLayer(p,half*1.04f,new Color(0,.5f,0,color.a));OvalLayer(p,half,color);}
            void OvalLayer(Vector3 p,Vector3 half,Color color)
            {int start=v.Count,rings=mobile?2:5,sides=mobile?4:8;for(int j=0;j<=rings;j++){float latitude=j*Mathf.PI/rings;for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(p+Vector3.Scale(half,new Vector3(Mathf.Sin(latitude)*Mathf.Cos(a),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(a))));var shade=color;shade.g*=.65f+.35f*(1-j/(float)rings);c.Add(shade);}}for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){int a=start+j*sides+i,b=start+j*sides+(i+1)%sides;t.Add(a);t.Add(b);t.Add(a+sides);t.Add(b);t.Add(b+sides);t.Add(a+sides);}}
            void Cube(Vector3 p,Vector3 h,Color color,Quaternion q)
            {int start=v.Count;for(int i=0;i<8;i++){v.Add(p+q*Vector3.Scale(h,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));c.Add(color);}int[] faces={0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};foreach(int k in faces)t.Add(start+k);}
            public void Segment(Vector3 a,Vector3 b,float width,Color color)
            {Tube(a,b,width*1.18f,new Color(0,.6f,0,color.a));Tube(a,b,width,color);}
            public void Rail(Vector3 a,Vector3 b,float width,Color color)
            {var q=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);for(int layer=0;layer<2;layer++){float w=width*(layer==0?1.6f:1);var tint=layer==0?new Color(0,.6f,0,color.a):color;for(int side=0;side<2;side++){Vector3 offset=q*(side==0?Vector3.right:Vector3.forward)*w;int start=v.Count;v.Add(a-offset);v.Add(a+offset);v.Add(b-offset);v.Add(b+offset);for(int i=0;i<4;i++)c.Add(tint);t.Add(start);t.Add(start+1);t.Add(start+2);t.Add(start+1);t.Add(start+3);t.Add(start+2);}}}
            void Tube(Vector3 a,Vector3 b,float width,Color color)
            {int start=v.Count,sides=mobile?4:8;var q=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);for(int j=0;j<2;j++)for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;v.Add((j==0?a:b)+q*new Vector3(Mathf.Cos(angle)*width,0,Mathf.Sin(angle)*width));var shade=color;shade.g*=.72f+.28f*Mathf.Max(0,Mathf.Cos(angle));c.Add(shade);}for(int i=0;i<sides;i++){int k=(i+1)%sides;t.Add(start+i);t.Add(start+k);t.Add(start+i+sides);t.Add(start+k);t.Add(start+k+sides);t.Add(start+i+sides);}}
            public void Ring(float radius,float y,float width,Color color,int n){for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;Segment(new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius),new Vector3(Mathf.Sin(b)*radius,y,Mathf.Cos(b)*radius),width,color);}}
            public void Sword(Vector3 p,Quaternion q,float scale,Color color){Segment(p+q*Vector3.down*.3f*scale,p+q*Vector3.up*1.3f*scale,.055f*scale,color);Segment(p+q*Vector3.left*.25f*scale,p+q*Vector3.right*.25f*scale,.07f*scale,color);Segment(p+q*Vector3.up*.2f*scale,p+q*Vector3.up*1.3f*scale,.012f*scale,new Color(1,2,2,color.a));}
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
