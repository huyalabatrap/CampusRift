# Void Wall (skill 1): placement

Quick cast in the style of Free Fire's Gloo Wall. The skill never refuses a spot; it fits the wall to the spot.

| Input | Result |
| --- | --- |
| Tap Q (PC) / tap the Void Wall button (mobile) | Wall deploys at once, square to where the camera looks |
| Hold Q past 0.18 s | Aim preview appears; release deploys, wheel moves it near/far, right click cancels |
| Hold + drag (mobile) | Same preview; drag steers and sets distance, release deploys |
| Press again during the 0.35 s cooldown | Queued (0.45 s window) and cast the moment the cooldown ends |

`VoidWallPlacement` fitting, in order:
1. Threat assist: a monster within 14 m and 55 degrees of the aim turns the wall square across its approach, at most halfway to it. Monsters behind you never change the aim.
2. Solid geometry in the way (walls, closed doors, props) pulls the wall in front of it, so it never lands in the next room.
3. Corridor/doorway fit: with side walls on both sides the wall narrows (minimum 1.1 m) and centres; with one side wall it slides flush against it at full width.
4. Ground: sampled under both ends and the middle; the wall sits on the lowest so stairs and slopes leave no gap underneath. With no ground in reach it stays at your height.

The preview ghost glides to the solved spot; the deployed wall always uses the exact solution.

Tests (Play Mode, via MCP): `Tools/void_test.cs` (integration suite, `Artifacts/VoidWall/Validation.json`) and `Tools/void_quickcast_test.cs` (tap/hold/cancel/buffer/fit/threat, `Artifacts/VoidWall/QuickCast.json`). Both force focus-independent input settings while they run.
