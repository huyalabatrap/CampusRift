using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
using Debug = UnityEngine.Debug;

namespace CampusRift.Enemies
{
    public enum MinionState { Spawn, Chase, Windup, Strike, Recover, Dead }

    // Light behaviour for the horde: chase → red warning → strike/shoot → recover (plan §3.3, P04-T03).
    // No perception model: minions always know where the player is, and the Director decides who may attack.
    [DisallowMultipleComponent, RequireComponent(typeof(MinionMotor))]
    public sealed class MinionBrain : MonoBehaviour
    {
        public const float SpawnSeconds = 1f, RecoverSeconds = 0.5f, FarDistance = 40f;
        public MinionState State { get; private set; } = MinionState.Dead;
        public int Strikes { get; private set; }
        public int Shots { get; private set; }
        public int Interrupts { get; private set; }
        public float LastWindupSeconds { get; private set; }
        public float WindupStartedAt { get; private set; }
        public float LastStrikeAt { get; private set; }
        public event Action<MinionBrain> WindupStarted, Struck;
        EnemyInstance owner; MinionMotor motor; MonsterVitality vitality; StatusEffectHost status; Animator animator;
        EnemyDirector director; Transform player; PlayerMonsterHealth playerHealth;
        Vector3 modelScale = Vector3.one; float stateUntil, nextThink, nextAttackAt, lastHit;
        Vector3 lastDestination;
        static readonly int AttackTrigger = Animator.StringToHash("Attack"), HitTrigger = Animator.StringToHash("Hit"), DieTrigger = Animator.StringToHash("Die"),
            AttackSpeed = Animator.StringToHash("AttackSpeed");
        Collider[] bodyColliders;
        EnemyTelegraph basicWarning;EnemyAbilityRunner abilities;float nextDodge,dodgeUntil;int evaluatedZone;
        readonly CampusRift.Skills.DangerZone[] dangerZones=new CampusRift.Skills.DangerZone[64];
        public int DodgeAttempts {get;private set;}
        public int Dodges {get;private set;}
        float heardUntil; Vector3 heardPoint;
        public int HeardSounds { get; private set; }
        public bool HearingPursuit => Time.time < heardUntil;
        public bool WaitingAtDoor { get; private set; }
        public bool GuardingDoor { get; private set; }
        public CombatFaction Faction=>vitality!=null?vitality.Faction:CombatFaction.Hostile;
        CampusRift.Skills.SoulAlly soul;MonsterVitality alliedOpponent;float alliedNext;
        void OnEnable() { if(GetComponent<EnemyInstance>()?.ARSession??false)return; SoundEventBus.Emitted += Hear; }
        void Hear(SoundEvent sound)
        {
            if (owner == null || Tier < 3 || State == MinionState.Dead || sound.SoundType == PlayerSoundType.Walk) return;
            if (Vector3.Distance(transform.position,sound.Position) > Mathf.Min(45,12+sound.Loudness*8)) return;
            HeardSounds++; heardPoint=sound.Position; heardUntil=Time.time+4; nextThink=0;
        }

        void Awake()
        {
            if(GetComponent<EnemyInstance>()?.ARSession??false)return;
            motor = GetComponent<MinionMotor>(); vitality = GetComponent<MonsterVitality>(); status = GetComponent<StatusEffectHost>();
            animator = GetComponentInChildren<Animator>(); bodyColliders = GetComponentsInChildren<Collider>();
            abilities=GetComponent<EnemyAbilityRunner>();
            if (animator != null) modelScale = animator.transform.localScale;
            vitality.DefeatedOnce += OnDefeated; vitality.Damaged += OnDamaged;
        }
        void OnDestroy() { if (vitality != null) { vitality.DefeatedOnce -= OnDefeated; vitality.Damaged -= OnDamaged; } }

        public void Configure(EnemyInstance instance)
        {
            owner = instance; director = EnemyDirector.Ensure(); player = director.FindPlayer(); playerHealth = director.Player;
            float spawn=owner.Animation!=null?owner.Animation.SpawnSeconds:SpawnSeconds;
            State = MinionState.Spawn; stateUntil = Time.time + spawn; nextThink = 0; lastDestination = Vector3.positiveInfinity;
            nextAttackAt = Time.time + SpawnSeconds + UnityEngine.Random.Range(0.4f, 1.2f);
            Strikes = Shots = Interrupts = 0;
            LastWindupSeconds = WindupStartedAt = LastStrikeAt = 0;
            HeardSounds=0;heardUntil=0;WaitingAtDoor=GuardingDoor=false;
            DodgeAttempts=Dodges=0;nextDodge=dodgeUntil=0;evaluatedZone=0;basicWarning?.Hide();
            foreach (var c in bodyColliders) if (c != null) c.enabled = true;
            SetModelScale(owner.Animation!=null?1f:.2f);
            motor.Stop();
        }

