using System.Collections.Generic;
using UnityEngine;
using CampusRift.Controls;
namespace CampusRift.Learning
{
    public sealed partial class LearningShrine
    {
        Mesh steleMesh; Light readyLight; CampusExplorer glowPlayer;float nextGlowCheck;
        void BuildAncientStele()
        {
            stone=Mat("Weathered grey stone / moss",new Color(.86f,.87f,.82f));stone.SetTexture("_BaseMap",Resources.Load<Texture2D>("P23/weathered-stone"));stone.SetFloat("_Smoothness",.08f);
            edge=Mat("Recessed cracks",new Color(.10f,.12f,.10f));glow=Mat("Jade engraved inlay",new Color(.18f,.63f,.45f),true);
            Piece("Lower weathered step",new Vector3(0,.10f,0),new Vector3(1.46f,.20f,.94f),stone,3);
            Piece("Middle stone step",new Vector3(0,.28f,0),new Vector3(1.25f,.17f,.79f),stone,-2);
            Piece("Stele socket",new Vector3(0,.43f,0),new Vector3(.99f,.17f,.62f),stone,2);
            // Convex chipped profile, with a real face and side thickness rather than a black box.
            var outline=new[]{new Vector2(-.43f,.5f),new Vector2(.43f,.5f),new Vector2(.43f,1.84f),new Vector2(.30f,2.07f),new Vector2(.04f,2.13f),new Vector2(-.24f,2.08f),new Vector2(-.44f,1.90f)};
            var vs=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();
            for(int side=0;side<2;side++)foreach(var p in outline){vs.Add(new Vector3(p.x,p.y,side==0?-.22f:.22f));uv.Add(new Vector2(p.x+.5f,p.y*.45f));}
            int n=outline.Length;for(int i=1;i<n-1;i++){ts.Add(0);ts.Add(i+1);ts.Add(i);ts.Add(n);ts.Add(n+i);ts.Add(n+i+1);}
            for(int i=0;i<n;i++){int j=(i+1)%n;ts.Add(i);ts.Add(j);ts.Add(n+j);ts.Add(i);ts.Add(n+j);ts.Add(n+i);}
            steleMesh=new Mesh{name="Chipped ancient stele"};steleMesh.SetVertices(vs);steleMesh.SetUVs(0,uv);steleMesh.SetTriangles(ts,0);steleMesh.RecalculateNormals();steleMesh.RecalculateBounds();
            var slab=new GameObject("Ancient carved slab",typeof(MeshFilter),typeof(MeshRenderer));slab.layer=2;slab.transform.SetParent(transform,false);slab.GetComponent<MeshFilter>().sharedMesh=steleMesh;slab.GetComponent<MeshRenderer>().sharedMaterial=stone;
            Stroke("Fracture A",new Vector2(.28f,2.04f),new Vector2(.19f,1.66f),.013f,edge,-.224f);
            Stroke("Fracture B",new Vector2(.19f,1.66f),new Vector2(.31f,1.49f),.010f,edge,-.224f);
            Stroke("Fracture branch",new Vector2(.21f,1.75f),new Vector2(.37f,1.79f),.009f,edge,-.224f);
            Stroke("Lower fracture",new Vector2(-.41f,.76f),new Vector2(-.25f,.98f),.012f,edge,-.224f);
            for(int row=0;row<4;row++)for(int col=0;col<2;col++)
            {
                float x=-.19f+col*.33f,y=1.78f-row*.29f;
                Carve(new Vector2(x-.065f,y+.075f),new Vector2(x+.065f,y+.075f));
                Carve(new Vector2(x,y+.075f),new Vector2(x,y-.095f));
                Carve(new Vector2(x-.075f,y-.015f),new Vector2(x+.075f,y-.015f));
                Carve(new Vector2(x,y-.095f),new Vector2(x+(row%2==0?.067f:-.067f),y-.045f));
            }
            var light=new GameObject("Jade readiness glow",typeof(Light));light.transform.SetParent(transform,false);light.transform.localPosition=new Vector3(0,1.15f,-.35f);readyLight=light.GetComponent<Light>();readyLight.type=LightType.Point;readyLight.range=1.4f;readyLight.intensity=.18f;readyLight.color=new Color(.35f,.85f,.62f);readyLight.shadows=LightShadows.None;
        }
        void Carve(Vector2 a,Vector2 b){Stroke("Recessed rune groove",a,b,.038f,edge,-.226f);Stroke("Thin jade inlay",a,b,.012f,glow,-.23f);}
        void Stroke(string name,Vector2 a,Vector2 b,float width,Material material,float z)
        {var delta=b-a;var p=Piece(name,new Vector3((a.x+b.x)*.5f,(a.y+b.y)*.5f,z),new Vector3(width,delta.magnitude,.005f),material);p.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg);}
        void UpdateReadyLight()
        {
            if(readyLight==null||Time.time<nextGlowCheck)return;nextGlowCheck=Time.time+.25f;
            if(glowPlayer==null)glowPlayer=FindAnyObjectByType<CampusExplorer>();
            readyLight.enabled=!Used&&glowPlayer!=null&&(glowPlayer.transform.position-transform.position).sqrMagnitude<36&&Safe&&LearningService.Instance?.Engine.LearnedPool().Count>0;
        }
    }
}
