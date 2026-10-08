using UnityEngine;
using UnityEngine.Audio;

namespace CampusRift.Skills
{
    [CreateAssetMenu(menuName = "Campus Rift/Skills/Giant Hand Seal")]
    public sealed class GiantHandConfig : ScriptableObject
    {
        [Header("Combat")]
        [Min(1)] public float range = 16, radius = 2.8f, cooldown = 18;
        // V2: damage is a share of the player's Công (300%), element Thổ. damage/edgeDamage are legacy and unused.
        [Min(0)] public float damagePercent = 3f;
        public CampusRift.Combat.Element element = CampusRift.Combat.Element.Tho;
        [Min(0)] public float damage = 60, edgeDamage = 60, stagger = 3f;
        [Min(0.1f)] public float verticalTolerance = 1.35f;
        [Header("Sequence")]
        [Min(0.1f)] public float summonTime = 0.58f, descentTime = 0.18f, aftermathTime = 1.5f;
        public float maximumHeight = 5.5f;
        [Header("Presentation")]
        public Mesh handMesh, inlayMesh, planeMesh, shardMesh;
        public Material handMaterial, sigilMaterial, particleMaterial;
        public AudioMixerGroup output;
        public AudioClip cast, charge, rift, descent, impact, aftershock, dissipate, unavailable;
    }
}
