"""Write the final Job 8 report only after APK verification and restoration."""
from pathlib import Path
import json,hashlib

root=Path.cwd();base=root/'task/batch-1007';fix=base/'fix8'
names=['PlayerCombat','SkillSet1Edges','P12AudioEndingPlayTest','P17Smoke','UIPlayAcceptance']
rows={n:json.loads((fix/'runs'/n/'row.json').read_text(encoding='utf-8')) for n in names}
raw={n:json.loads((fix/'runs'/n/'result.json').read_text(encoding='utf-8')) for n in names}
assert len(list((fix/'runs').glob('*/invoked.json')))==5
assert all((fix/'runs'/n/'console.json').exists() for n in names)
assert 'Touch buttons do not overlap and stay on screen at 1920x1080' in raw['PlayerCombat']['passed']
assert 'ice control expires at 2.5s' in raw['SkillSet1Edges']['passed']
assert 'tutorial-mobile text fits' in raw['P17Smoke']['passed']
assert rows['P12AudioEndingPlayTest']['status']=='PASS' and rows['UIPlayAcceptance']['status']=='PASS'
verification=json.loads((fix/'build/verification.json').read_text(encoding='utf-8'))
assert (fix/'build/DONE.txt').read_text(encoding='utf-8')=='Succeeded'
assert verification['signatureV2'] and verification['clipComponentsPrivate']
assert hashlib.sha256((root/verification['apk']).read_bytes()).hexdigest()==verification['sha256']
handoff=json.loads((fix/'handoff.json').read_text(encoding='utf-8'))
assert handoff['platform']=='Android' and handoff['scene']=='Assets/Scenes/SampleScene.unity'
assert not any(handoff[k] for k in ['playing','dirty','compiling','updating'])
assert not json.loads((fix/'console-final.json').read_text(encoding='utf-8')).get('data')
restoration=json.loads((fix/'restoration.json').read_text(encoding='utf-8'))
assert all(r['matchesBackup'] for r in restoration['saveFiles'])
counts=lambda n:f"{rows[n]['passed']} PASS / {rows[n]['failed']} FAIL"
text=f'''# Báo cáo Job 8 — Fix Remaining — 07/10/2026

Đã tiếp quản từ snapshot/PROGRESS trên disk, hoàn tất đúng 5 suite có trong brief, **mỗi suite một lượt nguyên vẹn**. Không chạy lại suite đã PASS khác, không viết test mới. Các sửa runtime sau retest Job7 đã có trên disk và được giữ để xác nhận. Cả **5 mục FAIL gốc đều PASS** trong lượt này; kết quả toàn suite là **4 PASS / 1 FAIL**.

**PlayerCombat toàn suite vẫn FAIL:** tôi đã bỏ sót DevMode trước lượt chạy. Prefs thật là số nguyên `DevMode=1`, `NoCooldown=1`, nhưng snapshot8 cũ dùng GetString nên ghi chuỗi rỗng. Fixture giả định Luyện Khí 1/cost/cooldown thường vì vậy có 20 assertion sai môi trường. Layout gốc và các kích thước khác đều PASS. Giữ nguyên kết quả FAIL, không chạy lại theo giới hạn một lượt. Đã lưu prefs đúng kiểu, tắt dev tạm trước 4 fixture còn lại và phục hồi khi bàn giao. Các kiểm tra combat này chưa được xác nhận lại ở môi trường chuẩn trong Job8.

| Suite | Nguyên nhân mục FAIL gốc và sửa đã xác nhận | Kết quả toàn suite / bằng chứng |
|---|---|---|
| PlayerCombatPlayTest | Resize đổi transform tổ tiên do FitFrame trong LateUpdate; canvas scale/rect riêng không phản ánh hết. MobileControlsHUD chạy sau FitFrame, theo dõi localToWorldMatrix và đặt lại cụm nút theo screen/safe area. Giữ nguyên harness kiểm giao nhau/on-screen. Layout 1920×1080, 2340×1080, 1600×1200 đều PASS. | **FAIL · {counts('PlayerCombat')}**; [raw](fix8/runs/PlayerCombat/result.json), [row](fix8/runs/PlayerCombat/row.json). 20 FAIL do DevMode như giải thích trên. |
| SkillSet1EdgePlayTest | Retest Job7 bị điều phối ngắt trước DONE; phép đo expiry đã được tính từ thời điểm apply thực thay giả định windup. Chạy full fixture và đợi hoàn tất; giữ expiry 2.5s. | **PASS · {counts('SkillSet1Edges')}**; [raw](fix8/runs/SkillSet1Edges/result.json), [DONE](fix8/runs/SkillSet1Edges/DONE.txt). |
| P12AudioEndingPlayTest | Cinematic mới tạo trong frame dài từng cộng cả delta trước Begin. P21 dùng clock realtime bắt đầu tại Begin, cập nhật tick khi pause để không catch-up; Restore khôi phục canvas/player/camera. Full suite xác nhận duck/restore audio, hide HUD, skip và reveal lặp. | **PASS · {counts('P12AudioEndingPlayTest')}**; [raw](fix8/runs/P12AudioEndingPlayTest/result.json). Reveal đo **3.5202s**, giữ ngưỡng **3.4 ≤ elapsed < 3.8s**. |
| P17Smoke | Caption skill được dịch từ key English `VOID WALL` sau bước rút gọn, nên chỉ sửa tên tiếng Việt chưa đủ. CompactSkillName xử lý cả key English và HƯ KHÔNG BÍCH, hiển thị HƯ KHÔNG/WALL; giữ tên đầy đủ trong skillbook. | **PASS · {counts('P17Smoke')}**; [raw](fix8/runs/P17Smoke/result.json), [audit 28 text / 0 issue](fix8/runs/P17Smoke/tutorial-mobile-text-audit.json), [ảnh](fix8/runs/P17Smoke/tutorial-mobile.png). |
| UIPlayAcceptance | Slider.UpdateVisuals stretch handle theo chiều Y; sizeDelta.y dương cộng thêm chiều cao ngoài track. SettingsUI đặt offsetY/sizeDelta.y=0 cho handle ngang; giữ ComicTheme và audit. | **PASS · {counts('UIPlayAcceptance')}**; [raw](fix8/runs/UIPlayAcceptance/result.json). Settings Video/Audio/Gameplay và pointer slider đều PASS. |

P12 chỉ bổ sung `Measure` elapsed vào report hiện có; không thay assertion hay nới ngưỡng. UIPlay chạy đầy đủ phần chức năng (`FailedItemsOnly=false`), giữ `SkipCaptureMatrix=true` theo TEST-POLICY. Ledger và console riêng từng suite ở [fix8/runs](fix8/runs/); [Summary](fix8/Summary.json). Không sửa kết quả lịch sử Job7.

APK development: [CampusRift-20261007-batch1007-dev2.apk](../../{verification['apk']}), **{verification['bytes']:,} byte** ({verification['bytes']/1024/1024:.2f} MiB).

SHA256: `{verification['sha256']}`.

BuildPlayer một lần, Succeeded trong **54.28s, 0 lỗi, 71 cảnh báo**; xem [build summary](fix8/build/build-summary.txt). Kiểm như Job7: IL2CPP/ARM64, GLES3-only, sensorLandscape, debuggable/debug keystore; chữ ký APK v2 PASS. Gesture model STORED/hash đúng; Vosk data STORED, tất cả file Vosk hash đúng StreamingAssets; README giải nén qua asset stream. DEX có GestureBridge/onHands/setHandCount, VoiceBridge, ClipBridge/consent/service, Vosk/JNA/MediaPipe; đủ native libs ARM64. Manifest đủ quyền Camera/Microphone/foreground MediaProjection, Activity/Service clip private. Bằng chứng: [verification](fix8/build/verification.json), [manifest](fix8/build/AndroidManifest.txt), [badging](fix8/build/badging.txt), [signature](fix8/build/apksigner.txt).

'''
if verification['realmeConnected']:
    assert (fix/'build/adb-install.txt').exists() and (fix/'build/logcat-unity.txt').exists()
    text+='Realme WGH6S8I7GIMBGQKR đã cài/mở app; bằng chứng install/launch/logcat và màn chọn mode AR ở fix8/build/.\n\n'
