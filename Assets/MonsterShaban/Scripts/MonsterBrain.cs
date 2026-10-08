using UnityEngine;

namespace CampusRift.Monsters
{
    public enum MonsterState { Idle, Patrol, Investigate, Chase, Search, Attack, ReturnToPatrol }

    [RequireComponent(typeof(MonsterNavigation), typeof(MonsterPerception), typeof(MonsterMemory))]
    public sealed class MonsterBrain : MonoBehaviour
    {
        public MonsterAIConfig config;
        public Vector3[] patrolPoints;
        [SerializeField] MonsterState currentState = MonsterState.Idle;
        [SerializeField] string intent = "";
        public MonsterState CurrentState => currentState;
        // Human-readable reason for what the monster is doing now (debug panel / logs).
        public string Intent => intent;
        public bool Roaming => roaming;
        public float PressureSeconds { get; private set; }
        public float Pressure => config == null || !config.enableTimeEscalation ? 0 : Mathf.Clamp01(PressureSeconds / Mathf.Max(1, config.EscalationSeconds));
        public float MovementMultiplier
        {
            get
            {
                if(config == null || !config.enableTimeEscalation)return 1;
                float initial=Mathf.Max(.1f,config.ChaseSpeed);
                float steps=Mathf.Floor(PressureSeconds/Mathf.Max(.1f,config.SpeedIncreaseInterval));
                float speed=Mathf.Min(Mathf.Max(initial,config.MaxChaseSpeed),initial+steps*Mathf.Max(0,config.ChaseSpeedPerStep));
                return speed/initial;
            }
        }
        public float AttackRateMultiplier => config == null || !config.enableTimeEscalation ? 1 : Mathf.Lerp(1, Mathf.Max(1, config.MaxAttackRateMultiplier), Pressure);
        public event System.Action<MonsterState> StateChanged;
        MonsterNavigation navigation;
        MonsterPerception perception;
        MonsterMemory memory;
        MonsterSearch search;
        MonsterPrediction prediction;
        MonsterCombat combat;
        MonsterVitality vitality;
        MonsterBelief belief;
        MonsterHearing hearing;
        MonsterTargetAssessment assessment;
        bool forceSearch;
        int processedScanRevision = -1;
        public MonsterTargetType CurrentVisualTarget => assessment != null ? assessment.VisibleTarget : MonsterTargetType.None;
        public MonsterTargetAssessment Assessment => assessment;
        MonsterElevatorAwareness lifts;
        InterceptionPlanner planner;
        CampusRift.Skills.MonsterBarrierTactics barriers;
        float nextDecision, stateSince, handledSoundTime = -10000, huntEndedAt = -10000, interceptUntil = -1, lastDecisionAt = -10000;
        int patrolIndex;
        bool patrolTargetAssigned, roaming;
        Vector3 interceptPoint;

