from ar_fix3_local import *
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.HelpSeen",1);UIValidation.SetResolution(1600,720);var settings=UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/XR/Resources/XRSimulationRuntimeSettings.asset");var so=new UnityEditor.SerializedObject(settings);so.FindProperty("m_EnvironmentScanParams.m_RaysPerCast").intValue=256;so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.ClearDirty(settings);return true;')
clear();code('UnityEditor.EditorApplication.isPlaying=true;return true;');background()
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>();c.Continue();c.Continue();return true;')
for i in range(30):
    if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
    time.sleep(.2)
else:raise RuntimeError('no provider')
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().InputBlocked=true;return true;')
for yaw in [155,170,180,195,210,180]:
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,'+str(yaw)+',0);return true;')
    time.sleep(.2)
for i in range(40):
    if code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().ReticleValid;'):break
    time.sleep(.2)
else:raise RuntimeError('no valid reticle')
code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.InputBlocked=false;p.Confirm();return true;')
for i in range(40):
    if code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().Root!=null;'):break
    time.sleep(.2)
else:raise RuntimeError('no anchor')
time.sleep(.5)
hidden=code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();int count=0,visible=0,enabled=0;foreach(var plane in p.planes.trackables){count++;foreach(var r in plane.GetComponentsInChildren<UnityEngine.Renderer>(true))if(r.enabled)visible++;foreach(var v in plane.GetComponentsInChildren<UnityEngine.XR.ARFoundation.ARPlaneMeshVisualizer>(true))if(v.enabled)enabled++;}return new {count,visible,visualizersEnabled=enabled,managerEnabled=p.planes.enabled,p.AnchorMethod};')
assert hidden['count']>0 and hidden['visible']==0 and hidden['visualizersEnabled']==0 and not hidden['managerEnabled'],hidden
save(out/'plane-visual-hidden.json',hidden)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().StartBattlefield();var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);return true;')
errors=console(out/'visual-fix-console.json');assert errors['data']==[],errors
print(hidden,flush=True)
