from b1007_runner import *
build=OUT.parent/'build';build.mkdir(exist_ok=True)
if (build/'queued.json').exists():raise RuntimeError('Build already queued; inspect DONE.txt before any additional build')
stop();code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
state=value(code('return new{playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating,building=BuildPipeline.isBuildingPlayer,target=EditorUserBuildSettings.activeBuildTarget.ToString(),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,development=EditorUserBuildSettings.development,buildAppBundle=EditorUserBuildSettings.buildAppBundle,exportAsGoogleAndroidProject=EditorUserBuildSettings.exportAsGoogleAndroidProject};'))
save(build/'prebuild-state.json',state)
assert state['target']=='Android' and not any(state[k] for k in ['playing','compiling','updating','building','dirty']),state
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save(build/'prebuild-console.json',console);assert not console.get('data'),console
apk='APK-Test/CampusRift-20261007-batch1007-dev.apk'
src='''PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
PlayerSettings.defaultInterfaceOrientation=UnityEditor.UIOrientation.AutoRotation;
PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
System.IO.Directory.CreateDirectory("task/batch-1007/build");
UnityEditor.EditorApplication.CallbackFunction callback=null;
callback=()=>{UnityEditor.EditorApplication.update-=callback;try{CampusRift.BuildTools.AndroidApkBuild.BuildTo("APK_PATH",true);System.IO.File.Copy("APK-Test/build-summary.txt","task/batch-1007/build/build-summary.txt",true);System.IO.File.WriteAllText("task/batch-1007/build/DONE.txt","Succeeded");}catch(System.Exception e){System.IO.File.WriteAllText("task/batch-1007/build/DONE.txt",e.ToString());}};
UnityEditor.EditorApplication.update+=callback;return "Development ARM64 GLES3 landscape build queued";'''.replace('APK_PATH',apk)
save(build/'queued.json',dict(apk=apk,utc=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())))
print(code(src),flush=True);progress('Mốc build: đã queue một APK development ARM64/GLES3/landscape '+apk+'. Theo dõi build/DONE.txt, không queue lại khi chưa biết kết quả.')
