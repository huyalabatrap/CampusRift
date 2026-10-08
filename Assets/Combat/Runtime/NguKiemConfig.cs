using UnityEngine;

namespace CampusRift.Combat
{
    // Ngự Kiếm Thuật: the basic attack (plan §6). Numbers here are the first tuning pass.
    [CreateAssetMenu(menuName = "Campus Rift/Combat/Ngự Kiếm Config")]
    public sealed class NguKiemConfig : ScriptableObject
    {
        public Element element = Element.Moc;
        [Header("Aim")]
        [Min(1)] public float range = 12f;
        [Range(10, 180)] public float coneAngle = 60f;
        [Min(1)] public float lockRange = 25f;
        [Header("Combo")]
        public float[] chainPercent = { 1f, 1f, 1.6f };
        [Min(0.1f)] public float comboWindow = 0.9f;
        [Min(0.05f)] public float swingInterval = 0.32f;
        [Header("Linh Lực")]
        [Min(0)] public float swingSpiritCost = 8f;
        [Min(0)] public float pierceSpiritCost = 20f;
        // Refund on a hit; small, so the basic attack is paid for rather than free.
        [Min(0)] public float hitSpiritRefund = 2f;
        [Header("Piercing sword (hold)")]
        [Min(0.1f)] public float holdSeconds = 0.8f;
        public float piercePercent = 2.5f;
        [Min(1)] public float pierceLength = 15f;
        [Min(0.1f)] public float pierceRadius = 0.6f;
        [Min(1)] public float pierceSpeed = 45f;
        [Header("Swords")]
        [Range(1, 6)] public int swordCount = 3;
        [Min(1)] public float flightSpeed = 30f;
        [Min(1)] public float returnSpeed = 26f;
        [Min(0.1f)] public float hitRadius = 0.6f;
        [Min(0.5f)] public float missDistance = 7f;
        [Header("Presentation")]
        [ColorUsage(false, true)] public Color bladeColor = new Color(.08f, .85f, .42f);
        [ColorUsage(false, true)] public Color tipColor = new Color(.55f, 1f, .75f);
        public Material bladeMaterial, trailMaterial;
        public AudioClip swingClip, hitClip;
    }
}
