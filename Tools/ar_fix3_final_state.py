from ar_fix3_local import *
script=Path('task/ar/fix2-final-state.cs').read_text(encoding='utf-8').replace('orientation=UnityEditor.PlayerSettings', 'settings=UnityEngine.PlayerPrefs.GetString("CampusRift.Settings.v1"),tutorial=UnityEngine.PlayerPrefs.GetInt("CampusRift.AR.HelpSeen",0),simulationRays=new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/XR/Resources/XRSimulationRuntimeSettings.asset")).FindProperty("m_EnvironmentScanParams.m_RaysPerCast").intValue,\norientation=UnityEditor.PlayerSettings')
state=code(script);save(out/'final-state.json',state)
errors=console(out/'final-console.json')
assert not any(state[k] for k in ['playing','compiling','updating','building','dirty','androidStartup','editorStartup','androidAutomatic','editorAutomatic','runeShaderError','shadowShaderError','planeShaderError','portrait','portraitUpsideDown','automaticGraphics','preTransform']),state
assert state['target']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and state['loaderInactive'],state
assert state['graphics']==[11] and state['landscapeLeft'] and state['landscapeRight'] and state['orientation']=='AutoRotation',state
assert errors['data']==[],errors
before=json.loads((out/'settings-before.json').read_text(encoding='utf-8-sig'))
assert all(state[k]==before[k] for k in ['enterPlay','enterEnabled','dev','inputBackground','inputEditor','floor','safety','tutorial']),state
assert state['simulationRays']==10,state
assert state['settings']==before['settings'],state
print(json.dumps(state,ensure_ascii=False,indent=2),flush=True)
