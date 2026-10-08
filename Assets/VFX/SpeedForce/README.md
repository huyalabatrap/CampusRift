# Speed Force boost VFX

Hold Shift (PC) or the boost control (mobile) while moving: `SpeedForceVFX` on `Campus Explorer` adds a Flash-style lightning look on top of the existing afterimages. It only reads `CampusExplorer.IsSprinting`; movement, energy and input are untouched.

| Layer | What it does | Asset |
| --- | --- | --- |
| Body arcs | 6 jagged bolts strike between hips, chest, head, hands, arms, legs and feet (or leap off the body), re-strike every 50-110 ms | `SF_Bolt` + Kenney `trace_01` |
| Lightning trail | 3 bolts (chest, hips, knees) dragged along the last 0.3 s of the running path, re-jittered every 45 ms, thinning toward the tail | `SF_Bolt` |
| Crackle | Electric blobs flicker over the body | Kenney `spark_01-04` (2x2 sheet) |
| Forks | Short bolts shoot outward | Kenney `spark_05/06` |
| Sparks | Streaks kicked up from the feet | Kenney `trace_02` |
| Ignition | Flare, fork/crackle burst and a zap when the boost starts | Kenney `flare_01`, Kenney sci-fi `laserSmall_000-002` |
| Light | Flickering point light (no shadows) | - |
| Screen lines | Faint radial speed lines behind the HUD, gameplay state only | `Textures/SpeedLines.png` (project-made) |

Colors: `primaryColor` gold lightning, `secondaryColor` rift violet (`secondaryChance` of bolts). The additive shader pushes vertex colors into HDR (`_Intensity` on each `SF_*` material), so the scene Bloom makes the cores glow. Afterimages were re-tinted gold to violet at 0.28 opacity.

Re-apply everything (materials, component on the scene player and `CampusExplorer.prefab`, afterimage tint) with **Campus Rift/VFX/Setup Speed Force Boost**. Call `SpeedForceVFX.Clear()` after custom teleports; jumps over 4 m clear it automatically.

Licenses: Kenney Particle Pack and Sci-fi Sounds are CC0 (`License-Kenney-particle-pack.txt`, `Audio/License-Kenney-sci-fi.txt`). Source zip kept in `Artifacts/SpeedForce`.
