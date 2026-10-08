using UnityEngine;

namespace CampusRift.Skills
{
    public enum DangerShape { Circle, Cone, Capsule }
    public struct DangerZone
    {
        public int token;
        public Vector3 center, direction;
        public float radius, angle, expires;
        public DangerShape shape;
        public bool Contains(Vector3 point)
        {
            Vector3 offset = Vector3.ProjectOnPlane(point - center, Vector3.up);
            if (Mathf.Abs(point.y - center.y) > 3) return false;
            if (shape == DangerShape.Capsule)
            {
                float distance = Mathf.Clamp(Vector3.Dot(offset, direction.normalized), 0, angle);
                return (offset - direction.normalized * distance).sqrMagnitude <= radius * radius;
            }
            if (offset.sqrMagnitude > radius * radius) return false;
            return shape != DangerShape.Cone || offset.sqrMagnitude < .01f || Vector3.Angle(direction, offset) <= angle * .5f;
        }
    }
    // Fixed slots; expiry is checked on reads, even while no registry MonoBehaviour exists.
    public static class DangerZoneRegistry
    {
        static readonly DangerZone[] zones = new DangerZone[64];
        static int sequence;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { System.Array.Clear(zones, 0, zones.Length); sequence = 0; }
        public static int Register(Vector3 center, float radius, float duration, DangerShape shape = DangerShape.Circle,
            Vector3 direction = default(Vector3), float angle = 90)
        {
            for (int i = 0; i < zones.Length; i++)
                if (zones[i].token == 0 || zones[i].expires <= Time.time)
                {
                    int token = ++sequence;
                    zones[i] = new DangerZone { token = token, center = center, radius = radius, expires = Time.time + duration,
                        shape = shape, direction = direction, angle = angle };
                    return token;
                }
            return 0;
        }
        public static void Remove(int token) { for (int i = 0; i < zones.Length; i++) if (zones[i].token == token) zones[i].token = 0; }
        public static int CopyActive(DangerZone[] destination)
        {
            int count = 0;
            for (int i = 0; i < zones.Length && count < destination.Length; i++)
                if (zones[i].token != 0 && zones[i].expires > Time.time) destination[count++] = zones[i];
            return count;
        }
        public static bool IsDangerous(Vector3 point)
        {
            for (int i = 0; i < zones.Length; i++) if (zones[i].token != 0 && zones[i].expires > Time.time && zones[i].Contains(point)) return true;
            return false;
        }
    }
}
