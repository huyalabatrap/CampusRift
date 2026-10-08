from ar_session import *
if '--resume' not in __import__('sys').argv: code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.Floor",0);return true;')
call('read_console',{'action':'clear'})
if '--resume' not in __import__('sys').argv: call('manage_editor',{'action':'play'})
time.sleep(4);background()
for attempt in range(60):
    if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
    time.sleep(2)
else:raise RuntimeError('Simulation camera did not start')
for i in range(12):
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.1f,1,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(38,'+str(105+i*5)+',0);return true;')
    time.sleep(.4)
time.sleep(4)
state=code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();var rows=new System.Collections.Generic.List<object>();foreach(var q in p.planes.trackables)rows.Add(new {id=q.trackableId.ToString(),center=q.transform.TransformPoint(q.center),size=q.size,area=CampusRift.AR.ARPlaneScoring.Area(q.boundary.ToArray()),tracking=q.trackingState.ToString()});return new {placed=p.Root!=null,message=p.Message,planes=rows};')
save('task/ar/m2-observed.json',state);print(json.dumps(state),flush=True)
