# Shaban hunter upgrade

Existing architecture audited: Brain, Perception, Memory, Navigation, Search, Prediction,
RoomGraph/A*, Hearing, Combat, Audio, Debug and the movement/sound adapters.

Preserve the single monster, prefab identity, FBX, audio clip, seven states, combat windup,
damage/cooldown, physical door operation and 3D audio/occlusion. New planning uses only
sensory snapshots and static campus topology. Predictions never refresh real evidence.

Baseline backup: `Backups/ShabanHunterUpgrade-20260927`.
Phase reports: `Assets/MonsterShaban/Validation/Hunter`.

Traversal prerequisites: 314 flights, 628 walk and 628 sprint trials passed; 506 doors,
1012 direction trials passed. Parking structure and 150 parked vehicles now have mesh
collision. Camera remains the player's Main Camera.

Implementation order: memory; motion/trail; pursuit; probability search/negative evidence;
graph metadata/cost; interception; utility/hysteresis; landing; route habits; recovery;
debug; measured profiling; full regression.