        void OnDisable() { SoundEventBus.Emitted -= Hear; WaitingAtDoor=GuardingDoor=false; if (director != null && owner != null) director.Release(owner); }

        float Tier => owner != null ? owner.scaling.aiTier : 0;

        void SetModelScale(float factor) { if (animator != null) animator.transform.localScale = modelScale * factor; }

        Vector3 PlayerChest => player.position + Vector3.up * 1.1f;
        Vector3 Eye => transform.position + Vector3.up * 1.0f;
        // Distance that counts floors: a player on another storey is far away even when directly overhead.
        float FlatDistance => ReachDistance(player.position);
        float ReachDistance(Vector3 point)
        {
            Vector3 delta = point - transform.position; float flat = new Vector2(delta.x, delta.z).magnitude, dy = Mathf.Abs(delta.y);
            return dy < 1.8f ? flat : flat + dy * 3f;
        }

        void Update()
        {
            if(Faction==CombatFaction.Ally){TickSoul();return;}
            if (owner == null || owner.archetype == null || State == MinionState.Dead) return;
            long start = Stopwatch.GetTimestamp();
            if (player == null) { player = director.FindPlayer(); playerHealth = director.Player; }
            if (player != null) Tick();
            director.AddAiTicks(Stopwatch.GetTimestamp() - start);
        }

        void Tick()
        {
            if(State!=MinionState.Spawn&&TickAlliedOpponent())return;
            bool playerDead = playerHealth != null && playerHealth.IsDead;
            switch (State)
            {
                case MinionState.Spawn:
                    if(owner.Animation==null)SetModelScale(Mathf.SmoothStep(0.2f, 1f, 1f - Mathf.Clamp01((stateUntil - Time.time) / SpawnSeconds)));
                    if (Time.time >= stateUntil) { SetModelScale(1f); State = MinionState.Chase;owner.Animation?.Play("Idle_Alert",1,.32f); }
                    break;
                case MinionState.Chase:
                    if (playerDead) { motor.Stop(); break; }
                    if(abilities!=null&&abilities.Busy)break;
                    if(GetComponent<ExpandedEnemyRuntime>()?.Busy??false)break;
                    if(Time.time<dodgeUntil)break;
                    if (Time.time >= nextThink)
                    {
                        // Near monsters think 5×/s, far ones 2×/s, and start staggered so the horde does not spike.
                        nextThink = Time.time + (FlatDistance > FarDistance ? 0.5f : 0.2f) + UnityEngine.Random.Range(0f, 0.05f);
                        WaitingAtDoor=GuardingDoor=false;
                        if(TryDodge())break;
                        var retreat=ExpandedEnemyRuntime.RetreatTarget(owner);
                        if(retreat!=null){if(ReachDistance(retreat.transform.position)>3)motor.MoveTo(retreat.transform.position);else motor.Stop();break;}
                        if(GetComponent<ExpandedEnemyRuntime>()?.Think(player)??false)break;
                        if(Tier>=3 && director.Tactics!=null)
                        {
                            if(director.Tactics.YieldEntrance(owner))break;
                            if(director.Tactics.TryPosition(owner,out var door,out bool guard))
                            {
                                GuardingDoor=guard;
                                if(ReachDistance(door)>.7f){motor.MoveTo(door);break;}
                                motor.Stop();motor.FaceTowards(player.position);WaitingAtDoor=true;
                                owner.Animation?.Play(guard?"Idle_Combat":"Idle_Alert",1,.3f);
                                if(FlatDistance>owner.archetype.attackRange+.2f)break;
                            }
                            else if(HearingPursuit && FlatDistance>owner.archetype.attackRange+1 && !(director.Squad?.TryGet(owner,out _)??false))
                            {motor.MoveTo(heardPoint);if(ReachDistance(heardPoint)<1)heardUntil=0;break;}
                        }
                        if(abilities!=null&&abilities.TryUse(player))break;
                        if (owner.archetype.ranged) ThinkRanged(); else ThinkMelee();
                    }
                    break;
                case MinionState.Windup:
                    // The warned cone/capsule direction stays fixed so a lateral dodge remains valid.
                    if(owner.archetype.id!="liem-hon")motor.FaceTowards(player.position);
                    if (motor.Held || playerDead) { Interrupt(); break; }
                    if (Time.time >= stateUntil) Strike();
                    break;
                case MinionState.Strike:
                    State = MinionState.Recover; stateUntil = Time.time + RecoverSeconds;
                    break;
                case MinionState.Recover:
                    if (Time.time >= stateUntil) { director.Release(owner); State = MinionState.Chase; }
                    break;
            }
        }

