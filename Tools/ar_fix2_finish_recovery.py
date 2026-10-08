from ar_fix2_local import *
save(out/'recovery-pose.json',code(Path('task/ar/fix2-recover-pose.cs').read_text(encoding='utf-8')))
script=Path('Tools/ar_fix2_scene.py').read_text(encoding='utf-8')
script=script[script.index('for i in range(45):'):]
script=script.replace("'runtime-wiring.json'","'recovered-wiring-initial.json'").replace("'scene-console.json'","'recovered-scene-console.json'")
# Match the game camera to the simulation view used to collect real planes/anchors.
script=script.replace("c.transform.LookAt(f.Root.position+UnityEngine.Vector3.up*.10f);return true;", "c.transform.LookAt(f.Root.position+UnityEngine.Vector3.up*.10f);f.placement.view.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);return true;")
exec(script)
