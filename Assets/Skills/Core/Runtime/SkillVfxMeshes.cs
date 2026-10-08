using System.Collections.Generic;
using UnityEngine;
namespace CampusRift.Skills
{
    // Built once at pool warm-up. No imported model, collider or runtime material copy.
    public static class SkillVfxMeshes
    {
        public static Mesh Crystal()
        {
            var m = new Mesh { name = "P10 crystal" };
            m.vertices = new[] { new Vector3(0,1.6f,0),new Vector3(-.28f,.25f,-.25f),new Vector3(.28f,.25f,-.25f),new Vector3(.28f,.25f,.25f),new Vector3(-.28f,.25f,.25f),Vector3.zero };
            m.triangles = new[] {0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        public static Mesh Lotus()
        {
            var v = new List<Vector3>(); var t = new List<int>();var colors=new List<Color>();var uv=new List<Vector2>();
            for (int layer=0;layer<3;layer++) for (int p=0;p<8;p++)
            {
                float a=(p/8f+layer*.0625f)*Mathf.PI*2, radius=.65f-layer*.16f;
                Vector3 radial=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)), side=new Vector3(radial.z,0,-radial.x);
                int start=v.Count;
                for(int s=0;s<=8;s++)
                {
                    float f=s/8f, width=Mathf.Sin(f*Mathf.PI)*.26f;
                    Vector3 center=radial*(.06f+radius*f)+Vector3.up*(.1f+layer*.08f+f*f*.65f);
                    v.Add(center-side*width);v.Add(center);v.Add(center+side*width);colors.Add(new Color(.18f,.05f,.01f));colors.Add(Color.white);colors.Add(new Color(.18f,.05f,.01f));uv.Add(new Vector2(0,f));uv.Add(new Vector2(.5f,f));uv.Add(new Vector2(1,f));
                    if(s>0){int k=start+(s-1)*3;for(int half=0;half<2;half++){int a0=k+half;t.Add(a0);t.Add(a0+3);t.Add(a0+1);t.Add(a0+1);t.Add(a0+3);t.Add(a0+4);}}
                }
            }
            var mesh=Build("P10 24 petal lotus",v,t);mesh.SetColors(colors);mesh.SetUVs(0,uv);return mesh;
        }
        public static Mesh Bell()
        {
            var v=new List<Vector3>();var t=new List<int>();
            const int segments=40, levels=12;
            for(int y=0;y<=levels;y++)
            {
                float f=y/(float)levels, r=.18f+.72f*Mathf.Sqrt(Mathf.Max(0,1-f*f))+.24f*Mathf.Exp(-f*16);
                for(int i=0;i<=segments;i++){float a=i/(float)segments*Mathf.PI*2;v.Add(new Vector3(Mathf.Sin(a)*r,f*2.2f,Mathf.Cos(a)*r));}
            }
            for(int y=0;y<levels;y++)for(int i=0;i<segments;i++){int k=y*(segments+1)+i;t.Add(k);t.Add(k+segments+1);t.Add(k+1);t.Add(k+1);t.Add(k+segments+1);t.Add(k+segments+2);}
            return Build("P10 lathed bell",v,t);
        }
        public static Mesh Sphere()
        {
            var v=new List<Vector3>();var t=new List<int>();const int n=24, rings=12;
            for(int y=0;y<=rings;y++)for(int x=0;x<=n;x++){float a=x/(float)n*Mathf.PI*2,b=y/(float)rings*Mathf.PI;v.Add(new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a)));}
            for(int y=0;y<rings;y++)for(int x=0;x<n;x++){int k=y*(n+1)+x;t.Add(k);t.Add(k+1);t.Add(k+n+1);t.Add(k+1);t.Add(k+n+2);t.Add(k+n+1);}
            return Build("P10 sphere",v,t);
        }
        public static Mesh Disc()
        {
            var v=new List<Vector3>();var t=new List<int>();v.Add(Vector3.zero);
            for(int i=0;i<=48;i++){float a=i/48f*Mathf.PI*2;v.Add(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)));if(i>0){t.Add(0);t.Add(i+1);t.Add(i);}}
            return Build("P10 ground decal",v,t);
        }
        public static Mesh Annulus()
        {
            var v=new List<Vector3>();var t=new List<int>();const int segments=96,rows=6;
            for(int row=0;row<=rows;row++)for(int i=0;i<=segments;i++)
            {float a=i/(float)segments*Mathf.PI*2,r=Mathf.Lerp(.43f,1,row/(float)rows);v.Add(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}
            for(int row=0;row<rows;row++)for(int i=0;i<segments;i++)
            {int k=row*(segments+1)+i;t.Add(k);t.Add(k+1);t.Add(k+segments+1);t.Add(k+1);t.Add(k+segments+2);t.Add(k+segments+1);}
            return Build("P10 broad spiral accretion disc",v,t);
        }
        public static Mesh BellFragment()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int row=0;row<3;row++)for(int i=0;i<4;i++)
            {float a=(-35+i*70f/3)*Mathf.Deg2Rad;v.Add(new Vector3(Mathf.Sin(a),row*.38f,Mathf.Cos(a)-1));}
            for(int row=0;row<2;row++)for(int i=0;i<3;i++)
            {int k=row*4+i;t.Add(k);t.Add(k+1);t.Add(k+4);t.Add(k+1);t.Add(k+5);t.Add(k+4);}
            return Build("P10 curved bell metal fragment",v,t);
        }
        public static Mesh ImpactStar()
        {
            var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
            v.Add(Vector3.zero);colors.Add(new Color(4,3.6f,2,1));
            for(int i=0;i<=24;i++){float a=i/24f*Mathf.PI*2,r=i%2==0?(i%6==0?1:.7f):.22f;v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));colors.Add(i%2==0?new Color(.2f,.07f,.003f,1):new Color(2,1.4f,.03f,1));if(i>0){t.Add(0);t.Add(i);t.Add(i+1);}}
            var m=Build("Golden star impact",v,t);m.SetColors(colors);return m;
        }
        static Mesh Build(string name,List<Vector3> v,List<int> t){var m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
    }
}
