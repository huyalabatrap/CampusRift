"""One fix1 capture or relevant smoke; restore Editor options and retain fresh evidence."""
from pathlib import Path
import json,time,sys
from p13_cli import call

root=Path('task/perf');job=sys.argv[1]
types={'capture':('FirePerformanceCapture','task/perf/after-fix1-DONE.txt'),
       'fire':('FireBreathPlayTest','Artifacts/SkyBeast/FireBreath-DONE.txt'),
       'sky':('SkyBeastPlayTest','Artifacts/SkyBeast/SkyBeast-DONE.txt')}
typ,done=types[job]
def code(s):return call('execute_code',{'action':'execute','code':s})
def save(name,data):(root/name).write_text(json.dumps(data,indent=2),encoding='utf-8')

call('manage_editor',{'action':'stop'})
old=code('var old=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled;UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return old;')['data']['result']
save('fix1-'+job+'-reload-original.json',old)
call('read_console',{'action':'clear'});begin=time.time()
try:
 call('manage_editor',{'action':'play'});time.sleep(1.5)
 setup='var h=new UnityEngine.GameObject("PERF-FIRE fix1 '+job+'").AddComponent<CampusRift.SkyBeast.'+typ+'>();'
 if job=='capture':setup+='h.Label="after-fix1";h.QuickFix1=true;h.Diagnostics=false;h.OtherEffects=false;h.HoldForDebugger=true;'
 code(setup+'return "started";')
 print('START',job,flush=True)
 deadline=time.time()+100
 while time.time()<deadline:
  p=Path(done)
  if p.exists() and p.stat().st_mtime>=begin:break
  time.sleep(.5)
 else:raise RuntimeError('No fresh completion: '+job)
 errors=call('read_console',{'action':'get','types':['error'],'count':40,'format':'detailed'})
 save('fix1-'+job+'-console.json',errors)
 print(p.read_text(encoding='utf-8-sig'),flush=True);print('ERRORS',errors,flush=True)
 save('fix1-'+job+'-run.json',dict(started=begin,finished=time.time(),done=done,fresh=True,errors=errors))
 if job in ('sky','fire'):
  name='SkyBeast' if job=='sky' else 'FireBreath'
  (root/('fix1-'+name+'.json')).write_bytes(Path('Artifacts/SkyBeast/'+name+'.json').read_bytes())
 else:
  evidence=code('var b=UnityEngine.Object.FindAnyObjectByType<CampusRift.SkyBeast.FireMeteorBatch>();var m=b.GetComponent<UnityEngine.MeshFilter>().sharedMesh;return new {target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(),quality=UnityEngine.QualitySettings.GetQualityLevel(),pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,comicInk=CampusRift.UI.ComicRendering.Enabled,renderers=b.GetComponents<UnityEngine.Renderer>().Length,vertices=m.vertexCount,triangles=m.triangles.Length/3,live=UnityEngine.Object.FindAnyObjectByType<CampusRift.SkyBeast.FireBreathVisuals>().LiveMeteors};')
  save('fix1-batch-evidence.json',evidence)
finally:
 call('manage_editor',{'action':'stop'})
 code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(old).lower()+';return "restored";')