        void Awake()
        {
            navigation = GetComponent<MonsterNavigation>(); perception = GetComponent<MonsterPerception>();
            memory = GetComponent<MonsterMemory>(); search = GetComponent<MonsterSearch>();
            prediction = GetComponent<MonsterPrediction>();
            combat = GetComponent<MonsterCombat>();
            vitality = GetComponent<MonsterVitality>();
            planner = GetComponent<InterceptionPlanner>();
            barriers = GetComponent<CampusRift.Skills.MonsterBarrierTactics>();
            // Older prefab instances may predate these components; they are self-configuring.
            lifts = GetComponent<MonsterElevatorAwareness>();
            if (lifts == null) { lifts = gameObject.AddComponent<MonsterElevatorAwareness>(); lifts.config = config; }
            belief = GetComponent<MonsterBelief>();
            if (belief == null) { belief = gameObject.AddComponent<MonsterBelief>(); belief.config = config; }
            hearing = GetComponent<MonsterHearing>();
            assessment = GetComponent<MonsterTargetAssessment>();
            if (assessment == null) assessment = gameObject.AddComponent<MonsterTargetAssessment>();
        }
        void OnEnable() { if (perception != null) perception.EvidenceUpdated += Observe; }
        void OnDisable() { if (perception != null) perception.EvidenceUpdated -= Observe; }
        void Observe()
        {
            processedScanRevision = perception.ScanRevision;
            assessment.Evaluate(perception, hearing);
            if (assessment.RejectedNow)
            {
                memory.RejectPhantom(assessment.RejectedPosition, assessment.RejectedSpawnTime);
                handledSoundTime = memory.LastHeardTime;
                forceSearch = true;
            }
            if (assessment.VisibleTarget == MonsterTargetType.None) return;
            if (assessment.VisibleTarget == MonsterTargetType.Player) forceSearch = false;
            var source = assessment.VisibleTarget == MonsterTargetType.Player ?
                MonsterEvidenceSource.PlayerCandidate : MonsterEvidenceSource.PhantomCandidate;
            memory.ObserveVisual(assessment.ChosenPosition, assessment.ChosenVelocity,
                assessment.ChosenTime, assessment.VisibleTarget == MonsterTargetType.Player ?
                assessment.PlayerConfidence : assessment.PhantomConfidence, source);
            if (belief != null) belief.ObserveSighting(assessment.ChosenPosition,
                assessment.ChosenVelocity, assessment.ChosenTime,
                source == MonsterEvidenceSource.PlayerCandidate ? perception.Trail.Trend : MovementTrend.Unknown);
        }
        void Update()
        {
            // Losing sight or being stunned does not reset difficulty; a new game does.
            if(config != null && (vitality == null || !vitality.Defeated) &&
                (CampusRift.UI.UIStateManager.Instance == null || CampusRift.UI.UIStateManager.Instance.GameplayInputEnabled ||
                    CampusRift.UI.UIStateManager.Instance.State == CampusRift.UI.UIState.Modal))
                PressureSeconds += Time.deltaTime;
            if (config == null || Time.time < nextDecision) return;
            nextDecision = Time.time + config.DecisionRefreshRate;
            Decide();
        }

        // Most recent real evidence: a sighting, a sound, or an observed lift trip.
        public float LatestEvidenceTime => Mathf.Max(memory.LatestTime, belief != null ? belief.LastEvidenceTime : -10000);