else:
    text+='[adb devices](fix8/build/adb-devices.txt) không thấy Realme WGH6S8I7GIMBGQKR hoặc thiết bị nào. Chưa install/launch hay xác nhận Sảnh/màn chọn mode AR trên máy thật.\n\n'
text+='Đã phục hồi save/settings/PlayerPrefs đúng kiểu (gồm dev prefs gốc), InputSettings, ProjectSettings, GameView và EnterPlay options. Unity **Android / Edit / SampleScene sạch, Console 0 lỗi**, XR loader inactive. [Restoration](fix8/restoration.json), [handoff](fix8/handoff.json), [console](fix8/console-final.json). Backup: `Backups/Fix-Remaining-pre-20261007/`. Hash các `task/run-*.ps1` và `task/codex-accounts.json` không đổi.\n'
(base/'REPORT-8-FIX-REMAINING.md').write_text(text,encoding='utf-8')
with (base/'PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n- Job8 hoàn tất: đã ghi REPORT-8-FIX-REMAINING.md sau build/verification/restoration. Đúng5 suite,1lần mỗi suite;5mụcFAIL gốc PASS; full suite4PASS/1FAIL(PlayerCombat20assertion nhiễm DevMode, không rerun). APK dev2 '+str(verification['bytes'])+'byte/SHA256 '+verification['sha256']+'. Unity Android/Edit/SampleScene sạch, Console0.\n')
print('Final report written:',base/'REPORT-8-FIX-REMAINING.md')
