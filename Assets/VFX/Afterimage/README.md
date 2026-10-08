# Character afterimage trail

Hold Shift while moving in the Game view to leave cyan ghost poses that fade toward violet. The effect snapshots the character's original running animation. It is configured on `Campus Explorer` in `SampleScene` and on `Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab`.

`CharacterAfterimageTrail` uses actual sprint movement, so walking, standing still, or pushing against a wall does not emit new poses. Existing poses finish fading after sprint release. Returning to spawn, teleporting more than 4 m in one frame, or disabling the effect clears the trail. The transparent URP shader respects scene depth, preserves hair/face alpha cutouts, and casts no shadows. Ghosts have no colliders.

## Tuning

Select `Campus Explorer` and edit **Character Afterimage Trail** before entering Play Mode:

| Setting | Default |
| --- | --- |
| Capacity | 6 reusable poses |
| Sample interval | 0.065 seconds |
| Lifetime | 0.36 seconds |
| Minimum spacing | 0.24 m |
| Opacity | 0.48 |
| Colors | Bright cyan to violet |

The nine source meshes exclude the character's duplicate black outline shells. Meshes and materials are pooled; baking a new pose still uses CPU skinning. Sampling happens in LateUpdate after animation evaluation.

## Dash integration

The dash hook triggers VFX only. No dash key or dash movement has been added.

```csharp
// Call when your future dash movement starts; emission requires actual movement.
GetComponent<CampusRift.CharacterAfterimageTrail>().TriggerDash(0.25f);
```

Call `ClearTrail()` when implementing another respawn or teleport mechanism.

## Validation

Verified in Unity Play Mode with the real character controller and original animation: sprint emission and distinct animated poses; walking, stationary Shift and wall-contact suppression; release fade; dash-hook duration without moving the character; teleport/return cleanup; six-pose mesh reuse; source-scale silhouettes and depth occlusion. No shader errors were reported.
