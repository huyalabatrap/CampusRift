using System;
using UnityEngine;

namespace CampusRift
{
    [Serializable]
    public sealed class AutomaticDoorLeaf
    {
        public Transform transform;
        public Vector3 closedPosition;
        public Quaternion closedRotation = Quaternion.identity;
        public Vector3 slideOffset;
        public float swingAngle;

        public void Apply(float amount, float direction)
        {
            if (transform == null) return;
            float eased = Mathf.SmoothStep(0, 1, amount);
            transform.localPosition = closedPosition + slideOffset * eased;
            transform.localRotation = closedRotation * Quaternion.Euler(0, swingAngle * direction * eased, 0);
        }
    }

    [DefaultExecutionOrder(-75)]
    public sealed class CampusAutomaticDoor : MonoBehaviour
    {
        public CampusExplorer player;
        public AutomaticDoorLeaf[] leaves;
        public Bounds doorway;
        public Vector3 normal = Vector3.forward;
        [Min(0.5f)] public float openingDistance = 1.8f;
        [Min(0.5f)] public float keepingOpenDistance = 2.3f;
        [Min(0.1f)] public float animationSeconds = 0.65f;
        [Min(0f)] public float closeDelay = 1.2f;
        public float OpenAmount { get; private set; }
        public bool IsOpen => OpenAmount >= 0.999f;
        public bool IsClosed => OpenAmount <= 0.001f;

        CharacterController character;
        float timeUntilClose;
        float swingDirection = 1;
        float visitorUntil;
        Vector3 visitorPosition;
        Collider[] leafColliders;
        bool playerPassage;

        // A nearby Shaban can operate the same physical door as the player.
        public void RequestOpenFrom(Vector3 position)
        {
            if (position.y + 1.7f < doorway.min.y + .15f || position.y + .25f > doorway.max.y - .15f) return;
            if ((doorway.ClosestPoint(position + Vector3.up) - (position + Vector3.up)).sqrMagnitude > 25) return;
            visitorPosition = position;
            visitorUntil = Time.time + 0.5f;
        }

        void Awake()
        {
            if (player == null) player = FindAnyObjectByType<CampusExplorer>();
            if (player != null) character = player.GetComponent<CharacterController>();
            leafColliders = GetComponentsInChildren<Collider>();
            foreach (var leaf in leaves) leaf.Apply(0, 1);
        }

        void Update() => Tick(Mathf.Min(Time.deltaTime, 0.05f));

        public void Tick(float dt)
        {
            if (player == null || dt <= 0) return;
            bool visitor = Time.time < visitorUntil;
            bool nearby = PlayerNear(OpenAmount > 0 ? keepingOpenDistance : openingDistance) || visitor;
            bool obstructed = IsDoorwayBlocked();
            if (nearby || obstructed) timeUntilClose = closeDelay;
            else timeUntilClose -= dt;
            bool open = nearby || obstructed || timeUntilClose > 0;
            // An automatic leaf must not sweep the capsule into a wall or stair rail.
            // Keep its environment / vision collision; allow the admitted player through
            // while opening, and restore solid player collision once fully closed.
            SetPlayerPassage(open || !IsClosed);
            if (open && IsClosed)
            {
                // For a hinge along the door's left edge, positive rotation moves toward normal.
                // Always swing away from the person approaching from either side.
                Vector3 approach = visitor ? visitorPosition : player.transform.position;
                swingDirection = Vector3.Dot(approach - doorway.center, normal) >= 0 ? -1 : 1;
            }
            float amount = Mathf.MoveTowards(OpenAmount, open ? 1f : 0f, dt / animationSeconds);
            if (Mathf.Approximately(amount, OpenAmount)) return;
            OpenAmount = amount;
            foreach (var leaf in leaves) leaf.Apply(amount, swingDirection);
            Physics.SyncTransforms();
        }

        void SetPlayerPassage(bool allow)
        {
            if (playerPassage == allow || character == null || leafColliders == null) return;
            playerPassage = allow;
            foreach(var leaf in leafColliders)
                if(leaf != null) Physics.IgnoreCollision(character,leaf,allow);
        }

        void OnDisable() => SetPlayerPassage(false);

        bool PlayerNear(float distance)
        {
            Vector3 delta = player.transform.position - doorway.center;
            // A person descending a flight can be above the old foot-height band.
            // Use capsule overlap with the doorway height, without activating the next floor.
            Bounds body = character != null && character.enabled ? character.bounds
                : new Bounds(player.transform.position + Vector3.up * 0.85f, new Vector3(0.48f, 1.7f, 0.48f));
            if (body.max.y < doorway.min.y + 0.15f || body.min.y > doorway.max.y - 0.15f) return false;
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            float halfWidth = Mathf.Abs(tangent.x) * doorway.extents.x + Mathf.Abs(tangent.z) * doorway.extents.z;
            float side = Mathf.Max(0, Mathf.Abs(Vector3.Dot(delta, tangent)) - halfWidth);
            float forward = Vector3.Dot(delta, normal);
            // Start opening in time for a sprinting approach; moving away adds no range.
            Vector3 toward = Vector3.ProjectOnPlane(-delta, Vector3.up).normalized;
            float approachSpeed = Mathf.Max(0f, Vector3.Dot(player.PlanarVelocity, toward));
            distance += Mathf.Min(approachSpeed, player.runSpeed) * animationSeconds;
            return side * side + forward * forward <= distance * distance;
        }

        public bool IsDoorwayBlocked()
        {
            if (character == null || !character.enabled) return false;
            Bounds safety = doorway;
            safety.Expand(new Vector3(Mathf.Abs(normal.x) * 1.1f, 0.1f, Mathf.Abs(normal.z) * 1.1f));
            return safety.Intersects(character.bounds);
        }
    }
}
