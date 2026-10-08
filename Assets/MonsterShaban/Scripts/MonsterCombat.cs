using UnityEngine;
using UnityEngine.Events;
using CampusRift.Skills;

namespace CampusRift.Monsters
{
    public sealed class MonsterCombat : MonoBehaviour
    {
        public MonsterAIConfig config;
        public UnityEvent AttackStarted = new UnityEvent();
        [SerializeField] bool windingUp;
        [SerializeField] float nextAttackTime;
        public bool WindingUp => windingUp;
        public bool FollowingPlayer => windingUp && target != null;
        public bool IsAttacking => windingUp || Time.time < recoverAt;
        public const float ClipImpactTime = 0.35f;
        public const float ClipDuration = 0.9f;
        public int Hits { get; private set; }
        MonsterPerception perception;
        MonsterVitality vitality;
        CampusRift.Combat.StatusEffectHost status;
        // Seal/stun suppression, freeze and shock all stop an attack from starting or landing.
        bool Held => (vitality != null && vitality.Suppressed) || (status != null && status.Immobilized);
        MonsterBrain brain;
        public float AttackRate => brain != null ? brain.AttackRateMultiplier : 1f;
        public float WindupSeconds => Mathf.Max(0.05f, config.AttackWindup / AttackRate);
        public float CooldownSeconds => Mathf.Max(0.2f, config.AttackCooldown / AttackRate);
        MonsterElevatorAwareness lifts;
        Animator animator;
        PlayerMonsterHealth target;
        VoidWall barrierTarget;
        float strikeAt;
        float recoverAt;
        bool hasAttackTrigger;
        bool hasAttackState, hasAttackSpeed;
        static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
        static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        void Awake()
        {
            perception = GetComponent<MonsterPerception>(); animator = GetComponentInChildren<Animator>();
            vitality = GetComponent<MonsterVitality>();
            status = GetComponent<CampusRift.Combat.StatusEffectHost>();
            brain = GetComponent<MonsterBrain>();
            lifts = GetComponent<MonsterElevatorAwareness>();
            if(animator != null && animator.runtimeAnimatorController != null)
            {
                hasAttackState = animator.HasState(0, AttackState);
                foreach(var parameter in animator.parameters)
                {
                    if(parameter.name=="Attack" && parameter.type==AnimatorControllerParameterType.Trigger)hasAttackTrigger=true;
                    if(parameter.nameHash==AttackSpeed && parameter.type==AnimatorControllerParameterType.Float)hasAttackSpeed=true;
                }
            }
        }
        // Strike distance for the current sighting: one metre more when reaching into an open lift car.
        public float Reach
        {
            get
            {
                if (lifts == null) lifts = GetComponent<MonsterElevatorAwareness>();
                return config.AttackDistance + (lifts != null && perception.CanSeePlayer && lifts.CanReachIntoCar(perception.ObservedPosition) ? 1f : 0f);
            }
        }
        public void TryAttack()
        {
            if(Held)return;
            if (config == null || IsAttacking || Time.time < nextAttackTime || !perception.CanSeePlayer || perception.SightThroughGlass) return;
            if (Vector3.Distance(transform.position,perception.ObservedPosition)>Reach) return;
            target = perception.player != null ? perception.player.GetComponent<PlayerMonsterHealth>() : null;
            if(target==null || target.CurrentHealth<=0)return;
            barrierTarget=null;
            BeginSwing();
        }
        public bool TryAttackBarrier(VoidWall barrier)
        {
            if(Held)return false;
            if(config==null || IsAttacking || Time.time<nextAttackTime || barrier==null || !barrier.IsSolid)return false;
            Vector3 point=barrier.StrikePoint(transform.position);
            if(Vector3.Distance(perception.Eye,point)>config.AttackDistance+0.15f || !perception.ClearLine(perception.Eye,point,barrier.transform))return false;
            target=null;barrierTarget=barrier;BeginSwing();return true;
        }
        void BeginSwing()
        {
            float windup = WindupSeconds;
            float speed = ClipImpactTime / windup;
            windingUp=true;strikeAt=Time.time+windup;
            recoverAt = Time.time + ClipDuration / speed;
            nextAttackTime = Mathf.Max(Time.time + CooldownSeconds, recoverAt);
            if(hasAttackState)
            {
                if(hasAttackSpeed)animator.SetFloat(AttackSpeed, speed);
                animator.CrossFadeInFixedTime(AttackState, Mathf.Min(0.045f, windup * 0.2f), 0, 0);
            }
            else if(hasAttackTrigger)animator.SetTrigger("Attack");
            AttackStarted.Invoke();
        }
        void Update()
        {
            if(Held){Interrupt();return;}
            if(!windingUp || Time.time<strikeAt)return;
            windingUp=false;
            if(barrierTarget!=null)
            {
                var wall=barrierTarget;barrierTarget=null;
                Vector3 barrierPoint=wall.StrikePoint(transform.position);
                if(wall.IsSolid && Vector3.Distance(perception.Eye,barrierPoint)<=config.AttackDistance+0.15f && perception.ClearLine(perception.Eye,barrierPoint,wall.transform))
                    wall.Damage(config.Damage,barrierPoint);
                return;
            }
            // Validate sight again at impact: moving behind a door or wall cancels damage.
            perception.Scan();
            if(target==null || !perception.CanSeePlayer || perception.SightThroughGlass || Vector3.Distance(transform.position,perception.ObservedPosition)>Reach)return;
            Vector3 direction = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up).normalized;
            Vector3 point = target.transform.position + Vector3.up * 1.1f - direction * 0.18f;
            // Visibility over an obstacle is not sufficient for a physical strike through it.
            if (!perception.ClearLine(perception.Eye, point, target.transform)) return;
            if (target.TryTakeDamage(config.Damage, point, direction)) Hits++;
        }
        public void Interrupt(){windingUp=false;recoverAt=0;target=null;barrierTarget=null;}
        void OnDisable(){Interrupt();}
    }
}
