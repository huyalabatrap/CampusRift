using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CampusRift.Monsters
{
    public enum BeliefMode : byte { Moving, Holding, InLift }

    [Serializable]
    public struct BeliefParticle
    {
        public int Node, Next, Previous;     // Next < 0: standing on Node
        public float Progress, Length;       // metres along Node -> Next
        public float Speed, WalkSpeed;       // flee / calm speeds (m/s)
        public float FleeUntil;              // running away until this time
        public float Until;                  // Holding: resume time. InLift: exit time. Moving: no lift before this time
        public Vector2 Heading;              // preferred planar direction
        public sbyte Vertical;               // +1 upstairs, -1 downstairs, 0 no preference
        public float Weight;
        public BeliefMode Mode;
        public short Lift, LiftFrom, LiftTo; // car being ridden
        public float BoardTime;
        public short ViaLift, ViaFloor;      // provenance of the last (unconfirmed) ride
        public float ViaTime;
    }

    // "Where could the player be now?" as a particle filter over the static campus topology
    // (occupancy-map style). Each particle is one hypothesis that keeps moving like a person
    // would: fleeing along its last seen heading, then walking, pausing in rooms, climbing
    // stairs or taking a lift. Only this monster's own perceptions change it: sightings,
    // sounds it heard, places it looked at and found empty, and elevator displays/chimes.
    // It never reads the player's transform.
    [DisallowMultipleComponent]
    public sealed class MonsterBelief : MonoBehaviour
    {
        public MonsterAIConfig config;
        [SerializeField] bool active;
        [SerializeField] int reseeds;
        public bool Active => active;
        public float LastEvidenceTime { get; private set; } = -10000;
        public Vector3 LastEvidencePosition { get; private set; }
        public float LastSightTime { get; private set; } = -10000;
        public int Reseeds => reseeds;
        public CampusNavGraph Graph => graph;
        public BeliefParticle[] Particles => particles;
        public int RaysLastTick { get; private set; }
        public float EffectiveSampleSize { get; private set; }

        CampusNavGraph graph;
        CampusNavGraph.Scratch scratch;
        BeliefParticle[] particles = new BeliefParticle[0], resampled;
        float[] nodeSeenAt;
        readonly float[] choice = new float[16];
        readonly System.Collections.Generic.Dictionary<long, bool> visibleCache = new System.Collections.Generic.Dictionary<long, bool>(128);
        readonly System.Collections.Generic.List<int> nearby = new System.Collections.Generic.List<int>(96);
        MonsterPerception perception;
        MonsterMemory memory;
        MonsterNavigation navigation;
        MonsterBrain brain;
        MonsterElevatorAwareness lifts;
        float nextTick, lastTick, handledSound = -10000, lastReseed = -10000;
        int lastEvidenceNode = -1, cursor, nodeCursor;
        bool sightLostHandled = true;

        void Awake()
        {
            perception = GetComponent<MonsterPerception>(); memory = GetComponent<MonsterMemory>();
            navigation = GetComponent<MonsterNavigation>(); brain = GetComponent<MonsterBrain>();
            lifts = GetComponent<MonsterElevatorAwareness>();
            if (config == null && brain != null) config = brain.config;
        }
        void Start() { if (lifts == null) lifts = GetComponent<MonsterElevatorAwareness>(); }
        // The brain submits the selected visual candidate. Subscribing to player-only
        // SightUpdated here would silently reveal the real player through the decoy.

        public bool EnsureGraph()
        {
            if (graph != null && particles.Length > 0) return true;
            if (config == null || navigation == null || navigation.Agent == null) return false;
            graph = CampusNavGraph.For(navigation.roomGraph, navigation.Agent.agentTypeID);
            if (graph == null) return false;
            scratch = graph.CreateScratch();
            particles = new BeliefParticle[Mathf.Clamp(config.BeliefParticles, 64, 1024)];
            resampled = new BeliefParticle[particles.Length];
            nodeSeenAt = new float[graph.Count];
            for (int i = 0; i < nodeSeenAt.Length; i++) nodeSeenAt[i] = -10000;
            return true;
        }

        public float NodeSeenAt(int node) => nodeSeenAt != null && node >= 0 && node < nodeSeenAt.Length ? nodeSeenAt[node] : -10000;

        void Update()
        {
            if (config == null || Time.time < nextTick) return;
            nextTick = Time.time + config.BeliefRefreshRate;
            float dt = Mathf.Clamp(Time.time - lastTick, 0, 1f);
            lastTick = Time.time;
            Step(dt);
        }

        // One belief update: new sounds, sight loss, motion, negative evidence, resampling.
        public void Step(float dt) => Step(dt, Time.time);

        public void Step(float dt, float now)
        {
            if (config == null) return;
            if (memory != null && memory.LastHeardTime > handledSound)
            {
                handledSound = memory.LastHeardTime;
                ObserveSound(memory.LastHeardPosition, memory.LastSoundConfidence, memory.LastHeardTime, memory.SoundMovementDirection);
            }
            if (!active || !EnsureGraph()) return;
            if (!sightLostHandled && now - LastSightTime > config.VisionRefreshRate * 1.6f)
            {
                sightLostHandled = true;
                if (lifts != null) lifts.OnPlayerLostFromSight(this);
            }
            Propagate(dt, now);
            if (perception != null && perception.isActiveAndEnabled &&
                (brain == null || brain.CurrentVisualTarget == MonsterTargetType.None)) ApplyNegativeEvidence();
            NormalizeAndResample();
        }

        // ---------------- Evidence ----------------

        public void ObserveSighting(Vector3 position, Vector3 velocity, float time, MovementTrend trend = MovementTrend.Unknown)
        {
            if (!EnsureGraph()) return;
            int node = graph.NearestConnected(position, navigation.Agent.agentTypeID, scratch);
            if (node < 0) return;
            active = true; sightLostHandled = false;
            LastSightTime = time; Evidence(position, node, time);
            Vector2 planar = new Vector2(velocity.x, velocity.z);
            float speed = planar.magnitude;
            Vector2 heading = speed > 0.5f ? planar / speed : Vector2.zero;
            bool chased = brain != null && (brain.CurrentState == MonsterState.Chase || brain.CurrentState == MonsterState.Attack);
            bool fleeing = speed > 4.5f || (chased && speed > 1.5f);
            sbyte vertical = (sbyte)(trend == MovementTrend.ChangingFloor || Mathf.Abs(velocity.y) > 0.6f ? Math.Sign(velocity.y) : 0);
            float weight = 1f / particles.Length;
            for (int i = 0; i < particles.Length; i++)
            {
                Spawn(ref particles[i], node, heading, fleeing ? 30f : 80f, fleeing, vertical, time);
                if(fleeing) particles[i].Speed=Mathf.Max(particles[i].Speed,
                    Mathf.Min(config.MaxObservedSpeed,speed*Random.Range(.85f,1.15f)));
                particles[i].Weight = weight;
            }
        }

        public void ObserveSound(Vector3 position, float confidence, float time, Vector3 direction)
        {
            if (!EnsureGraph()) return;
            int node = graph.NearestConnected(position, navigation.Agent.agentTypeID, scratch);
            if (node < 0) return;
            Vector2 heading = new Vector2(direction.x, direction.z);
            heading = heading.sqrMagnitude > 0.01f ? heading.normalized : Vector2.zero;
            if (!active)
            {
                active = true;
                for (int i = 0; i < particles.Length; i++)
                {
                    Spawn(ref particles[i], node, heading, 70f, Random.value < 0.4f, 0, time);
                    particles[i].Weight = 1f / particles.Length;
                }
            }
            else
            {
                float sigma = Mathf.Lerp(9f, 3.5f, Mathf.Clamp01(confidence));
                float inv = 1f / (2f * sigma * sigma);
                for (int i = 0; i < particles.Length; i++)
                {
                    ref var p = ref particles[i];
                    if (p.Mode == BeliefMode.InLift) { p.Weight *= 0.2f; continue; } // riders make no footsteps
                    Vector3 d = PositionOf(p) - position;
                    float w = 0.05f + Mathf.Exp(-(d.x * d.x + d.z * d.z) * inv);
                    if (Mathf.Abs(d.y) > 2.2f) w *= 0.25f;
                    p.Weight *= w;
                }
                // Part of the belief restarts at the sound, where the old hypotheses may have no support.
                Normalize();
                int replace = particles.Length / 4;
                for (int k = 0; k < replace; k++)
                {
                    int i = Lowest();
                    Spawn(ref particles[i], node, heading, 70f, Random.value < 0.5f, 0, time);
                    particles[i].Weight = 1.5f / particles.Length;
                }
                Normalize();
            }
            Evidence(position, node, time);
        }

        void Evidence(Vector3 position, int node, float time)
        {
            if (time < LastEvidenceTime) return;
            LastEvidenceTime = time; LastEvidencePosition = position; lastEvidenceNode = node;
        }

        // The player was seen entering (or very likely entered) a lift car at `floor`.
        public void ObserveLiftBoarding(int lift, int floor, float time, float probability)
        {
            if (!EnsureGraph() || lift < 0 || lift >= graph.Lifts.Length) return;
            active = true;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (p.Mode == BeliefMode.InLift && p.Lift == lift) continue;
                if (Random.value < probability) Board(ref p, lift, floor, time);
            }
            Normalize();
            if (time > LastEvidenceTime) { LastEvidenceTime = time; LastEvidencePosition = graph.Lifts[lift].Lobby[floor]; lastEvidenceNode = graph.Lifts[lift].Landing[floor]; }
        }

        // The car was seen (display) or heard (chime) standing at `floor` after a ride. Riders get
        // off there. Hypotheses that rode this car and "got off" elsewhere had the right idea (the
        // lift) but the wrong floor: they are moved to this floor and caught up in time.
        public void ObserveLiftStop(int lift, int floor, float time, float sinceTime)
        {
            if (!EnsureGraph() || lift < 0) return;
            float now = Time.time, here = 0;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (p.Mode == BeliefMode.InLift && p.Lift == lift)
                {
                    if (p.BoardTime > time) continue;
                    p.LiftTo = (short)floor;
                    p.Until = Mathf.Max(now + 0.2f, Mathf.Min(p.Until, time + 1.5f));
                    here += p.Weight;
                }
                else if (p.ViaLift == lift && p.ViaTime >= sinceTime)
                {
                    if (p.ViaFloor != floor) Reroute(ref p, lift, floor, Mathf.Max(time, p.ViaTime), now);
                    here += p.Weight;
                }
            }
            Normalize();
            // A stop after a ride is fresh evidence of where the rider got off.
            if (time > LastEvidenceTime && here > 0.05f)
            { LastEvidenceTime = time; LastEvidencePosition = graph.Lifts[lift].Lobby[floor]; lastEvidenceNode = graph.Lifts[lift].Landing[floor]; }
        }

        void Reroute(ref BeliefParticle p, int lift, int floor, float exitTime, float now)
        {
            if (floor < 0 || floor >= graph.Lifts[lift].Landing.Length || graph.Lifts[lift].Landing[floor] < 0) return;
            p.Lift = (short)lift; p.LiftTo = (short)floor;
            ExitLift(ref p, exitTime);
            Vector3 monster = transform.position;
            for (float t = exitTime; t < now; t += 0.5f) Advance(ref p, Mathf.Min(0.5f, now - t), Mathf.Min(now, t + 0.5f), monster);
        }

        // The car was seen moving past `floor` in direction `dir` (+1 up / -1 down) on the trip that
        // started at `sinceTime`: nobody got off at the floors it has passed, and riders get off
        // somewhere ahead of it.
        public void ObserveLiftPassing(int lift, int floor, int dir, float time, float sinceTime = float.PositiveInfinity)
        {
            if (!EnsureGraph() || lift < 0 || dir == 0) return;
            var elevator = graph.Lifts[lift].Elevator;
            int first = dir > 0 ? floor + 1 : 0, last = dir > 0 ? elevator.FloorCount - 1 : floor - 1;
            if (first > last) return;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                bool aboard = p.Mode == BeliefMode.InLift && p.Lift == lift;
                bool earlyExit = !aboard && p.ViaLift == lift && p.ViaTime >= sinceTime && (dir > 0 ? p.ViaFloor <= floor : p.ViaFloor >= floor);
                if (earlyExit)
                {
                    // It "got off" where the car has not stopped yet: still aboard.
                    p.Mode = BeliefMode.InLift; p.Lift = (short)lift; p.LiftFrom = p.ViaFloor; p.BoardTime = p.ViaTime - 1f;
                    p.ViaLift = p.ViaFloor = -1; p.Next = -1; p.Progress = 0;
                    aboard = true; p.LiftTo = (short)floor;
                }
                if (!aboard || (dir > 0 ? p.LiftTo > floor : p.LiftTo < floor)) continue;
                p.LiftTo = (short)Random.Range(first, last + 1);
                p.Until = time + RideSeconds(lift, floor, p.LiftTo) - elevator.doorDuration;
            }
            Normalize();
        }

        // The open car at `floor` was seen from the lobby with nobody inside: whoever rode it got
        // off earlier, at a floor this monster did not see. Riders become such hypotheses.
        public void ObserveLiftEmpty(int lift, int floor, float time)
        {
            if (!EnsureGraph() || lift < 0) return;
            var info = graph.Lifts[lift];
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (p.Mode != BeliefMode.InLift || p.Lift != lift) continue;
                // A rider due here may already have stepped out into the lobby (sight then decides).
                int exit = p.LiftTo;
                if (exit < 0 || exit >= info.Landing.Length || info.Landing[exit] < 0)
                {
                    exit = p.LiftFrom;
                    for (int tries = 0; tries < 8 && (exit < 0 || info.Landing[exit] < 0); tries++) exit = Random.Range(0, info.Landing.Length);
                }
                p.LiftTo = (short)exit;
                ExitLift(ref p, time);
                p.Weight *= 0.5f;
            }
            Normalize();
        }

        // The car moved and stopped at `floor` with no rider in the belief: only the player moves
        // the lifts, so someone called it there or rode it there. Part of the belief moves there.
        public void ObserveLiftActivity(int lift, int floor, float time, float share)
        {
            if (!EnsureGraph() || lift < 0) return;
            int node = graph.Lifts[lift].Landing[floor];
            if (node < 0) return;
            active = true;
            int count = Mathf.RoundToInt(particles.Length * Mathf.Clamp01(share));
            Vector3 outward = graph.Lifts[lift].Elevator.outward;
            for (int k = 0; k < count; k++)
            {
                int i = Lowest();
                Spawn(ref particles[i], node, new Vector2(outward.x, outward.z), 70f, Random.value < 0.5f, 0, time);
                particles[i].Weight = 1.5f / particles.Length;
            }
            Normalize();
            if (time > LastEvidenceTime) { LastEvidenceTime = time; LastEvidencePosition = graph.Lifts[lift].Lobby[floor]; lastEvidenceNode = node; }
        }

        public void Deactivate() { active = false; }

        // ---------------- Motion model ----------------

        void Spawn(ref BeliefParticle p, int node, Vector2 heading, float spread, bool fleeing, sbyte vertical, float now)
        {
            p.Node = node; p.Next = -1; p.Previous = -1; p.Progress = 0; p.Length = 1;
            p.Speed = Random.Range(config.BeliefFleeSpeed.x, config.BeliefFleeSpeed.y);
            p.WalkSpeed = Random.Range(config.BeliefWalkSpeed.x, config.BeliefWalkSpeed.y);
            p.FleeUntil = fleeing ? now + Random.Range(config.BeliefFleeSeconds.x, config.BeliefFleeSeconds.y) : now;
            p.Heading = heading.sqrMagnitude > 0.01f && Random.value < 0.85f ? Rotate(heading, Random.Range(-spread, spread)) : Random.insideUnitCircle.normalized;
            p.Vertical = vertical; p.Mode = BeliefMode.Moving; p.Until = now + 3f;
            p.Lift = p.LiftFrom = p.LiftTo = -1; p.ViaLift = p.ViaFloor = -1; p.ViaTime = -10000; p.BoardTime = -10000;
        }

        static float PlanarSq(Vector3 d) => d.x * d.x + d.z * d.z;

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        void Propagate(float dt, float now)
        {
            Vector3 monster = transform.position;
            for (int i = 0; i < particles.Length; i++) Advance(ref particles[i], dt, now, monster);
        }

        void Advance(ref BeliefParticle p, float dt, float now, Vector3 monster)
        {
            switch (p.Mode)
            {
                case BeliefMode.Holding:
                    if (now >= p.Until) { p.Mode = BeliefMode.Moving; p.Until = now + 2f; p.Heading = Random.insideUnitCircle.normalized; }
                    return;
                case BeliefMode.InLift:
                    if (now >= p.Until) ExitLift(ref p, now);
                    return;
            }
            float travel = (now < p.FleeUntil ? p.Speed : p.WalkSpeed) * dt;
            for (int guard = 0; guard < 8 && travel > 0 && p.Mode == BeliefMode.Moving; guard++)
            {
                if (p.Next < 0) { ChooseNext(ref p, now, monster); if (p.Next < 0) return; }
                float left = p.Length - p.Progress;
                if (travel < left) { p.Progress += travel; return; }
                travel -= left;
                Arrive(ref p, now, monster);
            }
        }

        void ChooseNext(ref BeliefParticle p, float now, Vector3 monster)
        {
            int start = graph.MoveStart[p.Node], end = graph.MoveStart[p.Node + 1];
            int count = Mathf.Min(end - start, choice.Length);
            if (count <= 0) { p.Next = -1; return; }
            bool fleeing = now < p.FleeUntil;
            float kappa = fleeing ? 3f : 1.2f;
            Vector3 here = graph.Position[p.Node];
            // Only a monster on the same floor is something to walk away from.
            bool sameFloor = Mathf.Abs(here.y - monster.y) < 2.5f;
            float toMonster = sameFloor ? PlanarSq(here - monster) : float.PositiveInfinity;
            float total = 0;
            for (int k = 0; k < count; k++)
            {
                int to = graph.MoveTo[start + k];
                Vector3 d = graph.Position[to] - here;
                float planar = Mathf.Sqrt(d.x * d.x + d.z * d.z);
                float align = planar > 0.3f ? (p.Heading.x * d.x + p.Heading.y * d.z) / planar : 0f;
                float w = Mathf.Exp(kappa * align);
                if (Mathf.Abs(d.y) > 0.8f)
                    w *= p.Vertical == 0 ? config.BeliefStairWeight : Math.Sign(d.y) == p.Vertical ? 1.8f : 0.25f;
                if (to == p.Previous) w *= 0.1f;
                // A person avoids walking back towards the monster.
                if (toMonster < 400f && PlanarSq(graph.Position[to] - monster) < toMonster) w *= 0.3f;
                choice[k] = w; total += w;
            }
            float pick = Random.value * total;
            int chosen = count - 1;
            for (int k = 0; k < count; k++) { pick -= choice[k]; if (pick <= 0) { chosen = k; break; } }
            p.Next = graph.MoveTo[start + chosen];
            p.Length = graph.MoveLength[start + chosen];
            p.Progress = 0;
            Vector3 step = graph.Position[p.Next] - here;
            Vector2 dir = new Vector2(step.x, step.z);
            if (dir.sqrMagnitude > 0.09f) p.Heading = Vector2.Lerp(p.Heading, dir.normalized, fleeing ? 0.3f : 0.6f).normalized;
        }

        void Arrive(ref BeliefParticle p, float now, Vector3 monster)
        {
            p.Previous = p.Node; p.Node = p.Next; p.Next = -1; p.Progress = 0;
            var kind = graph.Kind[p.Node];
            if (p.Vertical != 0 && kind != RoomNodeKind.StairLanding && Random.value < 0.35f) p.Vertical = 0;
            bool fleeing = now < p.FleeUntil;
            int lift = graph.LiftOfNode[p.Node];
            if (lift >= 0 && now >= p.Until && Random.value < config.BeliefLiftChance * (fleeing ? 1f : 0.5f))
            {
                Board(ref p, lift, graph.LiftFloorOfNode[p.Node], now);
                return;
            }
            if (!fleeing)
            {
                float hold = graph.RoomInterior[p.Node] ? config.BeliefRoomHoldChance : config.BeliefHoldChance * (kind == RoomNodeKind.StairLanding ? 0.3f : 1f);
                if (Random.value < hold)
                {
                    p.Mode = BeliefMode.Holding;
                    p.Until = now + Random.Range(config.BeliefHoldSeconds.x, config.BeliefHoldSeconds.y);
                    return;
                }
            }
            ChooseNext(ref p, now, monster);
        }

        void Board(ref BeliefParticle p, int lift, int floor, float now)
        {
            var info = graph.Lifts[lift];
            p.Mode = BeliefMode.InLift; p.Lift = (short)lift; p.LiftFrom = (short)floor; p.BoardTime = now;
            int exit = lifts != null ? lifts.PredictExitFloor(lift, floor, now) : -1;
            if (exit < 0 || exit >= info.Landing.Length)
            {
                exit = floor;
                for (int tries = 0; tries < 8 && (exit == floor || info.Landing[exit] < 0); tries++) exit = Random.Range(0, info.Landing.Length);
            }
            p.LiftTo = (short)exit;
            p.Until = now + RideSeconds(lift, floor, exit);
            p.Next = -1; p.Progress = 0;
        }

        public float RideSeconds(int lift, int from, int to) => CampusNavGraph.RideSeconds(graph.Lifts[lift].Elevator, from, to);

        void ExitLift(ref BeliefParticle p, float now)
        {
            var info = graph.Lifts[p.Lift];
            int floor = p.LiftTo;
            if (floor < 0 || floor >= info.Landing.Length || info.Landing[floor] < 0)
                for (floor = 0; floor < info.Landing.Length && info.Landing[floor] < 0; floor++) { }
            if (floor >= info.Landing.Length) { p.Weight = 0; p.Mode = BeliefMode.Holding; p.Until = float.PositiveInfinity; return; }
            p.ViaLift = p.Lift; p.ViaFloor = (short)floor; p.ViaTime = now;
            p.Mode = BeliefMode.Moving; p.Node = info.Landing[floor]; p.Next = -1; p.Previous = -1; p.Progress = 0;
            p.Lift = -1; p.Vertical = 0;
            // Stepping out with the monster close by means running; otherwise a person walks off
            // (and often ducks into a nearby room).
            Vector3 toMonster = graph.Position[p.Node] - transform.position;
            bool threatened = Mathf.Abs(toMonster.y) < 2.5f && PlanarSq(toMonster) < 25f * 25f;
            p.FleeUntil = threatened ? now + Random.Range(2f, 6f) : now + (Random.value < 0.3f ? Random.Range(0.5f, 2f) : 0f);
            Vector3 outward = info.Elevator.outward;
            p.Heading = Rotate(new Vector2(outward.x, outward.z), Random.Range(-150f, 150f));
            p.Until = now + 10f; // not straight back into the car
        }

        // ---------------- Observation update ----------------

        void ApplyNegativeEvidence()
        {
            float range = config.VisionDistance, half = config.VisionAngle * 0.5f;
            Vector3 eye = perception.Eye, forward = transform.forward;
            float cosHalf = Mathf.Cos(half * Mathf.Deg2Rad);
            visibleCache.Clear();
            int rays = 0, budget = config.BeliefRaysPerTick;
            int n = particles.Length, offset = cursor;
            for (int k = 0; k < n; k++)
            {
                int i = (k + offset) % n;
                ref var p = ref particles[i];
                if (p.Mode == BeliefMode.InLift || p.Weight <= 0) continue;
                Vector3 feet = PositionOf(p);
                Vector3 d = feet + Vector3.up * 1.1f - eye;
                float sq = d.sqrMagnitude;
                // Right next to it: the monster would notice a body even outside its view cone.
                if (sq < 4f && Mathf.Abs(d.y) < 1.5f) { p.Weight *= 0.25f; continue; }
                if (sq > range * range || Vector3.Dot(forward, d) < cosHalf * Mathf.Sqrt(sq)) continue;
                long key = Quantize(feet);
                if (!visibleCache.TryGetValue(key, out bool visible))
                {
                    if (rays >= budget) continue;
                    visible = perception.ClearSight(eye, feet + Vector3.up * 1.1f, null);
                    rays++; visibleCache[key] = visible;
                }
                if (visible) p.Weight *= config.BeliefMissLikelihood;
            }
            cursor = (offset + 97) % Mathf.Max(1, n);
            rays += MarkSeenNodes(eye, forward, cosHalf, range, Mathf.Max(4, budget / 5));
            RaysLastTick = rays;
        }

        // Searched-space memory: graph nodes the monster has actually looked at, and when.
        int MarkSeenNodes(Vector3 eye, Vector3 forward, float cosHalf, float range, int budget)
        {
            nearby.Clear();
            graph.QueryRadius(transform.position, Mathf.Min(range, 24f), 4f, nearby);
            int rays = 0, count = nearby.Count;
            for (int k = 0; k < count && rays < budget; k++)
            {
                int node = nearby[(k + nodeCursor) % count];
                if (Time.time - nodeSeenAt[node] < 1.5f) continue;
                Vector3 d = graph.Position[node] + Vector3.up * 1.1f - eye;
                float sq = d.sqrMagnitude;
                if (sq > 1f && Vector3.Dot(forward, d) < cosHalf * Mathf.Sqrt(sq)) continue;
                rays++;
                if (sq <= 1f || perception.ClearSight(eye, graph.Position[node] + Vector3.up * 1.1f, null)) nodeSeenAt[node] = Time.time;
            }
            nodeCursor += 7;
            return rays;
        }

        static long Quantize(Vector3 p) =>
            ((long)(Mathf.FloorToInt(p.x) + 4096) * 8192 + (Mathf.FloorToInt(p.y) + 4096)) * 8192 + (Mathf.FloorToInt(p.z) + 4096);

        void Normalize()
        {
            double sum = 0;
            for (int i = 0; i < particles.Length; i++) sum += particles[i].Weight;
            if (sum <= 1e-9) return;
            float inv = (float)(1.0 / sum);
            for (int i = 0; i < particles.Length; i++) particles[i].Weight *= inv;
        }

        void NormalizeAndResample()
        {
            double sum = 0;
            for (int i = 0; i < particles.Length; i++) sum += particles[i].Weight;
            if (sum < 1e-6) { Reseed(); return; }
            float inv = (float)(1.0 / sum); double squares = 0;
            for (int i = 0; i < particles.Length; i++) { particles[i].Weight *= inv; squares += particles[i].Weight * particles[i].Weight; }
            EffectiveSampleSize = (float)(1.0 / Math.Max(squares, 1e-12));
            if (EffectiveSampleSize >= particles.Length * 0.5f) return;
            // Systematic resampling keeps the surviving hypotheses in proportion to their weight.
            int n = particles.Length; float step = 1f / n, u = Random.value * step, c = particles[0].Weight;
            for (int j = 0, i = 0; j < n; j++)
            {
                float target = u + j * step;
                while (target > c && i < n - 1) { i++; c += particles[i].Weight; }
                resampled[j] = particles[i];
                resampled[j].Weight = step;
            }
            var swap = particles; particles = resampled; resampled = swap;
        }

        // Everything the belief proposed was seen empty. Spread fresh hypotheses from the last
        // real evidence over the ground a person could have covered since, and down-weight the
        // places this monster has already looked at recently.
        void Reseed()
        {
            float now = Time.time;
            if (now - lastReseed < 2f)
            {
                // Rate-limited: keep the surviving shape instead of rebuilding every tick.
                for (int i = 0; i < particles.Length; i++) particles[i].Weight = 1f / particles.Length;
                return;
            }
            lastReseed = now; reseeds++;
            if (lastEvidenceNode < 0) { active = false; return; }
            float elapsed = Mathf.Clamp(now - LastEvidenceTime, 2f, 90f);
            Vector3 monster = transform.position;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                Spawn(ref p, lastEvidenceNode, Vector2.zero, 180f, Random.value < 0.5f, 0, LastEvidenceTime);
                float simulated = elapsed * Random.Range(0.25f, 1f), t = LastEvidenceTime;
                for (float s = 0; s < simulated; s += 0.5f) { t += 0.5f; Advance(ref p, 0.5f, t, monster); }
                if (p.Mode == BeliefMode.InLift && p.Until < now) ExitLift(ref p, now);
                int node = p.Mode == BeliefMode.InLift ? -1 : p.Next >= 0 && p.Progress > p.Length * 0.5f ? p.Next : p.Node;
                p.Weight = node >= 0 && now - nodeSeenAt[node] < 20f ? 0.1f : 1f;
            }
            Normalize();
        }

        int Lowest()
        {
            int best = 0; float weight = float.PositiveInfinity;
            for (int i = 0; i < particles.Length; i++) if (particles[i].Weight < weight) { weight = particles[i].Weight; best = i; }
            return best;
        }

        // ---------------- Queries ----------------

        public Vector3 PositionOf(in BeliefParticle p)
        {
            if (p.Mode == BeliefMode.InLift)
            {
                var lift = graph.Lifts[p.Lift];
                return lift.Lobby[Mathf.Clamp(p.LiftTo, 0, lift.Lobby.Length - 1)];
            }
            if (p.Next >= 0 && p.Length > 0.01f)
                return Vector3.Lerp(graph.Position[p.Node], graph.Position[p.Next], Mathf.Clamp01(p.Progress / p.Length));
            return graph.Position[p.Node];
        }

        public int NodeOf(in BeliefParticle p)
        {
            if (p.Mode == BeliefMode.InLift) return graph.Lifts[p.Lift].Landing[Mathf.Clamp(p.LiftTo, 0, graph.Lifts[p.Lift].Landing.Length - 1)];
            return p.Next >= 0 && p.Progress > p.Length * 0.5f ? p.Next : p.Node;
        }

        // Probability mass per node. Riders count at the landing where they are expected to get off.
        public void AccumulateNodeMass(float[] mass, float riderShare = 0.8f)
        {
            Array.Clear(mass, 0, mass.Length);
            if (!active || graph == null) return;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                int node = NodeOf(p);
                if (node < 0 || node >= mass.Length) continue;
                mass[node] += p.Mode == BeliefMode.InLift ? p.Weight * riderShare : p.Weight;
            }
        }

        public float MassNear(Vector3 point, float radius, float vertical)
        {
            if (!active || graph == null) return 0;
            float mass = 0, r2 = radius * radius;
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].Mode == BeliefMode.InLift) continue;
                Vector3 d = PositionOf(particles[i]) - point;
                if (Mathf.Abs(d.y) <= vertical && d.x * d.x + d.z * d.z <= r2) mass += particles[i].Weight;
            }
            return mass;
        }

        // The car is watched standing at its boarding floor: riders cannot have left yet.
        public void DelayRiders(int lift, float time)
        {
            if (!active || graph == null) return;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (p.Mode != BeliefMode.InLift || p.Lift != lift || p.BoardTime > time) continue;
                p.LiftTo = p.LiftFrom;
                p.Until = Mathf.Max(p.Until, time + 2.5f);
            }
        }

        public float RiderMass(int lift)
        {
            float mass = 0;
            for (int i = 0; i < particles.Length; i++)
                if (particles[i].Mode == BeliefMode.InLift && (lift < 0 || particles[i].Lift == lift)) mass += particles[i].Weight;
            return mass;
        }

        // Mass of hypotheses riding `lift`, or that left it since `since`, at a floor this monster
        // has not confirmed (confirmedFloor < 0: none confirmed yet).
        public float UnconfirmedLiftMass(int lift, float since, int confirmedFloor = -1)
        {
            float mass = 0;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (p.Mode == BeliefMode.InLift && p.Lift == lift) { if (p.LiftTo != confirmedFloor) mass += p.Weight; }
                else if (p.ViaLift == lift && p.ViaTime >= since && p.ViaFloor != confirmedFloor) mass += p.Weight;
            }
            return mass;
        }

        float[] summaryMass;
        public string Summary()
        {
            if (!active || graph == null) return "Belief: idle";
            if (summaryMass == null || summaryMass.Length != graph.Count) summaryMass = new float[graph.Count];
            var mass = summaryMass;
            AccumulateNodeMass(mass, 1f);
            int best = 0; for (int i = 1; i < mass.Length; i++) if (mass[i] > mass[best]) best = i;
            float riders = RiderMass(-1);
            return $"Belief: top {graph.Label[best]} {mass[best]:P0}, riding {riders:P0}, ESS {EffectiveSampleSize:F0}, reseeds {reseeds}, rays {RaysLastTick}";
        }
    }
}