        bool CanAttackNow => Time.time >= nextAttackAt && !motor.Held && (playerHealth == null || !playerHealth.IsDead) &&
            (director.Squad==null||director.Squad.CanAttack(owner));
        float MeleeReach => owner.archetype.id=="hoa-trung"?3:owner.archetype.id=="liem-hon"?4:owner.archetype.attackRange*1.4f;

        void ThinkMelee()
        {
            float distance = FlatDistance; var archetype = owner.archetype;
            Vector3 destination = director.DestinationFor(owner);
            if(director.Squad!=null&&director.Squad.Move(owner))
            {
                if(distance<=archetype.attackRange+.2f&&CanAttackNow&&CombatLine.Clear(Eye,PlayerChest,transform,true)&&director.TryAcquire(owner))BeginWindup();
                return;
            }
            // Do not crowd the player. Without a ring the monster stops in reach; on the ring it stops at its slot.
            bool ring = director.TierFor(owner.scaling.aiTier).surround;
            if(director.Adapting(owner)&&CanAttackNow&&director.TryAcquire(owner)){destination=player.position;ring=false;}
            if (ring ? ReachDistance(destination) < 0.45f : distance <= archetype.attackRange * 0.8f) motor.Stop();
            else if ((destination - lastDestination).sqrMagnitude > 0.25f || !HasPath) { motor.MoveTo(destination); lastDestination = destination; }
            if (distance <= archetype.attackRange + 0.2f && CanAttackNow && CombatLine.Clear(Eye, PlayerChest, transform, true) && director.TryAcquire(owner))
                BeginWindup();
        }

        bool HasPath => motor.OnMesh && motor.Agent.hasPath;

        void ThinkRanged()
        {
            var archetype = owner.archetype; float distance = FlatDistance;
            bool sight = CombatLine.Clear(Eye, PlayerChest, transform, true);
            if(director.Squad!=null&&director.Squad.Move(owner))
            {
                motor.FaceTowards(player.position);
                if(sight&&distance<=archetype.preferredMax+1.5f&&CanAttackNow&&director.TryAcquire(owner))BeginWindup();
                return;
            }
            Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up).normalized;
            if (distance > archetype.preferredMax || !sight)
            {
                // Approach until the band is reached (or a clear line opens up).
                Vector3 goal = player.position - toPlayer * (archetype.preferredMax * 0.85f);
                if (!sight) goal = player.position;
                if ((goal - lastDestination).sqrMagnitude > 1f || !HasPath) { motor.MoveTo(goal); lastDestination = goal; }
            }
            else if (distance < archetype.preferredMin)
            {
                Vector3 away = transform.position - toPlayer * (archetype.preferredMin - distance + 2.5f);
                if ((away - lastDestination).sqrMagnitude > 1f) { motor.MoveTo(away); lastDestination = away; }
            }
            else motor.Stop();
            if (sight && distance <= archetype.preferredMax + 1.5f && CanAttackNow && director.TryAcquire(owner)) BeginWindup();
        }

        void BeginWindup()
        {
            var tier = director.TierFor(owner.scaling.aiTier);
            float windup = owner.archetype.id=="liem-hon"?.6f:owner.archetype.id=="hoa-trung"?.8f:owner.archetype.id=="quang-ma"?.9f:tier.windup; LastWindupSeconds = windup; WindupStartedAt = Time.time;
            State = MinionState.Windup; stateUntil = Time.time + windup;
            motor.Stop();
            TelegraphOutline.Show(gameObject, windup);
            bool halo=owner.archetype.id=="quang-ma";
            if(owner.Animation!=null)basicWarning=halo?EnemyTelegraph.ShowHalo(transform.position+Vector3.up*(motor.Agent.height+.45f),PlayerChest-transform.position,windup):EnemyTelegraph.Show(transform.position,transform.forward,owner.archetype.ranged?1:MeleeReach,windup,
                owner.archetype.id=="liem-hon"?CampusRift.Skills.DangerShape.Cone:CampusRift.Skills.DangerShape.Circle,120);
            if(owner.Animation!=null)owner.Animation.BeginAttack(windup);
            else if (animator != null)
            {
                // Speed the clip so that its impact frame lands exactly when the warning ends.
                float speed = Mathf.Clamp(owner.archetype.attackClipSeconds * owner.archetype.impactFraction / windup, 0.3f, 3f);
                animator.SetFloat(AttackSpeed, speed); animator.SetTrigger(AttackTrigger);
            }
            WindupStarted?.Invoke(this);
        }

