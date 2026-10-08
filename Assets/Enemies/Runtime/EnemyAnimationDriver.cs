using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;

namespace CampusRift.Enemies
{
    // The gameplay owns translation. This driver owns clip selection and the windup/impact clock.
    [DefaultExecutionOrder(80), DisallowMultipleComponent]
    public sealed class EnemyAnimationDriver : MonoBehaviour
    {
        CampusRift.AR.ARCombatContext arContext;
        float SessionNow => arContext!=null?arContext.Now:Time.time;

        public EnemyAnimationProfile profile;
        public Animator Animator {get;private set;}
        public string CurrentState {get;private set;}
        public float AttackImpactAt {get;private set;}
        public float PlaybackRate {get;private set;}=1;
        public bool Dead {get;private set;}
        public float AirborneHeight {get;set;}
        public float SpawnSeconds=>profile!=null?profile.spawnSeconds:1;
        public float DeathSeconds=>profile!=null?profile.deathSeconds:.9f;
        EnemyInstance owner; StatusEffectHost status; MinionMotor motor;
        Transform[] heads; Transform hips; Quaternion[] headRest; float lockedUntil, nextTaunt, lastYaw,appliedAirborneHeight;
        string controlState; Vector3 priorPosition; MaterialPropertyBlock block;
        Renderer[] renderers;