        public void Decide()
        {
            // Direct test calls and disabled brain ticks still consume the last completed scan.
            if (perception != null && assessment != null && processedScanRevision != perception.ScanRevision)
                Observe();
            if(vitality!=null && vitality.Suppressed)
            {intent=vitality.Defeated?"Defeated by the seal":"Suppressed by Giant Hand Seal";return;}
            // Riding a lift the agent is off the NavMesh, yet the hunt goes on.
            if (!navigation.Ready && !navigation.Riding) { SetState(MonsterState.Idle); return; }
            // Commit to the swing and recovery. The player can dodge out of the impact range.
            if (combat != null && combat.IsAttacking)
            {
                SetState(MonsterState.Attack); FollowAttack(); intent = "Attack swing / recovery"; return;
            }
            memory.Decay();
            if(barriers!=null && memory.HasEvidence && barriers.Tick(assessment.VisibleTarget!=MonsterTargetType.None?assessment.ChosenPosition:memory.LatestPosition))
            { SetState(MonsterState.Investigate); intent=barriers.Decision; return; }
            // Vision runs at 8 Hz, decisions at 4 Hz: a glimpse that began and ended between two
            // decisions (the player crossing the view while the monster turns) is still a sighting.
            bool glimpsed = memory.HasSeen && memory.LastSeenTime > lastDecisionAt;
            lastDecisionAt = Time.time;
            if (assessment.VisibleTarget != MonsterTargetType.None)
            {
                roaming = false;
                bool real = assessment.VisibleTarget == MonsterTargetType.Player;
                // Glazing lets it see, not strike: it must walk around to the door.
                if (real && combat != null && !perception.SightThroughGlass && perception.DistanceToPlayer <= combat.Reach)
                {
                    Vector3 facing = Vector3.ProjectOnPlane(perception.ObservedPosition-transform.position,Vector3.up);
                    if(facing.sqrMagnitude>0.01f)transform.rotation=Quaternion.LookRotation(facing);
                    combat.TryAttack();
                    if(combat.IsAttacking)
                    {SetState(MonsterState.Attack);FollowAttack();intent="Attacking";return;}
                }
                SetState(MonsterState.Chase);
                navigation.SetSpeed(config.ChaseSpeed);
                Vector3 pursuit = prediction == null ? assessment.ChosenPosition : real ?
                    prediction.Predict(perception,memory) : prediction.PredictCandidate(assessment.ChosenPosition,assessment.ChosenVelocity);
                Vector3 goal = real ? ChooseIntercept(pursuit) : pursuit;
                intent = real ? (goal == pursuit ? (perception.SightThroughGlass ? "Chasing (seen through glass)" : "Chasing") : intent)
                    : "Chasing uncertain visual + footstep trail";
                if (!navigation.MoveTo(goal, assessment.ChosenVelocity.sqrMagnitude>1) && (navigation.LastMoveFailed || navigation.Status == NavigationStatus.Unreachable))
                {
                    // Out of reach (ledge, roof, other side of a gap): take the closest reachable spot.
                    if (navigation.MoveNear(assessment.ChosenPosition, 6f)) intent = "Target out of reach: closing in";
                }
                return;
            }
            if (forceSearch)
            {
                forceSearch = false; roaming = false; StartSearch();
                intent = "Visual disproved; searching last evidence";
                return;
            }
            if (glimpsed && currentState != MonsterState.Chase && currentState != MonsterState.Attack && currentState != MonsterState.Search)
            { roaming = false; SetState(MonsterState.Chase); }
            if (currentState == MonsterState.Chase || currentState == MonsterState.Attack)
            {
                // Visibility itself remains false. Brief occlusion keeps the last pursuit target
                // without consulting the hidden player's transform.
                if(Time.time-memory.LastSeenTime<config.LostSightGraceTime)
                {
                    navigation.SetSpeed(config.ChaseSpeed);
                    navigation.MoveTo(prediction != null ? prediction.PredictLost(memory) : memory.LatestPosition);
                    return;
                }
                StartSearch();
            }
            if (memory.LastHeardTime > handledSoundTime && memory.LastHeardTime > memory.LastSeenTime)
            {
                handledSoundTime = memory.LastHeardTime;
                roaming = false;
                SetState(MonsterState.Investigate);
            }
            switch (currentState)
            {
                case MonsterState.Idle: SetState(MonsterState.Patrol); break;
                case MonsterState.Patrol: Patrol(); break;
                case MonsterState.Investigate:
                    intent = "Investigating a sound";
                    bool freshSound=Time.time-memory.LastHeardTime<config.HuntFreshSeconds;
                    navigation.SetSpeed(Vector3.Distance(transform.position,memory.LastHeardPosition)<3?config.SearchSpeed:
                        freshSound?config.ChaseSpeed:config.InvestigateSpeed);
                    navigation.MoveTo(memory.LastHeardPosition);
                    if ((Time.time - stateSince > 0.6f && navigation.Arrived) || Time.time - memory.LastHeardTime > 10 || (!freshSound && memory.MemoryConfidence < 0.15f) ||
                        (Time.time - stateSince > 1.5f && navigation.LastMoveFailed)) StartSearch();
                    break;
                case MonsterState.Search:
                    if (search != null) search.Tick();
                    SetIntent("Hunting: ", search);
                    // The hunt lasts while evidence keeps coming; SearchDuration counts from the latest one.
                    if (Time.time - Mathf.Max(stateSince, LatestEvidenceTime) >= config.SearchDuration || search == null || search.Finished)
                        EndHunt();
                    break;
                case MonsterState.ReturnToPatrol:
                    // Roaming the likely areas is a patrol of its own; otherwise walk back to the route.
                    if (CanRoam || (patrolTargetAssigned && navigation.Arrived)) { SetState(MonsterState.Patrol); if (CanRoam) Patrol(); break; }
                    Patrol();
                    break;
            }
        }

