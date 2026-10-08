using UnityEngine;
using CampusRift.Monsters;

namespace CampusRift.Skills
{
    public struct HandSealTarget
    {
        public bool valid;
        public bool openSky;
        public Vector3 point, normal;
        public float height, visualScale;
        public string reason;
    }

    // Shared by preview, confirmation and impact. No NavMesh dependency.
    public sealed class GiantHandTargeting
    {
        static float Units(Transform owner){var ar=owner.GetComponent<AR.ARCombatContext>();return ar!=null?ar.scale:1;}
        readonly RaycastHit[] hits = new RaycastHit[128];
        readonly Collider[] blockers = new Collider[128];

        bool IsActor(Collider c, Transform owner)
        { return c.transform.IsChildOf(owner) || c.GetComponentInParent<MonsterBrain>() != null || c.GetComponentInParent<MonsterVitality>() != null; }

        public bool Cast(Vector3 start, Vector3 direction, float distance, Transform owner, out RaycastHit nearest)
        {
            nearest = default;
            if (distance <= 0.001f) return false;
            int count = Physics.RaycastNonAlloc(start, direction, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (IsActor(hits[i].collider, owner) || hits[i].distance >= best) continue;
                best = hits[i].distance; nearest = hits[i];
            }
            return best < float.PositiveInfinity;
        }

        public bool Clear(Vector3 from, Vector3 to, Transform owner)
        {
            Vector3 delta = to - from;
            return !Cast(from, delta.normalized, Mathf.Max(0, delta.magnitude - 0.08f*Units(owner)), owner, out _);
        }

        // Find the closest living monster that can actually receive a seal. A blocked
        // monster does not prevent locking another target in the same range.
        public HandSealTarget NearestMonster(Transform owner, GiantHandConfig config, out MonsterVitality monster)
        {
            monster = null;
            var result = Invalid(owner.position, "NO MONSTER IN RANGE");
            float closest = float.PositiveInfinity;
            // A locked-on monster takes priority when the seal can reach it.
            var lockOn = owner.GetComponent<Combat.TargetLock>();
            if (lockOn != null && lockOn.Current != null && lockOn.Current.isActiveAndEnabled && !lockOn.Current.Defeated &&
                (lockOn.Current.transform.position - owner.position).sqrMagnitude <= config.range * config.range)
            {
                var locked = Evaluate(lockOn.Current.transform.position, owner, config);
                if (locked.valid) { monster = lockOn.Current; return locked; }
            }
            for (int index = 0; index < MonsterVitality.Active.Count; index++)
            {
                var candidate = MonsterVitality.Active[index];
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.Defeated) continue;
                float distance = (candidate.transform.position - owner.position).sqrMagnitude;
                if (distance >= closest || distance > config.range * config.range) continue;
                var placement = Evaluate(candidate.transform.position, owner, config);
                if (!placement.valid) continue;
                closest = distance;
                monster = candidate;
                result = placement;
            }
            return result;
        }

        public HandSealTarget Aim(Camera camera, Transform owner, GiantHandConfig config)
            => Aim(camera,owner,config,new Vector2(.5f,.5f));
        public HandSealTarget Aim(Camera camera, Transform owner, GiantHandConfig config,Vector2 viewport)
        {
            var ray = camera != null ? camera.ViewportPointToRay(new Vector3(viewport.x,viewport.y,0))
                : new Ray(owner.position + Vector3.up * 1.2f, owner.forward + Vector3.down * 0.3f);
            Vector3 point;
            if (Cast(ray.origin, ray.direction, config.range + 8, owner, out var hit))
            {
                if (hit.normal.y > 0.5f) point = hit.point;
                else
                {
                    // A wall never becomes a portal into the room behind it.
                    Vector3 probe = hit.point - ray.direction * 0.3f;
                    if (!Cast(probe + Vector3.up * 0.1f, Vector3.down, 4, owner, out var floor))
                        return Invalid(hit.point, "AIM AT GROUND");
                    point = floor.point;
                }
            }
            else
            {
                Vector3 forward = Vector3.ProjectOnPlane(ray.direction, Vector3.up).normalized;
                point = owner.position + forward * 8;
            }
            return Evaluate(point, owner, config);
        }

        public HandSealTarget Evaluate(Vector3 point, Transform owner, GiantHandConfig config)
        {
            if ((point - owner.position).magnitude > config.range) return Invalid(point, "OUT OF RANGE");
            if (!Cast(point + Vector3.up * (0.35f*Units(owner)), Vector3.down, Units(owner), owner, out var floor) || floor.normal.y < 0.55f)
                return Invalid(point, "NEEDS A SURFACE");
            point = floor.point;
            var lift = floor.collider.GetComponentInParent<CampusElevator>();
            if (lift != null && lift.IsMoving) return Invalid(point, "MOVING LIFT");
            if (!Clear(owner.position + Vector3.up * (1.2f*Units(owner)), point + Vector3.up * (0.22f*Units(owner)), owner))
                return Invalid(point, "LINE OF SIGHT BLOCKED");

            float units=Units(owner);float scale = 1;
            // Keep the hand and overhead rift inside the available horizontal space.
            for (int i = 0; i < 8; i++)
            {
                Vector3 direction = Quaternion.Euler(0, i * 45, 0) * Vector3.forward;
                if (Cast(point + Vector3.up * (0.65f*units), direction, 2.3f*units, owner, out var side))
                    scale = Mathf.Min(scale, (side.distance - 0.12f*units) / (2.3f*units));
            }
            if (scale < 0.26f) return Invalid(point, "MOVE AWAY FROM WALL");
            float height = config.maximumHeight;
            bool openSky = true;
            for (int i = 0; i < 9; i++)
            {
                Vector3 offset = i == 0 ? Vector3.zero : Quaternion.Euler(0,(i-1)*45,0)*Vector3.forward*(2.1f*scale*units);
                if (Cast(point + offset + Vector3.up * (0.3f*units), Vector3.up, config.maximumHeight+units, owner, out var ceiling))
                {height = Mathf.Min(height, ceiling.distance - 0.25f*units);openSky=false;}
            }
            if (height < 1.05f*units) return Invalid(point, "NOT ENOUGH HEADROOM");
            // Rays alone miss thin pillars between samples. Check the complete swept
            // effect volume, shrinking the spectral hand instead of clipping a column.
            while (scale >= 0.26f && !FitsVolume(point,height,scale,owner)) scale *= 0.8f;
            if (scale < 0.26f) return Invalid(point, "MOVE AWAY FROM WALL");
            return new HandSealTarget { valid=true, point=point+Vector3.up*(0.035f*units), normal=floor.normal,
                height=height, visualScale=scale*units, openSky=openSky, reason="READY" };
        }

        bool FitsVolume(Vector3 point,float height,float scale,Transform owner)
        {
            float units=Units(owner);float bottom=0.45f*units,top=height+0.25f*units;
            int count=Physics.OverlapBoxNonAlloc(point+Vector3.up*((bottom+top)*0.5f),
                new Vector3(2.3f*scale*units,(top-bottom)*0.5f,2.3f*scale*units),blockers,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
            if(count==blockers.Length)return false;
            for(int i=0;i<count;i++)if(!IsActor(blockers[i],owner))return false;
            return true;
        }

        static HandSealTarget Invalid(Vector3 p, string reason)
        { return new HandSealTarget { point=p, normal=Vector3.up, reason=reason, visualScale=1 }; }
    }
}
