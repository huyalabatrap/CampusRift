#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CampusRift;
using CampusRift.Monsters;
using UnityEngine;
using UnityEngine.AI;

// Editor-only harness, attached temporarily through MCP. It never ships in a build.
// Drives MonsterNavigation alone (brain, senses and the player's controls off) over routes that
// need the NavMesh links (steep stair flights, door drops) and the lift-versus-stairs choice.
public sealed class ShabanTraversalPlayTest : MonoBehaviour
{
    public string output = "Assets/MonsterShaban/Validation/TraversalPlayMode.txt";
    public float timeScale = 2f;
    public string only = ""; // run only routes whose name contains this

    sealed class Route
    {
        public string Name; public Vector3 Start, Target; public bool RideLifts = true; public string Expect = "any";
        public int MinLinks; public float Timeout = 120, Speed = 6.2f;
        public bool KeepPosition;          // continue from where the previous route ended
        public string FaceLift;            // stand facing this lift's doors first (reads its display)
        public string ParkLift; public int ParkFloor; // move that car to a floor before the route
    }

    bool running;

    void Update()
    {
        // The editor may lose focus while MCP drives it; keep the gameplay state (and pace) running.
        var ui = CampusRift.UI.UIStateManager.Instance;
        if (ui != null && ui.State != CampusRift.UI.UIState.Gameplay && ui.State != CampusRift.UI.UIState.Modal) ui.EnterScene(true);
        if (running && Time.timeScale != timeScale) Time.timeScale = timeScale;
    }

    static bool Walkable(ref Vector3 point)
    {
        if (!NavMesh.SamplePosition(point, out var hit, 3f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - point.y) > 1f) return false;
        point = hit.position; return true;
    }

    static CampusElevator Lift(string building)
    {
        foreach (var e in Object.FindObjectsByType<CampusElevator>()) if (e.building == building) return e;
        return null;
    }

    // Test fixture: run the car's own state machine until it stands closed at `floor`.
    static void Park(CampusElevator e, int floor)
    {
        e.RequestFloor(floor);
        for (int i = 0; i < 6000 && (e.IsMoving || e.CurrentFloor != floor || e.State != CampusElevator.LiftState.Closed); i++) e.Tick(0.05f);
    }

