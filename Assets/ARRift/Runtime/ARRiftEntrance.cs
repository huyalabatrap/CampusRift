using UnityEngine;
using UnityEngine.AI;
using CampusRift.Enemies;
namespace CampusRift.AR
{
    // A visual entrance, followed by landing on the existing horizontal combat disc.
    public sealed class ARRiftEntrance : MonoBehaviour
    {
        ARBattlefield field; ARMinionBrain brain; EnemyInstance owner; NavMeshAgent agent;
        Vector3 start,landing; float born,duration; bool active; Collider[] colliders;
        public void Begin(ARBattlefield value,Vector3 origin,bool wall)
        {
            Cancel();field=value;owner=GetComponent<EnemyInstance>();brain=GetComponent<ARMinionBrain>();agent=GetComponent<NavMeshAgent>();
            start=origin;landing=value.Root.position+Vector3.ProjectOnPlane(origin-value.Root.position,Vector3.up).normalized*value.placement.Radius*.82f;
            born=value.Clock;duration=wall?1.6f:1.1f;active=true;brain.enabled=false;if(agent!=null)agent.enabled=false;
            colliders=GetComponentsInChildren<Collider>();foreach(var c in colliders)c.enabled=false;transform.position=start;
        }
        void Update()
        {
            if(!active||field==null||field.Paused)return;
            if(!owner.Alive){Cancel();return;}
            float u=Mathf.Clamp01((field.Clock-born)/duration);
            // First emerge from the wall, then fall. Nav/gravity/skill ranges only resume after landing.
            var emerge=start+Vector3.ProjectOnPlane(landing-start,Vector3.up).normalized*.2f*field.Scale;
            transform.position=u<.3f?Vector3.Lerp(start,emerge,u/.3f):Vector3.Lerp(emerge,landing,Mathf.Pow((u-.3f)/.7f,2))+Vector3.up*Mathf.Sin((u-.3f)/.7f*Mathf.PI)*.2f*field.Scale;
            if(u>=1){active=false;foreach(var c in colliders)if(c!=null)c.enabled=true;bool ranged=brain.Ranged;brain.Configure(field);brain.SetRanged(ranged);brain.enabled=true;}
        }
        public void Cancel(){if(!active)return;active=false;if(brain!=null)brain.enabled=true;if(colliders!=null)foreach(var c in colliders)if(c!=null)c.enabled=true;}
        void OnDisable(){Cancel();}
    }
}
