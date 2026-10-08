using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Enemies;
namespace CampusRift.AR
{
    public sealed class ARMinionBrain : MonoBehaviour
    {
        public MinionState State {get;private set;}public int Strikes {get;private set;}public Vector3 Velocity {get;private set;}
        EnemyInstance owner;ARBattlefield field;NavMeshAgent agent;float until,impact;bool hit;Vector3 prior;ARPlayerCombat playerCombat;float nextRanged;public bool Ranged {get;private set;}
        public void SetRanged(bool value){Ranged=value;nextRanged=field.Clock+2.5f;}
        void Awake(){owner=GetComponent<EnemyInstance>();agent=GetComponent<NavMeshAgent>();GetComponent<CampusRift.Monsters.MonsterVitality>().DefeatedOnce+=Died;}
        void OnDestroy(){if(owner!=null&&owner.Vitality!=null)owner.Vitality.DefeatedOnce-=Died;}
        public void Configure(ARBattlefield value)
        {field=value;playerCombat=field.GetComponent<ARPlayerCombat>();Ranged=false;nextRanged=field.Clock+2.5f;Strikes=0;State=MinionState.Spawn;until=field.Clock+(owner.Animation!=null?owner.Animation.SpawnSeconds:1);prior=transform.position;
            if(agent!=null){agent.enabled=false;agent.agentTypeID=field.AgentType;agent.radius=.28f*field.Scale;agent.height=1.8f*field.Scale;agent.baseOffset=0;agent.speed=owner.Speed*field.Scale;agent.acceleration=20*field.Scale;agent.angularSpeed=360;agent.stoppingDistance=.6f*field.Scale;
                if(field.NavigationReady&&NavMesh.SamplePosition(transform.position,out var p,field.Scale,new NavMeshQueryFilter{agentTypeID=field.AgentType,areaMask=NavMesh.AllAreas})){transform.position=p.position;agent.enabled=true;agent.Warp(p.position);}}
        }
        void Stop(){if(agent!=null&&agent.enabled&&agent.isOnNavMesh){agent.isStopped=true;agent.velocity=Vector3.zero;}Velocity=Vector3.zero;}
        void Update()
        {
            if(field==null)return;bool pause=field.Paused;if(owner.Animation!=null&&owner.Animation.Animator!=null&&pause)owner.Animation.Animator.speed=0;
            if(pause){Stop();return;}float now=field.Clock;
            if(State==MinionState.Dead){float death=owner.Animation!=null?owner.Animation.DeathSeconds:1;owner.Animation?.Dissolve(Mathf.Clamp01((now-(until-death))/death));if(now>=until)owner.Pool.Release(owner);return;}
            if(field.Shrine==null||field.Shrine.IsDead){Stop();return;}
            if(owner.Vitality.Suppressed||(owner.Status!=null&&(owner.Status.Immobilized||owner.Status.Has(StatusType.Stun)||owner.Status.Has(StatusType.Pulled)))){Stop();if(State==MinionState.Windup){State=MinionState.Recover;until=now+.4f;}return;}
            if(State==MinionState.Spawn){Stop();if(now>=until)State=MinionState.Chase;return;}
            if(State==MinionState.Windup){Stop();if(!hit&&now>=impact){hit=true;if(Vector3.Distance(transform.position,field.Shrine.transform.position)<=owner.archetype.attackRange*field.Scale*1.5f){field.Shrine.TakeDamage(owner.Damage);Strikes++;}}if(now>=until){State=MinionState.Recover;until=now+owner.archetype.attackCooldown;}return;}
            if(State==MinionState.Recover){Stop();if(now>=until)State=MinionState.Chase;return;}
            if(Ranged&&playerCombat!=null&&playerCombat.Enabled){Stop();if(now>=nextRanged){nextRanged=now+4.5f;owner.Animation?.BeginAttack(1.2f);playerCombat.Launch(owner);}return;}
            var delta=Vector3.ProjectOnPlane(field.Shrine.transform.position-transform.position,Vector3.up);
            if(delta.magnitude<=owner.archetype.attackRange*field.Scale){Stop();State=MinionState.Windup;hit=false;float windup=owner.Animation!=null&&owner.Animation.profile!=null?owner.Animation.profile.attackImpactSeconds:.55f;owner.Animation?.BeginAttack(windup);impact=now+windup;until=impact+.35f;return;}
            float speed=owner.Speed*field.Scale*(owner.Status!=null?owner.Status.SpeedMultiplier:1)*(GetComponent<AREliteAffix>()?.SpeedMultiplier??1);
            if(agent!=null&&agent.enabled&&agent.isOnNavMesh){agent.isStopped=false;agent.speed=speed*(playerCombat?.ClockRate??1);agent.SetDestination(field.Shrine.transform.position);}
            else {Vector3 separation=Vector3.zero;foreach(var e in EnemyDirector.Instance.Active){if(e==owner||e==null||!e.Alive)continue;var away=Vector3.ProjectOnPlane(transform.position-e.transform.position,Vector3.up);float d=away.magnitude;if(d<.7f*field.Scale&&d>.001f)separation+=away.normalized*(1-d/(.7f*field.Scale));}var movement=(delta.normalized+separation).normalized*speed*field.CombatDelta;var next=transform.position+movement;var local=field.Root.InverseTransformPoint(next);local.y=0;float extent=field.placement.Radius/field.Scale;var flat=Vector2.ClampMagnitude(new Vector2(local.x,local.z),extent*.9f);transform.position=field.Root.TransformPoint(new Vector3(flat.x,0,flat.y));}
            if(delta.sqrMagnitude>.0001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*8);
            Velocity=(transform.position-prior)/Mathf.Max(.001f,Time.deltaTime);prior=transform.position;
        }
        void Died(){if(field==null||State==MinionState.Dead)return;State=MinionState.Dead;Stop();if(agent!=null)agent.enabled=false;foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;owner.Animation?.Die(owner.LastDamage.direction);until=field.Clock+(owner.Animation!=null?owner.Animation.DeathSeconds:1);EnemyDirector.Instance?.Unregister(owner,true);owner.RaiseDied();}
        void OnDisable(){Stop();}
        void LateUpdate()
        {var animator=owner!=null&&owner.Animation!=null?owner.Animation.Animator:null;if(field==null||animator==null)return;if(field.Paused)animator.speed=0;else if(State==MinionState.Dead)animator.speed=owner.Animation.PlaybackRate*(playerCombat?.ClockRate??1);else animator.speed*=playerCombat?.ClockRate??1;}
    }
}
