using UnityEngine;

namespace CampusRift.AR
{
    // Final world-space guard for the original skill renderers. Only AR owns this component.
    // Meshes keep their shape; lines are clipped to the disc; escaped particles expire.
    [DefaultExecutionOrder(500)]
    public sealed class ARVisualBounds : MonoBehaviour
    {
        public ARCombatContext context;
        Renderer[] renderers;
        ParticleSystem[] particles;
        readonly ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[128];
        void Start()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            particles = GetComponentsInChildren<ParticleSystem>(true);
        }
        void LateUpdate()
        {
            if (context == null || context.battlefield == null || context.battlefield.Root == null) return;
            float radius = context.battlefield.placement.Radius * .96f;
            foreach (var r in renderers)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if(context.CameraEffect!=null&&r.transform.IsChildOf(context.CameraEffect))continue;
                if (r is LineRenderer line)
                {
                    float margin = Mathf.Min(radius * .2f, line.widthMultiplier * .5f);
                    for (int i = 0; i < line.positionCount; i++)
                    {
                        var p = line.GetPosition(i);
                        if (!line.useWorldSpace) p = line.transform.TransformPoint(p);
                        p = context.ConstrainVisual(p, margin);
                        line.SetPosition(i, line.useWorldSpace ? p : line.transform.InverseTransformPoint(p));
                    }
                }
                else if (r is MeshRenderer && r.GetComponent<TMPro.TMP_Text>() == null && r.name != "Ink hull" && r.name != "Rain sword ink")
                {
                    // Include both horizontal axes so rotated crystals and palms fit too.
                    var b = r.bounds;
                    float extent = new Vector2(b.extents.x, b.extents.z).magnitude;
                    if (extent > radius)
                    {
                        r.transform.localScale *= radius / extent;
                        b = r.bounds; extent = new Vector2(b.extents.x, b.extents.z).magnitude;
                    }
                    r.transform.position += context.ConstrainVisual(b.center, extent) - b.center;
                }
            }
            foreach (var ps in particles)
            {
                if (ps == null || !ps.gameObject.activeInHierarchy || ps.particleCount == 0) continue;
                if(context.CameraEffect!=null&&ps.transform.IsChildOf(context.CameraEffect))continue;
                var main = ps.main;
                int count = ps.GetParticles(buffer);
                for (int i = 0; i < count; i++)
                {
                    Vector3 p = main.simulationSpace == ParticleSystemSimulationSpace.World ? buffer[i].position : ps.transform.TransformPoint(buffer[i].position);
                    float margin = buffer[i].GetCurrentSize(ps) * .5f;
                    if ((context.ConstrainVisual(p, margin) - p).sqrMagnitude > .000001f) buffer[i].remainingLifetime = 0;
                }
                ps.SetParticles(buffer, count);
            }
        }
    }
}