        void Awake()
        {
            arContext=GetComponent<CampusRift.AR.ARCombatContext>();
            Animator=GetComponentInChildren<Animator>(); owner=GetComponent<EnemyInstance>();
            status=GetComponent<StatusEffectHost>(); motor=GetComponent<MinionMotor>();
            renderers=GetComponentsInChildren<Renderer>(true); block=new MaterialPropertyBlock();
            hips=System.Array.Find(GetComponentsInChildren<Transform>(true),t=>t.name=="Hips"||t.name=="Hip");
            if(Animator!=null){Animator.applyRootMotion=false;Animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;}
            if(profile!=null && profile.headBones!=null)
            {
                heads=new Transform[profile.headBones.Length];headRest=new Quaternion[heads.Length];
                var all=GetComponentsInChildren<Transform>(true);
                for(int i=0;i<heads.Length;i++)foreach(var t in all)if(t.name==profile.headBones[i]){heads[i]=t;headRest[i]=t.localRotation;break;}
            }
        }
        public void ResetLife()
        {
            Dead=false;AirborneHeight=appliedAirborneHeight=0;lockedUntil=0;controlState=null;priorPosition=transform.position;lastYaw=transform.eulerAngles.y;
            nextTaunt=SessionNow+Random.Range(9,15);Dissolve(0);
            if(Animator!=null){Animator.speed=1;Animator.Rebind();Animator.Update(0);}
            CurrentState=null;Play("Spawn",1,SpawnSeconds);
        }
        public bool HasState(string state)=>Animator!=null && Animator.HasState(0,UnityEngine.Animator.StringToHash("Base Layer."+state));
        public void Play(string state,float rate=1,float hold=0)
        {
            if(Animator==null || !HasState(state))return;
            CurrentState=state;PlaybackRate=Mathf.Max(.01f,rate);Animator.speed=PlaybackRate;
            Animator.CrossFadeInFixedTime(state,profile!=null?profile.blendSeconds:.12f,0,0);
            lockedUntil=SessionNow+hold;
        }
        public void BeginAttack(float windup,string state=null,float authoredImpact=-1)
        {
            state=string.IsNullOrEmpty(state)?profile.attack:state;
            float impact=authoredImpact>0?authoredImpact:profile.attackImpactSeconds;
            float rate=impact/Mathf.Max(.05f,windup);var clip=profile.Clip(state);
            // Fixed-time blend is part of the strike clock; start at clip time zero.
            Play(state,rate,clip!=null?clip.length/rate:windup+.5f);
            AttackImpactAt=SessionNow+windup;
        }
        public void Die(Vector3 incoming)
        {
            Dead=true;controlState=null;
            Play(Vector3.Dot(transform.forward,incoming)>0?"Death_Forward":"Death_Backward",1,DeathSeconds+1);
        }
        public void Hit(DamageInfo hit)
        {
            if(Dead || owner==null || owner.Vitality.Defeated || SessionNow<lockedUntil)return;
            if(status!=null&&(status.Has(StatusType.Freeze)||status.Has(StatusType.Pulled)||status.Has(StatusType.Stun)||status.Has(StatusType.Shock)))return;
            // A control pose can expire just before damage is delivered. Its next
            // Update must not erase the fresh recoil at the end of a pull/expulsion.
            controlState=null;
            var local=transform.InverseTransformDirection(-hit.direction);
            string side=Mathf.Abs(local.x)>Mathf.Abs(local.z)?(local.x>0?"Right":"Left"):(local.z>0?"Front":"Back");
            bool heavy=hit.isHeavy||hit.source==DamageSource.Reaction;
            Play(heavy?"Stagger":"Hit_"+side,1,heavy?.45f:.32f);
        }
        void Update()
        {
            if(arContext!=null&&arContext.Paused)return;
            // Undo our previous additive pose even when Animator transforms are culled.
            if(hips!=null&&appliedAirborneHeight!=0)hips.position-=Vector3.up*appliedAirborneHeight;
            appliedAirborneHeight=0;
            if(Animator==null || profile==null || Dead)return;
            if(heads!=null)for(int i=0;i<heads.Length;i++)if(heads[i]!=null)heads[i].localRotation=headRest[i];
            if(status!=null && status.Has(StatusType.Freeze)){Animator.speed=0;return;}
            string control=status!=null && status.Has(StatusType.Pulled)?"Stagger":
                status!=null&&(status.Has(StatusType.Stun)||status.Has(StatusType.Shock))?"Stun":null;
            if(control!=null){if(controlState!=control){controlState=control;Play(control,1);}else if(control=="Stagger"&&Animator.GetCurrentAnimatorStateInfo(0).normalizedTime>.92f)Play(control,1);Animator.speed=1;return;}
            if(controlState!=null){controlState=null;lockedUntil=0;CurrentState=null;}
            float slow=status!=null?status.SpeedMultiplier:1;
            if(SessionNow<lockedUntil){Animator.speed=PlaybackRate*slow;return;}
            var flying=GetComponent<FlyingMotor>();
            Vector3 velocity=arContext!=null?GetComponent<CampusRift.AR.ARMinionBrain>().Velocity/arContext.scale:flying!=null?flying.Velocity:motor!=null?motor.Velocity:(transform.position-priorPosition)/Mathf.Max(Time.deltaTime,.001f);
            priorPosition=transform.position;float speed=Vector3.ProjectOnPlane(velocity,Vector3.up).magnitude;
            float turn=Mathf.DeltaAngle(lastYaw,transform.eulerAngles.y);lastYaw=transform.eulerAngles.y;
            string desired;float rate;
            if(speed<.08f){desired=profile.idle;rate=slow;
                var brain=GetComponent<MinionBrain>();
                var squad=EnemyDirector.Instance?.Squad;
                if(squad!=null&&squad.TryGet(owner,out var assignment)&&assignment.guarding)
                {desired="Idle_Alert";if(CurrentState!=desired)Play(desired,slow);return;}
                if(brain!=null&&brain.WaitingAtDoor){desired=brain.GuardingDoor&&HasState("Idle_Combat")?"Idle_Combat":"Idle_Alert";if(CurrentState!=desired)Play(desired,slow);return;}
                var player=EnemyDirector.Instance!=null?EnemyDirector.Instance.PlayerTransform:null;
                if(player!=null){float angle=Vector3.SignedAngle(transform.forward,player.position-transform.position,Vector3.up);
                    if(Mathf.Abs(angle)>60){desired=angle<0?"Turn_Left":"Turn_Right";rate=slow;}
                    else if(SessionNow>=nextTaunt && Vector3.Distance(player.position,transform.position)>12){nextTaunt=SessionNow+Random.Range(12,20);Play("Taunt",1,2.8f);return;}}
            }
            else{
                Vector3 local=transform.InverseTransformDirection(velocity);
                desired=Mathf.Abs(local.x)>Mathf.Abs(local.z)*1.2f?(local.x<0?"Strafe_Left":"Strafe_Right"):
                    local.z<-.1f?"Walk_Backward":speed>2.1f?"Run_Forward":"Walk_Forward";
                float authored=desired=="Run_Forward"?profile.runMetersPerSecond:profile.walkMetersPerSecond;
                // Source clips have tiny in-place strides. FootPlant supplies the actual stance/swing;
                // keep the torso animation within a readable cadence instead of accelerating it 30x.
                rate=GetComponent<EnemyFootPlant>()!=null?Mathf.Clamp(speed/(desired=="Run_Forward"?4:1.8f),.45f,1.8f):speed/Mathf.Max(.05f,authored);
            }
            Animator.SetFloat("Speed",speed<.08f?0:desired=="Run_Forward"?1:.4f);
            if(CurrentState!=desired)Play(desired,Mathf.Max(.01f,rate));
            PlaybackRate=Mathf.Max(.01f,rate);Animator.speed=PlaybackRate;
        }
        void LateUpdate()
        {
            if(arContext!=null&&arContext.Paused)return;
            // Rigs use different local vertical axes. Apply the flight arc after the
            // Animator's pose in world space, rather than modifying a local Y curve.
            if(!Dead&&hips!=null&&AirborneHeight>0){hips.position+=Vector3.up*AirborneHeight;appliedAirborneHeight=AirborneHeight;}
            if(Dead || heads==null || status!=null&&status.Has(StatusType.Freeze))return;
            var player=EnemyDirector.Instance!=null?EnemyDirector.Instance.PlayerTransform:null;if(player==null)return;
            for(int i=0;i<heads.Length;i++)if(heads[i]!=null){var direction=heads[i].parent.InverseTransformDirection(player.position+Vector3.up-heads[i].position);
                float yaw=Mathf.Clamp(Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-35,35)+(heads.Length>1?(i%2==0?-6:6):0);
                float pitch=Mathf.Clamp(-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg,-18,18);
                heads[i].localRotation*=Quaternion.Euler(pitch,yaw,0);}
        }
        public void Dissolve(float value)
        {
            if(renderers==null)return;
            foreach(var r in renderers)if(r!=null){r.GetPropertyBlock(block);block.SetFloat("_Dissolve",value);r.SetPropertyBlock(block);}
        }
        void OnDisable(){if(Animator!=null)Animator.speed=1;Dead=false;AirborneHeight=appliedAirborneHeight=0;CurrentState=null;controlState=null;lockedUntil=0;Dissolve(0);}
    }
}
