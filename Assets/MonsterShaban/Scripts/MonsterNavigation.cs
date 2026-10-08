using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace CampusRift.Monsters
{
    public enum NavigationStatus { Idle, Direct, Partial, GraphRoute, Unreachable, Recovering, Link, Lift }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class MonsterNavigation : MonoBehaviour, CampusRift.Combat.IMotionHold
    {
        public MonsterAIConfig config;
        public RoomGraph roomGraph;
        [SerializeField] Vector3 currentDestination;
        [SerializeField] bool destinationReachable;
        [SerializeField] NavigationStatus status;
        [SerializeField] int recoveries;
        [SerializeField] int liftRides;
        public Vector3 CurrentDestination => currentDestination;
        public bool DestinationReachable => destinationReachable;
        public NavigationStatus Status => status;
        public int Recoveries => recoveries;
        public int LiftRides => liftRides;
        // True when the last request produced no usable movement at all (no path, or a destination
        // that recently trapped the agent). Callers pick another goal instead of standing still.
        public bool LastMoveFailed { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public bool Ready => Agent != null && Agent.enabled && Agent.isOnNavMesh;
        public bool Arrived => Ready && ride == null && routeIndex >= route.Count && SegmentArrived;
        bool SegmentArrived => !Agent.pathPending && (!Agent.hasPath || Agent.remainingDistance <= Agent.stoppingDistance + 0.35f);
        public IReadOnlyList<Vector3> Route => route;
        public int RouteIndex => routeIndex;
        readonly List<Vector3> route = new List<Vector3>();
        int routeIndex;
        Vector3 requestedDestination, lastEvidence;
        float nextStrategyTime;
        NavMeshPath path;
        MonsterVitality vitality;
        MonsterBrain brain;
        float baseAcceleration;
        // Status slows (Chill, Freeze) multiply on top of the brain's own pacing.
        public float ScaleSpeed(float speed) => speed * (brain != null ? brain.MovementMultiplier : 1f) * (statusEffects != null ? statusEffects.SpeedMultiplier : 1f);
        CampusRift.Combat.StatusEffectHost statusEffects;
        public float ChaseSpeed => ScaleSpeed(config.ChaseSpeed);
        float nextPathTime;
        bool hasDestination;
        bool doorBlocked;
        // Stuck detection / recovery
        Vector3 progressAnchor;
        float stuckTimer, doorWait, lastRecovery = -100, sidestepUntil = -1;
        int recoveryStage;
        Vector3 failedDestination; float failedUntil = -1;
        // Off-mesh links (steep flights, doors onto a lower flight)
        bool onLink; Vector3 linkStart, linkEnd; float linkWait;
        int barrierRevision;
        float barrierRefreshAt=-1;
        Vector3 previousPosition;
        static CampusAutomaticDoor[] doors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetDoors() => doors = null;

        void Awake() { Agent = GetComponent<NavMeshAgent>(); baseAcceleration=Agent.acceleration; brain=GetComponent<MonsterBrain>(); path = new NavMeshPath(); body = GetComponent<Collider>(); vitality=GetComponent<MonsterVitality>(); statusEffects=GetComponent<CampusRift.Combat.StatusEffectHost>(); }
        void Start() { lifts = GetComponent<MonsterElevatorAwareness>(); previousPosition=transform.position; }
        public void SetSpeed(float speed)
        {
            if (!Ready) return;
            Agent.speed = ScaleSpeed(speed);
            Agent.acceleration = Mathf.Max(baseAcceleration, Agent.speed * 4f);
        }
        public void Stop()
        {
            // Stepping into, riding or out of a car cannot be interrupted; waiting for one can.
            if (ride != null) { if (ride.Phase >= RidePhase.Boarding) return; AbortRide("stopped"); }
            // ResetPath clears isStopped in Unity 6; apply the stop after clearing the route.
            if (Ready) { Agent.ResetPath(); Agent.isStopped = true; Agent.velocity = Vector3.zero; }
            hasDestination = false; route.Clear(); routeIndex = 0; status = NavigationStatus.Idle; sidestepUntil = -1; nextPathTime = 0;
        }
        public void HoldForDoor(bool blocked)
        {
            doorBlocked = blocked;
            if (Ready && hasDestination) Agent.isStopped = blocked || (vitality != null && vitality.Suppressed);
        }

        public void HoldForSeal()
        {
            // Preserve a link/boarding route so it can resume after the stun. ResetPath
            // would complete an off-mesh link immediately and move the monster away.
            if (!Riding && (!Ready || !Agent.isOnOffMeshLink)) Stop();
            if (Ready) { Agent.isStopped = true; Agent.velocity = Vector3.zero; }
        }

        public bool IsFailedDestination(Vector3 point) =>
            Time.time < failedUntil && (point - failedDestination).sqrMagnitude < 2.5f * 2.5f;

        // Never warp to a sampled point. Invalid/unreachable evidence is handled by search.
        public bool MoveTo(Vector3 evidence, bool movingTarget = false)
        {
            if(vitality!=null && vitality.Suppressed)return false;
            if (config == null) return false;
            if (Ready) Agent.autoBraking = !movingTarget;
            if (ride != null) return ContinueRide(evidence);
            if (!Ready) return false;
            if (Agent.isOnOffMeshLink) return true;
            lastEvidence = evidence;
            if (config.RideLifts && ConsiderLift(evidence)) return true;
            return MoveToCore(evidence);
        }

        bool MoveToCore(Vector3 evidence)
        {
            if (path == null) path = new NavMeshPath();
            if (Time.time < sidestepUntil) return true;
            if (Time.time < nextPathTime) return destinationReachable;
            // Keep following a strategic route while its goal stays roughly where it was: a moving
            // target must not flip the monster between partial NavMesh paths and graph routes.
            float tolerance = Mathf.Max(3f, 0.2f * Vector3.Distance(transform.position, requestedDestination));
            if (routeIndex < route.Count && (evidence - requestedDestination).sqrMagnitude < tolerance * tolerance)
            {
                if (SegmentArrived) routeIndex++;
                if (routeIndex < route.Count)
                {
                    nextPathTime = Time.time + config.PathRefreshRate;
                    if (Agent.CalculatePath(route[routeIndex], path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        Agent.SetPath(path); Agent.isStopped = doorBlocked; hasDestination = true;
                        status = NavigationStatus.GraphRoute; LastMoveFailed = false;
                        return true;
                    }
                    route.Clear(); routeIndex = 0;
                }
            }
            if (hasDestination && (evidence - currentDestination).sqrMagnitude < 0.64f && Agent.hasPath && !Agent.isPathStale)
                return destinationReachable;
            nextPathTime = Time.time + config.PathRefreshRate;
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(evidence, out hit, 2f, Agent.areaMask) || Mathf.Abs(hit.position.y - evidence.y) > 1.5f)
            {
                destinationReachable = false; LastMoveFailed = !hasDestination; status = NavigationStatus.Unreachable;
                return false;
            }
            if (IsFailedDestination(hit.position))
            {
                destinationReachable = false; LastMoveFailed = true; status = NavigationStatus.Unreachable;
                return false;
            }
            currentDestination = hit.position;
            bool found = Agent.CalculatePath(hit.position, path);
            destinationReachable = found && path.status == NavMeshPathStatus.PathComplete;
            bool viaGraph = false;
            if (!destinationReachable && Time.time >= nextStrategyTime && roomGraph != null)
            {
                nextStrategyTime = Time.time + 0.75f;
                requestedDestination = hit.position;
                routeIndex = 0;
                if (RoomPathfinder.Find(roomGraph, transform.position, hit.position, route) && route.Count > 0)
                {
                    if (Agent.CalculatePath(route[0], path) && path.status == NavMeshPathStatus.PathComplete)
                    { destinationReachable = true; found = true; viaGraph = true; }
                }
            }
            else if (destinationReachable) { route.Clear(); routeIndex = 0; }
            if (found && path.corners.Length > 1)
            {
                // A partial path still brings the monster to the closest reachable point.
                Agent.isStopped = doorBlocked; Agent.SetPath(path); hasDestination = true; LastMoveFailed = false;
                status = viaGraph ? NavigationStatus.GraphRoute : destinationReachable ? NavigationStatus.Direct : NavigationStatus.Partial;
            }
            else if (!destinationReachable)
            {
                bool standingThere = found && path.corners.Length <= 1;
                Stop();
                LastMoveFailed = !standingThere;
                status = NavigationStatus.Unreachable;
            }
            return destinationReachable;
        }

        // For a point off the walkable surface (ledge, roof, car top): go to the closest walkable
        // spot under/next to it instead of standing still.
        public bool MoveNear(Vector3 point, float radius)
        {
            if (!Ready || config == null || ride != null) return ride != null;
            if (!NavMesh.SamplePosition(point, out var hit, radius, Agent.areaMask) || IsFailedDestination(hit.position)) return false;
            if ((hit.position - currentDestination).sqrMagnitude < 0.64f && hasDestination) return true;
            nextPathTime = 0;
            MoveTo(hit.position);
            return !LastMoveFailed;
        }

        void Update()
        {
            if (config == null) return;
            if(vitality!=null && vitality.Suppressed)
            {
                HoldForSeal();stuckTimer=0;progressAnchor=transform.position;
                // A stunned passenger is still carried by its platform, but cannot walk
                // into/out of the lift or advance a stair link until suppression ends.
                if(ride!=null && ride.Phase==RidePhase.Riding && ride.Info.Elevator!=null)
                {
                    float cabinY=ride.Info.Elevator.cabin.position.y;
                    transform.position+=Vector3.up*(cabinY-ride.CabinY);ride.CabinY=cabinY;
                }
                return;
            }
            // Carving becomes visible to queries on a later frame. Invalidate now and once
            // more after it settles; never rebake the campus or its multi-floor graph.
            if(barrierRevision!=CampusRift.Skills.VoidWall.Revision)
            {
                barrierRevision=CampusRift.Skills.VoidWall.Revision;barrierRefreshAt=Time.time+0.18f;
                if(Ready && !Agent.isOnOffMeshLink && !Riding){ResetPlanning();failedUntil=-1;}
            }
            if(barrierRefreshAt>=0 && Time.time>=barrierRefreshAt)
            {
                barrierRefreshAt=-1;
                if(Ready && !Agent.isOnOffMeshLink && !Riding){ResetPlanning();failedUntil=-1;}
            }
            // Links are climbed on the way to anything, a lift lobby included.
            if (Ready && Agent.isOnOffMeshLink) { TraverseLink(); stuckTimer = 0; progressAnchor = transform.position; return; }
            onLink = false;
            if (ride != null) { UpdateRide(); stuckTimer = 0; progressAnchor = transform.position; return; }
            if (!Ready || !hasDestination) { stuckTimer = 0; doorWait = 0; return; }
            if (doorBlocked)
            {
                // A door that never finishes opening must not hold the monster forever.
                doorWait += Time.deltaTime;
                if (doorWait > 4f) { doorWait = 0; Recover("door did not open"); }
                return;
            }
            doorWait = 0;
            bool wantsToMove = !Agent.isStopped && !Agent.pathPending && Agent.hasPath &&
                Agent.remainingDistance > Agent.stoppingDistance + 0.4f && Agent.speed > 0.5f;
            if (!wantsToMove || (transform.position - progressAnchor).sqrMagnitude > 0.35f * 0.35f)
            {
                progressAnchor = transform.position; stuckTimer = 0;
                if (Time.time - lastRecovery > 8f) recoveryStage = 0;
                return;
            }
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= config.StuckSeconds) { stuckTimer = 0; progressAnchor = transform.position; Recover("no progress"); }
        }

        // Escalating recovery: fresh path, then a sidestep, then give up on that destination for a
        // while so the caller chooses a different goal. Never teleports.
        void Recover(string reason)
        {
            recoveries++;
            lastRecovery = Time.time;
            status = NavigationStatus.Recovering;
            if (recoveryStage == 0)
            {
                recoveryStage = 1;
                Vector3 target = lastEvidence;
                ResetPlanning();
                MoveToCore(target);
                return;
            }
            if (recoveryStage == 1)
            {
                recoveryStage = 2;
                Vector3 away = Agent.desiredVelocity.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, Agent.desiredVelocity.normalized) : Random.insideUnitSphere;
                away.y = 0;
                if (away.sqrMagnitude < 0.01f) away = Vector3.right;
                away = away.normalized * (Random.value < 0.5f ? -1f : 1f);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 probe = transform.position + Quaternion.Euler(0, i * 55f, 0) * away * Random.Range(1.5f, 2.5f);
                    if (NavMesh.SamplePosition(probe, out var hit, 1f, Agent.areaMask) && Mathf.Abs(hit.position.y - transform.position.y) < 0.6f &&
                        Agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        Agent.SetPath(path); Agent.isStopped = false; sidestepUntil = Time.time + 1.2f;
                        nextPathTime = sidestepUntil; return;
                    }
                }
            }
            recoveryStage = 0;
            failedDestination = currentDestination; failedUntil = Time.time + 10f;
            Stop();
            LastMoveFailed = true;
            status = NavigationStatus.Unreachable;
        }

        void ResetPlanning()
        {
            nextPathTime = 0; nextStrategyTime = 0; hasDestination = false; route.Clear(); routeIndex = 0; sidestepUntil = -1;
            if (Ready) Agent.ResetPath();
        }

        public bool Reachable(Vector3 target, out Vector3 sampled)
        {
            sampled = target;
            if (!Ready) return false;
            if (path == null) path = new NavMeshPath();
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(target, out hit, 1.5f, Agent.areaMask) || Mathf.Abs(hit.position.y - target.y) > 1f) return false;
            sampled = hit.position;
            return Agent.CalculatePath(sampled, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        // ---------------- Off-mesh links: climbed on foot ----------------

        // Links bridge what the agent cannot walk but a body can climb: stair flights steeper than the
        // agent slope, or a door opening onto a flight a metre lower. Walk the link, opening doors on it.
        void TraverseLink()
        {
            var data = Agent.currentOffMeshLinkData;
            if (!onLink) { onLink = true; linkStart = transform.position; linkEnd = data.endPos + Vector3.up * Agent.baseOffset; linkWait = 0; status = NavigationStatus.Link; }
            if (!DoorsOpenAlong(transform.position, linkEnd) && linkWait < 3f) { linkWait += Time.deltaTime; return; }
            if (StepToward(linkEnd, Mathf.Clamp(Agent.speed * 0.6f, 1.6f, 3.2f)))
            {
                Agent.CompleteOffMeshLink();
                // The agent still carries the velocity it had before the link: without this it drifts
                // back and can hop onto the link again the other way.
                Vector3 heading = linkEnd - linkStart; heading.y = 0;
                Agent.velocity = heading.sqrMagnitude > 0.01f ? heading.normalized * Mathf.Min(Agent.speed, 3.2f) : Vector3.zero;
                onLink = false; LinksTraversed++;
                // Plan again from the far side: the old corridor can lead straight back over the link.
                nextPathTime = 0;
                if (ride != null) ride.NextPath = 0;
                else if (hasDestination) { hasDestination = false; MoveToCore(lastEvidence); }
            }
        }

        bool DoorsOpenAlong(Vector3 a, Vector3 b)
        {
            if (doors == null) doors = FindObjectsByType<CampusAutomaticDoor>();
            Vector3 d = b - a; float length = d.magnitude;
            if (length < 0.01f) return true;
            var ray = new Ray(a + Vector3.up * 0.3f, d / length);
            bool open = true;
            foreach (var door in doors)
            {
                if (door == null || !door.isActiveAndEnabled) continue;
                var zone = door.doorway; zone.Expand(new Vector3(0.3f, 0.6f, 0.3f));
                if (!zone.IntersectRay(ray, out float hitAt) || hitAt > length + 0.3f) continue;
                Vector3 at = a + d / length * hitAt; at.y = door.doorway.min.y;
                door.RequestOpenFrom(at);
                if (door.OpenAmount < 0.9f) open = false;
            }
            return open;
        }

        bool StepToward(Vector3 target, float speed)
        {
            Vector3 to = target - transform.position;
            Vector3 next=Vector3.MoveTowards(transform.position,target,speed*Time.deltaTime);
            if(CampusRift.Skills.VoidWall.Blocking(transform.position,next,0.25f)!=null)return false;
            Vector3 flat = new Vector3(to.x, 0, to.z);
            if (flat.sqrMagnitude > 0.0025f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 1f - Mathf.Exp(-10f * Time.deltaTime));
            float step = speed * Time.deltaTime;
            if (to.magnitude <= step) { transform.position = target; return true; }
            transform.position += to.normalized * step;
            return false;
        }

        void LateUpdate()
        {
            // Swept contact guard covers the brief carving delay. Clamp to the near side;
            // this is collision correction, not a warp to a sampled NavMesh position.
            if(Ready && !Agent.isOnOffMeshLink && !Riding && (transform.position-previousPosition).sqrMagnitude<16f)
            {
                var wall=CampusRift.Skills.VoidWall.Blocking(previousPosition,transform.position,Agent.radius*0.85f);
                if(wall!=null && !wall.BlocksSegment(previousPosition,previousPosition,Agent.radius*0.85f))
                {
                    Vector3 low=previousPosition,high=transform.position;
                    for(int i=0;i<8;i++){Vector3 middle=(low+high)*0.5f;if(wall.BlocksSegment(previousPosition,middle,Agent.radius*0.85f))high=middle;else low=middle;}
                    Agent.Move(low-transform.position);Agent.velocity=Vector3.zero;
                }
            }
            previousPosition=transform.position;
        }

        // ---------------- Lifts: taken when faster than the stairs ----------------

        enum RidePhase { ToLanding, Calling, Boarding, Riding, Exiting }
        sealed class LiftRide
        {
            public int Lift, From, To, Car, GoalFloor = -1;
            public CampusNavGraph.Lift Info;
            public RidePhase Phase;
            public Vector3 Goal;
            public float PhaseSince, LastCall = -100, LastCheck, NextPath, CabinY, WalkBudget, Estimate, StoppedSince = -1;
            public readonly List<Vector3> Waypoints = new List<Vector3>(3);
        }
        LiftRide ride;
        MonsterElevatorAwareness lifts;
        Collider body;
        CampusNavGraph graph;
        CampusNavGraph.Scratch graphScratch;
        float[] walkCost;
        float nextLiftCheck;
        [SerializeField] string lastLiftDecision = "";

        public bool UsingLift => ride != null;
        // Stepping in, carried by the car or stepping out: the agent is off the NavMesh meanwhile.
        public bool Riding => ride != null && ride.Phase >= RidePhase.Boarding;
        public string LiftPlan => ride == null ? "" :
            $"lift {ride.Info.Elevator.building} F{ride.From + 1}->F{ride.To + 1}: {ride.Phase} (est. {ride.Estimate:F0}s)";
        // Last lift-versus-stairs comparison (debug panel / logs).
        public string LastLiftDecision => lastLiftDecision;
        public int LinksTraversed { get; private set; }

        bool EnsureGraph()
        {
            if (graph != null && walkCost != null && walkCost.Length == graph.Count) return true;
            if (Agent == null || roomGraph == null) return false;
            graph = CampusNavGraph.For(roomGraph, Agent.agentTypeID);
            if (graph == null) return false;
            graphScratch = graph.CreateScratch();
            walkCost = new float[graph.Count];
            return true;
        }

        static int FloorOf(CampusElevator lift, float height)
        {
            int floor = CampusNavGraph.FloorAt(lift, height);
            return Mathf.Abs(lift.floors[floor].height - height) < 1.6f ? floor : -1;
        }

        // Floor of `point` served by this ride's lift with a walkable lobby, else -1.
        int ServedFloor(Vector3 point)
        {
            int floor = FloorOf(ride.Info.Elevator, point.y);
            return floor >= 0 && ride.Info.Landing[floor] >= 0 ? floor : -1;
        }

        NavMeshQueryFilter Filter => new NavMeshQueryFilter { agentTypeID = Agent.agentTypeID, areaMask = Agent.areaMask };

        // Seconds to walk the NavMesh path: its length at `speed`, plus braking and re-accelerating
        // at every turn (a switchback staircase turns 180 degrees twice per floor, which costs the
        // agent more time than the flights themselves). Negative when there is no walkable path.
        float WalkSeconds(Vector3 from, Vector3 to, float speed)
        {
            var filter = Filter;
            if (!NavMesh.SamplePosition(to, out var hit, 2f, filter)) return -1f;
            if (!NavMesh.CalculatePath(from, hit.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
                return Mathf.Abs(hit.position.y - from.y) < 1.5f ? Vector3.Distance(from, hit.position) * 1.6f / speed : -1f;
            var corners = path.corners;
            float length = 0, turns = 0;
            for (int i = 1; i < corners.Length; i++)
            {
                length += Vector3.Distance(corners[i - 1], corners[i]);
                if (i + 1 >= corners.Length) continue;
                Vector3 a = corners[i] - corners[i - 1], b = corners[i + 1] - corners[i]; a.y = 0; b.y = 0;
                if (a.sqrMagnitude > 0.0001f && b.sqrMagnitude > 0.0001f) turns += Vector3.Angle(a, b) / 180f;
            }
            return length / speed + turns * 2f * speed / Mathf.Max(1f, Agent.acceleration);
        }

        // Time to `goal` on foot, stairs included: the NavMesh path when there is one, else the
        // walkable graph (its edges are measured paths) with the same turning allowance per floor.
        float StairsSeconds(Vector3 goal, float speed)
        {
            float direct = WalkSeconds(transform.position, goal, speed);
            if (direct >= 0f) return direct;
            graph.TravelCosts(transform.position, walkCost, graphScratch, 900f);
            int goalNode = graph.Nearest(goal, 1.8f, 12f);
            if (goalNode < 0 || float.IsInfinity(walkCost[goalNode])) return float.PositiveInfinity;
            float floors = Mathf.Abs(goal.y - transform.position.y) / 3.5f;
            return (walkCost[goalNode] + Vector3.Distance(goal, graph.Position[goalNode])) / speed + floors * 4f * speed / Mathf.Max(1f, Agent.acceleration);
        }

        // How long until a car stands open at `floor`, from what this monster knows about the car
        // (its floor display, when it has read it); unknown cars get the lift's average wait.
        float ExpectedWait(int lift, int floor, float maxAge)
        {
            var info = graph.Lifts[lift]; var e = info.Elevator;
            var k = lifts != null && lift < lifts.Knowledge.Length ? lifts.Knowledge[lift] : null;
            if (k == null || Time.time - k.ReadAt > maxAge || k.Floor < 0) return info.Wait;
            if (k.Floor == floor && !k.Moving) return e.doorDuration;
            // A moving car first finishes its trip (doors open and close there) before coming here.
            return info.Seconds[Mathf.Clamp(k.Floor, 0, e.FloorCount - 1), floor] + (k.Moving ? 3f + e.holdOpenSeconds : 0f);
        }

        // Take a lift only when walking to it, waiting, riding and walking out beats the stairs by
        // LiftAdvantageSeconds. Costs come from the walkable graph and the lift's own timings.
        bool ConsiderLift(Vector3 goal)
        {
            if (Time.time < nextLiftCheck || Mathf.Abs(goal.y - transform.position.y) < 2.8f) return false;
            nextLiftCheck = Time.time + 2.5f;
            if (!EnsureGraph() || graph.Lifts.Length == 0) return false;
            float speed = Mathf.Max(2.5f, Agent.speed);
            float stairs = StairsSeconds(goal, speed);
            graph.TravelCosts(transform.position, walkCost, graphScratch, 900f);
            float best = float.PositiveInfinity; int bestLift = -1, bestFrom = -1, bestTo = -1; float bestWalk = 0;
            for (int l = 0; l < graph.Lifts.Length; l++)
            {
                var info = graph.Lifts[l]; var e = info.Elevator;
                if (e == null || !e.isActiveAndEnabled) continue;
                int from = FloorOf(e, transform.position.y), to = FloorOf(e, goal.y);
                if (from < 0 || to < 0 || from == to || info.Landing[from] < 0 || info.Landing[to] < 0) continue;
                if (float.IsInfinity(walkCost[info.Landing[from]]) || walkCost[info.Landing[from]] > 70f) continue;
                float walkIn = WalkSeconds(transform.position, info.Lobby[from], speed);
                float walkOut = WalkSeconds(info.Lobby[to], goal, speed);
                if (walkIn < 0 || walkOut < 0 || walkOut * speed > 60f) continue;
                float time = walkIn + ExpectedWait(l, from, 30f) + info.Seconds[from, to] + 3f + walkOut;
                if (time < best) { best = time; bestLift = l; bestFrom = from; bestTo = to; bestWalk = walkIn; }
            }
            if (bestLift < 0) return false;
            string stairsText = float.IsInfinity(stairs) ? "none" : stairs.ToString("F0") + "s";
            // Freshly seeing the car already waiting here removes the uncertain recall wait.
            // Keep the larger safety margin for cars elsewhere or stale/unseen information.
            var knowledge=lifts!=null&&bestLift<lifts.Knowledge.Length?lifts.Knowledge[bestLift]:null;
            bool waitingHere=knowledge!=null&&knowledge.Floor==bestFrom&&!knowledge.Moving&&Time.time-knowledge.ReadAt<=5f;
            float advantage=waitingHere?Mathf.Min(1f,config.LiftAdvantageSeconds):config.LiftAdvantageSeconds;
            bool take = best + advantage < stairs;
            lastLiftDecision = $"lift {graph.Lifts[bestLift].Elevator.building} F{bestFrom + 1}->F{bestTo + 1} {best:F0}s vs stairs {stairsText}: {(take ? "lift" : "stairs")}";
            if (!take) return false;
            ride = new LiftRide { Lift = bestLift, From = bestFrom, To = bestTo, GoalFloor = bestTo, Info = graph.Lifts[bestLift], Goal = goal, Estimate = best,
                Phase = RidePhase.ToLanding, PhaseSince = Time.time, WalkBudget = bestWalk * 2f + 10f };
            ResetPlanning();
            status = NavigationStatus.Lift; LastMoveFailed = false; destinationReachable = true; currentDestination = goal;
            NoteOwn(90f);
            return true;
        }

        void NoteOwn(float seconds) { if (lifts != null && ride != null) lifts.NoteOwnRide(ride.Lift, ride.From, ride.To, seconds); }

        bool ContinueRide(Vector3 goal)
        {
            lastEvidence = goal;
            int floor = ServedFloor(goal);
            if (ride.Phase <= RidePhase.Calling)
            {
                if (floor < 0 || floor == ride.From)
                {
                    // The goal is no longer up/down this lift: walk instead.
                    AbortRide("goal moved off the lift's floors");
                    return Ready && MoveToCore(goal);
                }
                if (floor != ride.To) { ride.To = floor; NoteOwn(60f); }
            }
            ride.Goal = goal; ride.GoalFloor = floor; currentDestination = goal;
            LastMoveFailed = false; destinationReachable = true;
            return true;
        }

        void AbortRide(string reason)
        {
            if (ride == null) return;
            lastLiftDecision = LiftPlan + " aborted: " + reason;
            ride = null;
            nextLiftCheck = Time.time + 8f; // do not flip straight back to the same plan
            status = NavigationStatus.Idle;
            if (Ready) ResetPlanning();
        }

        void SetPhase(RidePhase phase) { ride.Phase = phase; ride.PhaseSince = Time.time; }

        void UpdateRide()
        {
            var e = ride.Info.Elevator;
            if (e == null) { AbortRide("lift gone"); return; }
            // Something else placed the agent back on the NavMesh (reset, respawn): the ride is over.
            if (ride.Phase >= RidePhase.Boarding && Agent.enabled) { if (body != null) CampusElevator.RegisterOccupant(body); ride = null; ResetPlanning(); return; }
            status = NavigationStatus.Lift;
            float now = Time.time;
            switch (ride.Phase)
            {
                case RidePhase.ToLanding:
                {
                    if (!Ready) { AbortRide("off the NavMesh"); return; }
                    Vector3 lobby = ride.Info.Lobby[ride.From], to = lobby - transform.position;
                    if (new Vector2(to.x, to.z).magnitude < Mathf.Max(1.2f, Agent.stoppingDistance + 0.6f) && Mathf.Abs(to.y) < 1.2f)
                    {
                        Agent.ResetPath(); hasDestination = false; SetPhase(RidePhase.Calling);
                        return;
                    }
                    if (now >= ride.NextPath)
                    {
                        ride.NextPath = now + 0.5f; nextPathTime = 0;
                        MoveToCore(lobby); status = NavigationStatus.Lift;
                        if (LastMoveFailed) { AbortRide("lobby unreachable"); return; }
                    }
                    if (now - ride.PhaseSince > ride.WalkBudget) AbortRide("too slow to reach the lift");
                    return;
                }
                case RidePhase.Calling:
                {
                    Face(-e.outward);
                    bool here = e.CurrentFloor == ride.From && !e.IsMoving;
                    if (here && e.DoorAmount > 0.9f) { BeginBoarding(e); return; }
                    bool opening = here && (e.State == CampusElevator.LiftState.Opening || e.State == CampusElevator.LiftState.Open);
                    if (!opening && !e.IsQueued(ride.From) && now - ride.LastCall > 1.5f)
                    {
                        // Press the call button (again if the car closed or left without us).
                        e.RequestFloor(ride.From); ride.LastCall = now; NoteOwn(60f);
                    }
                    if (now - ride.LastCheck > 1f) { ride.LastCheck = now; if (!StillWorthWaiting()) { AbortRide("the stairs are faster now"); return; } }
                    if (now - ride.PhaseSince > config.LiftMaxWait) AbortRide("no car came");
                    return;
                }
                case RidePhase.Boarding:
                    if (e.IsMoving || e.CurrentFloor != ride.From)
                    {   // Cannot happen while the monster holds the doorway; never follow a car that left.
                        BeginExit(e, ride.From); return;
                    }
                    if (StepAlong(config.LiftBoardSpeed))
                    {
                        SetPhase(RidePhase.Riding);
                        ride.CabinY = e.cabin.position.y;
                        if (body != null) CampusElevator.UnregisterOccupant(body); // the doors may now close with us inside
                        e.RequestFloor(ride.To); ride.LastCall = now; NoteOwn(60f);
                    }
                    return;
                case RidePhase.Riding:
                {
                    // Carried by the car: follow its exact vertical displacement, like its passenger.
                    float dy = e.cabin.position.y - ride.CabinY; ride.CabinY = e.cabin.position.y;
                    if (dy != 0f) transform.position += Vector3.up * dy;
                    if (e.IsMoving) { ride.StoppedSince = -1; return; }
                    if (ride.StoppedSince < 0) ride.StoppedSince = now;
                    int floor = e.CurrentFloor;
                    bool open = e.DoorAmount > 0.9f, lobby = ride.Info.Landing[floor] >= 0;
                    float stopped = now - ride.StoppedSince;
                    if (open && lobby && ((floor == ride.To && floor != ride.From) || floor == ride.GoalFloor ||
                        (floor != ride.From && stopped > 8f) || stopped > 25f))
                    {
                        // Destination, the floor the goal moved to, or a car that will not go on.
                        BeginExit(e, floor); return;
                    }
                    if (!(open && floor == ride.To) && !e.IsQueued(ride.To) && now - ride.LastCall > 2f)
                    { e.RequestFloor(ride.To); ride.LastCall = now; NoteOwn(60f); }
                    return;
                }
                case RidePhase.Exiting:
                    if (StepAlong(config.LiftBoardSpeed)) FinishRide();
                    return;
            }
        }

        // While waiting at the doors the display is in view: re-check that the car still beats the stairs.
        bool StillWorthWaiting()
        {
            var k = lifts != null && ride.Lift < lifts.Knowledge.Length ? lifts.Knowledge[ride.Lift] : null;
            if (k == null || Time.time - k.ReadAt > 2f || !EnsureGraph()) return true; // cannot read the display: keep waiting
            float speed = Mathf.Max(2.5f, Agent.speed);
            float walkOut = WalkSeconds(ride.Info.Lobby[ride.To], ride.Goal, speed);
            float liftTime = ExpectedWait(ride.Lift, ride.From, 2f) + ride.Info.Seconds[ride.From, ride.To] + 3f + Mathf.Max(0, walkOut);
            float stairs = StairsSeconds(ride.Goal, speed);
            ride.Estimate = liftTime;
            return liftTime < stairs + 2f;
        }

        void Face(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1f - Mathf.Exp(-6f * Time.deltaTime));
        }

        Vector3 CarCenter(CampusElevator e, int car, int floor)
        {
            var space = e.cabinSpaces[Mathf.Clamp(car, 0, e.cabinSpaces.Length - 1)];
            return new Vector3(space.center.x, e.floors[floor].height + 0.05f, space.center.z);
        }

        void BeginBoarding(CampusElevator e)
        {
            // The car whose doors are closest; step through them to its centre.
            var landing = e.floors[ride.From]; int car = 0; float best = float.PositiveInfinity;
            for (int c = 0; c < landing.entrances.Length; c++)
            {
                float d = (landing.entrances[c] - transform.position).sqrMagnitude;
                if (d < best) { best = d; car = c; }
            }
            ride.Car = car;
            ride.Waypoints.Clear();
            ride.Waypoints.Add(landing.entrances[car] + Vector3.up * 0.05f);
            ride.Waypoints.Add(CarCenter(e, car, ride.From));
            if (Ready) { Agent.ResetPath(); hasDestination = false; }
            Agent.enabled = false;
            SetPhase(RidePhase.Boarding);
            if (lifts != null) lifts.NoteOwnBoarding(ride.Lift, ride.From);
        }

        void BeginExit(CampusElevator e, int floor)
        {
            if (body != null) CampusElevator.RegisterOccupant(body); // hold the doors while stepping out
            var entrances = e.floors[floor].entrances;
            ride.Waypoints.Clear();
            ride.Waypoints.Add(entrances[Mathf.Clamp(ride.Car, 0, entrances.Length - 1)] + Vector3.up * 0.05f);
            ride.Waypoints.Add(ride.Info.Lobby[floor]);
            ride.To = floor;
            SetPhase(RidePhase.Exiting);
        }

        bool StepAlong(float speed)
        {
            if (ride.Waypoints.Count == 0) return true;
            if (StepToward(ride.Waypoints[0], speed)) ride.Waypoints.RemoveAt(0);
            return ride.Waypoints.Count == 0;
        }

        void FinishRide()
        {
            // Back on the walkable surface where the monster now stands (the lobby point itself).
            if (NavMesh.SamplePosition(transform.position, out var hit, 1.5f, Agent.areaMask)) transform.position = hit.position;
            Agent.enabled = true;
            if (Agent.isOnNavMesh) Agent.Warp(transform.position);
            liftRides++;
            NoteOwn(15f);
            lastLiftDecision = LiftPlan.Replace(": Exiting", "") + " done";
            ride = null;
            nextLiftCheck = Time.time + 4f;
            status = NavigationStatus.Idle;
            ResetPlanning();
        }

        void OnEnable()
        {
            // An interrupted ride (component or object disabled mid-ride) leaves the agent off.
            if (ride == null && Agent != null && !Agent.enabled) Agent.enabled = true;
        }

        void OnDisable()
        {
            if (ride != null && Agent != null && !Agent.enabled && body != null) CampusElevator.RegisterOccupant(body);
            ride = null; onLink = false;
        }

        void OnDrawGizmosSelected()
        {
            if (ride == null || ride.Info == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, ride.Info.Lobby[ride.From] + Vector3.up * 0.5f);
            Gizmos.DrawWireCube(ride.Info.Lobby[ride.From] + Vector3.up, new Vector3(0.8f, 2f, 0.8f));
            Gizmos.DrawWireCube(ride.Info.Lobby[ride.To] + Vector3.up, new Vector3(0.8f, 2f, 0.8f));
            Gizmos.DrawLine(ride.Info.Lobby[ride.From] + Vector3.up, ride.Info.Lobby[ride.To] + Vector3.up);
        }
    }
}
