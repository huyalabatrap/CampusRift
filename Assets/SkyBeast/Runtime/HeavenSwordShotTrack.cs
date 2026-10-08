using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace CampusRift.SkyBeast
{
    [TrackClipType(typeof(HeavenSwordShot)),TrackBindingType(typeof(HeavenSwordCinematic)),TrackColor(1,.64f,.08f)]
    public sealed class HeavenSwordShotTrack:TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph,GameObject go,int inputCount)=>ScriptPlayable<HeavenSwordShotMixer>.Create(graph,inputCount);
    }
}