        void Interrupt()
        {
            Interrupts++;
            basicWarning?.Hide();basicWarning=null;
            var outline = GetComponent<TelegraphOutline>(); if (outline != null) outline.Hide();
            director.Release(owner);
            State = MinionState.Chase; nextAttackAt = Time.time + 0.6f; nextThink = 0;
        }

        void Strike()
        {
            var archetype = owner.archetype; State = MinionState.Strike; LastStrikeAt = Time.time;
            basicWarning?.Hide();basicWarning=null;
            nextAttackAt = Time.time + archetype.attackCooldown*(owner.Elite!=null?owner.Elite.AttackIntervalMultiplier:1)*(1-.4f*Mathf.Clamp01((owner.scaling.level-1)/9f));
            if (archetype.ranged)
            {
                Vector3 origin = Eye + transform.forward * 0.4f;
                Vector3 dir = (PlayerChest - origin).normalized;
                var bolt = EnemyProjectilePool.Ensure().Fire(origin, dir, archetype.id=="quang-ma"?6:archetype.projectileSpeed, owner.Damage, archetype.element, gameObject);
                if (bolt != null) Shots++;
            }
            else
            {
                // Sight and reach are checked again at impact: stepping away or behind cover cancels the blow.
                float reach=MeleeReach;
                bool angle=archetype.id!="liem-hon"||Vector3.Angle(transform.forward,Vector3.ProjectOnPlane(player.position-transform.position,Vector3.up))<=60;
                if (FlatDistance <= reach && angle && Mathf.Abs(player.position.y - transform.position.y) < 2f &&
                    CombatLine.Clear(Eye, PlayerChest, transform, true) && playerHealth != null)
                {
                    Vector3 dir = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up).normalized;
                    var info = DamageInfo.Create(owner.Damage, archetype.element, DamageSource.Melee, PlayerChest - dir * 0.2f, dir, gameObject);
                    if (playerHealth.ApplyDamage(info)) Strikes++;
                    if(owner.Animation!=null)EnemyAbilityRunner.Feedback(transform.position,archetype.id=="hoa-trung"?3:1);
                }
            }
            Struck?.Invoke(this);
        }

        void OnDamaged(DamageInfo info)
        {
            if (State == MinionState.Dead || animator == null || owner.Animation!=null || Time.time - lastHit < 0.35f) return;
            if (State == MinionState.Windup || State == MinionState.Strike) return;
            lastHit = Time.time; animator.SetTrigger(HitTrigger);
        }

        void OnDefeated()
        {
            if(Faction==CombatFaction.Ally){GetComponent<CampusRift.Skills.SoulAlly>()?.Dissolve();return;}
            if (State == MinionState.Dead) return;
            State = MinionState.Dead;
            basicWarning?.Hide();basicWarning=null;
            director.Release(owner); motor.Stop(); motor.Agent.enabled = false;
            var outline = GetComponent<TelegraphOutline>(); if (outline != null) outline.Hide();
            foreach (var c in bodyColliders) if (c != null) c.enabled = false;
            if(owner.Animation!=null)owner.Animation.Die(owner.LastDamage.direction);
            else if (animator != null) animator.SetTrigger(DieTrigger);
            director.Unregister(owner, true);
            owner.RaiseDied();
            StartCoroutine(Vanish());
        }

        public float DodgeChance=>owner.scaling.aiTier>=4?.6f:director.TierFor(owner.scaling.aiTier).dodgeChance;
        public bool DodgeRoll(float value)=>value<DodgeChance;
        bool TryDodge()
        {
            if(owner.scaling.aiTier<2 || Time.time<nextDodge || motor.Held)return false;
            int count=CampusRift.Skills.DangerZoneRegistry.CopyActive(dangerZones);
            for(int i=0;i<count;i++){
                var zone=dangerZones[i];if(!zone.Contains(transform.position)||zone.token==evaluatedZone)continue;
                evaluatedZone=zone.token;DodgeAttempts++;if(!DodgeRoll(UnityEngine.Random.value))return false;
                Vector3 away=Vector3.ProjectOnPlane(transform.position-zone.center,Vector3.up).normalized;if(away.sqrMagnitude<.01f)away=transform.right;
                for(int side=0;side<4;side++){
                    Vector3 direction=Quaternion.Euler(0,side*90,0)*away;Vector3 want=zone.center+direction*(zone.radius+1.5f);want.y=transform.position.y;
                    if(UnityEngine.AI.NavMesh.SamplePosition(want,out var hit,1.5f,UnityEngine.AI.NavMesh.AllAreas) && !zone.Contains(hit.position) &&
                        !UnityEngine.AI.NavMesh.Raycast(transform.position,hit.position,out var blocked,UnityEngine.AI.NavMesh.AllAreas) && motor.MoveTo(hit.position))
                    {Dodges++;nextDodge=Time.time+3;dodgeUntil=Time.time+.8f;return true;}}
                return false;
            }
            return false;
        }

        IEnumerator Vanish()
        {
            yield return new WaitForSeconds(owner.Animation!=null?owner.Animation.DeathSeconds:.9f);
            var pool = owner.Pool;
            if (pool != null) pool.PlayBurst(transform.position + Vector3.up * 0.8f, owner.archetype.element);
            float t = 0; const float duration = 0.6f; Vector3 start = transform.position;
            while (t < duration)
            {
                t += Time.deltaTime; float k = Mathf.Clamp01(t / duration);
                if(owner.Animation!=null)owner.Animation.Dissolve(k);
                else {SetModelScale(1f - k); transform.position = start + Vector3.down * (0.25f * k);}
                yield return null;
            }
            if (pool != null) pool.Release(owner); else gameObject.SetActive(false);
        }
        void TickSoul()
        {
            if(soul==null)soul=GetComponent<CampusRift.Skills.SoulAlly>();if(soul==null||!soul.Alive||motor.Held){motor.Stop();return;}
            MonsterVitality best=null;float distance=32*32;
            foreach(var m in MonsterVitality.Active){if(m==null||m.Defeated)continue;var d=m.transform.position-transform.position;float sq=d.sqrMagnitude;if(Mathf.Abs(d.y)>2.5f||sq>=distance)continue;distance=sq;best=m;}
            if(best==null){motor.Stop();return;}float reach=soul.AttackRange;
            if(distance>reach*reach)motor.MoveTo(best.transform.position);else{motor.Stop();motor.FaceTowards(best.transform.position);if(Time.time>=alliedNext&&CombatLine.Clear(Eye,best.transform.position+Vector3.up,transform)){alliedNext=Time.time+soul.AttackCooldown;soul.Strike(best);}}
        }
        bool TickAlliedOpponent()
        {
            CampusRift.Skills.SoulAlly best=null;float nearest=player!=null?(player.position-transform.position).sqrMagnitude:196;
            foreach(var item in HealingAllies.Active){var a=item as CampusRift.Skills.SoulAlly;if(a==null||!a.Alive)continue;float d=(a.transform.position-transform.position).sqrMagnitude;if(d<nearest&&Mathf.Abs(a.transform.position.y-transform.position.y)<2&&CombatLine.Clear(Eye,a.transform.position+Vector3.up,transform)){best=a;nearest=d;}}
            if(best==null)return false;if(motor.Held){motor.Stop();return true;}
            float reach=owner.archetype.ranged?owner.archetype.preferredMax:MeleeReach;
            if(State==MinionState.Chase){if(nearest>reach*reach)motor.MoveTo(best.transform.position);else{motor.Stop();motor.FaceTowards(best.transform.position);if(Time.time>=nextAttackAt&&director.TryAcquire(owner)){alliedOpponent=best.Body;State=MinionState.Windup;stateUntil=Time.time+.5f;owner.Animation?.BeginAttack(.5f);TelegraphOutline.Show(gameObject,.5f);}}}
            else if(State==MinionState.Windup&&Time.time>=stateUntil){if(alliedOpponent!=null&&!alliedOpponent.Defeated&&(alliedOpponent.transform.position-transform.position).sqrMagnitude<=reach*reach&&CombatLine.Clear(Eye,alliedOpponent.transform.position+Vector3.up,transform)){var hit=DamageInfo.Create(owner.Damage,owner.archetype.element,DamageSource.Melee,alliedOpponent.transform.position+Vector3.up,(alliedOpponent.transform.position-transform.position).normalized,gameObject);if(alliedOpponent.ApplyDamage(hit))Strikes++;}nextAttackAt=Time.time+owner.archetype.attackCooldown;State=MinionState.Recover;stateUntil=Time.time+.5f;}
            else if(State==MinionState.Recover&&Time.time>=stateUntil){director.Release(owner);State=MinionState.Chase;}
            return true;
        }
    }
}
