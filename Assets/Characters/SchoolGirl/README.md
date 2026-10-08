# School Girl campus explorer

Open `Assets/Scenes/SampleScene.unity`, press Play, and click the Game view.

| Control | Action |
| --- | --- |
| WASD / arrow keys | Move relative to the camera |
| Shift | Run with a fading afterimage trail |
| Space | Jump |
| Mouse | Orbit the camera |
| Mouse wheel | Zoom |
| E near an elevator / inside its cabin | Call elevator / choose destination floor |
| Escape | Release the mouse and stop movement input |
| Left click in Game view | Capture the mouse again |
| R | Return to the courtyard entrance |
| F1 | Toggle the controls overlay |

`Campus Explorer` uses a CharacterController and the project's existing Input System package. Its camera checks scenery colliders to avoid clipping through walls. Walk speed is 3.2 m/s and run speed is 10 m/s, with acceleration of 30 m/s² for a quick response to Shift. The original running animation speeds up with movement. Jump height is approximately 1 m. Falling below the campus returns the player to the entrance.

Sprinting leaves cyan-to-violet frozen poses from the original animation. See `Assets/VFX/Afterimage/README.md` for effect tuning and the callable dash VFX trigger.

Entrance, interior and stair doors open automatically when approached and close after the player leaves. Elevators also respond to proximity; press E inside the cabin to select a floor. See `Assets/AutomaticDoors/README.md` and `Assets/Collision/README.md` for the door and collision setup.

## Original animation

`Models/SchoolGirl.fbx` is a copy of the supplied `source/untitled1.fbx`. It uses its original Generic skeleton and contains one authored clip: `Object_4|CINEMA_4D____Object_4` (18 frames, 24 fps, 0.75 s). The importer loops this clip, and `Animations/SchoolGirlLocomotion.controller` references it directly. Movement is driven by the CharacterController, with root motion disabled.

The original running clip is also played more slowly during walking; the source has no separate walking or idle animation. `Animations/Idle.anim` is a standing pose derived from the rig's bind pose with the arms lowered. Idle and running blend automatically; the run cycle stops when movement is blocked.

## Materials and scale

The original FBX texture links point at temporary directories, so the accompanying textures are copied into `Textures/` and remapped through external URP materials:

| Material | Texture |
| --- | --- |
| body | Image_0 |
| head | Image_1 (color and alpha) |
| biaoqing | Image_3 (color and alpha) |
| hairA | Image_5 |
| hairB | Image_6 (color and alpha) |
| HatAndShoues | Image_8 |
| cloth | Image_9 |
| material | Black outline material |

The visual child is scaled to approximately 1.68 m. The original FBX and the external source files are preserved. Scenery collision uses box colliders for verified box meshes and mesh colliders for irregular floors, stairs, walls and other structures.
