using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using CampusRift.Monsters;
namespace CampusRift.AR
{
    [DefaultExecutionOrder(-80)]
    public sealed class ARDepthCollision : MonoBehaviour
    {
        public const int RayBudget=10;
        ARBattlefield field;ARTechSettings settings;ARRaycastManager rays;AROcclusionManager occlusion;
        readonly List<ARRaycastHit> hits=new List<ARRaycastHit>(4);readonly Dictionary<MonsterVitality,bool> hidden=new Dictionary<MonsterVitality,bool>();
        int budgetFrame=-1,used,actorIndex;float nextActor;EnvironmentDepthMode prior;bool ownsMode;
        public int RaysThisFrame=>budgetFrame==Time.frameCount?used:0;
        public bool Supported=>occlusion!=null&&occlusion.subsystem!=null&&occlusion.subsystem.subsystemDescriptor.environmentDepthImageSupported==UnityEngine.XR.ARSubsystems.Supported.Supported&&rays!=null&&rays.subsystem!=null&&(rays.subsystem.subsystemDescriptor.supportedTrackableTypes&TrackableType.Depth)!=0;
        public bool Active=>settings!=null&&settings.DepthCollision&&settings.Quality!=null&&settings.Quality.DepthCollisionAllowed&&Supported&&occlusion.subsystem.running&&rays.subsystem.running&&field.Root!=null&&!field.Paused;
        void Start(){field=GetComponent<ARBattlefield>();settings=GetComponent<ARTechSettings>();rays=field.placement.planes.GetComponent<ARRaycastManager>();occlusion=field.placement.view.GetComponent<AROcclusionManager>();}
        void Update()
        {
            if(Active&&!ownsMode){prior=occlusion.requestedEnvironmentDepthMode;ownsMode=true;occlusion.requestedEnvironmentDepthMode=EnvironmentDepthMode.Fastest;}
            if(!Active){hidden.Clear();Restore();return;}
            if(Time.unscaledTime<nextActor)return;nextActor=Time.unscaledTime+.04f;
            var actors=field.GetComponent<ARMonsterDirector>().Actors;if(actors.Count==0)return;actorIndex%=actors.Count;var e=actors[actorIndex++];
            if(e==null||!e.Alive)return;Vector3 target=e.transform.position+Vector3.up*field.Scale;
            hidden[e.Vitality]=OccludedAt(target);
        }
        public bool OccludedAt(Vector3 world)=>SurfaceAt(world,out var hit)&&Vector3.Distance(field.placement.view.transform.position,hit)+.06f<Vector3.Distance(field.placement.view.transform.position,world);
        public bool Hidden(MonsterVitality actor)=>Active&&actor!=null&&hidden.TryGetValue(actor,out bool value)&&value;
        public bool SurfaceAt(Vector3 world,out Vector3 surface)
        {
            surface=world;if(!Active)return false;if(budgetFrame!=Time.frameCount){budgetFrame=Time.frameCount;used=0;}if(used>=RayBudget)return false;
            var p=field.placement.view.WorldToScreenPoint(world);if(p.z<=0||p.x<0||p.x>Screen.width||p.y<0||p.y>Screen.height)return false;
            used++;hits.Clear();if(!rays.Raycast(new Vector2(p.x,p.y),hits,TrackableType.Depth)||hits.Count==0)return false;
            surface=hits[0].pose.position;return Vector3.Distance(field.placement.view.transform.position,surface)>=.15f;
        }
        public bool Sweep(Vector3 from,Vector3 to,out Vector3 contact)
        {
            contact=to;if(!Active)return false;Vector3 delta=to-from;
            // Depth is a view-dependent surface, never used as a placement polygon.
            for(int i=0;i<2;i++)if(SurfaceAt(i==0?to:Vector3.Lerp(from,to,.5f),out var point))
            {float t=delta.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(point-from,delta)/delta.sqrMagnitude):0;if(Vector3.Distance(point,from+delta*t)<=.045f){contact=point;return true;}}
            return false;
        }
        void Restore(){if(ownsMode&&occlusion!=null)occlusion.requestedEnvironmentDepthMode=prior;ownsMode=false;}
        void OnDisable(){Restore();hidden.Clear();}
    }
}
