using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class SkillSet1VfxConfig : ScriptableObject
    {
        public Material surface, ink, additive, fork, particles, ghost;
        public Material stroke,groundMark;
        public Material layeredWide,layeredSmall,layeredRing;
        public Material layeredSlash;
        public Material accretion;
        public Material lotusSurface,fireBillow,copperMark,impactStar,slashStroke,reactionSurface,reactionStroke,reactionIce;
        public Material vortexDebris;
        public Material fireRibbon,frostMist,layeredLightning;
        public Material solidEmission,fireFlipbook,smokeFlipbook;
        public AudioClip lightningCast, lightningHit, fireCast, fireHit, iceCast, iceHit, bell, voidCast, voidHit, sword;
        public NguKiemConfig rainConfig;
        public UnityEngine.Audio.AudioMixerGroup audioOutput;
    }
}
