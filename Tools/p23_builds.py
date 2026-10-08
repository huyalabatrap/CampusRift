"""Queue one P23 development/release build. Run only after regression review."""
from pathlib import Path
import json,time,sys
import unity_mcp as m
m.initialize();kind,variant=sys.argv[1:3];development=variant=='dev';folder=Path('Releases/2026-10-04-v1.0')/kind/variant;folder.mkdir(parents=True,exist_ok=True)
def call(name,args):
 for attempt in range(35):
  raw=m.call(name,args);v=raw.get('result',{}).get('structuredContent',raw.get('result',raw))
  if v.get('success') is not False:return v
  if v.get('message') and not any(x in str(v).lower() for x in ['not ready','retry','compil','domain','busy']):raise RuntimeError(json.dumps(v))
  time.sleep(2)
 raise RuntimeError(str(v))
def code(s):return call('execute_code',{'action':'execute','code':s})
call('manage_editor',{'action':'stop'})
platform='Android' if kind=='Android' else 'StandaloneWindows64'
# Development player includes only explicitly flagged, guarded measurement fixtures.
defines='P10_BENCH' if kind=='Windows' and development else ''
setup='UnityEditor.PlayerSettings.bundleVersion="1.0.0";UnityEditor.PlayerSettings.Android.bundleVersionCode=2;UnityEditor.PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone,"'+defines+'");UnityEditor.EditorUserBuildSettings.development='+str(development).lower()+';UnityEditor.AssetDatabase.SaveAssets();return UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(UnityEditor.BuildTargetGroup.'+('Android' if kind=='Android' else 'Standalone')+',UnityEditor.BuildTarget.'+platform+');'
print(code(setup),flush=True)
if kind=='Android':body='CampusRift.BuildTools.AndroidApkBuild.BuildTo("'+(folder/'CampusRift.apk').as_posix()+'",'+str(development).lower()+');'
else:
 body='var r=UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions{scenes=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(UnityEditor.EditorBuildSettings.scenes,x=>x.enabled),x=>x.path)),locationPathName="'+(folder/'CampusRift.exe').as_posix()+'",target=UnityEditor.BuildTarget.StandaloneWindows64,options='+( 'UnityEditor.BuildOptions.Development' if development else 'UnityEditor.BuildOptions.None')+'});var s=r.summary;System.IO.File.WriteAllText("'+(folder/'build-summary.txt').as_posix()+'","Result: "+s.result+"\\nTotal bytes: "+s.totalSize+"\\nDuration: "+s.totalTime+"\\nErrors: "+s.totalErrors+"\\nWarnings: "+s.totalWarnings);if(s.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build "+s.result);'
queue='UnityEditor.EditorApplication.CallbackFunction run=null;run=()=>{UnityEditor.EditorApplication.update-=run;try{'+body+'System.IO.File.WriteAllText("'+(folder/'DONE.txt').as_posix()+'","Succeeded");}catch(System.Exception e){System.IO.File.WriteAllText("'+(folder/'DONE.txt').as_posix()+'",e.ToString());}};UnityEditor.EditorApplication.update+=run;return "queued";'
result=code(queue);(folder/'queue.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(result,flush=True)
