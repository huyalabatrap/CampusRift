from ar_fix2_local import *
import shutil
save(out/'settings-before.json',code('return new { settings=UnityEngine.PlayerPrefs.GetString("CampusRift.Settings.v1"), floor=UnityEngine.PlayerPrefs.GetInt("CampusRift.AR.Floor",0), safety=UnityEngine.PlayerPrefs.GetInt("CampusRift.AR.Safety",0), enterPlay=(int)UnityEditor.EditorSettings.enterPlayModeOptions, enterEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled, dev=UnityEditor.EditorUserBuildSettings.development,inputBackground=(int)UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior,inputEditor=(int)UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode };'))
save(out/'configuration.json',code(Path('task/ar/fix2-config.cs').read_text()))
console(out/'compile-console.json')
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.Floor",0);UIValidation.SetResolution(2400,1080);return true;')
clear();code('UnityEditor.EditorApplication.isPlaying=true;return true;');time.sleep(3);background()
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>();c.Continue();c.Continue();return true;')
for i in range(60):
    if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
    time.sleep(1)
else:raise RuntimeError('Simulation provider not ready')
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=false;return true;')
for yaw in [155,170,180,195,210,180]:
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,'+str(yaw)+',0);return true;');time.sleep(.5)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
for i in range(45):
    time.sleep(1)
    if code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().Shrine!=null;'):break
else:raise RuntimeError('placement failed')
code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=f.Root.position+new UnityEngine.Vector3(.55f,1.0f,1.15f);c.transform.LookAt(f.Root.position+UnityEngine.Vector3.up*.10f);return true;')
time.sleep(3)
save(out/'runtime-wiring.json',code('var loader=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>();var bg=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.ARFoundation.ARCameraBackground>();var cam=bg.GetComponent<UnityEngine.Camera>();var data=cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();var pipeline=loader.SessionPipeline;var features=new System.Collections.Generic.List<string>();foreach(var f in pipeline.rendererDataList[0].rendererFeatures)features.Add(f.GetType().Name+":"+f.isActive);var canvases=new System.Collections.Generic.List<object>();foreach(var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None))if(c.name.StartsWith("AR"))canvases.Add(new {c.name,c.renderMode,active=c.gameObject.activeInHierarchy,rect=((UnityEngine.RectTransform)c.transform).rect});return new {loader.Ready,background=bg.enabled,backgroundRendering=bg.backgroundRenderingEnabled,sessionContains=System.Array.IndexOf(loader.sessionComponents,bg)>=0,renderer=pipeline.rendererDataList[0].name,actualRenderer=data.scriptableRenderer.GetType().Name,features,cam.rect,cam.pixelRect,cam.clearFlags,cam.backgroundColor,renderScale=pipeline.renderScale,canvases};'))
console(out/'scene-console.json')
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — sửa + compile / fixture\n- Đã khóa Player landscape-only AutoRotation, GLES3-only/manual và pre-transform=false; editor MobileControlSetup/ARRiftSetup dùng cùng cấu hình. AR Awake khóa LandscapeLeft, chờ surface ngang trước InitializeLoader, khôi phục hướng sau Shutdown. Camera full rect + renderer0 sau đổi pipeline.\n- Canvas Overlay toàn màn hình; ARScreenLayout fit nội dung1920×1080 vào Screen.safeArea; backdrop consent stretch toàn Canvas và tắt hẳn sau Accept. Đã thêm development-only ARDeviceDiagnostics: góc trên phải triple-tap, log [ARDiag] mỗi2s kể cả overlay ẩn.\n- Compile và XR Simulation fixture đạt; evidence configuration/runtime-wiring/scene-console trong fix2. Chưa chạy harness/build.\n')
print('AR fixture ready',flush=True)
