from ar_fix3_local import *
code('UnityEditor.AssetDatabase.Refresh();UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();return true;')
console(out/'compile-final-fixture.json')
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.HelpSeen",1);UIValidation.SetResolution(1600,720);var settings=UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/XR/Resources/XRSimulationRuntimeSettings.asset");var so=new UnityEditor.SerializedObject(settings);so.FindProperty("m_EnvironmentScanParams.m_RaysPerCast").intValue=256;so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.ClearDirty(settings);return true;')
clear();code('UnityEditor.EditorApplication.isPlaying=true;return true;');time.sleep(.3);background()
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>();c.Continue();c.Continue();return true;')
for i in range(30):
    if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
    time.sleep(.2)
code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,180,0);return true;')
for i in range(30):
    time.sleep(.2)
    if code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().Shrine!=null;'):break
else:raise RuntimeError('No battlefield')
code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);return true;')
console(out/'final-fixture-console.json')
print('Final fixture ready')
