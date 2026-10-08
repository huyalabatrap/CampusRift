#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using CampusRift;
using CampusRift.Monsters;
using UnityEngine;
using UnityEngine.AI;

// Editor-only Play Mode harness, attached temporarily through MCP. It never ships in a build.
// It drives the real CampusExplorer (planar velocity only, so footsteps/animation stay real),
// rides the real CampusElevator and records what the monster does without helping it.
// Scenarios: elevator (ride up and hide), elevator-return (ride up, then back down),
// stairs (run upstairs and hide), room (hide in a room on the same floor), sprint (outrun).
[DefaultExecutionOrder(-300)]
public sealed class ShabanHuntScenarioTest : MonoBehaviour
{
    public string scenario = "elevator";
    public string building = "E";
    public int rideToFloor = 4;
    public float observeSeconds = 90;
    public float monsterDistance = 13;
    // Optional fixed hiding spots (zero = pick one from the room graph). The automatic pick depends on
    // which rooms are reachable, so a navigation fix can move it; fixed spots keep runs comparable.
    public Vector3 hideAt, groundHideAt;
    public string output = "Temp/shaban-scenario.txt";

    MonsterBrain brain; MonsterPerception sight; MonsterMemory memory; MonsterNavigation nav;
    MonsterBelief belief; MonsterElevatorAwareness lifts;
    CampusExplorer player; CampusElevator lift;
    FieldInfo planar;
    Vector3 drive; bool driving;
    readonly StringBuilder log = new StringBuilder();
    float started, minDistance = float.PositiveInfinity, sameFloorAt = -1, reacquiredAt = -1, nearLandingAt = -1, lostAt = -1;
    string phase = "setup", lastReasoning, lastLiftEvent = "";
    int hitsBefore;
    NavMeshQueryFilter filter;
    Vector3 trackedLanding; bool trackLanding;

    void Update()
    {
        // The editor may lose focus while MCP drives it; keep the gameplay state running.
        var ui = CampusRift.UI.UIStateManager.Instance;
        if (ui != null && ui.State != CampusRift.UI.UIState.Gameplay && ui.State != CampusRift.UI.UIState.Modal) ui.EnterScene(true);
        if (driving && player != null)
        {
            Vector3 v = drive; v.y = 0;
            planar.SetValue(player, v);
        }
    }

    void Place(Transform t, Vector3 p, float yaw)
    {
        var agent = t.GetComponent<NavMeshAgent>();
        if (agent != null) { nav.Stop(); agent.enabled = false; }
        var cc = t.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        t.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
        if (cc != null) cc.enabled = true;
        if (agent != null) agent.enabled = true;
        Physics.SyncTransforms();
    }

    IEnumerator WalkTo(Vector3 target, float speed, float timeout = 30)
    {
        var path = new NavMeshPath();
        NavMesh.SamplePosition(player.transform.position, out var a, 2, filter);
        NavMesh.SamplePosition(target, out var b, 2, filter);
        Vector3[] corners = NavMesh.CalculatePath(a.position, b.position, filter, path) ? path.corners : new[] { target };
        float end = Time.time + timeout, nextRecord = Time.time + 0.5f;
        int index = 0;
        driving = true;
        while (index < corners.Length && Time.time < end)
        {
            Vector3 delta = corners[index] - player.transform.position; delta.y = 0;
            if (delta.magnitude < 0.45f) { index++; continue; }
            drive = delta.normalized * speed;
            if (Time.time >= nextRecord) { nextRecord = Time.time + 0.5f; Record(); }
            // Caught on the way: stop walking into the monster.
            if (brain.CurrentState == MonsterState.Attack && Vector3.Distance(player.transform.position, brain.transform.position) < 2f) { Record("caught while moving"); break; }
            yield return null;
        }
        drive = Vector3.zero;
        yield return null;
        driving = false;
    }

    // Straight walk (cabin interior has no reliable NavMesh while the car is elsewhere).
    IEnumerator WalkStraight(Vector3 target, float speed, float timeout = 6)
    {
        float end = Time.time + timeout;
        driving = true;
        while (Time.time < end)
        {
            Vector3 delta = target - player.transform.position; delta.y = 0;
            if (delta.magnitude < 0.25f) break;
            drive = delta.normalized * Mathf.Min(speed, delta.magnitude / Mathf.Max(Time.deltaTime, 0.001f));
            yield return null;
        }
        drive = Vector3.zero; yield return null; driving = false;
    }

    IEnumerator Observe(float seconds, float step = 0.5f)
    {
        float end = Time.time + seconds;
        while (Time.time < end) { Record(); yield return new WaitForSeconds(step); }
    }