    IEnumerator Start()
    {
        var brain = Object.FindAnyObjectByType<MonsterBrain>();
        brain.enabled = false;
        brain.GetComponent<MonsterPerception>().enabled = false;
        var hearing = brain.GetComponent<MonsterHearing>(); if (hearing != null) hearing.enabled = false;
        var search = brain.GetComponent<MonsterSearch>(); if (search != null) search.enabled = false;
        var explorer = Object.FindAnyObjectByType<CampusExplorer>(); if (explorer != null) explorer.enabled = false;
        var nav = brain.GetComponent<MonsterNavigation>();
        var agent = nav.Agent;
        var sharedConfig = nav.config;
        nav.config = Instantiate(sharedConfig); // route-specific RideLifts without touching the asset
        float originalScale = Time.timeScale; Time.timeScale = timeScale; Application.runInBackground = true; running = true;
        var results = new List<string>();
        var routes = new List<Route>
        {
            new Route { Name = "C stairs up F1->F3 (steep flights, links)", Start = new Vector3(-26.2f, 0f, 17.6f), Target = new Vector3(-26.2f, 8f, 17.6f), RideLifts = false, Expect = "stairs", MinLinks = 4 },
            new Route { Name = "C stairs down F3->F1 (links)", KeepPosition = true, Target = new Vector3(-26.2f, 0f, 17.6f), RideLifts = false, Expect = "stairs", MinLinks = 4 },
            new Route { Name = "A7N west door drop, lower flight -> F9 corridor", Start = new Vector3(46.6f, 30.96f, 30.4f), Target = new Vector3(42.4f, 32.01f, 30.4f), RideLifts = false, MinLinks = 1, Timeout = 60 },
            new Route { Name = "A7N west door drop, F9 corridor -> lower flight", KeepPosition = true, Target = new Vector3(46.6f, 30.96f, 30.4f), RideLifts = false, Timeout = 60 },
            new Route { Name = "C F1->F2 short hop: stairs are faster", Start = new Vector3(-26.2f, 0f, 17.6f), Target = new Vector3(-26.2f, 4f, 17.6f), Expect = "stairs", FaceLift = "C" },
            new Route { Name = "X F1->F11 on foot (reference)", Start = new Vector3(-38.3f, 0f, -28f), Target = new Vector3(-43.3f, 35.4f, -28f), RideLifts = false, Expect = "stairs", Timeout = 240 },
            new Route { Name = "X F1->F11, car seen parked at F13: stairs are faster", Start = new Vector3(-38.3f, 0f, -28f), Target = new Vector3(-43.3f, 35.4f, -28f), Expect = "stairs", FaceLift = "X", ParkLift = "X", ParkFloor = 12, Timeout = 240 },
            new Route { Name = "X F1->F11, car seen waiting at F1: lift is faster", Start = new Vector3(-38.3f, 0f, -28f), Target = new Vector3(-43.3f, 35.4f, -28f), Expect = "lift", FaceLift = "X", ParkLift = "X", ParkFloor = 0, Timeout = 240 },
            new Route { Name = "X F11->F1 right after that ride: downhill stairs are faster", KeepPosition = true, Target = new Vector3(-43.3f, 0f, -28f), Expect = "stairs", Timeout = 240 },
            new Route { Name = "V F1->F15 at investigate pace, car unseen: lift is faster", Start = new Vector3(0f, 0f, -38.7f), Target = new Vector3(0f, 48.5f, -38.7f), Expect = "lift", Speed = 3.8f, Timeout = 300 },
        };
        float referenceFoot = -1;
        foreach (var route in routes)
        {
            if (only.Length > 0 && !route.Name.Contains(only)) continue;
            Vector3 target = route.Target;
            if (!Walkable(ref target)) { results.Add($"{route.Name} SKIP: target not on the NavMesh"); continue; }
            if (!route.KeepPosition)
            {
                Vector3 start = route.Start;
                if (!Walkable(ref start)) { results.Add($"{route.Name} SKIP: start not on the NavMesh"); continue; }
                // Harness placement only; the monster itself never teleports.
                nav.Stop(); agent.enabled = false; brain.transform.position = start; agent.enabled = true; Physics.SyncTransforms();
            }
            if (!string.IsNullOrEmpty(route.ParkLift)) { var parked = Lift(route.ParkLift); if (parked != null) Park(parked, route.ParkFloor); }
            if (!string.IsNullOrEmpty(route.FaceLift))
            {
                var faced = Lift(route.FaceLift);
                if (faced != null) brain.transform.rotation = Quaternion.LookRotation(-faced.outward);
                float look = Time.time + 0.6f; while (Time.time < look) yield return null; // time to read the display
            }
            yield return null;
            nav.config.RideLifts = route.RideLifts;
            int links0 = nav.LinksTraversed, rides0 = nav.LiftRides, doorOverlaps = 0;
            float t0 = Time.time, deadline = t0 + route.Timeout, maxPlanar = 0, maxVertical = 0;
            string plan = "", firstDecision = "", jump = "", previousDecision = nav.LastLiftDecision;
            Vector3 previous = brain.transform.position;
            while (Time.time < deadline && Vector3.Distance(brain.transform.position, target) > 1.2f)
            {
                nav.SetSpeed(route.Speed);
                nav.MoveTo(target);
                if (nav.UsingLift) plan = nav.LiftPlan;
                if (firstDecision.Length == 0 && nav.LastLiftDecision != previousDecision) firstDecision = nav.LastLiftDecision;
                bool wasLink = agent.enabled && agent.isOnOffMeshLink; var wasStatus = nav.Status;
                yield return null;
                float dt = Time.deltaTime;
                if (dt > 0)
                {
                    Vector3 d = brain.transform.position - previous;
                    float planar = new Vector2(d.x, d.z).magnitude / dt, vertical = Mathf.Abs(d.y) / dt;
                    if (planar > maxPlanar) { maxPlanar = planar; jump = $"{previous:F2}->{brain.transform.position:F2} dt={dt:F3} {wasStatus}/{nav.Status} link={wasLink}/{agent.enabled && agent.isOnOffMeshLink}"; }
                    maxVertical = Mathf.Max(maxVertical, vertical);
                }
                previous = brain.transform.position;
                if (nav.Riding) continue;
                foreach (var c in Physics.OverlapCapsule(brain.transform.position + Vector3.up * 0.25f, brain.transform.position + Vector3.up * 1.5f, 0.2f, ~0, QueryTriggerInteraction.Ignore))
                { var door = c.GetComponentInParent<CampusAutomaticDoor>(); if (door != null && door.OpenAmount < 0.9f) doorOverlaps++; }
                File.WriteAllText("Temp/shaban-traversal-progress.txt", $"{route.Name}: {brain.transform.position} -> {target}; {nav.Status} {plan}; {Time.time - t0:F1}s");
            }
            float seconds = Time.time - t0;
            bool arrived = Vector3.Distance(brain.transform.position, target) <= 1.2f;
            int links = nav.LinksTraversed - links0, rides = nav.LiftRides - rides0;
            bool rode = rides > 0;
            bool expectOk = route.Expect == "any" || (route.Expect == "lift" ? rode : !rode);
            // Walking pace plus agent overshoot; slopes add vertical speed, cars move at 2.8 m/s. A jump is far above.
            bool noJump = maxPlanar < route.Speed * 1.6f + 1f && maxVertical < 10f;
            bool pass = arrived && expectOk && links >= route.MinLinks && doorOverlaps == 0 && noJump && nav.Ready;
            if (route.Name.Contains("(reference)") && arrived) referenceFoot = seconds;
            string compare = route.Expect == "lift" && referenceFoot > 0 && route.Name.StartsWith("X F1") ? $", on-foot reference={referenceFoot:F1}s" : "";
            string line = $"{route.Name} {(pass ? "PASS" : "FAIL")}: {seconds:F1}s, arrived={arrived}, liftRides={rides}, links={links}, maxPlanar={maxPlanar:F1}m/s, maxVertical={maxVertical:F1}m/s, closedDoorOverlaps={doorOverlaps}, ready={nav.Ready}{compare}" +
                (firstDecision.Length > 0 ? $"\n    decision: {firstDecision}" : "") + (plan.Length > 0 ? $"\n    plan: {plan}" : "") + (!noJump ? $"\n    fastest frame: {jump}" : "");
            results.Add(line); Debug.Log("[Traversal] " + line);
            nav.Stop();
            // A car that was used must close again (the monster must not hold its doors).
            float wait = Time.time + 12f;
            var lifts = Object.FindObjectsByType<CampusElevator>();
            while (Time.time < wait) { bool open = false; foreach (var e in lifts) if (e.DoorAmount > 0.01f) open = true; if (!open) break; yield return null; }
            foreach (var e in lifts) if (e.DoorAmount > 0.01f) results.Add($"    note: lift {e.building} doors still {e.DoorAmount:F2} open at F{e.CurrentFloor + 1} ({e.State})");
        }
        running = false; Time.timeScale = originalScale; nav.config = sharedConfig;
        File.WriteAllLines(output, results);
        File.WriteAllText("Temp/shaban-traversal-progress.txt", "DONE\n" + string.Join("\n", results));
    }
}
#endif
