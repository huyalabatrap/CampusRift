using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Monsters;

namespace CampusRift.Enemies
{
    // Movement of a light monster on the campus NavMesh (same agent type as Shaban). Stairs use the existing
    // NavMeshLinks automatically; unlike Shaban, minions never ride lifts.
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public sealed class MinionMotor : MonoBehaviour, IMotionHold
    {
        public NavMeshAgent Agent { get; private set; }
        public Animator Animator { get; private set; }
        MonsterVitality vitality; StatusEffectHost status; EnemyInstance owner;
        float baseSpeed, holdUntil;
        bool stopRequested, hasDestination, squadPath;
        float alliedSpeed;
        public void ConfigureAlly(float speed){owner=null;alliedSpeed=speed;baseSpeed=speed;holdUntil=0;stopRequested=false;}
        static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        public bool Held => (vitality != null && vitality.Suppressed) || (status != null && status.Immobilized) || Time.time < holdUntil;
        public bool OnMesh => Agent != null && Agent.enabled && Agent.isOnNavMesh;
        public Vector3 Velocity => OnMesh ? Agent.velocity : Vector3.zero;
        public float RemainingDistance => OnMesh && hasDestination && !Agent.pathPending ? Agent.remainingDistance : float.PositiveInfinity;

        void Awake()
        {
            Agent = GetComponent<NavMeshAgent>(); vitality = GetComponent<MonsterVitality>(); status = GetComponent<StatusEffectHost>();
            Animator = GetComponentInChildren<Animator>();
        }

        public void Configure(EnemyInstance instance)
        {
            owner = instance; baseSpeed = instance.Speed; stopRequested = false; hasDestination = false; holdUntil = 0;
            alliedSpeed=0;squadPath=false;
            if (OnMesh) { Agent.ResetPath(); Agent.isStopped = false; Agent.velocity = Vector3.zero; }
            if (Animator != null) { Animator.Rebind(); Animator.Update(0); }
            ApplySpeed();
        }

        float SpeedNow => (owner!=null?owner.Speed:alliedSpeed>0?alliedSpeed:baseSpeed) * (status != null ? status.SpeedMultiplier : 1f) *
            (squadPath&&owner!=null&&EnemyDirector.Instance!=null&&EnemyDirector.Instance.Squad!=null?EnemyDirector.Instance.Squad.FormationSpeed(owner):1f);
        void ApplySpeed() { if (Agent != null) { Agent.speed = Mathf.Max(0.01f, SpeedNow); Agent.acceleration = Mathf.Max(12f, SpeedNow * 4f); } }

        // Puts the agent on the NavMesh at (or near) a point; false when there is no NavMesh within 3 m.
        public bool Place(Vector3 position)
        {
            if (!NavMesh.SamplePosition(position, out var hit, 3f, Agent.areaMask)) return false;
            Agent.enabled = true;
            if (!Agent.Warp(hit.position)) { transform.position = hit.position; }
            hasDestination = false; stopRequested = false; return OnMesh;
        }

        public bool MoveTo(Vector3 point)
        {
            if (!OnMesh) return false;
            squadPath=false;
            stopRequested = false;
            hasDestination = Agent.SetDestination(point); return hasDestination;
        }

        public bool FollowPath(NavMeshPath path)
        {
            if (!OnMesh || path == null || path.status != NavMeshPathStatus.PathComplete) return false;
            stopRequested = false;
            hasDestination = Agent.SetPath(path);
            squadPath=hasDestination;
            return hasDestination;
        }

        public void Stop()
        {
            stopRequested = true; hasDestination = false;
            squadPath=false;
            if (Agent != null) Agent.updateRotation = true;
            if (OnMesh) { Agent.ResetPath(); Agent.velocity = Vector3.zero; }
        }

        public void FaceTowards(Vector3 point, float turnRate = 14f)
        {
            Vector3 flat = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
            if (flat.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 1f - Mathf.Exp(-turnRate * Time.deltaTime));
        }

        // Seals, freeze and stun call this every frame (MonsterVitality.Update / StatusEffectHost.Update).
        public void HoldForSeal() { holdUntil = Time.time + 0.1f; if (OnMesh) { Agent.velocity = Vector3.zero; } }

        void Update()
        {
            if (!OnMesh) return;
            ApplySpeed();
            // The agent rotates itself while moving; a held monster stays put.
            Agent.isStopped = Held || stopRequested;
            if (Agent.isStopped) Agent.velocity = Vector3.zero;
            if (Animator != null && owner!=null && owner.Animation==null)
            {
                float speed = Agent.velocity.magnitude;
                Animator.SetFloat(MoveSpeed, speed, 0.08f, Time.deltaTime);
            }
        }
    }
}
