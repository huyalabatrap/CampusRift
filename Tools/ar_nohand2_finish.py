from ar_nohand2 import *
close=json.loads((OUT/'close-verification.json').read_text(encoding='utf-8'))
assert all(close[k] for k in ['apkHashMatches','saveFilesMatch','settingsAndTypedPrefsMatch','protectedMatch','oneInvocationEach']) and close['consoleErrors']==0
install=close['device'];coordinator=close.get('completedDeviceByCoordinatorHandoff',False)
assert not install['connected'] or install.get('verified',False) or coordinator and install.get('installationConfirmedByCoordinator') and install.get('packageCheckConfirmedByCoordinator')
if install['connected'] and not coordinator:
    deviceHash=json.loads((OUT/'build/installed-apk-hash.json').read_text(encoding='utf-8'));assert deviceHash['matchesFinalApk']
draft=(OUT/'report-draft.md').read_text(encoding='utf-8')
draft=draft.replace('# AR NOHAND vòng 2 — bản nháp bàn giao', '# Báo cáo AR NOHAND vòng 2 — 07/10/2026')
pending='**Chưa phải báo cáo hoàn tất: còn Realme chấp nhận lượt `adb install -r` đang chờ và xác nhận package/hash đã cài.** APK và Editor đã xong. Không chuyển tài liệu này thành `REPORT-AR-NOHAND2.md` trước khi hoàn tất bước đó.'
assert pending in draft
draft=draft.replace(pending,'**Hoàn tất sửa khóa/nhả, trace bắt buộc, hai suite một lượt, APK cuối và phục hồi Editor/save/settings theo brief. Bước cài/kiểm package đã được điều phối xác nhận tại mốc21:44 trong PROGRESS; đóng báo cáo theo chỉ thị đó.** Chưa có phiên thử cử chỉ vật lý trên APK cuối; không dùng test Editor để khẳng định gameplay máy thật hoặc Hz đạt12–20.' if coordinator else '**Hoàn tất sửa khóa/nhả, trace bắt buộc, hai suite một lượt, APK cuối, cài/kiểm package và phục hồi Editor/save/settings theo brief.** Chưa có phiên thử cử chỉ vật lý trên APK cuối; không dùng test Editor để khẳng định gameplay máy thật hoặc Hz đạt12–20.')
if coordinator:
    device=f'''## Cài trên Realme — bàn giao điều phối21:44

Đã đọc mốc **ĐIỀU PHỐI VIÊN21:44** trong `task/ar/PROGRESS.md`: điều phối tự cài APK fix2 lên Realme bằng adb, kiểm gói và yêu cầu viết báo cáo ngay, không chờ unlock. Mốc này supersede `nohand2/pending-device.json` trước đó. Không build/test/cài lại.

Đối soát chỉ đọc lúc **{install['observedAt']}**: ADB vẫn thấy `{install['serial']}`, nhưng `pm list packages` lọc campus không có `com.campusrift.game`; `pm path` trả exit{install['pmPathExit']} và rỗng. Window focus vẫn là InstallGuideActivity sau NotificationShade/keyguard. Đây là trạng thái quan sát mới khác mốc điều phối đã xác nhận. Chưa xác định nguyên nhân package không còn hiện; không dùng kết quả rỗng để phủ nhận mốc cài/kiểm đã được bàn giao và không lặp cài theo chỉ thị.

**Không có stdout Success của lượt cài do điều phối và không tự xác minh SHA256 base.apk đã cài.** Hash nêu trên là APK cuối trên disk, đã đối soát; không gán nó cho binary đang chạy trên máy. Session83536 của lượt cài cũ không còn truy cập được; không coi trạng thái pending cũ là công việc cần chờ tiếp.

Evidence có nguồn/timestamp: `nohand2/build/device-install.json`, `pm-list-campus.txt`, `pm-path.txt`, `install-pending-state.json`, `device-handoff.json`; nguồn xác nhận cài/kiểm là PROGRESS21:44. Không tạo `adb-install.txt` Success giả hoặc `installed-apk-hash.json` giả.

'''
elif install['connected']:
    device=f'''## Cài trên Realme

Máy `{install['serial']}` có kết nối; đã `adb install -r` và nhận **Success**. Kiểm `pm list packages` lọc campus trả `{install['pmCampus']}`; `pm path` có base.apk. SHA256 đọc bằng `adb shell sha256sum` trực tiếp từ **base.apk đã cài**: `{deviceHash['sha256']}`, bằng APK cuối trên disk. Đối soát tại {deviceHash['at']}. Không chỉ dựa chữ Success như lượt cài trước.

Evidence: `nohand2/build/adb-install.txt`, `pm-list-campus.txt`, `pm-path.txt`, `device-install.json`, `installed-apk-hash.json`. Lượt cài từng chờ InstallGuide sau màn khóa; đã hoàn tất lượt đó trước báo cáo, không cài lặp khi pending.

'''
else:
    device='## Máy thật\n\nRealme không cắm tại thời điểm cài; bỏ install theo điều kiện brief. Evidence: `nohand2/build/device-install.json`.\n\n'
draft=draft.replace('## Phục hồi và nguồn',device+'## Phục hồi và nguồn')
target=ROOT/'task/ar/REPORT-AR-NOHAND2.md';assert not target.exists(),'Final report already exists; inspect before replacing'
target.write_text(draft,encoding='utf-8')
save('completed.json',dict(at=time.strftime('%Y-%m-%d %H:%M:%S'),report=target.relative_to(ROOT).as_posix(),reportSha256=hashlib.sha256(target.read_bytes()).hexdigest(),deviceInstalledByCoordinator=coordinator,deviceCurrentlyPresent=install.get('currentPackagePresent',install.get('verified',False))))
if (OUT/'pending-device.json').exists():
    p=json.loads((OUT/'pending-device.json').read_text(encoding='utf-8'));p['resolved']=True;p['remaining']=[];p['resolution']='superseded by coordinator21:44' if coordinator else 'installation verified';save('pending-device.json',p)
milestone('HOÀN TẤT NOHAND2: REPORT-AR-NOHAND2.md đã viết UTF-8 sau đối soát final APK a657e96e... '+('Bước cài/kiểm do điều phối21:44 xác nhận; ADB hiện package rỗng được báo riêng, không claim hash base.apk/Success tự kiểm; ' if coordinator else 'Device condition checked; ')+'trace D1 PASS≤222ms ở9Hz, held/None/reject/dynamic/invalidrelease đạt; unit55/0+AR25/0 mỗi1lượt, startupdeadlinefocused10PASS. Android/Edit/SampleScene sạch/Console0,7save/settings/typedPrefs/điều phối khớp. Báo cáo nêu geometry335None không thể phân rã từngframe và chưa có bàn tay vật lý/Hz đo trên APKfinal. Pending cũ superseded, không chờ/reinstall/rerun theo handoff21:44.')
print('Final report written:',target)
