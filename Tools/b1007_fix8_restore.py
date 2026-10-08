from b1007_fix8 import *

snap=json.loads((FIX.parent/'8-original-editor.json').read_text(encoding='utf-8'))['data']['result']
stop();code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
destination=Path(snap['savePath'])
save_root=BACKUP/'save'/destination.name
assert save_root.is_dir(),save_root
rows=[]
for source in save_root.rglob('*'):
    if not source.is_file():continue
    relative=source.relative_to(save_root);target=destination/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
    rows.append(dict(path=relative.as_posix(),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),matchesBackup=target.read_bytes()==source.read_bytes()))
changed=[]
for source in (BACKUP/'ProjectSettings').rglob('*'):
    if not source.is_file():continue
    target=ROOT/'ProjectSettings'/source.relative_to(BACKUP/'ProjectSettings')
    if target.read_bytes()!=source.read_bytes():changed.append(target.relative_to(ROOT).as_posix());shutil.copy2(source,target)
statements=[]
build_settings=json.loads((FIX/'build/prebuild-state.json').read_text(encoding='utf-8'))
for key in ['development','buildAppBundle','exportAsGoogleAndroidProject']:
    statements.append('EditorUserBuildSettings.'+key+'='+str(build_settings[key]).lower()+';')
dev=json.loads((FIX/'typed-developer-prefs.json').read_text(encoding='utf-8'))
devkeys={row['key'] for row in dev}
for key,v in snap['prefs'].items():
    if key in devkeys:continue
    statements.append('PlayerPrefs.DeleteKey('+json.dumps(key)+');' if v is None else 'PlayerPrefs.SetString('+json.dumps(key)+','+json.dumps(v)+');')
for row in dev:
    statements.append('PlayerPrefs.SetInt('+json.dumps(row['key'])+','+str(row['integer'])+');' if row['exists'] else 'PlayerPrefs.DeleteKey('+json.dumps(row['key'])+');')
input_asset=BACKUP/'Assets/Controls/CampusInputSettings.asset'
input_text=input_asset.read_text(encoding='utf-8-sig')
background=int(re.search(r'm_BackgroundBehavior: (\d+)',input_text)[1]);editor_input=int(re.search(r'm_EditorInputBehaviorInPlayMode: (\d+)',input_text)[1])
statements += ['PlayerPrefs.SetInt("CampusRift.AR.Floor",'+str(snap['floor'])+');','PlayerPrefs.SetInt("CampusRift.AR.Occlusion",'+str(snap['occlusion'])+');','PlayerPrefs.Save();','EditorSettings.enterPlayModeOptionsEnabled='+str(snap['enterPlayEnabled']).lower()+';','EditorSettings.enterPlayModeOptions=(EnterPlayModeOptions)'+str(snap['enterPlayOptions'])+';','var gv=System.Type.GetType("UnityEditor.GameView,UnityEditor");var win=UnityEditor.EditorWindow.GetWindow(gv);gv.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(win,'+str(snap['gameViewIndex'])+');','UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)'+str(background)+';','UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)'+str(editor_input)+';','UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);','return true;']
result=code('\n'.join(statements));save(FIX/'restoration.json',dict(saveFiles=rows,projectSettingsRestored=changed,inputBackground=background,inputEditorBehavior=editor_input,typedDeveloperPrefs=dev,editor=result))
print('Restored',len(rows),'save files;',len(changed),'ProjectSettings files',flush=True)
call('refresh_unity',{})
state=value(code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8-sig')));save(FIX/'handoff.json',state)
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save(FIX/'console-final.json',console)
assert not console.get('data'),console
assert not any(state[k] for k in ['playing','dirty','compiling','updating']) and state['platform']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and state['xrLoader'] is None,state
protected=json.loads((FIX/'protected-files.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in protected.items())
milestone('Phục hồi save/settings/typed DevMode prefs/InputSettings/ProjectSettings/GameView/EnterPlay. Unity Android/Edit/SampleScene sạch, Console0, XR loader inactive. Protected run-*.ps1/accounts hash không đổi. Evidence fix8/restoration.json, handoff.json, console-final.json.')
