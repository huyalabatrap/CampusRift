from ar_session import *
import sys
if '--resume' not in sys.argv:
    print(code(Path('task/ar/m2-install.cs').read_text(encoding='utf-8')),flush=True)
    code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.Floor",0);return true;')
    call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'})
background()
for attempt in range(90):
    if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
    time.sleep(1)
else:raise RuntimeError('Simulation did not start')
for floor in [False,True]:
    name='floor' if floor else 'table'
    code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.SetFloor('+str(floor).lower()+');p.enabled=false;return true;')
    pos='-1.2f,1.4f,1.2f' if floor else '.75f,1.5f,.1f'
    yaw=135 if floor else 180
    for offset in [-35,-15,0,15,35,15,0]:
        code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3('+pos+');c.transform.rotation=UnityEngine.Quaternion.Euler(40,'+str(yaw+offset)+',0);return true;')
        time.sleep(.6)
    code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
    state=None
    for i in range(15):
        time.sleep(1);state=code(Path('task/ar/m2-state.cs').read_text())
        if state['placed']:break
    save('task/ar/m2-'+name+'.json',state);print(name,json.dumps(state),flush=True)
    assert state['placed'],state
    code('UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/m2-placement-'+name+'.png");return true;');time.sleep(1)
console('task/ar/m2-console.json')