        void FollowAttack()
        {
            // Keep closing during the windup; otherwise a sprint always escapes a
            // stationary melee swing even when the monster has already caught up.
            if(combat.FollowingPlayer && perception.CanSeePlayer && !perception.SightThroughGlass && perception.ObservedVelocity.sqrMagnitude>1)
            {
                navigation.SetSpeed(config.ChaseSpeed);
                navigation.MoveTo(prediction!=null?prediction.Predict(perception,memory):perception.ObservedPosition,true);
            }
            else navigation.Stop();
        }

        Vector3 ChooseIntercept(Vector3 pursuit)
        {
            if (planner == null || !config.EnableInterception || prediction == null) return pursuit;
            if (Time.time < interceptUntil && perception.DistanceToPlayer > 5f) return interceptPoint;
            // Only when the player outruns a direct chase and is not already within reach.
            if (perception.Trail.PlayerSpeed < navigation.ChaseSpeed * 0.9f || perception.DistanceToPlayer < 7f) return pursuit;
            planner.Plan(perception, memory, prediction);
            if (!planner.HasIntercept) return pursuit;
            interceptPoint = planner.SelectedInterceptPoint; interceptUntil = Time.time + 1.5f;
            intent = "Cutting off at " + planner.TargetRoom;
            return interceptPoint;
        }

        void StartSearch()
        {
            SetState(MonsterState.Search);
            if (search == null) return;
            // Walking to the last sighting is pointless when it was inside a lift car that left.
            bool rider = belief != null && belief.Active && belief.RiderMass(-1) > 0.5f;
            search.SpeedLimit = float.PositiveInfinity;
            search.Begin(memory.LatestPosition, memory.LatestVelocity, !rider);
        }

        void EndHunt()
        {
            memory.Forget(); SetState(MonsterState.ReturnToPatrol); patrolTargetAssigned = false;
            huntEndedAt = Time.time; roaming = false;
        }

        SearchMode intentMode; string intentReason, intentPrefix;
        void SetIntent(string prefix, MonsterSearch source)
        {
            if (source == null) { intent = "Searching"; return; }
            if (prefix == intentPrefix && source.Mode == intentMode && ReferenceEquals(source.GoalReason, intentReason)) return;
            intentPrefix = prefix; intentMode = source.Mode; intentReason = source.GoalReason;
            intent = prefix + source.Mode + " -> " + source.GoalReason;
        }

        bool CanRoam => belief != null && belief.Active && search != null && Time.time - huntEndedAt < config.RoamDuration;

        void Patrol()
        {
            // After a hunt, keep drifting through the areas the player most plausibly is in
            // (calm pace, same belief), then fall back to the fixed patrol route.
            if (CanRoam)
            {
                if (!roaming) { roaming = true; search.Begin(belief.LastEvidencePosition, Vector3.zero, false); }
                search.SpeedLimit = config.PatrolSpeed;
                search.Tick();
                SetIntent("Roaming likely areas: ", search);
                patrolTargetAssigned = false;
                if (!search.Finished) return;
                belief.Deactivate(); // nothing plausible left to roam towards
            }
            if (roaming) { roaming = false; if (belief != null) belief.Deactivate(); }
            if (search != null) search.SpeedLimit = float.PositiveInfinity;
            intent = "Patrol route";
            navigation.SetSpeed(config.PatrolSpeed);
            if (patrolPoints == null || patrolPoints.Length == 0) { navigation.Stop(); return; }
            if (patrolTargetAssigned && navigation.Arrived) patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            bool reachable = navigation.MoveTo(patrolPoints[patrolIndex]);
            patrolTargetAssigned = true;
            if (!reachable) patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
        void SetState(MonsterState next)
        {
            if (currentState == next) return;
            currentState = next; stateSince = Time.time;
            StateChanged?.Invoke(next);
        }
    }
}
