from ar_fix3_local import *
console(out/'initial-placement-console.json')
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>();if(c!=null)c.Shutdown();foreach(var a in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.ARFoundation.ARAnchor>(UnityEngine.FindObjectsSortMode.None))a.enabled=false;var xr=UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager;if(xr.activeLoader!=null){xr.StopSubsystems();xr.DeinitializeLoader();}UnityEditor.EditorApplication.isPlaying=false;return true;')
for i in range(30):
    time.sleep(.2)
    if not code('return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;'):break
clear();code('UnityEditor.AssetDatabase.Refresh();UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();return true;')
print('Stopped and refreshing')
