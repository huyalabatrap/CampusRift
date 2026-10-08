using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CampusRift.SkyBeast
{
    // Fixed, prewarmed meshes. Metres in local Y: tip 0, guard 115, pommel 150.
    public sealed class HeavenSwordHeroVisual : MonoBehaviour
    {
        sealed class Builder
        {
            public readonly List<Vector3> v=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();readonly List<Color> colors=new List<Color>();readonly List<int> t=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            {int n=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});for(int i=0;i<4;i++)colors.Add(color);t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public void Line(Vector3 a,Vector3 b,float width,Color color)
            {var side=Vector3.Cross((b-a).normalized,Vector3.forward)*width;Quad(a-side,a+side,b+side,b-side,color);}
            public Mesh Mesh(string name){var m=new Mesh{name=name};m.SetVertices(v);m.SetUVs(0,uv);m.SetColors(colors);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
        readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
        Transform halo;Material body,runes,aura;Renderer[] draws;
        MeshRenderer Draw(string name,Builder builder,Shader shader,out Material material)
        {var go=new GameObject(name);go.transform.SetParent(transform,false);var mesh=builder.Mesh(name);meshes.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();material=new Material(Resources.Load<Material>(shader.name.EndsWith("Hero")?"P15/HeavenHero":"P15/HeavenAura"));materials.Add(material);r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return r;}
        public void Build()
        {
            var b=new Builder();
            // Eight-sided cross section makes the ridge and bevel catch different light.
            float[] y={0,16,84,109,115};float[] w={.04f,3.7f,7.5f,6.4f,4.8f};float[] depth={.05f,1.3f,2.8f,2.5f,2};
            Vector3 P(int level,int side){var ring=new[]{new Vector2(0,-1),new Vector2(.78f,-.32f),new Vector2(1,0),new Vector2(.78f,.32f),new Vector2(0,1),new Vector2(-.78f,.32f),new Vector2(-1,0),new Vector2(-.78f,-.32f)};return new Vector3(ring[side%8].x*w[level],y[level],ring[side%8].y*depth[level]);}
            for(int j=0;j<4;j++)for(int i=0;i<8;i++)b.Quad(P(j,i+1),P(j,i),P(j+1,i),P(j+1,i+1),Color.white);
            void Box(Vector3 c,Vector3 s,Color color){var a=c-s*.5f;var z=c+s*.5f;b.Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,z.y,a.z),new Vector3(z.x,z.y,a.z),new Vector3(z.x,a.y,a.z),color);b.Quad(new Vector3(z.x,a.y,z.z),new Vector3(z.x,z.y,z.z),new Vector3(a.x,z.y,z.z),new Vector3(a.x,a.y,z.z),color);b.Quad(new Vector3(a.x,z.y,a.z),new Vector3(a.x,z.y,z.z),new Vector3(z.x,z.y,z.z),new Vector3(z.x,z.y,a.z),color);b.Quad(new Vector3(a.x,a.y,z.z),new Vector3(a.x,a.y,a.z),new Vector3(z.x,a.y,a.z),new Vector3(z.x,a.y,z.z),color);b.Quad(new Vector3(a.x,a.y,z.z),new Vector3(a.x,z.y,z.z),new Vector3(a.x,z.y,a.z),new Vector3(a.x,a.y,a.z),color);b.Quad(new Vector3(z.x,a.y,a.z),new Vector3(z.x,z.y,a.z),new Vector3(z.x,z.y,z.z),new Vector3(z.x,a.y,z.z),color);}
            Box(new Vector3(0,117,0),new Vector3(31,4,5),Color.white);
            Box(new Vector3(-13,119,0),new Vector3(5,8,5),Color.white);Box(new Vector3(13,119,0),new Vector3(5,8,5),Color.white);
            Box(new Vector3(0,134,0),new Vector3(3.4f,28,3.5f),new Color(.65f,.45f,.22f));
            for(int j=0;j<5;j++)Box(new Vector3(0,123+j*5,0),new Vector3(4.4f,1,4.5f),Color.white);
            Box(new Vector3(0,148,0),new Vector3(7,4,6),Color.white);
            var bodyDraw=Draw("Bevels and raised ridge",b,Shader.Find("Campus Rift/Heaven Sword Hero"),out body);
            var outline=new GameObject("Bronze ink silhouette");outline.transform.SetParent(transform,false);outline.AddComponent<MeshFilter>().sharedMesh=bodyDraw.GetComponent<MeshFilter>().sharedMesh;
            var ink=new Material(Resources.Load<Material>("P15/HeavenInk"));materials.Add(ink);var inkDraw=outline.AddComponent<MeshRenderer>();inkDraw.sharedMaterial=ink;inkDraw.shadowCastingMode=ShadowCastingMode.Off;inkDraw.receiveShadows=false;
            b=new Builder();var glow=new Color(5.5f,4.1f,1.15f);
            // Angular seal script in two columns, offset from the centre ridge, on both faces.
            for(int face=-1;face<=1;face+=2)for(int j=0;j<10;j++)
            {float h=22+j*8,z=face*3.1f;float x=(j%2==0?-1:1)*2.1f;
                var c=new Vector3(x,h,z);b.Line(c+new Vector3(-.8f,2,0),c+new Vector3(.8f,.6f,0),.22f,glow);b.Line(c+new Vector3(.8f,.6f,0),c+new Vector3(-.4f,-1,0),.22f,glow);b.Line(c+new Vector3(-.4f,-1,0),c+new Vector3(.6f,-2.6f,0),.22f,glow);b.Line(c+new Vector3(-1,-1,0),c+new Vector3(1,-1,0),.18f,glow);}
            b.Line(new Vector3(0,16,-3.2f),new Vector3(0,113,-3.2f),.22f,glow);b.Line(new Vector3(0,16,3.2f),new Vector3(0,113,3.2f),.22f,glow);
            Draw("Flowing golden seal runes",b,Shader.Find("Campus Rift/Heaven Sword Aura"),out runes);
            runes.SetFloat("_RuneFlow",1);
            b=new Builder();
            for(int ring=0;ring<3;ring++)for(int i=0;i<96;i++)
            {float a=i*Mathf.PI*2/96,z=(i+1)*Mathf.PI*2/96,r=12+ring*2.5f,h=32+ring*33;Vector3 RingPoint(float angle,float radius)=>new Vector3(Mathf.Cos(angle)*radius,h+Mathf.Sin(angle*3)*2,Mathf.Sin(angle)*radius);b.Quad(RingPoint(a,r-1),RingPoint(a,r+1),RingPoint(z,r+1),RingPoint(z,r-1),new Color(3.8f,2.4f,.3f,.65f));}
            // Broad transparent aureole behind the luminous blade.
            b.Quad(new Vector3(-15,5,4),new Vector3(15,5,4),new Vector3(15,119,4),new Vector3(-15,119,4),new Color(2.5f,1.1f,.06f,.18f));
            halo=Draw("Three rotating aureole rings",b,Shader.Find("Campus Rift/Heaven Sword Aura"),out aura).transform;
            draws=GetComponentsInChildren<Renderer>();Hide();
        }
        public void Show(Vector3 tip,Quaternion rotation,float scale,float alpha,float clock)
        {transform.SetPositionAndRotation(tip,rotation);transform.localScale=Vector3.one*scale;foreach(var r in draws)r.enabled=alpha>0;foreach(var m in materials){m.SetFloat("_Alpha",alpha);m.SetFloat("_Clock",clock);}bool reduced=UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false;runes.SetFloat("_Alpha",alpha*(reduced?.7f:.7f+.3f*Mathf.Sin(clock*5)));halo.localRotation=Quaternion.Euler(0,clock*25,0);}
        public void Hide(){if(draws!=null)foreach(var r in draws)r.enabled=false;}
        void OnDestroy(){foreach(var m in meshes)Destroy(m);foreach(var m in materials)Destroy(m);}
    }
}
