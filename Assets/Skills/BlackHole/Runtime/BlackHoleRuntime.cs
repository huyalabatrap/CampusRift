using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Enemies;
namespace CampusRift.Skills
{
    public sealed class BlackHoleRuntime:Set1SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(.6f,.08f,1);
        protected override float Range=>15*WorldScale;
        protected override float AimRadius=>9;
        struct Pull
        {
            public AR.ARCombatContext ar;public MonsterVitality victim;public NavMeshAgent agent;public Behaviour motor,brain;
            public bool updatePosition,updateRotation,stopped,motorEnabled,brainEnabled;
            public Vector3 releaseStart,releaseEnd;
            public Transform model;public Vector3 modelPosition;public Quaternion modelRotation;
        }
        readonly Pull[] pulled=new Pull[64];int count,danger;bool expelled;
        SkillVfxPool.Node vortex,accretion;
        public int PulledCount=>count;
        public int NavigationFailures {get;private set;}
        protected override void OnCast()
        {
            count=0;expelled=false;NavigationFailures=0;danger=DangerZoneRegistry.Register(point,9*WorldScale,3.5f);
            vortex=vfx.Spawn(SkillVfxKind.Vortex,point+Vector3.up*(1.8f*WorldScale),Accent,3.55f,1.35f,vfx.config.voidCast,vfx.config.voidCast);
            accretion=vfx.Spawn(SkillVfxKind.Accretion,point+Vector3.up*(1.8f*WorldScale),Accent,3.55f,3.1f);
            if(Mastered)vfx.Spawn(SkillVfxKind.Ring,point+Vector3.up*(1.8f*WorldScale),new Color(.9f,.5f,1),3.55f,1.7f);
            vfx.Spawn(SkillVfxKind.Scorch,point,new Color(.09f,.012f,.14f),3.6f,8);
            vfx.Spawn(SkillVfxKind.Ring,point,Accent,3.7f,9);GetComponent<SkillCastPose>()?.Play(.55f,-8,60);
        }
        void Gather()
        {
            for(int i=0;i<MonsterVitality.Active.Count;i++)
            {
                var m=MonsterVitality.Active[i];if(!InArea(m,point,9))continue;
                var status=m.GetComponent<StatusEffectHost>();if(m.resistHardControl){if(status!=null)status.Apply(StatusType.Chill,Mathf.Max(.01f,3-elapsed),.5f,gameObject);continue;}
                bool seen=false;for(int j=0;j<count;j++)if(pulled[j].victim==m){seen=true;break;}if(seen||count==pulled.Length)continue;
                var context=m.GetComponent<AR.ARCombatContext>();var agent=m.GetComponent<NavMeshAgent>();bool navigable=agent!=null&&agent.enabled&&agent.isOnNavMesh&&!agent.isOnOffMeshLink;if(!navigable&&context==null)continue;if(!navigable)agent=null;
                var p=new Pull{ar=context,victim=m,agent=agent,updatePosition=agent!=null&&agent.updatePosition,updateRotation=agent!=null&&agent.updateRotation,stopped=agent!=null&&agent.isStopped};
                var minion=m.GetComponent<MinionMotor>();p.motor=minion!=null?(Behaviour)minion:m.GetComponent<MonsterNavigation>();
                var brain=m.GetComponent<MinionBrain>();p.brain=brain!=null?(Behaviour)brain:m.GetComponent<MonsterBrain>();
                p.model=VisualRig(m);if(p.model!=null){p.modelPosition=p.model.localPosition;p.modelRotation=p.model.localRotation;}
                if(p.motor!=null){p.motorEnabled=p.motor.enabled;p.motor.enabled=false;}if(p.brain!=null){p.brainEnabled=p.brain.enabled;p.brain.enabled=false;}
                if(agent!=null){agent.isStopped=true;agent.velocity=Vector3.zero;agent.updatePosition=false;agent.updateRotation=false;}
                pulled[count++]=p;if(status!=null)status.Apply(StatusType.Pulled,Mathf.Max(.01f,3-elapsed),0,gameObject);
            }
        }
        protected override void TickCast(float dt)
        {
            if(!expelled&&elapsed<3)
            {
                if(Mastered)EnemyProjectilePool.Instance?.PullInto(point+Vector3.up*(1.8f*WorldScale),9,dt);
                Gather();for(int i=0;i<count;i++)
                {var p=pulled[i];if(p.victim==null||p.victim.Defeated||!p.victim.gameObject.activeInHierarchy)continue;Vector3 delta=Vector3.ProjectOnPlane(point-p.victim.transform.position,Vector3.up);Vector3 next=p.victim.transform.position+delta.normalized*Mathf.Min(delta.magnitude,dt*(3*WorldScale+delta.magnitude*.8f));Move(p,next);
                    // Only the visual rig orbits/lifts; collider and agent stay on NavMesh.
                    if(p.model!=null){float a=elapsed*3.5f+i*2.399f,w=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.8f));p.model.localPosition=p.modelPosition+new Vector3(Mathf.Cos(a)*1.7f,.45f+.2f*Mathf.Sin(a),Mathf.Sin(a)*1.7f)*w;
                        Vector3 toward=Vector3.ProjectOnPlane(point-p.model.position,Vector3.up).normalized;
                        Vector3 localToward=p.model.parent!=null?p.model.parent.InverseTransformDirection(toward):toward;
                        Quaternion lean=Quaternion.FromToRotation(Vector3.up,(Vector3.up*3+localToward).normalized);
                        p.model.localRotation=lean*Quaternion.AngleAxis(elapsed*90,Vector3.up)*p.modelRotation;}
                }
            }
            if(!expelled&&elapsed>=3)
            {
                expelled=true;if(vortex!=null){vortex.Progress=0;vortex.Fade(.45f);vortex.StopLoop();}if(accretion!=null)accretion.Fade(.4f);vfx.Burst(point+Vector3.up*WorldScale,Accent,2.3f,vfx.config.voidHit);
                vfx.Spawn(SkillVfxKind.Repulsion,point,Accent,.8f,9);
                vfx.Spawn(SkillVfxKind.Shockwave,point,Accent,.8f,9);
                for(int i=0;i<count;i++)if(pulled[i].victim!=null) pulled[i].victim.GetComponent<StatusEffectHost>()?.Consume(StatusType.Pulled);
                for(int i=0;i<MonsterVitality.Active.Count;i++){var m=MonsterVitality.Active[i];if(InArea(m,point,9))Hit(m,2.5f,DamageSource.Skill,true);}
                for(int i=0;i<count;i++)
                {var p=pulled[i];RestoreModel(p);if(p.victim==null)continue;p.releaseStart=p.victim.transform.position;Vector3 dir=Vector3.ProjectOnPlane(p.releaseStart-point,Vector3.up).normalized;if(dir.sqrMagnitude<.01f)dir=Quaternion.Euler(0,i*137.5f,0)*Vector3.forward;p.releaseEnd=p.releaseStart+dir*(4*WorldScale);pulled[i]=p;}
                if(LastHitCount>0)impact.Pulse(.9f,.065f);vfx.Fragments(point+Vector3.up*WorldScale,Accent,SkillVfxPool.MobileQuality?4:10);
                vfx.Spawn(SkillVfxKind.Scorch,point,new Color(.08f,.015f,.12f),2.8f,5);
            }
            if(expelled)for(int i=0;i<count;i++){var p=pulled[i];if(p.victim!=null&&p.victim.isActiveAndEnabled)Move(p,Vector3.Lerp(p.releaseStart,p.releaseEnd,Mathf.Clamp01((elapsed-3)/.35f)));}
            if(elapsed>=3.4f)Finish();
        }
        static Transform VisualRig(MonsterVitality victim)
        {
            var animator=victim.GetComponentInChildren<Animator>();
            if(animator!=null&&animator.transform!=victim.transform)return animator.transform;
            // The supplied Generic models put Animator on the gameplay root. Orbit
            // their rig wrapper instead: moving that root overwrites the pull and
            // restoring it at cleanup teleports the collider back to its start.
            foreach(var skin in victim.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var rig=skin.rootBone;if(rig==null)continue;
                while(rig.parent!=null&&rig.parent!=victim.transform)rig=rig.parent;
                if(rig.parent==victim.transform)return rig;
            }
            return null;
        }
        void Move(Pull p,Vector3 destination)
        {
            if(p.victim==null)return;
            if(p.ar!=null){destination=p.ar.Constrain(destination);if(p.agent!=null&&p.agent.enabled&&p.agent.isOnNavMesh){NavMeshHit sample;if(NavMesh.SamplePosition(destination,out sample,p.ar.scale,new NavMeshQueryFilter{agentTypeID=p.agent.agentTypeID,areaMask=p.agent.areaMask}))destination=sample.position;else return;p.agent.nextPosition=destination;}p.victim.transform.position=destination;return;}
            if(p.agent==null||!p.agent.enabled)return;
            NavMeshHit next,edge;
            if(!NavMesh.SamplePosition(destination,out next,1,p.agent.areaMask))return;
            Vector3 current=p.victim.transform.position;
            if(Mathf.Abs(next.position.y-current.y)>1)return;
            if(NavMesh.Raycast(current,next.position,out edge,p.agent.areaMask))next.position=edge.position;
            p.victim.transform.position=next.position;p.agent.nextPosition=next.position;
        }
        static void RestoreModel(Pull p){if(p.model!=null){p.model.localPosition=p.modelPosition;p.model.localRotation=p.modelRotation;}}
        protected override void Cleanup()
        {
            DangerZoneRegistry.Remove(danger);
            for(int i=0;i<count;i++)
            {
                var p=pulled[i];RestoreModel(p);if(p.agent!=null)
                {
                    if(p.agent.enabled&&p.victim!=null&&p.victim.gameObject.activeInHierarchy)
                    {
                        NavMeshHit hit;
                        if(p.ar!=null?NavMesh.SamplePosition(p.victim.transform.position,out hit,2*p.ar.scale,new NavMeshQueryFilter{agentTypeID=p.agent.agentTypeID,areaMask=p.agent.areaMask}):NavMesh.SamplePosition(p.victim.transform.position,out hit,2,p.agent.areaMask))p.agent.Warp(hit.position);else NavigationFailures++;
                        p.agent.updatePosition=p.updatePosition;p.agent.updateRotation=p.updateRotation;if(p.agent.isOnNavMesh)p.agent.isStopped=p.stopped;
                    }
                    if(p.motor!=null)p.motor.enabled=p.motorEnabled;if(p.brain!=null)p.brain.enabled=p.brainEnabled;
                }
                if(p.victim!=null)p.victim.GetComponent<StatusEffectHost>()?.Consume(StatusType.Pulled);
                pulled[i]=default(Pull);
            }
        }
    }
}
