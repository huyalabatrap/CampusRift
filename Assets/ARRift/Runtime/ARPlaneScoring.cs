using System.Collections.Generic;
using UnityEngine;
namespace CampusRift.AR
{
    // Pure geometry: also used by the placement validation harness.
    public static class ARPlaneScoring
    {
        public static bool DiscFits(IList<Vector2> polygon,Vector2 center,float radius,float margin=.01f)=>polygon!=null&&polygon.Count>=3&&Inside(polygon,center)&&EdgeDistance(polygon,center)+1e-5f>=radius+margin;
        public static float Area(IList<Vector2> p) { float a=0;for(int i=0;i<p.Count;i++){var b=p[(i+1)%p.Count];a+=p[i].x*b.y-b.x*p[i].y;}return Mathf.Abs(a)*.5f; }
        public static bool Inside(IList<Vector2> p,Vector2 q)
        { bool inside=false;for(int i=0,j=p.Count-1;i<p.Count;j=i++){if((p[i].y>q.y)!=(p[j].y>q.y)&&q.x<(p[j].x-p[i].x)*(q.y-p[i].y)/(p[j].y-p[i].y)+p[i].x)inside=!inside;}return inside; }
        public static float EdgeDistance(IList<Vector2> p,Vector2 q)
        { float d=float.MaxValue;for(int i=0;i<p.Count;i++){var a=p[i];var v=p[(i+1)%p.Count]-a;d=Mathf.Min(d,Vector2.Distance(q,a+v*Mathf.Clamp01(Vector2.Dot(q-a,v)/Mathf.Max(.000001f,v.sqrMagnitude))));}return d; }
        public static Vector2 Incenter(IList<Vector2> p,out float radius)
        {
            radius=0;if(p.Count<3)return Vector2.zero;var lo=p[0];var hi=p[0];foreach(var q in p){lo=Vector2.Min(lo,q);hi=Vector2.Max(hi,q);}var best=(lo+hi)*.5f;
            // Bound work on enormous tracked floors, retaining 10 cm sampling for normal rooms.
            float step=Mathf.Max(.1f,Mathf.Max(hi.x-lo.x,hi.y-lo.y)/100f);
            for(float x=lo.x;x<=hi.x;x+=step)for(float y=lo.y;y<=hi.y;y+=step){var q=new Vector2(x,y);if(!Inside(p,q))continue;float d=EdgeDistance(p,q);if(d>radius){radius=d;best=q;}}
            return best;
        }
        public static float Convexity(IList<Vector2> p)
        {
            if(p.Count<3)return 0;var sorted=new List<Vector2>(p);sorted.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));var hull=new List<Vector2>();
            foreach(var q in sorted){while(hull.Count>=2&&Cross(hull[hull.Count-1]-hull[hull.Count-2],q-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);hull.Add(q);}int lower=hull.Count;
            for(int i=sorted.Count-2;i>=0;i--){var q=sorted[i];while(hull.Count>lower&&Cross(hull[hull.Count-1]-hull[hull.Count-2],q-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);hull.Add(q);}hull.RemoveAt(hull.Count-1);
            return Area(p)/Mathf.Max(.001f,Area(hull));
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        public static float Score(float area,float minimum,float distance,float downAngle,float convexity,float facing)
        { if(area<minimum||distance<.25f||distance>3f||downAngle<5||downAngle>85||convexity<.6f||facing<=0)return -1;return area*2f+3f-distance+facing+convexity; }
    }
}
