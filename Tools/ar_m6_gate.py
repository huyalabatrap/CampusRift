from ar_session import *
Path('task/ar/m6').mkdir(exist_ok=True)
print(code('var r=CampusRift.UI.ComicTextAudit.Scan("camera consent");CampusRift.UI.ComicTextAudit.Save(r,"task/ar/m6/camera-audit.json");return new {issues=r.issues.Count,loader=UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader!=null};'),flush=True)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>().Continue();return true;')
background()
for i in range(60):
 if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
 time.sleep(1)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().SetFloor(false);UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=false;return true;')
for yaw in [155,170,180,195,210,180]:
 code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,'+str(yaw)+',0);return true;');time.sleep(.5)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
for i in range(35):
 time.sleep(1)
 if code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().Shrine!=null;'):break
else:raise RuntimeError('placement failed')
code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=f.Root.position+new UnityEngine.Vector3(.4f,.7f,.8f);c.transform.LookAt(f.Root.position+UnityEngine.Vector3.up*.05f);return true;')
time.sleep(4)
code('var r=CampusRift.UI.ComicTextAudit.Scan("AR HUD");CampusRift.UI.ComicTextAudit.Save(r,"task/ar/m6/hud-initial.json");UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/m6-hud.png");return r.issues.Count;')
console('task/ar/m6/console.json');print('M6 HUD captured',flush=True)