    Vector3 HideSpot(Vector3 from, float minPath, float maxPath)
    {
        Vector3 best = from; float bestLength = -1;
        var p = new NavMeshPath();
        foreach (var node in nav.roomGraph.Nodes)
        {
            if (Mathf.Abs(node.WorldPosition.y - from.y) > 0.8f || !node.RoomID.EndsWith("/inside")) continue;
            if (Vector3.Distance(node.WorldPosition, from) > maxPath) continue;
            if (!NavMesh.CalculatePath(from, node.WorldPosition, filter, p) || p.status != NavMeshPathStatus.PathComplete) continue;
            float length = 0; for (int i = 1; i < p.corners.Length; i++) length += Vector3.Distance(p.corners[i - 1], p.corners[i]);
            if (length < minPath || length > maxPath) continue;
            if (length > bestLength) { bestLength = length; best = node.WorldPosition; }
        }
        return best;
    }

    void Record(string note = null)
    {
        Vector3 p = player.transform.position, m = brain.transform.position;
        float d = Vector3.Distance(p, m);
        bool after = phase.StartsWith("after");
        if (after) minDistance = Mathf.Min(minDistance, d);
        if (after && sameFloorAt < 0 && Mathf.Abs(p.y - m.y) < 1.6f) sameFloorAt = Time.time - started;
        if (after && reacquiredAt < 0 && sight.CanSeePlayer) reacquiredAt = Time.time - started;
        if (trackLanding && nearLandingAt < 0 && Vector3.Distance(m, trackedLanding) < 6f) nearLandingAt = Time.time - started;
        if (lostAt < 0 && phase != "setup" && phase != "doors" && !sight.CanSeePlayer && brain.CurrentState != MonsterState.Chase && brain.CurrentState != MonsterState.Attack)
            lostAt = Time.time - started;
        log.AppendLine($"{Time.time - started,6:F1} {phase,-14} P{p:F1} M{m:F1} d={d:F1} st={brain.CurrentState} see={sight.CanSeePlayer} " +
            $"dest={nav.CurrentDestination:F1} v={(nav.Agent.enabled ? nav.Agent.velocity.magnitude : 0f):F1} {note}");
        string reasoning = $"        intent: {brain.Intent} | {(belief != null ? belief.Summary() : "")} | nav {nav.Status}" +
            (nav.UsingLift ? " " + nav.LiftPlan : "");
        if (reasoning != lastReasoning) { log.AppendLine(reasoning); lastReasoning = reasoning; }
        if (lifts != null && lifts.LastEvent != lastLiftEvent) { lastLiftEvent = lifts.LastEvent; log.AppendLine($"        LIFT: {lastLiftEvent}"); }
    }

