from ar_nohand2 import *
connect()
verification=json.loads((OUT/'build/verification.json').read_text(encoding='utf-8'))
assert hashlib.file_digest((ROOT/verification['apk']).open('rb'),'sha256').hexdigest()==verification['sha256']
for name in ['ARGestureUnitTests','ARRiftPlayTest']:
    data=json.loads((OUT/'checks'/name/'result.json').read_text(encoding='utf-8'));assert not data['failed']
    assert json.loads((OUT/'checks'/name/'invoked.json').read_text())['count']==1
state=code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8'));save('handoff-final.json',state)
assert state['platform']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and not any(state[k] for k in ['playing','dirty','compiling','updating']) and state['xrLoader'] is None
console=call('read_console',{'action':'get','types':['error'],'count':100});save('console-at-close.json',console);assert not console['data']
snap=json.loads((OUT/'original-editor.json').read_text(encoding='utf-8'))
current=code((ROOT/'Tools/tech2_context.cs').read_text(encoding='utf-8'));save('editor-at-close.json',current)
assert all(current[k]==snap[k] for k in ['prefs','floor','occlusion','enterPlayEnabled','enterPlayOptions','gameViewIndex','savePath'])
dev=code('return new[]{"CampusRift.DevMode","CampusRift.DevMode.Invincible","CampusRift.DevMode.NoCooldown"}.Select(k=>new {key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray();');assert dev==snap['devPrefs']
assert all((Path(snap['savePath'])/p.relative_to(BACKUP/'save')).read_bytes()==p.read_bytes() for p in (BACKUP/'save').rglob('*') if p.is_file())
protected=json.loads((OUT/'protected-files.json').read_text());assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in protected.items())
assert (ROOT/'Assets/ARRift/Validation/ARRiftPlayTest.cs').read_bytes()==(BACKUP/'Assets/ARRift/Validation/ARRiftPlayTest.cs').read_bytes()
if '--editor-only' in sys.argv:
    save('editor-close-verification.json',dict(saveFilesMatch=True,settingsAndTypedPrefsMatch=True,protectedMatch=True,oneInvocationEach=True,consoleErrors=0,finalState=state))
    milestone('Đối soát độc lập Editor cuối đạt: 7save/settings/typedPrefs/Input/GameView/EnterPlay/điều phối khớp snapshot; Android/Edit/SampleScene sạch/Console0; source và APK/hash đã kiểm. Cài Realme đang chờ InstallGuide sau màn khóa: session83536/Tools/ar_nohand2_device.py đã gửi install-r, chưa Success, pm path rỗng. Đã yêu cầu người dùng unlock/approve trên điện thoại, không lặp install và không viết REPORT-AR-NOHAND2 khi còn bước này.')
    print('Editor preservation and final state verified; device installation pending')
    sys.exit(0)
install=json.loads((OUT/'build/device-install.json').read_text(encoding='utf-8'))
coordinatorHandoff='--coordinator-handoff' in sys.argv
if coordinatorHandoff:
    assert install.get('installationConfirmedByCoordinator') and install.get('packageCheckConfirmedByCoordinator')
    assert install['coordinatorHandoff']=='ĐIỀU PHỐI VIÊN 21:44'
elif install['connected']:
    assert install['verified']
    args=[str(ADB),'-s','WGH6S8I7GIMBGQKR']
    listing=subprocess.check_output(args+['shell','pm','list','packages'],text=True,encoding='utf-8');assert 'package:com.campusrift.game' in listing
    apkpath=subprocess.check_output(args+['shell','pm','path','com.campusrift.game'],text=True,encoding='utf-8').strip().splitlines()[0].removeprefix('package:')
    assert apkpath.startswith('/data/app/') and apkpath.endswith('/base.apk')
    installedHash=subprocess.check_output(args+['shell','sha256sum',apkpath],text=True,encoding='utf-8').split()[0]
    assert installedHash==verification['sha256'],(installedHash,verification['sha256'])
    save('build/installed-apk-hash.json',dict(serial='WGH6S8I7GIMBGQKR',path=apkpath,sha256=installedHash,matchesFinalApk=True,at=time.strftime('%Y-%m-%d %H:%M:%S')))
save('close-verification.json',dict(apkHashMatches=True,saveFilesMatch=True,settingsAndTypedPrefsMatch=True,protectedMatch=True,oneInvocationEach=True,consoleErrors=0,finalState=state,device=install,completedDeviceByCoordinatorHandoff=coordinatorHandoff))
# Correct two copied round-one status strings in the round-two portion only.
progress=ROOT/'task/ar/PROGRESS.md';s=progress.read_text(encoding='utf-8');at=s.index('\n## AR NOHAND2 —');tail=s[at:]
tail=tail.replace('Đã hoàn tất mock duy nhất và 2 suite một lượt mỗi cái; queue đúng một build APK', 'Đã hoàn tất trace D1 và 2 suite một lượt mỗi cái; queue build APK')
tail=tail.replace('Theo dõi nohand/build/DONE.txt','Theo dõi nohand2/build/DONE.txt').replace('Evidence nohand/build/verification.json','Evidence nohand2/build/verification.json')
progress.write_text(s[:at]+tail,encoding='utf-8')
milestone(('Đối soát cuối theo handoff21:44: điều phối xác nhận đã cài fix2/kiểm gói, không chờ unlock/reinstall. ADB hiện pm path rỗng; không tuyên bố hash base.apk đã cài được tự xác minh. ' if coordinatorHandoff else 'Đối soát cuối đạt: APKfinalhash khớp disk và base.apk đã cài Realme; pm package còn hiện. ')+'ModelSTORED/manifest/signature/ARM64/native/resource verified; unit55/0+AR25/0 mỗi1invocation, tracePASS, startupfocused10casePASS. Audit nguồn cuối9file, không rerun suite. 7save/settings/typedPrefs/GameView/Input/EnterPlay/điều phối khớp snapshot; Android/Edit/SampleScene sạch/Console0. Đủ điều kiện đóng brief theo handoff; viết REPORT-AR-NOHAND2 với giới hạn chưa có test bàn tay vật lý/Hz thực APKfinal.')
print('All final checks passed')
