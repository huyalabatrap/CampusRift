using System.Collections.Generic;
using UnityEngine;
using CampusRift.Skills;

namespace CampusRift.Monsters
{
    public sealed class MonsterPerception : MonoBehaviour
    {
        public MonsterAIConfig config;
        public CampusExplorer player;
        [SerializeField] bool canSeePlayer;
        [SerializeField] Vector3 observedPosition;
        [SerializeField] Vector3 observedVelocity;
        [SerializeField] bool observedGrounded;
        [SerializeField] float observedGravity = -25;
        [SerializeField] float distanceToPlayer = -1;
        [SerializeField] bool sightThroughGlass;
        public bool CanSeePlayer => canSeePlayer;
        public Vector3 ObservedPosition => observedPosition;
        public Vector3 ObservedVelocity => observedVelocity;
        public bool ObservedGrounded => observedGrounded;
        public float ObservedGravity => observedGravity;
        public float DistanceToPlayer => distanceToPlayer;
        // True when the current sighting only exists through glazing: seen, but not physically reachable.
        public bool SightThroughGlass => sightThroughGlass;
        public float ObservationTime { get; private set; } = -10000;
        [SerializeField] PlayerTrailHistory trail = new PlayerTrailHistory();
        public PlayerTrailHistory Trail => trail;
        public bool ObservedGrappling { get; private set; }
        public bool ObservedSwinging { get; private set; }
        public bool ObservedAirDashing { get; private set; }
        public Vector3 Eye => transform.position + Vector3.up * 1.5f;
        float nextVision;
        public bool TrackingPlayer => Time.time - ObservationTime <= config.TrackingRetention;
        public float PlayerVisionDistance => TrackingPlayer ? Mathf.Max(config.VisionDistance, config.TrackingDistance) : config.VisionDistance;
        public float PlayerVisionAngle => TrackingPlayer ? Mathf.Max(config.VisionAngle, config.TrackingAngle) : config.VisionAngle;
        CharacterController playerController;
        IPlayerMovementInfo movementInfo;
        readonly RaycastHit[] hits = new RaycastHit[64];
        public event System.Action SightUpdated;
        public event System.Action EvidenceUpdated;
        public int ScanRevision { get; private set; }
        public bool CanSeePhantom { get; private set; }
        public Vector3 PhantomPosition { get; private set; }
        public Vector3 PhantomVelocity { get; private set; }
        public float PhantomObservationTime { get; private set; } = -10000;
        public float DistanceToPhantom { get; private set; } = -1;
        public bool PhantomThroughGlass { get; private set; }

        // Glazing (windows, glass facades, glass door panes) stops bodies and dulls sound,
        // but light passes: vision treats it as transparent up to GlassVisionDistance.
        static readonly HashSet<EntityId> glass = new HashSet<EntityId>();
        static UnityEngine.SceneManagement.SceneHandle glassScene;
        static bool glassReady;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGlass() { glass.Clear(); glassReady = false; }

        void Awake()
        {
            if (player == null) player = FindAnyObjectByType<CampusExplorer>();
            if (player != null) playerController = player.GetComponent<CharacterController>();
            if (player != null) movementInfo = player.GetComponent<IPlayerMovementInfo>();
            EnsureGlass(gameObject.scene.handle);
        }

        static void EnsureGlass(UnityEngine.SceneManagement.SceneHandle scene)
        {
            if (glassReady && glassScene == scene) return;
            glassScene = scene; glassReady = true; glass.Clear();
            foreach (var collider in FindObjectsByType<Collider>())
                if (IsGlassName(collider.name)) glass.Add(collider.GetEntityId());
        }

