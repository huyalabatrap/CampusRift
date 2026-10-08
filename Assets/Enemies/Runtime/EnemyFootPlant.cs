using UnityEngine;
using System.Collections.Generic;
using CampusRift.Combat;
namespace CampusRift.Enemies
{
    // Generic rigs do not support Animator humanoid IK. Two-bone leg correction pins stance feet in world space.
    [DefaultExecutionOrder(180)]
    public sealed class EnemyFootPlant : MonoBehaviour
    {
        sealed class Leg {public Transform thigh,knee,foot;public Vector3 rest,target,lastWorld;public float upper,lower;public bool planted,hasWorld,independentFoot;public float lastPhase;}
        readonly List<Leg> legs=new List<Leg>();EnemyInstance owner;float phase;Vector3 prior;Transform hips;Vector3 hipsBind;
        public float MaxPlantSlip {get;private set;}
        public float MaxWorldSlide {get;private set;}
        public int PlantSamples {get;private set;}
        public bool EnabledForLocomotion {get;private set;}
        void Awake()
        {
            owner=GetComponent<EnemyInstance>();var all=GetComponentsInChildren<Transform>(true);
            hips=System.Array.Find(all,t=>t.name=="Hips"||t.name=="Hip");
            if(hips!=null)hipsBind=hips.localPosition;
            foreach(var t in all)if(t.name=="Foot.L"||t.name=="Foot.R"||t.name=="L_Foot"||t.name=="R_Foot"){
                var knee=t.parent;while(knee!=null && knee.name.Contains("Twist"))knee=knee.parent;
                // Quaternius Ninja uses independent animated foot controls under Root.
                bool independent=t.name.StartsWith("Foot.")&&knee!=null&&knee.name=="Root";
                if(independent)knee=System.Array.Find(all,b=>b.name=="LowerLeg."+t.name.Substring(5));
                var thigh=knee!=null?knee.parent:null;while(thigh!=null && thigh.name.Contains("Twist"))thigh=thigh.parent;
                if(knee==null||thigh==null)continue;
                legs.Add(new Leg{thigh=thigh,knee=knee,foot=t,independentFoot=independent,rest=transform.InverseTransformPoint(t.position),upper=Vector3.Distance(thigh.position,knee.position),lower=Vector3.Distance(knee.position,t.position)});
            }
            prior=transform.position;
        }
        void LateUpdate()
        {
            if(owner==null||owner.Animation==null||owner.Animation.Dead || owner.Status.Has(StatusType.Freeze)||owner.Status.Has(StatusType.Pulled)||owner.Status.Has(StatusType.Stun)||owner.Status.Has(StatusType.Shock)){ResetPlants();return;}
            string state=owner.Animation.CurrentState;EnabledForLocomotion=state!=null&&(state.StartsWith("Run")||state.StartsWith("Walk")||state.StartsWith("Strafe"));
            if(!EnabledForLocomotion || owner.archetype.isFlying){ResetPlants();return;}
            Vector3 velocity=owner.Motor.Velocity;float speed=velocity.magnitude;float dt=Time.deltaTime;
            if(speed<.1f){ResetPlants();return;}
            float length=legs.Count>0?(legs[0].upper+legs[0].lower):.6f;
            // The source rigs stand with nearly straight knees. A lowered pelvis supplies the
            // reach margin for a real forward stance without stretching either leg segment.
            if(hips!=null){hips.localPosition=hipsBind;hips.position-=Vector3.up*length*.18f;}
            float frequency=Mathf.Max(1.2f,speed/Mathf.Max(.25f,length*.95f));phase+=dt*frequency;
            for(int i=0;i<legs.Count;i++){
                var leg=legs[i];float p=Mathf.Repeat(phase+i*.5f,1);bool stance=p<.5f;
                Vector3 rest=transform.TransformPoint(leg.rest);Vector3 forward=velocity.normalized;
                float stride=Mathf.Min(length*.5f,speed/frequency*.5f);
                if(stance){if(!leg.planted || p<leg.lastPhase){leg.target=Ground(rest+forward*stride);leg.planted=true;leg.hasWorld=false;}}
                else {leg.planted=false;leg.hasWorld=false;leg.target=Ground(rest+forward*Mathf.Lerp(-stride,stride,(p-.5f)*2));leg.target+=Vector3.up*Mathf.Sin((p-.5f)*2*Mathf.PI)*length*.18f;}
                Vector3 before=leg.foot.position;Solve(leg,leg.target,forward);
                if(stance){float residual=Vector3.Distance(leg.foot.position,leg.target);MaxPlantSlip=Mathf.Max(MaxPlantSlip,residual);PlantSamples++;
                    if(leg.hasWorld)MaxWorldSlide=Mathf.Max(MaxWorldSlide,Vector3.Distance(leg.lastWorld,leg.foot.position));leg.lastWorld=leg.foot.position;leg.hasWorld=true;}
                leg.lastPhase=p;
            }
            prior=transform.position;
        }
        Vector3 Ground(Vector3 point)
        {
            if(Physics.Raycast(point+Vector3.up,Vector3.down,out var hit,2.5f,~((1<<7)|(1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))point.y=hit.point.y+Mathf.Max(.015f,legs[0].rest.y);
            return point;
        }
        static void Solve(Leg l,Vector3 target,Vector3 forward)
        {
            Vector3 origin=l.thigh.position;Vector3 delta=target-origin;float distance=Mathf.Clamp(delta.magnitude,.02f,l.upper+l.lower-.001f);Vector3 direction=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(forward,direction).normalized;if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;
            float along=(l.upper*l.upper+distance*distance-l.lower*l.lower)/(2*distance);float height=Mathf.Sqrt(Mathf.Max(0,l.upper*l.upper-along*along));
            Vector3 knee=origin+direction*along+bend*height;
            l.thigh.rotation=Quaternion.FromToRotation(l.knee.position-origin,knee-origin)*l.thigh.rotation;
            l.knee.rotation=Quaternion.FromToRotation(l.foot.position-l.knee.position,target-l.knee.position)*l.knee.rotation;
            if(l.independentFoot)l.foot.position=target;
        }
        // Outside locomotion the Animator owns the pelvis (spawn, recoil, death and jump).
        // Resetting it to the bind pose each frame erases those authored movements.
        void ResetPlants(){foreach(var l in legs){l.planted=false;l.hasWorld=false;}prior=transform.position;EnabledForLocomotion=false;}
        public void ResetMetrics(){MaxPlantSlip=MaxWorldSlide=0;PlantSamples=0;}
        void OnDisable(){ResetPlants();if(hips!=null)hips.localPosition=hipsBind;phase=0;ResetMetrics();}
    }
}
