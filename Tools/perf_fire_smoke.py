"""One existing FireBreath smoke; preserve the earlier P13 reports and restore reload settings."""
import json,time
from pathlib import Path
from p13_cli import call
root=Path('task/perf')
def code(s):return call('execute_code',{'action':'execute','code':s})
call('manage_editor',{'action':'stop'})
old=code('var old=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled;UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return old;')['data']['result']
call('read_console',{'action':'clear'})
begin=time.time()
try:
 call('manage_editor',{'action':'play'});time.sleep(1)
 code('new UnityEngine.GameObject("PERF-FIRE smoke").AddComponent<CampusRift.SkyBeast.FireBreathPlayTest>();return "started";')
 print('START FireBreath smoke',flush=True)
 done=Path('Artifacts/SkyBeast/FireBreath-DONE.txt');deadline=time.time()+90
 while time.time()<deadline:
  if done.exists() and done.stat().st_mtime>=begin:break
  time.sleep(.5)
 else:raise RuntimeError('No fresh FireBreath completion')
 data=json.loads(Path('Artifacts/SkyBeast/FireBreath.json').read_text(encoding='utf-8-sig'))
 (root/'FireBreath.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 errors=call('read_console',{'action':'get','types':['error'],'count':30,'format':'detailed'})
 (root/'smoke-console.json').write_text(json.dumps(errors,indent=2),encoding='utf-8')
 print(done.read_text(),flush=True);print(errors,flush=True)
 assert data['passed']==42 and data['failed']==0
 assert errors.get('data')==[]
 (root/'smoke-run.json').write_text(json.dumps({'start':begin,'end':time.time(),'fresh':True,'target':'Android','passed':42,'failed':0,'consoleErrors':0},indent=2))
finally:
 call('manage_editor',{'action':'stop'})
 code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(old).lower()+';return "restored original reload setting";')
