"""Builds are queued on the Editor main loop so MCP stays responsive for status."""
import pathlib,json,time,sys
import unity_mcp as m
m.initialize();kind=sys.argv[1];development=sys.argv[2]=='dev';folder=pathlib.Path('Artifacts/P17-Builds')/kind/sys.argv[2];folder.mkdir(parents=True,exist_ok=True)
if kind=='Android':
    path=(folder/'CampusRift.apk').as_posix()
    body='CampusRift.BuildTools.AndroidApkBuild.BuildTo("'+path+'",'+str(development).lower()+');'
else:
    path=(folder/'CampusRift.exe').as_posix()
    body='var report=UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions{scenes=System.Array.ConvertAll(UnityEditor.EditorBuildSettings.scenes,x=>x.path),locationPathName="'+path+'",target=UnityEditor.BuildTarget.StandaloneWindows64,options='+( 'UnityEditor.BuildOptions.Development' if development else 'UnityEditor.BuildOptions.None')+'});var s=report.summary;System.IO.File.WriteAllText("'+folder.as_posix()+'/build-summary.txt","Result: "+s.result+"\\nTotal bytes: "+s.totalSize+"\\nDuration: "+s.totalTime+"\\nErrors: "+s.totalErrors+"\\nWarnings: "+s.totalWarnings);if(s.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build result "+s.result);'
code='UnityEditor.EditorApplication.CallbackFunction run=null;run=()=>{UnityEditor.EditorApplication.update-=run;try{'+body+'System.IO.File.WriteAllText("'+folder.as_posix()+'/DONE.txt","Succeeded");}catch(System.Exception e){System.IO.File.WriteAllText("'+folder.as_posix()+'/DONE.txt",e.ToString());}};UnityEditor.EditorApplication.update+=run;return "queued '+kind+' '+sys.argv[2]+'";'
result=m.call('execute_code',{'action':'execute','code':code});(folder/'queue.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=True))
