using UnityEngine;
using CampusRift.Monsters;
using CampusRift.Skills;

namespace CampusRift.Combat
{
    // Line-of-effect for player attacks: level geometry blocks, actors and the player's own Void Walls do not.
    public static class CombatLine
    {
        static readonly RaycastHit[] hits = new RaycastHit[32];
        // Everything except actors and attack volumes (layers set up in P00-T04).
        public static readonly int SolidMask = ~((1 << 6) | (1 << 7) | (1 << 8) | (1 << 9) | (1 << 10));

        // voidWallsBlock: monsters cannot see or shoot through the player's barriers; the player's own attacks can.
        public static bool Clear(Vector3 from, Vector3 to, Transform ignore = null, bool voidWallsBlock = false)
        {
            Vector3 delta = to - from; float distance = delta.magnitude;
            if (distance < 0.05f) return true;
            int count = Physics.RaycastNonAlloc(from, delta / distance, hits, distance, SolidMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = hits[i].collider;
                if (ignore != null && c.transform.IsChildOf(ignore)) continue;
                // Legacy monsters kept the Default layer until their prefab was moved: treat any monster body as an actor.
                if (c.GetComponentInParent<MonsterVitality>() != null) continue;
                if (!voidWallsBlock && c.GetComponentInParent<VoidWall>() != null) continue;
                return false;
            }
            return true;
        }

        // First solid hit along a segment (for piercing swords); false when the segment is free.
        public static bool Block(Vector3 from, Vector3 to, out Vector3 point)
        {
            point = to; Vector3 delta = to - from; float distance = delta.magnitude;
            if (distance < 0.001f) return false;
            int count = Physics.RaycastNonAlloc(from, delta / distance, hits, distance, SolidMask, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity; bool found = false;
            for (int i = 0; i < count; i++)
            {
                var c = hits[i].collider;
                if (c.GetComponentInParent<MonsterVitality>() != null || c.GetComponentInParent<VoidWall>() != null) continue;
                if (hits[i].distance < best) { best = hits[i].distance; point = hits[i].point; found = true; }
            }
            return found;
        }

        public static Vector3 Chest(MonsterVitality monster)
        {
            var r = monster.GetComponentInChildren<Renderer>();
            return r != null ? new Vector3(monster.transform.position.x, Mathf.Lerp(monster.transform.position.y, r.bounds.max.y, 0.6f), monster.transform.position.z)
                             : monster.transform.position + Vector3.up * 1.1f;
        }
    }
}
