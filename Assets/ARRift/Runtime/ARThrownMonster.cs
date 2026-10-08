using UnityEngine;
using UnityEngine.AI;
using CampusRift.Enemies;
using CampusRift.Combat;
namespace CampusRift.AR
{
    // Body remains kinematic outside the single lifted/thrown lifecycle, including pooled reuse.
    public sealed class ARThrownMonster : MonoBehaviour
    {
        public bool Active {get;private set;}public bool Flying {get;private set;}
        ARBattlefield field;ARSkillCaster caster;EnemyInstance owner;ARMinionBrain brain;Rigidbody body;
        Collider[] colliders;bool[] colliderStates;Vector3 landing,prior;float born;bool ranged;
        public void Lift(ARBattlefield value,ARSkillCaster source)
        {
            field=value;caster=source;owner=GetComponent<EnemyInstance>();brain=GetComponent<ARMinionBrain>();
            landing=transform.position;ranged=brain.Ranged;brain.enabled=false;
            var agent=GetComponent<NavMeshAgent>();if(agent!=null)agent.enabled=false;
            colliders=GetComponentsInChildren<Collider>();colliderStates=new bool[colliders.Length];
            for(int i=0;i<colliders.Length;i++){colliderStates[i]=colliders[i].enabled;colliders[i].enabled=false;}
            body=GetComponent<Rigidbody>()??gameObject.AddComponent<Rigidbody>();body.isKinematic=false;body.useGravity=false;body.detectCollisions=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.constraints=RigidbodyConstraints.FreezeRotation;
            born=value.Clock;Active=true;Flying=false;enabled=true;prior=transform.position;
        }
        public void Release(Vector3 velocity,bool throwAllowed)
        {
            if(!Active)return;
            if(!throwAllowed||velocity.magnitude<.55f){Restore();return;}
            Flying=true;born=field.Clock;body.linearVelocity=Vector3.ClampMagnitude(velocity*field.Scale*5,field.Scale*9)+Vector3.up*field.Scale*1.8f;prior=transform.position;
        }
        void FixedUpdate()
        {
            if(!Active||field==null)return;
            if(field.Paused||!owner.Alive||field.GetComponent<ARMonsterDirector>().Finished){Restore();return;}
            if(!Flying)
            {
                Vector3 target=caster.AimValid?caster.Aim:landing;
                target=field.Root.position+Vector3.ClampMagnitude(Vector3.ProjectOnPlane(target-field.Root.position,Vector3.up),field.placement.Radius*.85f)+Vector3.up*field.Scale*1.5f;
                body.MovePosition(Vector3.Lerp(body.position,target,.25f));
                if(field.Clock-born>5)Restore();return;
            }
            body.AddForce(Vector3.down*9.81f*field.Scale,ForceMode.Acceleration);
        }
        void Update()
        {
            if(!Active||!Flying)return;
            if(field==null||field.Root==null||field.Paused||!owner.Alive){Restore();return;}
            var delta=transform.position-prior;var depth=field.GetComponent<ARDepthCollision>();if(depth!=null&&depth.Sweep(prior,transform.position,out var contact)){transform.position=contact;Restore();return;}
            foreach(var other in field.GetComponent<ARMonsterDirector>().Actors)
            {
                if(other==null||other==owner||!other.Alive)continue;
                Vector3 center=other.transform.position+Vector3.up*.75f*field.Scale;
                float t=delta.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(center-prior,delta)/delta.sqrMagnitude):0;
                if(Vector3.Distance(prior+delta*t,center)>.75f*field.Scale)continue;
                var hit=DamageInfo.Create(owner.Damage*3,Element.Kim,DamageSource.Skill,center,delta.normalized,caster.Caster);hit.skillId="ar-hand-throw";hit.isHeavy=true;
                other.Vitality.ApplyDamage(hit);Restore();return;
            }
            prior=transform.position;
            bool outside=Vector3.ProjectOnPlane(transform.position-field.Root.position,Vector3.up).magnitude>field.placement.Radius*1.08f;
            if(outside){Vanish();return;}
            if(transform.position.y<=field.Root.position.y+.05f*field.Scale||field.Clock-born>2.5f)Restore();
        }
        void Vanish()
        {
            Restore(false);foreach(var c in colliders)if(c!=null)c.enabled=true;brain.enabled=true;
            var elite=GetComponent<AREliteAffix>();bool eliteEnabled=elite!=null&&elite.enabled;if(elite!=null)elite.enabled=false;
            // Existing vitality/death path counts the disposal and releases the actor through its normal pool.
            var hit=DamageInfo.Create(owner.MaxHealth*100,Element.None,DamageSource.Environment,transform.position,Vector3.up,caster.Caster);hit.skillId="ar-off-disc";
            try{owner.Vitality.ApplyDamage(hit);}finally{if(elite!=null)elite.enabled=eliteEnabled;}
        }
        public void Restore(bool place=true)
        {
            if(!Active)return;Active=Flying=false;
            if(body!=null){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;body.detectCollisions=false;}
            if(colliders!=null)for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=colliderStates[i];
            if(place&&owner!=null&&owner.Alive&&field!=null&&field.Root!=null)
            {transform.position=landing;brain.Configure(field);brain.SetRanged(ranged);}
            if(brain!=null)brain.enabled=true;
        }
        void OnDisable(){Restore();}
    }
}
