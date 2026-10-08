"""Explicit P23 QA fixture launcher with fresh completion and compilation/domain retry."""
from pathlib import Path
import sys,time,json
import unity_mcp as m
m.initialize()
def call(name,args):
 for _ in range(35):
  raw=m.call(name,args);v=raw.get('result',{}).get('structuredContent',raw.get('result',raw))
  if v.get('success') is not False:return v
  if v.get('message') and not any(x in str(v).lower() for x in ['retry','ready','compil','domain','busy']):raise RuntimeError(str(v))
  time.sleep(2)
 raise RuntimeError(str(v))
def code(s):return call('execute_code',{'action':'execute','code':s})
fixtures={'polish':('CampusRift.UI.P23PolishCapture','task/p23/polish-photo-DONE.txt'),'perf':('CampusRift.UI.P23Performance','task/p23/perf-editor/DONE.txt')}
for name in sys.argv[1:]:
 kind,done=fixtures[name];call('manage_editor',{'action':'stop'});code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return "opened";')
 call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});time.sleep(3)
 stamp=time.time();print(code('new GameObject("P23 explicit '+name+'").AddComponent<'+kind+'>();return "started";'),flush=True)
 deadline=time.time()+220;path=Path(done)
 while time.time()<deadline:
  if path.exists() and path.stat().st_mtime>=stamp:break
  time.sleep(1)
 else:raise TimeoutError('No new DONE '+name)
 print(name+': '+path.read_text(encoding='utf-8-sig'),flush=True)
 result=call('read_console',{'action':'get','types':['error'],'count':50,'format':'detailed'});Path('task/p23/'+name+'-console.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
 call('manage_editor',{'action':'stop'})
