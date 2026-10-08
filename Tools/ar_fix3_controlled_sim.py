from ar_fix3_local import *
import shutil
assert not (out/'placement-DONE.txt').exists(),'Completed measurement exists'
original=Path('Assets/XR/Resources/XRSimulationRuntimeSettings.asset');shutil.copy2(original,out/'simulation-settings-original.asset')
if (out/'placement-timing.json').exists():shutil.copy2(out/'placement-timing.json',out/'placement-sparse-failed.json');(out/'placement-timing.json').unlink()
code('UnityEditor.AssetDatabase.Refresh();UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();return true;')
console(out/'compile-controlled.json')
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.HelpSeen",1);UIValidation.SetResolution(1600,720);var settings=UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/XR/Resources/XRSimulationRuntimeSettings.asset");var so=new UnityEditor.SerializedObject(settings);so.FindProperty("m_EnvironmentScanParams.m_RaysPerCast").intValue=256;so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.ClearDirty(settings);new UnityEngine.GameObject("Fix3 controlled scan").AddComponent<CampusRift.AR.ARFix3PlacementSmoke>();return true;')
clear();code('UnityEditor.EditorApplication.isPlaying=true;return true;');background()
for i in range(40):
    time.sleep(1)
    if (out/'placement-timing.json').exists():
        data=json.loads((out/'placement-timing.json').read_text(encoding='utf-8-sig'))
        if 'failed' in data:raise RuntimeError(str(data))
    if (out/'placement-DONE.txt').exists():break
else:raise TimeoutError('controlled simulation')
console(out/'placement-console.json')
progress('A controlled simulation\n- XR Simulation dùng256rays/scan trong fixture (gốc10rays đã thiếu patch; evidence giữ). Plane/anchor vẫn provider thật, không fake. Thời gian và loại anchor xem placement-timing.json. Tiếp drift/HUD screenshots + audit1lượt.')
print((out/'placement-timing.json').read_text(encoding='utf-8'))
