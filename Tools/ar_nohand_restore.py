from ar_nohand import *
connect();assert (OUT/'build/DONE.txt').read_text(encoding='utf-8').strip()=='Succeeded'
assert (OUT/'build/verification.json').exists(),'Verify APK first'
snap=json.loads((OUT/'original-editor.json').read_text(encoding='utf-8'));stop();code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
rows=[]
for source in (BACKUP/'save').rglob('*'):
 if not source.is_file():continue
 relative=source.relative_to(BACKUP/'save');target=Path(snap['savePath'])/relative;target.parent.mkdir(parents=True,exist_ok=True)
 if target.exists() and target.read_bytes()!=source.read_bytes():
  post=OUT/'post-check-save'/relative;post.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(target,post)
 shutil.copy2(source,target);rows.append(dict(path=relative.as_posix(),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),matchesBackup=target.read_bytes()==source.read_bytes()))
changed=[]
# Restore serialized settings and build-generated prefilter flags, retaining source fixes.
for branch in ['ProjectSettings','Assets/ARRift','Assets/Settings','Assets/Controls','Assets/XR']:
 for source in (BACKUP/branch).rglob('*'):
  if not source.is_file() or branch!='ProjectSettings' and source.suffix!='.asset':continue
  relative=source.relative_to(BACKUP);target=ROOT/relative
  if target.exists() and target.read_bytes()!=source.read_bytes():
   post=OUT/'build-generated'/relative;post.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(target,post);shutil.copy2(source,target);changed.append(relative.as_posix())
statements=[];devkeys={r['key'] for r in snap['devPrefs']}
for key,v in snap['prefs'].items():
 if key in devkeys:continue
 statements.append('PlayerPrefs.DeleteKey('+json.dumps(key)+');' if v is None else 'PlayerPrefs.SetString('+json.dumps(key)+','+json.dumps(v)+');')
for row in snap['devPrefs']:
 statements.append('PlayerPrefs.SetInt('+json.dumps(row['key'])+','+str(row['value'])+');' if row['exists'] else 'PlayerPrefs.DeleteKey('+json.dumps(row['key'])+');')
for field,name in [('floor','Floor'),('occlusion','Occlusion')]:statements.append('PlayerPrefs.SetInt("CampusRift.AR.'+name+'",'+str(snap[field])+');')
for key in ['development','buildAppBundle','exportAsGoogleAndroidProject']:statements.append('EditorUserBuildSettings.'+key+'='+str(snap['build'][key]).lower()+';')
statements+=['PlayerPrefs.Save();','EditorSettings.enterPlayModeOptionsEnabled='+str(snap['enterPlayEnabled']).lower()+';','EditorSettings.enterPlayModeOptions=(EnterPlayModeOptions)'+str(snap['enterPlayOptions'])+';','var gv=System.Type.GetType("UnityEditor.GameView,UnityEditor");var win=UnityEditor.EditorWindow.GetWindow(gv);gv.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(win,'+str(snap['gameViewIndex'])+');','UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)'+str(snap['build']['inputBackground'])+';','UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)'+str(snap['build']['inputEditor'])+';','EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);','return true;']
result=code('\n'.join(statements));save('restoration.json',dict(saveFiles=rows,assetsRestored=changed,typedDev=snap['devPrefs'],editor=result))
call('refresh_unity',{})
state=code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8'));save('handoff.json',state)
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save('console-final.json',console)
assert not any(state[k] for k in ['playing','dirty','compiling','updating']) and state['platform']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and state['xrLoader'] is None,state
assert not console.get('data'),console
protected=json.loads((OUT/'protected-files.json').read_text(encoding='utf-8'));assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in protected.items())
assert all((ROOT/rel).read_bytes()==(BACKUP/rel).read_bytes() for rel in ['Assets/ARRift/Validation/ARGestureUnitTests.cs','Assets/ARRift/Validation/ARRiftPlayTest.cs'])
save('protected-final.json',dict(orchestratorFilesUnchanged=True,existingSuitesUnchanged=True))
milestone('Phục hồi hoàn tất: '+str(len(rows))+' file save byte-identical, settings/typedDev/Input/GameView/EnterPlay/build prefs và prefilterassets về snapshot tiếp quản. Unity Android/Edit/SampleScene sạch, XR loader inactive, Console0; nohand/restoration.json/handoff.json/console-final.json. Hai suite gốc + run-*.ps1/accounts hash không đổi. APK verified; đủ điều kiện viết REPORT-AR-NOHAND.md cuối.')
print('Restored',len(rows),'save files; handoff and Console clean.')
