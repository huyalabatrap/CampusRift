from ar_fix2_local import *
# Continue the already-running fixture after a transient file-sharing error;
# do not repeat setup, overwrite the original settings snapshot, or restart XR.
for yaw in [180,195,210,180]:
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,'+str(yaw)+',0);return true;');time.sleep(.5)
resume="code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')"
exec(resume+Path('Tools/ar_fix2_scene.py').read_text(encoding='utf-8').split(resume,1)[1])
