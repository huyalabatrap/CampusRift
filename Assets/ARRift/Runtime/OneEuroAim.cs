using UnityEngine;
namespace CampusRift.AR
{
    public sealed class OneEuroAim
    {
        Vector2 raw,value,velocity;double time=-1;
        static float Alpha(float cutoff,float dt)=>1/(1+1/(2*Mathf.PI*cutoff*dt));
        public Vector2 Filter(Vector2 next,double now)
        {
            if(time<0||now-time>.5){raw=value=next;velocity=Vector2.zero;time=now;return value;}
            float dt=Mathf.Max(.001f,(float)(now-time));velocity=Vector2.Lerp(velocity,(next-raw)/dt,Alpha(1,dt));
            value=Vector2.Lerp(value,next,Alpha(1.4f+.02f*velocity.magnitude,dt));raw=next;time=now;return value;
        }
        public void Reset(){time=-1;}
    }
}
