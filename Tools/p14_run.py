"""Run one relevant smoke/capture and restore Play settings, never save fixtures."""
from pathlib import Path
import json,time,sys
from p13_cli import call
root=Path('task/p14');job=sys.argv[1]
types={'sky':('SkyBeastPlayTest','Artifacts/SkyBeast/SkyBeast-DONE.txt'),'fire':('FireBreathPlayTest','Artifacts/SkyBeast/FireBreath-DONE.txt'),'capture':('P14Capture','task/p14/capture-DONE.txt'),'dev':('P14DevSmoke','task/p14/dev-DONE.txt')}
typ,done=types[job]
call('manage_editor',{'action':'stop'})
old=call('execute_code',{'action':'execute','code':'var old=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled;UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return old;'})['data']['result']
call('read_console',{'action':'clear'});begin=time.time()
try:
 call('manage_editor',{'action':'play'})
 time.sleep(1.5) # Let the scene/domain finish reloading before injecting the fixture.
 setup='var h=new UnityEngine.GameObject("P14 '+job+'").AddComponent<CampusRift.SkyBeast.'+typ+'>();'
 if job=='capture' and len(sys.argv)>2 and sys.argv[2]=='fury':setup+='h.FuryOnly=true;'
 call('execute_code',{'action':'execute','code':setup+'return "started";'})
 print('START',job,flush=True)
 deadline=time.time()+100
 while time.time()<deadline:
  p=Path(done)
  if p.exists() and p.stat().st_mtime>=begin:break
  time.sleep(.5)
 else:raise RuntimeError('No fresh completion: '+job)
 errors=call('read_console',{'action':'get','types':['error'],'count':40,'format':'detailed'})
 (root/(job+'-console.json')).write_text(json.dumps(errors,indent=2),encoding='utf-8')
 print(p.read_text(encoding='utf-8-sig'),flush=True);print('ERRORS',errors,flush=True)
 (root/(job+'-run.json')).write_text(json.dumps(dict(started=begin,finished=time.time(),done=done,fresh=True,errors=errors),indent=2),encoding='utf-8')
 if job in ('sky','fire'):(root/('SkyBeast.json' if job=='sky' else 'FireBreath.json')).write_bytes(Path('Artifacts/SkyBeast/'+('SkyBeast' if job=='sky' else 'FireBreath')+'.json').read_bytes())
finally:
 call('manage_editor',{'action':'stop'})
 call('execute_code',{'action':'execute','code':'UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(old).lower()+';return "restored";'})
