# Multi-floor hunt — Play Mode scenarios

Harness: `ShabanHuntScenarioTest` (Editor only). It drives the real `CampusExplorer` (planar velocity,
so footsteps are real), rides the real `CampusElevator`, keeps the player alive and records what the
monster does. The monster only receives its own perceptions. Real New Game flow, SampleScene.

Columns: seconds after the scenario start. `lost` = monster stops seeing the player,
`landing` = monster within 6 m of the lift landing where the player got off, `found` = sees the player again.

## Baseline (before the upgrade)

| Scenario | Result |
| --- | --- |
| Lift E, F1 → F5, hide in a room | Monster walked into the empty ground-floor shaft, searched F1 only, `Forget()` at ~24 s, back to yard patrol. Never above F1 (closest 14.6 m, straight below). |

## After the upgrade (final code)

| Scenario | lost | landing | found | Notes |
| --- | --- | --- | --- | --- |
| Lift E F1 → F5, hide in a room | 1.3 | 22.7 | 24.8 | Saw the boarding, watched the display to the F5 stop, took the stairs. |
| Lift E F1 → F5 → straight back to F1 (feint), hide | 1.3 | 22.4 | ~50 (4/4 runs) | Read "car left F5" and "stopped at F1" at the F5 lobby, came back down. |
| Lift X F1 → F11 (13-floor tower), hide | 1.3 | 52.2 | 64.8 | Watched the display until the car stopped at F11, climbed 10 floors. |
| Run up the stairs to F3, hide in a room | 1.1 | – | 14.2 | Heard footsteps through the floor and the lift chime at F3. |
| Break line of sight into a room, same floor | – | – | 1.1 | Never lost the player (glass lobby, doors). |
| Sprint away across the yard | 1.3 | – | 1.8 | Reacquired while the player turned to hide. |

Earlier development runs worth keeping:

- Lift E without seeing the boarding (monster blocked by a pillar): inferred the ride from the
  display ("car moved to F5 — the player called or rode it") and found the player at 34.6 s.
- Repeatability: feint 4/4 after the final model fixes; lift E 3/3; stairs 3/3; lift X 3/3.
- The monster reaching the car before the doors close now keeps the doors open (it is never carried)
  and attacks the player inside, instead of standing in the empty shaft.

## Automated checks (Play Mode)

- `ShabanHunterValidation`: 01-Memory 8/8, 02-Motion 7/7, 03-Pursuit 5/5, 04-ProbabilitySearch 7/7,
  05-Graph 5/5, 06-Interception 6/6, 07-Belief 9/9, 08-Lifts 11/11, 09-Navigation 3/3.
- `ShabanBehaviorPlayTest`: 19/19 (the wall/search check now waits out `LostSightGraceTime`,
  which the earlier hunter upgrade introduced after this test was written).
- `ShabanNavigationPlayTest`: 3/3 routes, no closed-door overlaps.
- Player scripts compile for Windows standalone (no editor-only code in runtime assemblies).

## Measured cost (Editor, Mono)

| Item | Cost |
| --- | --- |
| Topology build (1,951 nodes, 61k edges), once per scene | 20 ms |
| Glass collider scan, once per scene | 2.8 ms |
| Belief tick (320 hypotheses, 5 Hz, ≤40 sight rays) | ~0.08 ms + rays |
| Search plan (Dijkstra + scoring, 1 Hz) | ~0.7 ms |
| Managed allocations per belief tick | 0 bytes |
