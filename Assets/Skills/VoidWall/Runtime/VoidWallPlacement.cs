using UnityEngine;

namespace CampusRift.Skills
{
    public struct WallPlacement
    {
        public bool valid;
        public Vector3 feet;
        public Quaternion rotation;
        public float width;
        public string reason;
        public bool assisted;
    }

    // Preview and deployment share this placement. It never refuses a location (quick casts must always
    // succeed), it only fits the wall: stops short of solid geometry, sits on the ground under its whole
    // span, fills corridors/doorways and, when a monster is ahead, turns square to its approach.
    public sealed class VoidWallPlacement
    {
        const float ChestHeight = 1.0f, Clearance = 0.18f, MinimumWidth = 1.1f, MaxStepUp = 1.4f, MaxStepDown = 1.8f;
        const float AssistRange = 14f, AssistCone = 55f;
        readonly RaycastHit[] hits = new RaycastHit[32];
        public Transform threat;

        public WallPlacement Evaluate(Vector3 origin, Vector3 forward, Transform owner, VoidWallConfig c)
            => Evaluate(origin, forward, owner, c, c.placementDistance);

        public WallPlacement Evaluate(Vector3 origin, Vector3 forward, Transform owner, VoidWallConfig c, float distance)
        {
            forward.y = 0;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.ProjectOnPlane(owner.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            distance = Mathf.Clamp(distance, c.minimumPlacementDistance, c.maximumPlacementDistance);

            // Threat assist: a monster roughly ahead gets the wall square across its path, between you and it.
            bool assisted = false;
            if (threat != null && threat.gameObject.activeInHierarchy)
            {
                var toThreat = Vector3.ProjectOnPlane(threat.position - origin, Vector3.up);
                float range = toThreat.magnitude;
                if (range > 0.6f && range < AssistRange && Vector3.Angle(forward, toThreat) < AssistCone)
                {
                    forward = toThreat / range;
                    distance = Mathf.Clamp(Mathf.Min(distance, range * 0.5f), c.minimumPlacementDistance, c.maximumPlacementDistance);
                    assisted = true;
                }
            }

            // Stop in front of solid geometry instead of dropping the wall behind it (or in the next room).
            Vector3 chest = origin + Vector3.up * ChestHeight;
            float reach = distance + c.thickness * 0.5f + Clearance;
            if (Cast(chest, forward, reach, 0.2f, owner, out float blocked))
                distance = Mathf.Clamp(blocked - c.thickness * 0.5f - Clearance, 0.35f, distance);

            var right = Vector3.Cross(Vector3.up, forward);
            var center = origin + forward * distance;
            float width = c.width, half = width * 0.5f;

            // Fit doorways and corridors: seal against side walls, never poke through them.
            Vector3 probe = new Vector3(center.x, chest.y, center.z);
            // Look a full width each way so a single side wall shifts the wall instead of shrinking it.
            Cast(probe, -right, width + 0.05f, 0.05f, owner, out float left);
            Cast(probe, right, width + 0.05f, 0.05f, owner, out float rightFree);
            if (left + rightFree < width)
            {
                width = Mathf.Max(MinimumWidth, left + rightFree + 0.06f);   // slight overlap seals the gap
                center += right * ((rightFree - left) * 0.5f);
            }
            else if (left < half) center += right * (half - left);
            else if (rightFree < half) center -= right * (half - rightFree);
            half = width * 0.5f;

            // Ground: sample under both ends and the middle; sit on the lowest so stairs and slopes leave no gap.
            float baseY = origin.y, lowest = float.PositiveInfinity;
            for (int s = 0; s < 3; s++)
                if (Ground(center + right * (s == 0 ? 0 : s == 1 ? -half * 0.8f : half * 0.8f), origin.y, owner, out float y)) lowest = Mathf.Min(lowest, y);
            if (!float.IsPositiveInfinity(lowest)) baseY = lowest + 0.02f;
            center.y = baseY;

            return new WallPlacement
            {
                valid = true,
                feet = center,
                rotation = Quaternion.LookRotation(forward),
                width = width,
                reason = assisted ? "LOCKED ON THREAT" : "READY",
                assisted = assisted
            };
        }

        bool Ground(Vector3 point, float referenceY, Transform owner, out float y)
        {
            y = 0;
            int count = Physics.RaycastNonAlloc(new Vector3(point.x, referenceY + MaxStepUp, point.z), Vector3.down, hits,
                MaxStepUp + MaxStepDown, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.normal.y <= 0.5f || hit.distance >= nearest || Ignored(hit.collider, owner)) continue;
                nearest = hit.distance; y = hit.point.y;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        bool Cast(Vector3 from, Vector3 direction, float length, float radius, Transform owner, out float distance)
        {
            distance = length;
            int count = Physics.SphereCastNonAlloc(from, radius, direction, hits, length, ~0, QueryTriggerInteraction.Ignore);
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                // Skip floors/ramps (they are walked on) and anything already overlapping the start point.
                if (hit.distance <= 0 || hit.normal.y > 0.6f || Ignored(hit.collider, owner)) continue;
                // Report the surface distance, not the sphere centre's travel.
                if (hit.distance + radius < distance) { distance = hit.distance + radius; any = true; }
            }
            return any;
        }

        bool Ignored(Collider collider, Transform owner)
        {
            if (collider == null || collider.transform.IsChildOf(owner)) return true;
            if (threat != null && collider.transform.IsChildOf(threat)) return true;
            if (collider.GetComponentInParent<Monsters.MonsterBrain>() != null) return true;   // monsters never shorten or narrow the wall
            if (collider.GetComponentInParent<VoidWall>() != null) return true;
            var body = collider.attachedRigidbody;
            return body != null && !body.isKinematic;
        }
    }
}
