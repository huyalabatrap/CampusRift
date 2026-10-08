from pathlib import Path
import time,json,sys
import unity_mcp as m
m.initialize()
def call(name,args):
 for attempt in range(35):
  raw=m.call(name,args);v=raw.get('result',{}).get('structuredContent',raw.get('result',raw))
  if v.get('success') is not False:return v
  if v.get('message') and not any(s in str(v).lower() for s in ['not ready','please retry','compil','domain reload','busy']):raise RuntimeError(json.dumps(v))
  time.sleep(2)
 raise RuntimeError(str(v))
typ,done=sys.argv[1:3];scene=sys.argv[3] if len(sys.argv)>3 else 'SampleScene';timeout=int(sys.argv[4]) if len(sys.argv)>4 else 300
def code(s):return call('execute_code',dict(action='execute',code=s))
call('manage_editor',dict(action='stop'));code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/'+scene+'.unity");return "ready";');call('read_console',dict(action='clear'));call('manage_editor',dict(action='play'));time.sleep(2)
setup='Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.Fullscreen=false;s.ResolutionWidth=1920;s.ResolutionHeight=1080;s.TextSize=0;s.AccessibleColors=CampusRift.UI.AccessiblePalette.Default;CampusRift.UI.SettingsManager.Instance.Apply(s,false);UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);'
start=time.time();create='var harness=new GameObject("P23 smoke").AddComponent<'+typ+'>();';
if len(sys.argv)>5:create+='harness.'+sys.argv[5]+';'
print(code(setup+create+'return "started";'),flush=True)
path=Path(done)
while time.time()-start<timeout:
 if path.exists() and path.stat().st_mtime>=start:
  print(path.read_text(encoding='utf-8-sig'),flush=True);break
 time.sleep(1)
else:print('TIMEOUT '+done,flush=True)
console=call('read_console',dict(action='get',types=['error','warning'],count=100,format='detailed'))
Path('task/p23/'+typ.split('.')[-1]+'-console.json').write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
call('manage_editor',dict(action='stop'))
