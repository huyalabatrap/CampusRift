"""Read-only delivery checks; does not run gameplay or regression suites."""
from ar_ui import *
from PIL import Image
import zipfile

connect()
state=code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8'))
assert state['platform']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity'
assert not any(state[k] for k in ['playing','dirty','compiling','updating']) and state['xrLoader'] is None
console=call('read_console',{'action':'get','types':['error'],'count':100})
assert not console.get('data'),console
save('handoff.json',state);save('console-final.json',console)
snapshot=json.loads((OUT/'original-editor.json').read_text(encoding='utf-8'))
current=code((ROOT/'Tools/tech2_context.cs').read_text(encoding='utf-8'))
assert all(current[k]==snapshot[k] for k in ['prefs','floor','occlusion','enterPlayEnabled','enterPlayOptions','gameViewIndex','savePath'])
dev=code('return new[]{"CampusRift.DevMode","CampusRift.DevMode.Invincible","CampusRift.DevMode.NoCooldown"}.Select(k=>new {key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray();')
assert dev==snapshot['devPrefs']
build=code('return new {EditorUserBuildSettings.development,EditorUserBuildSettings.buildAppBundle,EditorUserBuildSettings.exportAsGoogleAndroidProject,inputBackground=(int)UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior,inputEditor=(int)UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode};')
assert build==snapshot['build']
sources=json.loads((OUT/'sources.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((ROOT/r['path']).read_bytes()).hexdigest()==r['sha256'] for r in sources)
assert not (ROOT/'Assets/Editor/ARUIRecoveryConnect.cs').exists()
assert not (ROOT/'Assets/Editor/ARUIRecoveryConnect.cs.meta').exists()
protected=json.loads((OUT/'protected-files.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in protected.items() if p!='task/codex-accounts.json')
account=ROOT/'task/codex-accounts.json'
assert hashlib.sha256(account.read_bytes()).hexdigest()=='101feaad1ff77d6fc24c2ccde70d5a17e57904a3f404ca5113255fff0debecce'
saves=list(p for p in (BACKUP/'save').rglob('*') if p.is_file())
assert len(saves)==7 and all((Path(snapshot['savePath'])/p.relative_to(BACKUP/'save')).read_bytes()==p.read_bytes() for p in saves)
settings=list(p for p in (BACKUP/'ProjectSettings').rglob('*') if p.is_file())
assert all((ROOT/p.relative_to(BACKUP)).read_bytes()==p.read_bytes() for p in settings)
assert (ROOT/'Packages/manifest.json').read_bytes()==(BACKUP/'Packages/manifest.json').read_bytes()
assert json.loads((ROOT/'Packages/manifest.json').read_text(encoding='utf-8'))['dependencies']['com.unity.modules.physics']=='1.0.0'
layout=json.loads((OUT/'layout-summary.json').read_text(encoding='utf-8'));assert not layout['issues'] and len(layout['views'])==18
pngs=list((ROOT/'task/ar/screens/ui-fix').glob('*.png'));assert len(pngs)==18
for p in pngs:
 size=tuple(map(int,p.stem.rsplit('-',1)[1].split('x')))
 with Image.open(p) as im:assert im.size==size
verify=json.loads((OUT/'build/verification.json').read_text(encoding='utf-8'));apk=Path(verify['apk'])
assert apk.stat().st_size==verify['bytes'] and hashlib.sha256(apk.read_bytes()).hexdigest()==verify['sha256']
with zipfile.ZipFile(apk) as z:
 model=z.getinfo('assets/gesture_recognizer.task');assert model.compress_type==zipfile.ZIP_STORED
 assert hashlib.sha256(z.read(model)).hexdigest()==verify['gestureModel']['sha256']
device=json.loads((OUT/'build/device-install.json').read_text(encoding='utf-8'));assert device['skipped'] and not device['connected']
save('final-audit.json',dict(complete=True,sourceCount=len(sources),screenshots=len(pngs),saveFilesRestored=len(saves),projectSettingsByteIdentical=True,protectedRunnersAndValidationUnchanged=True,accountFileUntouchedDuringRecovery=True,temporaryEditorHelperRemoved=True,editor=state,consoleErrors=0,apk=verify['apk'],sha256=verify['sha256'],deviceSkipped=True,regressionRun=False))
milestone('Đối soát cuối đạt: 10 hash nguồn khớp APK đã build; 18 PNG đúng kích thước, layout issues=[]; 7 save và toàn bộ ProjectSettings byte-identical snapshot; typed prefs/Input/GameView/build flags đúng gốc. Xóa helper Editor tạm, compile xong, Android/Edit/SampleScene sạch, XR inactive, Console0. SHA APK/modelSTORED xác nhận lại. run-*.ps1/Validation giữ nguyên; accounts giữ nguyên hash lúc tiếp quản. Không chạy hồi quy. Công việc đã hoàn tất, viết REPORT cuối.')
print('All delivery checks passed; job complete.')
