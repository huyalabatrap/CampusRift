# Lifts when faster, NavMesh links, observation points — Play Mode

Harness: `ShabanTraversalPlayTest` (Editor only). Brain, senses and the player's controls are off;
`MonsterNavigation.MoveTo` is driven directly at chase pace (6.2 m/s) unless noted. The harness only
places the monster at each route start; every metre after that is walked, climbed or ridden.
`maxPlanar` is the fastest horizontal frame speed (a teleport would show tens of m/s). Result file:
`Validation/TraversalPlayMode.txt`.

| Route | Time | Lift | Links | Decision logged by the monster |
| --- | --- | --- | --- | --- |
| C stair core F1 → F3 (flights steeper than the agent slope) | 7.2 s | – | 4 | – |
| C stair core F3 → F1 | 7.9 s | – | 4 | – |
| A7N west door, lower flight → F9 corridor (1.05 m drop) | 1.6 s | – | 1 | – |
| A7N west door, F9 corridor → lower flight | 1.2 s | – | 0 | – |
| C F1 → F2 with the car at hand | 4.1 s | no | 2 | lift 10 s vs stairs 4 s: stairs |
| X F1 → F11 on foot (reference, lifts off) | 31.3 s | – | – | – |
| X F1 → F11, car seen parked at F13 | 31.2 s | no | – | lift 42 s vs stairs 33 s: stairs |
| X F1 → F11, car seen waiting at F1 | **23.5 s** | yes | – | lift 24 s vs stairs 33 s: lift |
| X F11 → F1 right after that ride | 25.9 s | yes | – | lift 26 s vs stairs 31 s: lift |
| V F1 → F15 at investigate pace (3.8 m/s), car unseen | 28.0 s | yes | – | lift 39 s vs stairs 49 s: lift (28 s once the display was read) |

All 10 routes pass: arrived, no frame faster than walking pace + agent overshoot, no overlap with a
closed automatic door, the agent back on the NavMesh after each ride, and every car used closed its
doors again afterwards (the monster does not hold them once it has stepped out).

Estimates versus measured times: stairs 33 s estimated / 31.3 s walked; lift 24 s / 23.5 s. Before the
turning allowance was added the stairs estimate was 14 s — switchback stairs cost the agent two
180° turns per floor, which at 6.2 m/s is more time than the flights themselves.

Bugs found and fixed while running this harness:

- After a link the agent kept the velocity it had before the link, drifted back onto the link the
  other way and Unity then moved it across the link in a single frame (a 5 m jump). The traversal
  now hands the agent a velocity along the link and plans again from the far side.
- The floor display was unreadable from right under it: the sight line to the panel grazed the door
  header. Sight is now checked to the air just in front of the panel face.
