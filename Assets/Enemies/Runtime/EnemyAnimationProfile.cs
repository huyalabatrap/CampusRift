using System;
using UnityEngine;

namespace CampusRift.Enemies
{
    [CreateAssetMenu(menuName="Campus Rift/Enemy Animation Profile")]
    public sealed class EnemyAnimationProfile : ScriptableObject
    {
        public string modelId;
        public string idle="Idle_Breathe", attack="Attack_Light", special="Attack_Heavy";
        public float walkMetersPerSecond=1, runMetersPerSecond=3, attackImpactSeconds=.55f;
        public float spawnSeconds=2.47f, deathSeconds=2.47f, blendSeconds=.12f;
        public string[] headBones, footBones;
        public AnimationClip[] clips;
        public AnimationClip Clip(string state)
        {
            foreach(var c in clips) if(c!=null && (c.name==state || c.name==modelId+"_"+state)) return c;
            return null;
        }
    }
}
