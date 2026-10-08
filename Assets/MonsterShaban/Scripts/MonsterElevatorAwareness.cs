using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CampusRift.Monsters
{
    // What this monster knows about the campus lifts, gathered the way a bystander would:
    // reading the floor display above landing doors it can see, hearing the arrival chime,
    // seeing the player step into a car and seeing an open car stand empty. Only this monster
    // walks the campus without riding, so any car movement it notices is the player's doing.
    // The knowledge feeds the belief ("the player rode lift E up to floor 5"); it never reads
    // the player's transform.
    [DisallowMultipleComponent]
    public sealed class MonsterElevatorAwareness : MonoBehaviour
    {
        [Serializable]
        public sealed class LiftKnowledge
        {
            public string Building;
            public int Floor = -1;          // last floor read from a display, chime or open doors
            public bool Moving;
            public int Direction;
            public float ReadAt = -10000;
            public float BoardedAt = -10000;
            public int BoardedFloor = -1;
            public float DepartedAt = -10000;
            public int LastStop = -1;
            public float LastStopAt = -10000;
            public string LastEvent = "";
        }

        public MonsterAIConfig config;
        public bool logEvents;
        [SerializeField] LiftKnowledge[] knowledge = new LiftKnowledge[0];
        public LiftKnowledge[] Knowledge => knowledge;
        public string LastEvent { get; private set; } = "";
        public float LastEventTime { get; private set; } = -10000;

        CampusNavGraph graph;
        MonsterPerception perception;
        MonsterBelief belief;
        MonsterNavigation navigation;
        Collider body;
        CampusElevator.LiftState[] previousState = new CampusElevator.LiftState[0];
        float nextTick, nextCall;
        int myCallLift = -1, myCallFloor = -1; float myCallAt = -10000;
        int ownLift = -1; float ownUntil = -10000; ulong ownFloors;
        int seenInsideLift = -1, seenInsideFloor, seenAtDoorLift = -1, seenAtDoorFloor;
        float seenInsideAt = -10000, seenAtDoorAt = -10000;

        void Awake()
        {
            perception = GetComponent<MonsterPerception>(); belief = GetComponent<MonsterBelief>();
            navigation = GetComponent<MonsterNavigation>(); body = GetComponent<CapsuleCollider>();
            if (config == null) { var brain = GetComponent<MonsterBrain>(); if (brain != null) config = brain.config; }
        }

        void OnEnable()
        {
            if (perception != null) perception.SightUpdated += OnSight;
            if (body != null) CampusElevator.RegisterOccupant(body);
        }

        void OnDisable()
        {
            if (perception != null) perception.SightUpdated -= OnSight;
            if (body != null) CampusElevator.UnregisterOccupant(body);
        }

        void Start()
        {
            if (belief == null) belief = GetComponent<MonsterBelief>();
            if (navigation != null && navigation.Agent != null) ElevatorShaftGuard.Ensure(navigation.Agent.agentTypeID);
            EnsureGraph();
        }

        bool EnsureGraph()
        {
            if (graph != null && knowledge.Length == graph.Lifts.Length) return true;
            if (navigation == null || navigation.Agent == null) return false;
            graph = CampusNavGraph.For(navigation.roomGraph, navigation.Agent.agentTypeID);
            if (graph == null) return false;
            knowledge = new LiftKnowledge[graph.Lifts.Length];
            previousState = new CampusElevator.LiftState[graph.Lifts.Length];
            for (int i = 0; i < knowledge.Length; i++)
            {
                knowledge[i] = new LiftKnowledge { Building = graph.Lifts[i].Elevator.building };
                previousState[i] = graph.Lifts[i].Elevator.State;
            }
            return true;
        }

        // A sighting snapshot inside a car (or right at its open doors) is remembered so that
        // losing sight a moment later is understood as "the player got into that lift".
        void OnSight()
        {
            if (!EnsureGraph()) return;
            Vector3 seen = perception.ObservedPosition;
            for (int l = 0; l < graph.Lifts.Length; l++)
            {
                var lift = graph.Lifts[l].Elevator;
                if (lift == null) continue;
                int floor = lift.IsMoving ? CampusNavGraph.FloorAt(lift, seen.y) : lift.CurrentFloor;
                if (lift.IsInside(seen)) { seenInsideLift = l; seenInsideFloor = floor; seenInsideAt = perception.ObservationTime; return; }
                if (lift.IsMoving || lift.DoorAmount < 0.3f) continue;
                foreach (var doorway in lift.floors[lift.CurrentFloor].doorways)
                {
                    Vector3 d = doorway.center - seen; d.y = 0;
                    if (d.sqrMagnitude < 2.6f * 2.6f && Mathf.Abs(seen.y - lift.floors[lift.CurrentFloor].height) < 1f)
                    { seenAtDoorLift = l; seenAtDoorFloor = lift.CurrentFloor; seenAtDoorAt = perception.ObservationTime; }
                }
            }
        }

        public void OnPlayerLostFromSight(MonsterBelief target)
        {
            if (!EnsureGraph()) return;
            float lastSight = target.LastSightTime;
            if (seenInsideLift >= 0 && lastSight - seenInsideAt < 0.3f)
            {
                var k = knowledge[seenInsideLift];
                k.BoardedAt = seenInsideAt; k.BoardedFloor = seenInsideFloor;
                target.ObserveLiftBoarding(seenInsideLift, seenInsideFloor, seenInsideAt, 0.92f);
                Event(seenInsideLift, "saw the player step into the car at F" + (seenInsideFloor + 1));
            }
            else if (seenAtDoorLift >= 0 && lastSight - seenAtDoorAt < 0.3f)
            {
                var k = knowledge[seenAtDoorLift];
                k.BoardedAt = seenAtDoorAt; k.BoardedFloor = seenAtDoorFloor;
                target.ObserveLiftBoarding(seenAtDoorLift, seenAtDoorFloor, seenAtDoorAt, 0.55f);
                Event(seenAtDoorLift, "lost the player at the open car doors, F" + (seenAtDoorFloor + 1));
            }
        }

        // This monster's own use of a lift (calling a car, riding it): those departures and its
        // stops at the floors it asked for say nothing about the player. A stop anywhere else
        // still does: someone called the car there.
        public void NoteOwnRide(int lift, int from, int to, float seconds)
        {
            if (lift != ownLift || Time.time >= ownUntil) ownFloors = 0;
            ownLift = lift; ownUntil = Time.time + seconds;
            if (from >= 0 && from < 64) ownFloors |= 1UL << from;
            if (to >= 0 && to < 64) ownFloors |= 1UL << to;
        }

        // Standing in the car: had the player been in it, the monster would see them.
        public void NoteOwnBoarding(int lift, int floor)
        {
            if (belief != null && belief.Active && perception != null && !perception.CanSeePlayer) belief.ObserveLiftEmpty(lift, floor, Time.time);
            if (EnsureGraph() && lift >= 0 && lift < knowledge.Length) Event(lift, "stepped into the car at F" + (floor + 1));
        }

        bool OwnRide(int l, float time) => l == ownLift && time < ownUntil;
        // A car heading for (or standing at) a floor this monster asked for is serving its own request;
        // one moving away from all of them was called by someone else.
        bool OwnTrip(int l, int floor, int dir, float time)
        {
            if (!OwnRide(l, time)) return false;
            for (int f = 0; f < 64; f++)
                if ((ownFloors & (1UL << f)) != 0 && (f == floor || System.Math.Sign(f - floor) == dir)) return true;
            return false;
        }
        bool OwnStop(int l, int floor, float time) => OwnRide(l, time) && floor >= 0 && floor < 64 && (ownFloors & (1UL << floor)) != 0;

        // Where would someone boarding `lift` at `from` now get off, given what this monster has seen?
        public int PredictExitFloor(int lift, int from, float time)
        {
            if (lift < 0 || lift >= knowledge.Length) return -1;
            var k = knowledge[lift];
            var e = graph.Lifts[lift].Elevator;
            if (Time.time - k.ReadAt < 3f && k.Moving && k.Direction != 0)
            {
                int first = k.Direction > 0 ? k.Floor + 1 : 0, last = k.Direction > 0 ? e.FloorCount - 1 : k.Floor - 1;
                if (first <= last) return Random.Range(first, last + 1);
            }
            return -1;
        }

        void Update()
        {
            if (config == null || Time.time < nextTick || !EnsureGraph()) return;
            nextTick = Time.time + 0.2f;
            Vector3 feet = transform.position;
            for (int l = 0; l < graph.Lifts.Length; l++)
            {
                var lift = graph.Lifts[l].Elevator;
                if (lift == null) continue;
                var state = lift.State;
                // The chime is a real sound at the cabin: audible nearby, through the shaft walls.
                if (previousState[l] == CampusElevator.LiftState.Moving && state != CampusElevator.LiftState.Moving && CanHearChime(lift))
                    Observe(l, lift.CurrentFloor, false, 0, Time.time, "heard the arrival chime");
                previousState[l] = state;
                int floor = LandingFloorAt(lift, feet.y);
                if (floor < 0 || !CanReadDisplay(lift, floor)) continue;
                bool moving = lift.IsMoving;
                int shown = DisplayedFloor(lift);
                Observe(l, shown, moving, moving ? Math.Sign(lift.TargetFloor - lift.CurrentFloor) : 0, Time.time, "read the floor display");
                if (!moving && lift.CurrentFloor == floor && lift.DoorAmount > 0.6f && !perception.CanSeePlayer && CanSeeCarInterior(lift, floor))
                    belief?.ObserveLiftEmpty(l, floor, Time.time);
                TryOpenCar(l, lift, floor);
            }
        }

        void Observe(int l, int floor, bool moving, int dir, float time, string source)
        {
            var k = knowledge[l];
            bool known = k.ReadAt > -1000;
            int previousFloor = k.Floor; bool wasMoving = k.Moving;
            k.Floor = floor; k.Moving = moving; k.Direction = dir; k.ReadAt = time;
            if (belief == null) return;
            if (moving)
            {
                if (!wasMoving || !known)
                {
                    k.DepartedAt = time;
                    // Only the player moves the cars (this monster never rides, and it remembers its
                    // own calls): the player is aboard, or called it and waits where it will stop.
                    // Riders model both, since they get off where the car stops.
                    int from = known && !wasMoving ? previousFloor : Mathf.Clamp(floor - dir, 0, graph.Lifts[l].Elevator.FloorCount - 1);
                    bool mine = (myCallLift == l && time - myCallAt < 20f) || OwnTrip(l, floor, dir, time);
                    if (!mine && belief.Active)
                    {
                        // Leaving right where the player was just lost (or seen boarding) nearly confirms the ride.
                        bool justBoarded = time - k.BoardedAt < 15f && k.BoardedFloor == from;
                        float target = justBoarded ? 0.9f : Mathf.Clamp(0.75f + belief.MassNear(graph.Lifts[l].Lobby[from], 7f, 1.5f), 0.75f, 0.9f);
                        float riders = belief.RiderMass(l);
                        if (!justBoarded) { k.BoardedAt = time; k.BoardedFloor = from; }
                        if (riders < target) belief.ObserveLiftBoarding(l, from, time, Mathf.Clamp01((target - riders) / Mathf.Max(0.01f, 1f - riders)));
                        Event(l, source + ": car left F" + (from + 1) + " — the player is aboard or called it");
                    }
                    else Event(l, source + ": car moving " + (dir > 0 ? "up" : "down") + " at F" + (floor + 1));
                }
                if (dir != 0 && !OwnTrip(l, floor, dir, time)) belief.ObserveLiftPassing(l, floor, dir, time, TripStart(k, time));
                return;
            }
            if (OwnStop(l, floor, time))
            {
                if (wasMoving || previousFloor != floor) Event(l, source + ": own ride, car at F" + (floor + 1));
                return;
            }
            if (known && !wasMoving && previousFloor == floor)
            {
                // Still standing where it was. Riders boarded here cannot have gone anywhere yet.
                if (k.BoardedFloor == floor && time - k.BoardedAt < 60f) belief.DelayRiders(l, time);
                return;
            }
            if (!known)
            {
                // First look at this lift since the player got in: where it stands now is where it went.
                if (k.BoardedAt > time - 90f && floor != k.BoardedFloor)
                {
                    k.LastStop = floor; k.LastStopAt = time;
                    belief.ObserveLiftStop(l, floor, time, TripStart(k, time));
                    Event(l, source + ": car now at F" + (floor + 1) + " (the rider got off there)");
                }
                else Event(l, source + ": car at F" + (floor + 1));
                return;
            }
            k.LastStop = floor; k.LastStopAt = time;
            float since = TripStart(k, time);
            if (belief.RiderMass(l) > 0.02f || k.BoardedAt > time - 90f)
            {
                belief.ObserveLiftStop(l, floor, time, since);
                Event(l, source + ": car stopped at F" + (floor + 1) + " (rider gets off there)");
            }
            else if (belief.Active && !(myCallLift == l && myCallFloor == floor && time - myCallAt < 30f))
            {
                belief.ObserveLiftActivity(l, floor, time, 0.6f);
                Event(l, source + ": car moved to F" + (floor + 1) + " — the player called or rode it");
            }
        }

        // Start of the ride this observation belongs to (boarding seen or inferred, else departure).
        static float TripStart(LiftKnowledge k, float time)
        {
            if (k.BoardedAt > time - 90f) return k.BoardedAt - 1f;
            if (k.DepartedAt > time - 90f) return k.DepartedAt - 1f;
            return time - 40f;
        }

        // With a probable rider inside a closed, idle car at this landing, press the call button
        // like anyone could: the doors open and the monster looks inside.
        void TryOpenCar(int l, CampusElevator lift, int floor)
        {
            if (Time.time < nextCall || lift.IsMoving || lift.CurrentFloor != floor || lift.DoorAmount > 0.05f || OwnRide(l, Time.time)) return;
            if (belief == null || belief.RiderMass(l) < 0.35f) return;
            Vector3 lobby = graph.Lifts[l].Lobby[floor];
            if ((lobby - transform.position).sqrMagnitude > 3f * 3f) return;
            nextCall = Time.time + 6f;
            myCallLift = l; myCallFloor = floor; myCallAt = Time.time;
            lift.RequestFloor(floor);
            Event(l, "pressed the call button at F" + (floor + 1) + " to look inside");
        }

        // A car is a small box: standing in its open doorway, the monster can reach the far wall.
        // Only upper floors need this (the ground-floor car interior is walkable NavMesh).
        public bool CanReachIntoCar(Vector3 target)
        {
            if (graph == null) return false;
            Vector3 self = transform.position;
            foreach (var info in graph.Lifts)
            {
                var lift = info.Elevator;
                if (lift == null || lift.IsMoving || lift.DoorAmount < 0.6f || !lift.IsInside(target)) continue;
                if (Mathf.Abs(self.y - lift.floors[lift.CurrentFloor].height) > 0.8f) continue;
                foreach (var doorway in lift.floors[lift.CurrentFloor].doorways)
                {
                    Vector3 d = doorway.ClosestPoint(self) - self; d.y = 0;
                    if (d.sqrMagnitude < 1.3f * 1.3f) return true;
                }
            }
            return false;
        }

        public static int DisplayedFloor(CampusElevator lift)
        {
            if (!lift.IsMoving) return lift.CurrentFloor;
            return CampusNavGraph.FloorAt(lift, lift.floors[0].height + lift.CabinOffset);
        }

        int LandingFloorAt(CampusElevator lift, float height)
        {
            int floor = CampusNavGraph.FloorAt(lift, height);
            return Mathf.Abs(lift.floors[floor].height - height) < 2.2f ? floor : -1;
        }

        bool CanReadDisplay(CampusElevator lift, int floor)
        {
            var landing = lift.floors[floor];
            Vector3 display = landing.display != null ? landing.display.transform.position : LandingCenter(lift, floor) + Vector3.up * 2.5f;
            display += lift.outward * 0.12f;
            Vector3 eye = perception.Eye, delta = display - eye;
            float range = config.LiftDisplayReadDistance;
            if (delta.sqrMagnitude > range * range) return false;
            // It must stand on the lobby side, facing the display, with a clear view of it.
            if (Vector3.Dot(eye - display, lift.outward) < 0.3f) return false;
            if (Vector3.Angle(transform.forward, delta) > 60f) return false;
            // The panel sits flush above the doors: sight is checked to the air just in front of its
            // face (its own frame and the door header would otherwise hide it from up close).
            Vector3 face = display + lift.outward * 0.25f;
            return perception.ClearSight(eye, face, null) || perception.ClearSight(eye, face + Vector3.up * 0.12f, null);
        }

        bool CanSeeCarInterior(CampusElevator lift, int floor)
        {
            if (lift.cabinSpaces == null || lift.cabinSpaces.Length == 0) return false;
            Vector3 inside = lift.cabinSpaces[0].center + Vector3.up * (lift.floors[floor].height - lift.floors[0].height);
            Vector3 eye = perception.Eye, delta = inside - eye;
            if (delta.sqrMagnitude > 18f * 18f || Vector3.Angle(transform.forward, delta) > config.VisionAngle * 0.5f) return false;
            return perception.ClearSight(eye, inside, null);
        }

        bool CanHearChime(CampusElevator lift)
        {
            if (lift.cabin == null) return false;
            Vector3 source = lift.cabin.position + Vector3.up * 1.2f;
            float range = config.LiftChimeHearingDistance;
            if (!perception.ClearLine(perception.Eye, source, lift.cabin)) range *= 0.6f;
            return (source - perception.Eye).sqrMagnitude <= range * range;
        }

        static Vector3 LandingCenter(CampusElevator lift, int floor)
        {
            var landing = lift.floors[floor]; Vector3 center = Vector3.zero;
            foreach (var entrance in landing.entrances) center += entrance;
            center /= Mathf.Max(1, landing.entrances.Length); center.y = landing.height;
            return center;
        }

        void Event(int lift, string text)
        {
            knowledge[lift].LastEvent = text;
            LastEvent = "Lift " + knowledge[lift].Building + ": " + text;
            LastEventTime = Time.time;
            if (logEvents) Debug.Log("[Shaban] " + LastEvent, this);
        }

        // Worth reading this lift's display: the player rode it recently and the floor where the car
        // stopped is not known yet (a stopped car keeps showing its floor until it is called again).
        public bool WorthWatching(int lift, float now)
        {
            if (lift < 0 || lift >= knowledge.Length) return false;
            var k = knowledge[lift];
            if (k.BoardedAt < -1000 || now - k.BoardedAt > 60f) return false;
            return k.LastStopAt <= k.BoardedAt;
        }

        public int ConfirmedFloor(int lift) =>
            lift >= 0 && lift < knowledge.Length && knowledge[lift].LastStopAt > knowledge[lift].BoardedAt ? knowledge[lift].LastStop : -1;

        public string Describe(int lift)
        {
            if (lift < 0 || lift >= knowledge.Length) return "";
            var k = knowledge[lift];
            if (k.ReadAt < -1000) return "Lift " + k.Building + ": unknown";
            return $"Lift {k.Building}: F{k.Floor + 1}{(k.Moving ? (k.Direction > 0 ? " ^" : " v") : "")} ({Time.time - k.ReadAt:F0}s ago)" +
                (k.BoardedAt > -1000 ? $", boarded F{k.BoardedFloor + 1} {Time.time - k.BoardedAt:F0}s ago" : "") +
                (k.LastStop >= 0 ? $", stop F{k.LastStop + 1}" : "");
        }
    }
}
