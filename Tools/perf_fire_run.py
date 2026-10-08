"""Short resumable PERF-FIRE Editor capture; keeps state evidence and restores settings."""
import sys,time,json
from pathlib import Path
from p13_cli import call
label=sys.argv[1]
root=Path('task/perf')
def code(s):return call('execute_code',{'action':'execute','code':s})
def save(name,data): (root/name).write_text(json.dumps(data,indent=2),encoding='utf-8')
call('manage_editor',{'action':'stop'})
code('foreach(var w in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>()){if(w.GetType().Name=="FrameDebuggerWindow"||w.GetType().Name=="ProfilerWindow")w.Close();}return "closed diagnostic windows for consistent FPS sampling";')
old=code('var old=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled; UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false; UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); return old;')['data']['result']
save(label+'-reload-original.json',old)
call('read_console',{'action':'clear'})
begin=time.time()
try:
 call('manage_editor',{'action':'play'})
 time.sleep(1)
 is_before=label=='before'
 code('var h=new UnityEngine.GameObject("PERF-FIRE capture").AddComponent<CampusRift.SkyBeast.FirePerformanceCapture>(); h.Label="'+label+'";h.Diagnostics='+str(is_before).lower()+';h.OtherEffects='+str(is_before or label in ('before-final','before-pc')).lower()+';h.DragonOnly='+str(len(sys.argv)>2 and sys.argv[2]=='dragon').lower()+';h.EnemiesOnly='+str(len(sys.argv)>2 and sys.argv[2]=='enemies').lower()+';return "started";')
 print('START',label,flush=True)
 done=root/(label+'-DONE.txt')
 until=time.time()+210
 last=0
 while time.time()<until:
  if done.exists() and done.stat().st_mtime>=begin:break
  if time.time()-last>20:
   p=root/(label+'.json')
   print('SAMPLES',len(json.loads(p.read_text())['samples']) if p.exists() else 0,flush=True);last=time.time()
  time.sleep(1)
 else: raise RuntimeError('No fresh completion')
 print(done.read_text(),flush=True)
 save(label+'-console.json',call('read_console',{'action':'get','types':['error'],'count':30,'format':'detailed'}))
 call('manage_editor',{'action':'pause'})
 save(label+'-frame-debugger-enable.json',call('manage_profiler',{'action':'frame_debugger_enable'}))
 time.sleep(1)
 events=[];cursor=0
 while True:
  r=call('manage_profiler',{'action':'frame_debugger_get_events','page_size':100,'cursor':cursor})
  events+=r.get('data',{}).get('events',[])
  if 'next_cursor' not in r.get('data',{}):break
  cursor=r['data']['next_cursor']
 save(label+'-frame-debugger.json',events)
 print('FRAME EVENTS',len(events),flush=True)
finally:
 try:call('manage_profiler',{'action':'frame_debugger_disable'})
 except Exception:pass
 call('manage_editor',{'action':'stop'})
 code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(old).lower()+'; return "restored";')
