"""Observe real XR Simulation planes after scanning; never inject plane data."""
from pathlib import Path
import json,time
import unity_mcp as m
m.initialize()
def code(s):
    for attempt in range(20):
        r=m.call('execute_code',{'action':'execute','code':s}).get('result',{}).get('structuredContent',{})
        if r.get('success'):return r['data']['result']
        if not any(k in str(r).lower() for k in ['no_unity_session','not ready','compiling','busy']):raise RuntimeError(str(r))
        time.sleep(2)
    raise RuntimeError(str(r))
code('UnityEngine.Application.runInBackground=true;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;foreach(var d in UnityEngine.InputSystem.InputSystem.devices)UnityEngine.InputSystem.InputSystem.EnableDevice(d);UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return "background input for fixture";')
time.sleep(3)
for i in range(12):
    yaw=105+i*5
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.1f,1.0f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(38,'+str(yaw)+',0);return "scan '+str(i)+'";')
    time.sleep(.4)
time.sleep(3)
result=code(Path('task/ar/m1-state.cs').read_text())
Path('task/ar/m1-simulation.json').write_text(json.dumps(result,indent=2))
assert result['ready'] and result['planes']>0 and result['session']=='SessionTracking' and not result['shaderError'],result
code('UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/m1-xrsim-planes.png");return "capture";')
time.sleep(1)
r=m.call('read_console',{'action':'get','types':['error'],'count':100,'format':'detailed'})
Path('task/ar/m1-simulation-console.json').write_text(json.dumps(r,indent=2))
print(json.dumps(result),flush=True)
