from ar_session import *
code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.SetFloor(true);p.enabled=false;return true;')
for yaw in [130,150,170,190,210,230,210,190,170,150,130]:
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(-1.0f,1.5f,1.2f);c.transform.rotation=UnityEngine.Quaternion.Euler(45,'+str(yaw)+',0);return true;')
    time.sleep(1)
code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.rotation=UnityEngine.Quaternion.Euler(40,175,0);UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
for i in range(20):
    time.sleep(1);state=code(Path('task/ar/m2-state.cs').read_text())
    if state['placed']:break
save('task/ar/m2-floor.json',state);print(state,flush=True);assert state['placed']
code('UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/m2-placement-floor.png");return true;');time.sleep(1)
console('task/ar/m2-console.json')
