from b1007_runner import *
for yaw in [155,170,180,195,210,180]:
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new Vector3(.75f,1.5f,.1f);c.transform.rotation=Quaternion.Euler(40,'+str(yaw)+',0);return true;');time.sleep(.55)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
time.sleep(2)
print(code((ROOT/'Tools/b1007_ar_diag.cs').read_text(encoding='utf-8')))