    IEnumerator Start()
    {
        Application.runInBackground = true;
        brain = FindAnyObjectByType<MonsterBrain>(); sight = brain.GetComponent<MonsterPerception>();
        memory = brain.GetComponent<MonsterMemory>(); nav = brain.GetComponent<MonsterNavigation>();
        belief = brain.GetComponent<MonsterBelief>(); lifts = brain.GetComponent<MonsterElevatorAwareness>();
        player = FindAnyObjectByType<CampusExplorer>();
        planar = typeof(CampusExplorer).GetField("planarVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var e in FindObjectsByType<CampusElevator>()) if (e.building == building) lift = e;
        filter = new NavMeshQueryFilter { agentTypeID = nav.Agent.agentTypeID, areaMask = NavMesh.AllAreas };
        var health = player.GetComponent<PlayerMonsterHealth>();
        if (health != null)
        {
            // Survive the whole recording; hits are still counted by MonsterCombat.
            health.maxHealth = 100000;
            typeof(PlayerMonsterHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(health, 100000f);
        }
        // Keep the monster out of the set-up (the player spawns in its view).
        brain.enabled = false; nav.Stop();
        hitsBefore = brain.GetComponent<MonsterCombat>().Hits;
        started = Time.time;
        yield return null;
        switch (scenario)
        {
            case "elevator": yield return ElevatorScenario(false); break;
            case "elevator-return": yield return ElevatorScenario(true); break;
            case "stairs": yield return StairsScenario(); break;
            case "room": yield return RoomScenario(); break;
            case "sprint": yield return SprintScenario(); break;
        }
        log.AppendLine($"RESULT scenario={scenario} minDistance={minDistance:F1} lostAt={lostAt:F1} sameFloorAt={sameFloorAt:F1} nearLandingAt={nearLandingAt:F1} " +
            $"reacquiredAt={reacquiredAt:F1} finalState={brain.CurrentState} hits={brain.GetComponent<MonsterCombat>().Hits - hitsBefore} " +
            $"recoveries={nav.Recoveries} reseeds={(belief != null ? belief.Reseeds : 0)}");
        File.WriteAllText(output, log.ToString() + "\nDONE\n");
    }

    bool Usable(Vector3 probe, Vector3 playerAt, out Vector3 spot)
    {
        spot = probe;
        if (!NavMesh.SamplePosition(probe, out var h, 1f, filter) || Mathf.Abs(h.position.y - playerAt.y) > 1f) return false;
        spot = h.position;
        return sight.ClearSight(h.position + Vector3.up * 1.5f, playerAt + Vector3.up * 1.1f, player.transform) && Connected(h.position, playerAt);
    }

    bool Connected(Vector3 a, Vector3 b)
    {
        var p = new NavMeshPath();
        if (!NavMesh.SamplePosition(b, out var hb, 2f, filter)) return false;
        if (NavMesh.CalculatePath(a, hb.position, filter, p) && p.status == NavMeshPathStatus.PathComplete) return true;
        return RoomPathfinder.Find(nav.roomGraph, a, hb.position, new System.Collections.Generic.List<Vector3>());
    }

    IEnumerator Chased(Vector3 monsterAt, Vector3 playerAt, Vector3 facing, float seconds)
    {
        // Nearest walkable spot around the requested one with a clear view of the player; if none,
        // any connected spot 6-14 m around the player that sees it.
        Vector3 spot = monsterAt; bool found = false;
        for (int ring = 0; ring <= 4 && !found; ring++)
            for (int k = 0; k < Mathf.Max(1, ring * 8) && !found; k++)
            {
                Vector3 probe = monsterAt + Quaternion.Euler(0, k * 360f / Mathf.Max(1, ring * 8), 0) * Vector3.forward * ring * 0.75f;
                if (Usable(probe, playerAt, out var h)) { spot = h; found = true; }
            }
        for (float radius = Mathf.Max(8f, monsterDistance); radius >= 8f && !found; radius -= 1.5f)
            for (int k = 0; k < 24 && !found; k++)
            {
                Vector3 probe = playerAt + Quaternion.Euler(0, k * 15f, 0) * Vector3.forward * radius;
                if (Usable(probe, playerAt, out var h)) { spot = h; found = true; }
            }
        if (!found) log.AppendLine("WARNING: no clear view from the requested monster start");
        NavMesh.SamplePosition(spot, out var mh, 3, filter);
        Place(brain.transform, mh.position, Quaternion.LookRotation(Vector3.ProjectOnPlane(playerAt - mh.position, Vector3.up)).eulerAngles.y);
        Place(player.transform, playerAt + Vector3.up * 0.08f, Quaternion.LookRotation(facing).eulerAngles.y);
        memory.Forget(); if (belief != null) belief.Deactivate();
        hitsBefore = brain.GetComponent<MonsterCombat>().Hits;
        brain.enabled = true;
        // The scenario starts with the monster spotting the player: look and decide right now, so the
        // start does not depend on the state the brain happened to be in when it was paused.
        sight.Scan(); brain.Decide();
        phase = "seen";
        yield return Observe(seconds, 0.2f);
    }

    IEnumerator ElevatorScenario(bool rideBack)
    {
        Vector3 outward = lift.outward;
        Vector3 door0 = lift.floors[0].entrances[0];
        Vector3 cabin0 = lift.cabinSpaces[0].center; cabin0.y = lift.floors[0].height + 0.05f;
        Place(player.transform, door0 + outward * 0.6f + Vector3.up * 0.08f, Quaternion.LookRotation(-outward).eulerAngles.y);
        lift.RequestFloor(0);
        phase = "doors";
        while (lift.State != CampusElevator.LiftState.Open) { Record(); yield return new WaitForSeconds(0.25f); }
        // The monster spots the player at the open car from across the lobby / through the glass front.
        yield return Chased(door0 + outward * monsterDistance, door0 + outward * 0.6f, -outward, 0.6f);
        phase = "enter-cabin";
        yield return WalkStraight(cabin0, 6f);
        lift.RequestFloor(rideToFloor);
        Record("requested floor " + (rideToFloor + 1));
        phase = "riding";
        float departDeadline = Time.time + 6f;
        while (!lift.IsMoving && Time.time < departDeadline) { Record(); yield return new WaitForSeconds(0.25f); }
        if (!lift.IsMoving) { log.AppendLine("CAUGHT AT THE DOORS: the monster reached the car before it left."); yield break; }
        while (lift.CurrentFloor != rideToFloor || lift.IsMoving || lift.DoorAmount < 0.95f) { Record(); yield return new WaitForSeconds(0.5f); }
        trackedLanding = lift.floors[rideToFloor].entrances[0] + outward * 1.4f; trackLanding = true;
        phase = "after-exit";
        Vector3 exitPoint = lift.floors[rideToFloor].entrances[0] + outward * 2.2f;
        yield return WalkStraight(exitPoint, 3.2f);
        NavMesh.SamplePosition(exitPoint, out var exitHit, 1.5f, filter);
        if (!rideBack)
        {
            Vector3 hide = HideSpot(exitHit.position, 8, 20);
            phase = "after-walk";
            Record("hide target " + hide.ToString("F1"));
            yield return WalkTo(hide, 3.2f);
            phase = "after-hide";
            yield return Observe(observeSeconds);
            yield break;
        }
        // Feint: step out, wait a moment, then ride straight back down while the monster climbs.
        yield return Observe(3f);
        phase = "after-return";
        lift.RequestFloor(rideToFloor);
        float wait = Time.time + 20f;
        while ((lift.CurrentFloor != rideToFloor || lift.IsMoving || lift.DoorAmount < 0.95f) && Time.time < wait) { Record(); yield return new WaitForSeconds(0.25f); }
        Vector3 cabinUp = lift.cabinSpaces[0].center; cabinUp.y = lift.floors[rideToFloor].height + 0.05f;
        yield return WalkStraight(cabinUp, 3.2f);
        lift.RequestFloor(0);
        wait = Time.time + 8f;
        while (!lift.IsMoving && Time.time < wait) { Record(); yield return new WaitForSeconds(0.25f); }
        if (!lift.IsMoving) { log.AppendLine("CAUGHT AT THE DOORS (return trip)."); yield break; }
        while (lift.CurrentFloor != 0 || lift.IsMoving || lift.DoorAmount < 0.95f) { Record(); yield return new WaitForSeconds(0.5f); }
        trackedLanding = lift.floors[0].entrances[0] + outward * 1.4f;
        yield return WalkStraight(lift.floors[0].entrances[0] + outward * 2.2f, 3.2f);
        NavMesh.SamplePosition(lift.floors[0].entrances[0] + outward * 2.2f, out var groundHit, 1.5f, filter);
        Vector3 ground = groundHideAt != Vector3.zero ? groundHideAt : HideSpot(groundHit.position, 8, 22);
        Record("ground hide target " + ground.ToString("F1"));
        yield return WalkTo(ground, 3.2f);
        phase = "after-hide2";
        yield return Observe(observeSeconds);
    }

    // The player runs up the nearest staircase two floors and hides in a room there.
    IEnumerator StairsScenario()
    {
        Vector3 door0 = lift.floors[0].entrances[0] + lift.outward * 1.4f;
        NavMesh.SamplePosition(door0, out var start, 2, filter);
        yield return Chased(start.position + lift.outward * monsterDistance, start.position, -lift.outward, 0.6f);
        phase = "after-run";
        float targetHeight = lift.floors[Mathf.Min(2, lift.FloorCount - 1)].height;
        Vector3 target = start.position; float best = float.PositiveInfinity;
        foreach (var node in nav.roomGraph.Nodes)
        {
            if (Mathf.Abs(node.WorldPosition.y - targetHeight) > 0.8f || !node.RoomID.EndsWith("/inside")) continue;
            float d = Vector3.Distance(node.WorldPosition, start.position);
            if (d < best) { best = d; target = node.WorldPosition; }
        }
        Record("stairs target " + target.ToString("F1"));
        yield return WalkTo(target, 9f, 40);
        phase = "after-hide";
        yield return Observe(observeSeconds);
    }

    // The player breaks line of sight and slips into a room on the same floor.
    IEnumerator RoomScenario()
    {
        Vector3 door0 = lift.floors[0].entrances[0] + lift.outward * 1.4f;
        NavMesh.SamplePosition(door0, out var start, 2, filter);
        yield return Chased(start.position + lift.outward * monsterDistance, start.position, -lift.outward, 0.6f);
        phase = "after-run";
        Vector3 hide = hideAt != Vector3.zero ? hideAt : HideSpot(start.position, 10, 26);
        Record("room target " + hide.ToString("F1"));
        yield return WalkTo(hide, 9f, 30);
        phase = "after-hide";
        yield return Observe(observeSeconds);
    }

    // The player outruns the monster across the yard and keeps going out of sight.
    IEnumerator SprintScenario()
    {
        Vector3 origin = new Vector3(8, 0.1f, -4);
        NavMesh.SamplePosition(origin, out var o, 3, filter);
        yield return Chased(o.position, o.position + Vector3.forward * 12f, Vector3.forward, 0.8f);
        phase = "after-run";
        yield return WalkTo(new Vector3(40, 0.1f, 30), 10f, 25);
        yield return WalkTo(HideSpot(player.transform.position, 6, 30), 5f, 20);
        phase = "after-hide";
        yield return Observe(observeSeconds);
    }
}
#endif
