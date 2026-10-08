from b1007_fix8 import *

paths=['Assets/Controls/Runtime/MobileControlsHUD.cs','Assets/CampusRiftUI/Runtime/SettingsUI.cs','Assets/SkyBeast/Runtime/P21StoryCinematic.cs','Assets/Combat/Validation/PlayerCombatPlayTest.cs','Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs','Assets/Enemies/Validation/P12AudioEndingPlayTest.cs','Assets/CampusRiftUI/Validation/P17Smoke.cs','Assets/CampusRiftUI/Validation/UIPlayValidation.cs','Assets/Controls/CampusInputSettings.asset']
for relative in paths:
    p=ROOT/relative;b=BACKUP/relative;b.parent.mkdir(parents=True,exist_ok=True)
    if not b.exists():shutil.copy2(p,b)
protected=list((ROOT/'task').glob('run-*.ps1'))+[ROOT/'task/codex-accounts.json']
save(FIX/'protected-files.json',{p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in protected if p.exists()})
milestone('Tiếp quản: Unity thực tế Android/Edit/SampleScene. fix8 không có invocation/result nào; backup thêm 8 source liên quan và InputSettings trước sửa. Giữ snapshot8 và backup save/settings của agent trước. Các sửa Job7 đã có: LateUpdate sau FitFrame + matrix tổ tiên; key VOID WALL rút gọn; clock realtime từ Begin; slider offsetY=0. Chỉ dùng các sửa đã có, không sửa harness layout hoặc nới ngưỡng.')

# Record the actual elapsed time in the existing suite's measurements.
p=ROOT/'Assets/Enemies/Validation/P12AudioEndingPlayTest.cs'
s=p.read_text(encoding='utf-8-sig')
old='began=Time.realtimeSinceStartup;yield return SkyBeastEnding.Play();Check(!SkyBeastEnding.Playing'
new='began=Time.realtimeSinceStartup;yield return SkyBeastEnding.Play();Measure("Repeated reveal elapsed real seconds="+(Time.realtimeSinceStartup-began).ToString("F4"));Check(!SkyBeastEnding.Playing'
assert old in s
p.write_text(s.replace(old,new,1),encoding='utf-8')
print(call('refresh_unity',{}))
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True})
save(FIX/'compile-before-suites.json',console)
assert not console.get('data'),console
milestone('Compile sạch trước 5 suite. P12 chỉ bổ sung Measure elapsed vào report hiện có, giữ nguyên 3.4 <= elapsed < 3.8 và mọi assertion. Không test mới.')
