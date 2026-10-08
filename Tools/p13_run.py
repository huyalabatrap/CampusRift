"""Single relevant smoke/capture, fresh Play session with durable outputs."""
import time,json,sys
from pathlib import Path
from p13_cli import call
name=sys.argv[1];root=Path('task/p13');root.mkdir(exist_ok=True)
jobs={
 'fire':('CampusRift.SkyBeast.FireBreathPlayTest','Artifacts/SkyBeast/FireBreath-DONE.txt'),
 'layer':('CampusRift.SkyBeast.P13LayerSmoke','Artifacts/SkyBeast/LayerSmoke-DONE.txt'),
 'capture':('CampusRift.SkyBeast.P13Capture','task/p13/capture-DONE.txt'),
 'window':('CampusRift.SkyBeast.P13Capture','task/p13/window-capture-DONE.txt'),
 'look':('CampusRift.SkyBeast.P13LookCapture','task/p13/look-capture-DONE.txt')}
typ,done=jobs[name]
call('manage_editor',{'action':'stop'})
original=call('execute_code',{'action':'execute','code':'bool old=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled; UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false; UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); return old;'})
(root/'editor-reload-original.json').write_text(json.dumps(original),encoding='utf-8')
call('read_console',{'action':'clear'})
start=time.time();call('manage_editor',{'action':'play'});time.sleep(1)
setup='var harness=new UnityEngine.GameObject("P13 '+name+'").AddComponent<'+typ+'>(); '
if name=='window':setup+='harness.WindowOnly=true; '
call('execute_code',{'action':'execute','code':setup+'return "started";'})
print('START',name,flush=True)
deadline=time.time()+90
while time.time()<deadline:
    p=Path(done)
    if p.exists() and p.stat().st_mtime>=start:break
    time.sleep(.5)
else:
    call('manage_editor',{'action':'stop'})
    call('execute_code',{'action':'execute','code':'UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true; return "restored original reload-disabled setting";'})
    raise RuntimeError('No fresh completion: '+name)
errors=call('read_console',{'action':'get','types':['error'],'count':30,'format':'detailed'})
(root/(name+'-console.json')).write_text(json.dumps(errors,indent=2),encoding='utf-8')
print(p.read_text(),flush=True);print('ERRORS',errors.get('data'),flush=True)
call('manage_editor',{'action':'stop'})
call('execute_code',{'action':'execute','code':'UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true; return "restored original reload-disabled setting";'})
(root/(name+'-run.json')).write_text(json.dumps({'started':start,'finished':time.time(),'done':done,'fresh':True,'errors':errors},indent=2),encoding='utf-8')
