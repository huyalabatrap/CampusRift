using UnityEngine;
using UnityEngine.Audio;

namespace CampusRift.Skills
{
    [CreateAssetMenu(menuName = "Campus Rift/Skills/Void Wall")]
    public sealed class VoidWallConfig : ScriptableObject
    {
        [Header("Gameplay")]
        public int charges = 3;
        // One charge returns every rechargeSeconds while below the maximum (0 = never).
        public float rechargeSeconds = 12f;
        public float width = 2.8f, height = 2.35f, thickness = 0.24f;
        public float placementDistance = 2.7f, minimumPlacementDistance = 0.8f, maximumPlacementDistance = 6f;
        public float lifetime = 12f, health = 75f, deployCooldown = 0.35f;
        // Barrier health as a share of the caster's max health (0.4 = 40%); health above is the fallback.
        [Range(0.05f, 2f)] public float healthShare = 0.4f;
        [Header("Presentation")]
        public Material wallMaterial, particleMaterial;
        public AudioMixerGroup output;
        public AudioClip preview, deploy, hum, impact, unstable, broken, expire, empty;
    }
}
