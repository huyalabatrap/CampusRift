from ar_ui import *
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
protected=json.loads((OUT/'protected-files.json').read_text(encoding='utf-8'))
differences={p:dict(snapshot=h,current=hashlib.sha256((ROOT/p).read_bytes()).hexdigest()) for p,h in protected.items() if hashlib.sha256((ROOT/p).read_bytes()).hexdigest()!=h}
assert not any(p!='task/codex-accounts.json' for p in differences),differences
assert (ROOT/'Assets/ARRift/Validation/ARRiftPlayTest.cs').read_bytes()==(BACKUP/'Assets/ARRift/Validation/ARRiftPlayTest.cs').read_bytes()
save('protected-final.json',dict(runnersUnchanged=True,allValidationSourcesUnchanged=True,accountFileEditedByThisTask=False,accountFileSnapshotDifference=differences,note='Account file already differs from the initial snapshot on recovery; leave current content untouched.'))
current=code((ROOT/'Tools/tech2_context.cs').read_text(encoding='utf-8'));save('editor-at-close.json',current)
assert all(current[k]==snap[k] for k in ['prefs','floor','occlusion','enterPlayEnabled','enterPlayOptions','gameViewIndex','savePath'])
dev=code('return new[]{"CampusRift.DevMode","CampusRift.DevMode.Invincible","CampusRift.DevMode.NoCooldown"}.Select(k=>new {key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray();');assert dev==snap['devPrefs']
assert all((ROOT/rel).read_bytes()==(BACKUP/rel).read_bytes() for rel in ['Packages/manifest.json','Assets/ARRift/Runtime/GestureStateMachine.cs','Assets/ARRift/Runtime/GestureRecognizerBridge.cs','Assets/ARRift/Runtime/ARHandMotion.cs','Assets/ARRift/Runtime/ARSkillCaster.cs','Assets/ARRift/Runtime/ARRecoveryGate.cs','Assets/ARRift/Runtime/FrameSampler.cs','Assets/ARRift/Runtime/GestureGeometry.cs'])
milestone('Phục hồi cuối: '+str(len(rows))+' file save byte-identical; settings/typed Dev/Input/GameView/EnterPlay/build prefs và prefilter assets về snapshot UI. Unity Android/Edit/SampleScene sạch, XR loader inactive, Console0. run-*.ps1, toàn bộ Validation và code nhận tay/D1/motion/native/recovery không đổi. codex-accounts.json đã khác snapshot khi tiếp quản (mtime22:51:45), không chỉnh sửa hoặc phục hồi file này. Bằng chứng restoration.json/handoff.json/console-final.json/protected-final.json. Tiếp đối soát giao APK/cài đặt và chỉ viết REPORT khi tất cả công việc kết thúc.')
print('Restored',len(rows),'save files; settings, source preservation and clean handoff verified.')
