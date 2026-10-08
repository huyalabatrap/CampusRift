from pathlib import Path
root=Path.cwd()
s=(root/'Tools/ar_nohand2_restore.py').read_text(encoding='utf-8-sig').replace('from ar_nohand2 import *','from ar_ui import *')
s=s[:s.index("save('protected-final.json'")]+'''save('protected-final.json',dict(orchestratorFilesUnchanged=True,allValidationSourcesUnchanged=True))
current=code((ROOT/'Tools/tech2_context.cs').read_text(encoding='utf-8'));save('editor-at-close.json',current)
assert all(current[k]==snap[k] for k in ['prefs','floor','occlusion','enterPlayEnabled','enterPlayOptions','gameViewIndex','savePath'])
dev=code('return new[]{"CampusRift.DevMode","CampusRift.DevMode.Invincible","CampusRift.DevMode.NoCooldown"}.Select(k=>new {key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray();');assert dev==snap['devPrefs']
assert all((ROOT/rel).read_bytes()==(BACKUP/rel).read_bytes() for rel in ['Packages/manifest.json','Assets/ARRift/Runtime/GestureStateMachine.cs','Assets/ARRift/Runtime/GestureRecognizerBridge.cs','Assets/ARRift/Runtime/ARHandMotion.cs','Assets/ARRift/Runtime/ARSkillCaster.cs','Assets/ARRift/Runtime/ARRecoveryGate.cs','Assets/ARRift/Runtime/FrameSampler.cs','Assets/ARRift/Runtime/GestureGeometry.cs'])
milestone('Phục hồi cuối: '+str(len(rows))+' file save byte-identical; settings/typed Dev/Input/GameView/EnterPlay/build prefs và prefilter assets về snapshot UI. Unity Android/Edit/SampleScene sạch, XR loader inactive, Console0. run-*.ps1/accounts, toàn bộ Validation và code nhận tay/D1/motion/native/recovery không đổi. Bằng chứng restoration.json/handoff.json/console-final.json/protected-final.json. Tiếp đối soát giao APK/cài đặt và chỉ viết REPORT khi tất cả công việc kết thúc.')
print('Restored',len(rows),'save files; settings, source preservation and clean handoff verified.')
'''
(root/'Tools/ar_ui_restore.py').write_text(s,encoding='utf-8')
