from ar_nohand import *
connect();build=OUT/'build';build.mkdir(exist_ok=True)
assert not (build/'queued.json').exists(),'Build already queued: monitor DONE; do not queue again'
for name in ['ARGestureUnitTests','OpenPalmMock','ARRiftPlayTest']:
 data=json.loads((OUT/'checks'/name/'result.json').read_text(encoding='utf-8'))
 assert not data.get('failed'),(name,data)
 console=json.loads((OUT/'checks'/name/'console.json').read_text(encoding='utf-8'));assert not console.get('data'),(name,console)
stop();code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
state=code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8'));save('build/prebuild-state.json',state)
assert state['platform']=='Android' and not any(state[k] for k in ['playing','compiling','updating','dirty']),state
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save('build/prebuild-console.json',console);assert not console.get('data'),console
apk='APK-Test/CampusRift-20261007-ar-nohand-fix.apk'
src='''PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
PlayerSettings.defaultInterfaceOrientation=UnityEditor.UIOrientation.AutoRotation;
PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
UnityEditor.EditorApplication.CallbackFunction callback=null;
callback=()=>{UnityEditor.EditorApplication.update-=callback;try{CampusRift.BuildTools.AndroidApkBuild.BuildTo("APK_PATH",true);System.IO.File.Copy("APK-Test/build-summary.txt","task/ar/nohand/build/build-summary.txt",true);System.IO.File.WriteAllText("task/ar/nohand/build/DONE.txt","Succeeded");}catch(System.Exception e){System.IO.File.WriteAllText("task/ar/nohand/build/DONE.txt",e.ToString());}};
UnityEditor.EditorApplication.update+=callback;return "Development ARM64 GLES3 landscape build queued";'''.replace('APK_PATH',apk)
save('build/queued.json',{'apk':apk,'utc':time.time()});save('build/queue-result.json',code(src))
milestone('Đã hoàn tất mock duy nhất và 2 suite một lượt mỗi cái; queue đúng một build APK development ARM64/GLES3/landscape APK-Test/CampusRift-20261007-ar-nohand-fix.apk. Theo dõi nohand/build/DONE.txt; không queue lại. Còn APK verify/SHA256, save/settings restoration, handoff/Console và report cuối.')
print('Queued',apk,flush=True)
