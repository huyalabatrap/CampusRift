using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace CampusRift.SkyBeast
{
    // Five authored clips per level. Gameplay still applies its hit exactly once through P14.
    [System.Serializable]
    public sealed class HeavenSwordShot : PlayableAsset, ITimelineClipAsset
    {
        public Vector3 from, to;
        public float beastWeight=.62f, fov=62;
        public bool followFall;
        public ClipCaps clipCaps=>ClipCaps.None;
        public override Playable CreatePlayable(PlayableGraph graph,GameObject owner)
        {
            var p=ScriptPlayable<HeavenSwordShotBehaviour>.Create(graph);
            var b=p.GetBehaviour();b.from=from;b.to=to;b.beastWeight=beastWeight;b.fov=fov;b.followFall=followFall;return p;
        }
    }
    public sealed class HeavenSwordShotBehaviour:PlayableBehaviour
    {
        public Vector3 from,to;public float beastWeight,fov;public bool followFall;
    }
    public sealed class HeavenSwordShotMixer:PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable,FrameData info,object playerData)
        {
            var cine=playerData as HeavenSwordCinematic;if(cine==null||!cine.Playing)return;
            for(int i=0;i<playable.GetInputCount();i++)if(playable.GetInputWeight(i)>0)
            {
                var clip=(ScriptPlayable<HeavenSwordShotBehaviour>)playable.GetInput(i);var b=clip.GetBehaviour();
                float t=Mathf.SmoothStep(0,1,(float)(clip.GetTime()/System.Math.Max(.001,clip.GetDuration())));
                cine.TimelineFrame(Vector3.Lerp(b.from,b.to,t),b.beastWeight,b.fov,b.followFall);break;
            }
        }
    }
}
