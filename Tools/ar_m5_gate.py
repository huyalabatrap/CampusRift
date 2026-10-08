from ar_session import *
print(code(Path('task/ar/m5-install.cs').read_text()),flush=True)
code('UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.Floor",0);UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;return true;')
call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});background()
for attempt in range(90):
    if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
    time.sleep(1)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=false;return true;')
for yaw in [155,170,180,195,210,180]:
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,'+str(yaw)+',0);return true;');time.sleep(.5)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
for i in range(35):
    time.sleep(1)
    if code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();return f.Shrine!=null;'):break
else:raise RuntimeError('Placement failed')
code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);return true;')
time.sleep(3)
print(code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=f.Root.position+new UnityEngine.Vector3(.4f,.7f,.8f);c.transform.LookAt(f.Root.position+UnityEngine.Vector3.up*.05f);return new {caster=f.GetComponent<CampusRift.AR.ARSkillCaster>().Caster!=null};'),flush=True)
console('task/ar/m5/preflight-console.json')
code('new UnityEngine.GameObject("AR smoke harness").AddComponent<CampusRift.AR.ARRiftPlayTest>();return true;')
for i in range(180):
    if Path('task/ar/m5/DONE.txt').exists():break
    time.sleep(1)
else:raise RuntimeError('AR harness did not finish')
console('task/ar/m5/console.json')
print(Path('task/ar/m5/DONE.txt').read_text(),flush=True)
