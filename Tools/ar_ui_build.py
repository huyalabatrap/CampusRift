from ar_ui import *
connect();build=OUT/'build';build.mkdir(exist_ok=True)
assert not (build/'queued.json').exists(),'Build already queued; monitor DONE'
assert not json.loads((OUT/'layout-summary.json').read_text(encoding='utf-8'))['issues']
stop();code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
state=code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8'));save('build/prebuild-state.json',state)
assert state['platform']=='Android' and not any(state[k] for k in ['playing','compiling','updating','dirty']),state
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save('build/prebuild-console.json',console);assert not console.get('data'),console
apk='APK-Test/CampusRift-20261007-ar-ui-fix.apk'
src='''PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
PlayerSettings.defaultInterfaceOrientation=UnityEditor.UIOrientation.AutoRotation;
PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
UnityEditor.EditorApplication.CallbackFunction callback=null;
callback=()=>{UnityEditor.EditorApplication.update-=callback;try{CampusRift.BuildTools.AndroidApkBuild.BuildTo("APK_PATH",true);System.IO.File.Copy("APK-Test/build-summary.txt","task/ar/ui-fix/build/build-summary.txt",true);System.IO.File.WriteAllText("task/ar/ui-fix/build/DONE.txt","Succeeded");}catch(System.Exception e){System.IO.File.WriteAllText("task/ar/ui-fix/build/DONE.txt",e.ToString());}};
UnityEditor.EditorApplication.update+=callback;return "Development ARM64 GLES3 landscape build queued";'''.replace('APK_PATH',apk)
save('build/queued.json',{'apk':apk,'utc':time.time()});save('build/queue-result.json',code(src))
milestone('Ảnh nguồn cuối đã duyệt và layout-summary không có issue; queue APK development ARM64/GLES3/landscape APK-Test/CampusRift-20261007-ar-ui-fix.apk. Không chạy hồi quy. Theo dõi ui-fix/build/DONE.txt, không queue lại; còn verify modelSTORED/manifest/Physics/SHA256, adb install-r và pm list packages, phục hồi save/settings và final report.')
print('Queued',apk,flush=True)
