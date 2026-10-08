# Huyễn Ảnh Dẫn Dụ / Phantom Decoy

Skill 3 is installed on `CampusExplorer.prefab` and the player instance in `SampleScene.unity`.
It has one reusable decoy, a 5.5 second maximum life, 7.4 m/s travel speed, and an
18 second cooldown. It deals no damage.

## Controls

- PC: press **G** to aim. Press **G** again or left click to release. Right click cancels.
- Mobile: hold the **PHANTOM** button, drag the intended direction, and release. Slide to
  **CANCEL** to abort. A tap with no drag uses the camera's forward direction.
- The third PC skill slot and the mobile button show ready, aiming, cooldown and unavailable states.

## How it works

`PhantomDecoySkill` builds one pooled visual actor from the player's Animator child only.
`PhantomDecoy` keeps the nine renderer parts already curated by `CharacterAfterimageTrail`;
four duplicate outline shells remain disabled. The original SchoolGirl locomotion controller
drives the run pose. The existing URP afterimage shader tints the clone blue and violet,
with a short reveal/fade and a few existing Void Wall shard particles.

At cast time the actor samples a safe NavMesh start and calculates one complete route toward
the chosen direction. A small CharacterController follows its corners, requests nearby
automatic doors to open, and dissolves if a physical obstruction prevents progress. The
route remains on walkable surfaces; it has no jump or teleport behavior. Only one instance
can be live. No player health, movement, camera or combat components are cloned.

`SoundEventBus` accepts anonymous sound observations from the player and the decoy. The
decoy emits a launch sound and running footfall evidence about every 0.34 seconds, with a
small procedural 3D footstep sample. Hearing uses the event location and collider only for
occlusion. It does not infer that every sound belongs to the player.

`MonsterPerception` scans both silhouettes using the same view cone and line of sight.
`MonsterTargetAssessment` scores visible candidates: a clone with matching recent footsteps
can draw Shaban away from a visible player, while a player already in attack range keeps priority. Shaban remembers
the selected observation, seeds its belief from it, predicts a short reachable route, and
investigates sounds when nothing is visible. After close sustained observation (about
0.65 s within 3.5 m or 2.1 s within 8 m), it rejects the shimmering clone and searches
the last evidence area. Expiry follows the ordinary lost sight grace and Search behavior.

## Source and license

- Visual rig and run animation: the project's supplied SchoolGirl model/controller.
- Ghost shader: the project's `Assets/VFX/Afterimage/Afterimage.shader` and material.
- Shards: the project's Void Wall mesh/material.
- Cast and fade audio: `forceField_004.ogg` and `forceField_002.ogg`, already imported from
  Kenney Sci-Fi Sounds under CC0. See `Assets/Skills/VoidWall/Audio/License-Kenney-sci-fi.txt`.
- Footfall audio: generated procedurally in memory; no third-party sample.

## Verification (Unity Editor)

- Compile: no C# errors. Play Mode creates one decoy with nine active skinned renderers.
- AI: phantom-only sight leads to Chase; sound-only evidence leads to Investigate; loss
  leads to Search; matching footsteps can make the phantom win while the player is visible;
  a player in attack range wins; close examination rejects the phantom and returns to Search.
- Input: injected PC **G** presses opened preview and cast. Mobile aim/drag/release cast
  along the requested vector. Existing mobile layout QA passed 39/39 checks.
- World: Entrance E opened and the decoy crossed it; a tested stair descent moved from
  y=1.81 to y=0.08 without falling; the clone dissolved on player defeat.
- Regression: Shaban behavior 19/19, Void Wall 26/26, Giant Hand Seal 66/66 checks passed.
- Render sample, same Editor view with/without the clone: visible skinned meshes 14→23,
  SetPass calls 47→55, triangles 5,606,320→5,628,413. This is a single Editor frame,
  not a device FPS guarantee. The clone shares the original meshes and caches its nine
  material variants for the entire session; casts do not instantiate another rig.

## Current limits

The cast chooses a horizontal direction. One NavMesh route is planned at launch; the actor
does not dynamically replan around moving vehicles. It dissolves after prolonged blockage
instead of phasing through. The mobile gesture was tested in the Editor's touch simulator;
physical Android device performance and feel should be checked before shipping.