        public static bool IsGlassName(string name) =>
            name.IndexOf("Glass", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Glazing", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.EndsWith("Window", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsGlass(Collider collider) => collider != null && glass.Contains(collider.GetEntityId());

        void Update()
        {
            if (config == null || Time.time < nextVision) return;
            nextVision = Time.time + config.VisionRefreshRate;
            Scan();
        }

        public void Scan()
        {
            ScanPlayer();
            ScanPhantom();
            ScanRevision++;
            EvidenceUpdated?.Invoke();
        }

        void ScanPlayer()
        {
            canSeePlayer = false;
            sightThroughGlass = false;
            distanceToPlayer = -1; // No live distance information while hidden.
            if (player == null || !player.gameObject.activeInHierarchy) return;
            Vector3 target = player.transform.position + Vector3.up * 1.1f;
            Vector3 delta = target - Eye;
            float range = PlayerVisionDistance;
            if (delta.sqrMagnitude > range * range) return;
            if (Vector3.Angle(transform.forward, delta) > PlayerVisionAngle * 0.5f) return;
            // Chest first; the head is still visible over railings, desks and low walls.
            bool chest = ClearSight(Eye, target, player.transform, out bool viaGlass);
            bool head = !chest && ClearSight(Eye, player.transform.position + Vector3.up * 1.6f, player.transform, out viaGlass);
            if (!chest && !head) return;
            canSeePlayer = true;
            sightThroughGlass = viaGlass;
            ObservationTime = Time.time;
            observedPosition = player.transform.position;
            trail.Observe(observedPosition,ObservationTime,config);
            observedVelocity = trail.SmoothedVelocity;
            observedGrounded = movementInfo != null ? movementInfo.IsGrounded : player.IsGrounded;
            observedGravity = movementInfo != null ? movementInfo.Gravity : player.gravity;
            ObservedGrappling = movementInfo != null && movementInfo.IsGrappling;
            ObservedSwinging = movementInfo != null && movementInfo.IsSwinging;
            ObservedAirDashing = movementInfo != null && movementInfo.IsAirDashing;
            distanceToPlayer = Vector3.Distance(transform.position, observedPosition);
            SightUpdated?.Invoke();
        }

        void ScanPhantom()
        {
            CanSeePhantom = false; DistanceToPhantom = -1; PhantomThroughGlass = false;
            var phantom = PhantomDecoy.Active;
            if (phantom == null || !phantom.Live) return;
            Vector3 target = phantom.transform.position + Vector3.up * 1.1f;
            Vector3 delta = target - Eye;
            // Both visible silhouettes use the same tracking cone during a chase.
            if (delta.sqrMagnitude > PlayerVisionDistance * PlayerVisionDistance ||
                Vector3.Angle(transform.forward, delta) > PlayerVisionAngle * .5f) return;
            bool chest = ClearSight(Eye, target, phantom.transform, out bool viaGlass);
            bool head = !chest && ClearSight(Eye, phantom.transform.position + Vector3.up * 1.6f,
                phantom.transform, out viaGlass);
            if (!chest && !head) return;
            CanSeePhantom = true; PhantomThroughGlass = viaGlass;
            PhantomPosition = phantom.transform.position;
            PhantomVelocity = phantom.Velocity;
            PhantomObservationTime = Time.time;
            DistanceToPhantom = Vector3.Distance(transform.position, PhantomPosition);
        }

        // Physical line: every collider blocks (used for sound, attacks and audio occlusion).
        public bool ClearLine(Vector3 from, Vector3 to, Transform ignoreTarget)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.001f) return true;
            int count = Physics.RaycastNonAlloc(from, delta.normalized, hits, delta.magnitude,
                config.EnvironmentMask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false; // Fail closed if the buffer overflows.
            for (int i = 0; i < count; i++)
            {
                Transform t = hits[i].transform;
                if (t.IsChildOf(transform) || (ignoreTarget != null && t.IsChildOf(ignoreTarget))) continue;
                return false;
            }
            return true;
        }

        // Visual line: like ClearLine, but glazing is transparent within GlassVisionDistance.
        public bool ClearSight(Vector3 from, Vector3 to, Transform ignoreTarget) => ClearSight(from, to, ignoreTarget, out _);

        public bool ClearSight(Vector3 from, Vector3 to, Transform ignoreTarget, out bool throughGlass)
        {
            throughGlass = false;
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.001f) return true;
            int count = Physics.RaycastNonAlloc(from, delta.normalized, hits, delta.magnitude,
                config.EnvironmentMask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform t = hits[i].transform;
                if (t.IsChildOf(transform) || (ignoreTarget != null && t.IsChildOf(ignoreTarget))) continue;
                if (IsGlass(hits[i].collider)) { throughGlass = true; continue; }
                return false;
            }
            return !throughGlass || delta.sqrMagnitude <= config.GlassVisionDistance * config.GlassVisionDistance;
        }

        // Would the player be seen standing at this point right now? Used for negative evidence.
        public bool CanSeePoint(Vector3 feet)
        {
            Vector3 target = feet + Vector3.up * 1.1f;
            Vector3 delta = target - Eye;
            float range = PlayerVisionDistance;
            if (delta.sqrMagnitude > range * range) return false;
            if (Vector3.Angle(transform.forward, delta) > PlayerVisionAngle * 0.5f) return false;
            return ClearSight(Eye, target, null);
        }

        public bool InViewCone(Vector3 point, float range)
        {
            Vector3 delta = point - Eye;
            if (delta.sqrMagnitude > range * range) return false;
            return Vector3.Angle(transform.forward, delta) <= config.VisionAngle * 0.5f;
        }
    }
}
