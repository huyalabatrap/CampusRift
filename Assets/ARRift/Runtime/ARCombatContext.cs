using UnityEngine;
namespace CampusRift.AR
{
    // Per-object scope; never changes global time, unlocks, or the player profile.
    public sealed class ARCombatContext : MonoBehaviour
    {
        public event System.Action<double> FirstVfx;public void VfxStarted(){FirstVfx?.Invoke(GestureRecognizerBridge.Now);}
        public ARBattlefield battlefield;
        // Only the camera guard may leave the placed disc. Cleared when that pooled node is reused.
        public Transform CameraEffect;
        public float scale=1;
        public bool allUnlocked;
        public bool Paused=>battlefield!=null&&battlefield.Paused;
        public float Now=>battlefield!=null?battlefield.Clock:Time.time;
        public float VisualRadius(float radius)=>battlefield!=null?Mathf.Min(radius,battlefield.placement.Radius*.85f):radius;
        public Vector3 ConstrainVisual(Vector3 world,float margin=0)
        {
            if(battlefield==null||battlefield.Root==null)return world;
            Vector3 center=battlefield.Root.position,delta=world-center;
            var flat=Vector2.ClampMagnitude(new Vector2(delta.x,delta.z),Mathf.Max(0,battlefield.placement.Radius*.96f-margin));
            return new Vector3(center.x+flat.x,world.y,center.z+flat.y);
        }
        public Vector3 Constrain(Vector3 world)
        {
            if(battlefield==null||battlefield.Root==null)return world;
            var p=battlefield.Root.InverseTransformPoint(world);var flat=Vector2.ClampMagnitude(new Vector2(p.x,p.z),battlefield.placement.Radius/scale*.94f);
            return battlefield.Root.TransformPoint(new Vector3(flat.x,.005f,flat.y));
        }
    }
}
