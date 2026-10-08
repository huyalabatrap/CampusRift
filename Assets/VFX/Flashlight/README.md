# Player flashlight

`PlayerFlashlight` on `Campus Explorer` hands the player a flashlight whenever the sky is dark (Settings > Video > Sky Brightness). It fades in below 0.5 brightness, is fully on at 0.25 and below, and is put away in daylight. `alwaysOn` forces it.

- **Model**: "Torch" by Quaternius, CC0 ([poly.pizza/m/WGsvr4KOZd](https://poly.pizza/m/WGsvr4KOZd)), centred and scaled to 0.24 m and converted GLB -> FBX with Blender (`Model/Flashlight.fbx`, URP Lit materials in `Model/`). The lens material glows while the light is on.
- **Grip**: solved from the right hand's finger bones (knuckle line = flashlight axis, lens on the index side) so it fits any rig with `R_Hand` + finger children. A two-bone IK raises the right arm to hold the light in front of the chest (`handOffset`) and curls the fingers around the handle (`fingerCurl`), blended in with the equip level. Runs before the afterimage/speed-force samplers so they see the same pose.
- **Beam**: shadowed spot light from the lens, 18 m range, 70 degree cone, pitched 8 degrees down, following where the character faces with a little hand sway; `FlashlightCookie.png` gives it a hot centre and reflector ring. A small shadowless fill above/behind the head keeps the character readable to the camera.
- Hidden with the body when the camera pushes in close.

Re-apply with **Campus Rift/VFX/Setup Player Flashlight** (scene player + `CampusExplorer.prefab`).
