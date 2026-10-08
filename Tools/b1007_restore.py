from b1007_runner import *
import hashlib
backup=ROOT/'Backups/Regression-APK-pre-20261006';snap=json.loads((OUT.parent/'7-original-editor.json').read_text(encoding='utf-8'))['data']['result']
stop();code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
rows=[];destination=Path(snap['savePath'])
for source in (backup/'save').rglob('*'):
    if not source.is_file():continue
    relative=source.relative_to(backup/'save');target=destination/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
    rows.append({'path':relative.as_posix(),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'matchesBackup':target.read_bytes()==source.read_bytes()})
changed=[]
for source in (backup/'ProjectSettings').rglob('*'):
    if not source.is_file():continue
    target=ROOT/'ProjectSettings'/source.relative_to(backup/'ProjectSettings')
    if target.exists() and target.read_bytes()!=source.read_bytes():changed.append(target.relative_to(ROOT).as_posix());shutil.copy2(source,target)
statements=[]
prebuild=OUT.parent/'build/prebuild-state.json'
if prebuild.exists():
    build_settings=json.loads(prebuild.read_text(encoding='utf-8'))
    for key in ['development','buildAppBundle','exportAsGoogleAndroidProject']:
        if key in build_settings:statements.append('EditorUserBuildSettings.'+key+'='+str(build_settings[key]).lower()+';')
for key,v in snap['prefs'].items():statements.append('PlayerPrefs.DeleteKey('+json.dumps(key)+');' if v is None else 'PlayerPrefs.SetString('+json.dumps(key)+','+json.dumps(v)+');')
input_asset=backup/'Assets/Controls/CampusInputSettings.asset'
input_text=input_asset.read_text(encoding='utf-8-sig')
background=int(re.search(r'm_BackgroundBehavior: (\d+)',input_text)[1]);editor_input=int(re.search(r'm_EditorInputBehaviorInPlayMode: (\d+)',input_text)[1])
statements += ['PlayerPrefs.SetInt("CampusRift.AR.Floor",'+str(snap['floor'])+');','PlayerPrefs.SetInt("CampusRift.AR.Occlusion",'+str(snap['occlusion'])+');','PlayerPrefs.Save();','EditorSettings.enterPlayModeOptionsEnabled='+str(snap['enterPlayEnabled']).lower()+';','EditorSettings.enterPlayModeOptions=(EnterPlayModeOptions)'+str(snap['enterPlayOptions'])+';','var gv=System.Type.GetType("UnityEditor.GameView,UnityEditor");var win=UnityEditor.EditorWindow.GetWindow(gv);gv.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(win,'+str(snap['gameViewIndex'])+');','UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)'+str(background)+';','UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)'+str(editor_input)+';','UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);','return true;']
result=code('\n'.join(statements));save(OUT.parent/'7-restoration.json',dict(saveFiles=rows,projectSettingsRestored=changed,inputBackground=background,inputEditorBehavior=editor_input,editor=result));print('Restored',len(rows),'save files;',len(changed),'ProjectSettings files')
call('refresh_unity',{});time.sleep(1)
state=value(code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8-sig')));save(OUT.parent/'7-handoff.json',state)
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save(OUT.parent/'7-console-final.json',console)
assert not console.get('data'),console
assert not any(state[k] for k in ['playing','dirty','compiling','updating']) and state['platform']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and state['xrLoader'] is None,state
progress('Mốc phục hồi: save/settings/ProjectSettings/PlayerPrefs/EnterPlay/GameView về snapshot; Unity Android/Edit/SampleScene sạch, Console0. Evidence7-restoration.json,7-handoff.json,7-console-final.json. Chỉ ghi REPORT sau khi kiểm APK và mọi mục hồi quy hoàn tất.')
