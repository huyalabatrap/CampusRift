from ar_session import *
out=Path('task/ar/fix1')
print(code('return new {playing=UnityEditor.EditorApplication.isPlaying,compiling=UnityEditor.EditorApplication.isCompiling};'),flush=True)
console(out/'compile-console.json')
print(code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.Floor",0);return true;'),flush=True)
call('read_console',{'action':'clear'})
call('manage_editor',{'action':'play'});background()
time.sleep(2)
print(code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>();c.Continue();c.Continue();UIValidation.SetResolution(1920,1080);return true;'),flush=True)
for i in range(60):
 if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
 time.sleep(1)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=false;return true;')
for yaw in [155,170,180,195,210,180]:
 code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,'+str(yaw)+',0);return true;');time.sleep(.5)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
for i in range(40):
 time.sleep(1)
 if code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().Shrine!=null;'):break
else:raise RuntimeError('placement failed')
code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=f.Root.position+new UnityEngine.Vector3(.55f,1.0f,1.15f);c.transform.LookAt(f.Root.position+UnityEngine.Vector3.up*.10f);return true;')
time.sleep(3)
save(out/'scene.json',code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var list=new System.Collections.Generic.List<string>();foreach(var b in f.GetComponents<UnityEngine.MonoBehaviour>())list.Add(b.GetType().Name);return new {scale=f.Scale,radius=f.placement.Radius,components=list,legacyHUD=f.GetComponent<CampusRift.AR.ARPlacementHUD>()!=null,paused=f.Paused};'))
console(out/'scene-console.json')
print('scene ready',flush=True)
