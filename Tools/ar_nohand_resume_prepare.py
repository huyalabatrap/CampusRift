from ar_nohand import *
connect()
snap=json.loads((OUT/'original-editor.json').read_text(encoding='utf-8'))
for name in ['Assets/ARRift','Assets/Plugins/Android','ProjectSettings','Assets/Controls/CampusInputSettings.asset','Assets/Settings/Mobile_RPAsset.asset','Assets/XR/Resources/XRSimulationRuntimeSettings.asset']:
 src=ROOT/name;dest=BACKUP/name;dest.parent.mkdir(parents=True,exist_ok=True)
 if src.is_dir():
  if not dest.exists():shutil.copytree(src,dest)
 elif src.exists() and not dest.exists():shutil.copy2(src,dest)
if not (BACKUP/'save').exists():shutil.copytree(snap['savePath'],BACKUP/'save')
protected=list((ROOT/'task').glob('run-*.ps1'))+[ROOT/'task/codex-accounts.json']
save('protected-files.json',{p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in protected if p.exists()})
save('console-before.json',call('read_console',{'action':'get','types':['error'],'count':100}))
print('Realme connected:',device_log())
stop()
milestone('Tiếp quản: đọc đủ report6/report5/GóiA và brief UTF-8; chưa có nguồn/evidence/report NOHAND. Unity thực tế Play/SampleScene, đã giữ snapshot trước Stop (khác handoff8 cũ). Snapshot nohand/original-editor.json giữ typed DevMode/save/settings/Input/GameView/EnterPlay; backup Backups/AR-NOHAND-pre-20261007. ADB không có Realme; device-logs/nohand-realme.txt ghi rõ. Không sửa điều phối/accounts. Chỉ một mock Open_Palm và mỗi suite ARGestureUnitTests/ARRiftPlayTest một lượt sau sửa.')
# Native/assets and source diffs against each pre-job backup.
for base in ['AR-Tech2-pre-20261006','AR-Hands-Study-pre-20261006','AR-Combat-pre-20261006','AR-Space-Modes-pre-20261006','AR-Modes-Core-pre-20261006']:
 parts=[]
 for rel in ['Assets/Plugins/Android/GestureBridge.kt','Assets/Plugins/Android/mainTemplate.gradle','Assets/ARRift/Runtime/GestureRecognizerBridge.cs','Assets/ARRift/Runtime/FrameSampler.cs','Assets/ARRift/Runtime/GestureStateMachine.cs','Assets/ARRift/Runtime/ARSkillCaster.cs','Assets/ARRift/Runtime/ARBattlefield.cs']:
  old=ROOT/'Backups'/base/rel;new=ROOT/rel
  if old.exists():parts.extend(difflib.unified_diff(old.read_text(encoding='utf-8-sig').splitlines(True),new.read_text(encoding='utf-8-sig').splitlines(True),fromfile=str(old),tofile=rel))
 (OUT/(base+'-diff.txt')).write_text(''.join(parts),encoding='utf-8')
print('Backed up, stopped, diff evidence saved.')
