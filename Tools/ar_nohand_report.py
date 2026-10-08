from ar_nohand import *
draft=(OUT/'report-draft.md').read_text(encoding='utf-8')
assert (OUT/'close-verification.json').exists()
ev=json.loads((OUT/'device-readiness.json').read_text(encoding='utf-8'))
assert ev['nativeReady'] and ev['gestureCount']==354 and ev['decisions']['release-required']==265
report=draft.replace('# Bản nháp bàn giao AR NOHAND — chưa hoàn tất','# Báo cáo AR NOHAND — bàn giao vòng 1, 07/10/2026',1)
report=report.replace('**Chưa được chuyển thành `task/ar/REPORT-AR-NOHAND.md`: còn cài APK cuối và xác nhận recognizer ready trên Realme.** Đây là tài liệu tiếp quản, không phải báo cáo hoàn tất.', '**Đã hoàn tất điều tra, sửa false blocker, diagnostics, kiểm Editor, APK và bàn giao vòng 1. Bản sửa vòng 1 CHƯA khắc phục được lỗi không tung chiêu trên máy thật.** Log bổ sung xác nhận recognizer sẵn sàng nhưng 0 charging / 0 fire; `requireRelease` vẫn kẹt. Theo mốc điều phối 21:16 trong PROGRESS, đóng báo cáo vòng này và chuyển vấn đề còn lại sang brief `PROMPT-AR-fix-NOHAND2.md`, không chờ unlock hoặc chạy lại test.\n\nCác evidence không ghi đường dẫn đầy đủ bên dưới nằm trong `task/ar/nohand/`; log máy nằm trong `task/ar/device-logs/`.')
report=report.replace('**Sai với khóa vĩnh viễn mặc định đã kiểm.** `ARModeSelectionHUD.cs:15,60` đóng khi SelectionConfirmed; `ARBattleHUD.cs:84` cập nhật MenuPaused; `ARBattlefield.cs:64,65` tính Paused và bật sampler tương ứng. Fixture XR sau xác nhận không paused/inputBlocked/Adjusting.', '**Đúng một phần sau bổ sung log thật: flags mode/pause có nhả, nhưng trạng thái D1 sau pause vẫn có thể kẹt.** `ARModeSelectionHUD.cs:15,60` đóng khi SelectionConfirmed; `ARBattleHUD.cs:84` cập nhật MenuPaused; `ARBattlefield.cs:64,65` tính Paused và bật sampler. Fixture XR sau xác nhận không paused/inputBlocked/Adjusting; log thật cũng có sampling=True/paused=False/input-blocked=False nhưng D1=requireRelease. `GestureStateMachine.cs:22,31,34` Suspend khi !allowed đặt latch, chỉ absence mới nhả. Vòng1 chưa sửa điểm này.')
report=report.replace('Không đổi mode/pause/capability. Diagnostics hiện riêng paused/input-blocked/sampling-paused.', 'Vòng1 không đổi mode/pause/capability hoặc luật nhả D1. Diagnostics hiện riêng paused/input-blocked/sampling-paused; lỗi latch sau Suspend cần vòng2.')
start=report.index('**Thiết bị đang chờ:**')
end=report.index('## Phục hồi và giới hạn',start)
section='''## Bằng chứng máy thật sau sửa motion — kết quả chưa đạt

Mốc điều phối21:16 ghi đã cài bằng adb và kiểm `pm list packages` có com.campusrift.game; người dùng đã thử. Không lặp install/chờ unlock theo bàn giao đó. Đọc toàn bộ `task/ar/device-logs/nohand-fix2.txt`, lọc Unity PID21238; trích log lưu `task/ar/device-logs/nohand-realme-ready.txt`, thống kê có dòng nguồn ở `device-readiness.json`.

Tại **20:51:00.654**, log dòng49356: `recognizer ready=True hands=1 delegate=CPU recovering=False`, ARSession=SessionTracking, BackgroundRendering=True, native error=none. Đây là ready của recognizer CPU thật; không dùng ready của loader hoặc MOCK trước khởi tạo để kết luận. Có callback mô hình và submitted tăng, chứng minh sampler/native hoạt động trong phiên đó.

354 ARGesture: release-required265 (**74.86%**), suspended50, dynamic-motion35, stale3, absent1; **charging0 / fire0**. Nhãn model: None170, Open_Palm110, Closed_Fist44, Victory22, Thumb_Down7, Thumb_Up1. Geometry=None335/354; Closed_Fist15, Thumb_Down2, Open_Palm2.

Tại20:51:19.185 (dòng65030), sampling=True, paused=False, input-blocked=False, motion=Idle/block=False, nhưng D1=requireRelease. Norm=63/world=63, CPU/input512x384, Hz9.5/latency128.9ms. Tại20:51:21.218, norm/world vẫn63 và D1=requireRelease. Cuối phiên Hz8.3–9.5, latency161.7–162.9ms. Vì vậy không phải thiếu toàn bộ world arrays, chưa đạt mục tiêu12–20Hz, và không thể quy tất cả geometry=None cho dữ liệu thiếu. D1 khi !allowed có thể return trước Evaluate, nên geometry ở các dòng suspended còn có thể là kết quả cũ. Log không chứa mọi vector/world quality để xác định chính xác từng frame None.

**Vì sao bản sửa chưa đủ:** `GestureStateMachine.Suspend()` :22 vẫn luôn requireRelease=true; `Process()` :31 gọi nó khi !allowed, và đổi epoch :26 cũng Suspend. Chỉ nhánh !hand :34 xóa latch sau>=200ms/>=3frame. Khi tay vẫn có landmark nhưng model=None, :33 vẫn coi hand=true nên không vào nhánh nhả; model/geometry không thể vượt nhánh requireRelease :39–40. Log có170 None nhưng chỉ1 absent, khớp khả năng latch tồn tại khi người chơi thả lỏng tay trong khung. Đây là giới hạn state-machine được code chứng minh và log hỗ trợ; nguồn nào đã đặt latch đầu tiên trong từng lượt chưa được log ghi lại. Vòng1 chưa có requireReleaseReason/heldLabel. Mock trước đó đưa None **không landmark** .7s trước Open_Palm, nên nhả latch thành công và bỏ sót tình huống máy thật này. Không dùng test55/0 và25/0 để tuyên bố chữa xong máy thật.

**Giới hạn tương quan build/log:** nohand-fix2 có timestamp20:50–20:53, trước build APK cuối20:54/verify20:55. Log không có hash APK; không thể chứng minh nó chạy binary SHA20a4... dù dùng cùng tên file. Đây là bằng chứng recognition của mã vòng1 trước bản vá material cuối. Trong đúng PID21238 vẫn có426 dòng ArgumentNullException, lần đầu20:51:00.246 (dòng48981). Vì thế câu “shader đã hết” trong brief vòng2 không khớp toàn bộ file này; không lấy log này làm chứng minh sửa shader cuối đã đạt trên máy. Shader/resource cuối được compile và kiểm đóng gói, chưa xác nhận runtime trên binary cuối. Cần kiểm log riêng đúng build ở vòng2.

Khi đóng báo cáo, ADB vẫn thấy Realme nhưng `pm path com.campusrift.game` trả1 và rỗng (`device-package-at-close.json`). Đây là trạng thái hiện tại khác mốc điều phối đã kiểm cài, không phủ nhận phiên runtime PID21238 trước đó; nguyên nhân package biến mất lần nữa chưa xác định. Không ghi cài APK cuối/ready cuối đã được tự xác minh theo hash, và không reinstall ngoài phạm vi bàn giao điều phối.

'''
report=report[:start]+section+report[end:]
report=report.replace('Chưa xác nhận casting thật, độ chính xác tay thật, performance12–20Hz, đa tay hoặc teardown native trên APK cuối. Build có73warnings; không coi0build errors/Console errors là0warnings. Chỉ tạo báo cáo hoàn tất sau khi device-ready đạt hoặc user cho phép bỏ kiểm máy.', 'Log máy thật đã xác nhận **casting chưa đạt** (0fire); chưa xác nhận performance12–20Hz, đa tay hoặc teardown native trên APK cuối. Build có73warnings; không coi0build errors/Console errors là0warnings. Read-only verification lúc đóng ở handoff-at-close.json/console-at-close.json/close-verification.json xác nhận Android/Edit/SampleScene, Console0 lỗi, save byte-identical, hash APK và file điều phối/suite nguyên. Không build/test lại. Báo cáo đóng vòng1 theo mốc điều phối; vấn đề requireRelease và kiểm đúng binary cuối còn dành cho vòng2.')
snippet='''
Đoạn sửa trọng tâm (`ARHandMotion.cs:20–25`):

```csharp
// Trước: State=Released; BlocksStatic=true;
// Sau: hủy dữ liệu/lifecycle không phải bằng chứng động tác thật.
State=ARHandMotionState.Released;
BlocksStatic=false;
Reason="cancelled";
```

Đoạn sửa shader (`RiftPlacementService.cs:140–142`):

```csharp
var template=Resources.Load<Material>("ARModes/FeaturePoints");
if(template==null) { /* cảnh báo một lần; bỏ visual optional */ return; }
dotMaterial=new Material(template);
```

'''
report=report.replace('## Kiểm tra đúng giới hạn brief',snippet+'## Kiểm tra đúng giới hạn brief',1)
target=ROOT/'task/ar/REPORT-AR-NOHAND.md';assert not target.exists(),'Existing report: inspect before replacing';target.write_text(report,encoding='utf-8')
pending=json.loads((OUT/'pending-device.json').read_text(encoding='utf-8'));pending['supersededAt']=time.strftime('%Y-%m-%d %H:%M:%S');pending['supersededBy']='PROGRESS coordinator21:16 and REPORT-AR-NOHAND.md';pending['remaining']=[];pending['outstandingProductIssue']='requireRelease still prevents real casts; PROMPT-AR-fix-NOHAND2.md';save('pending-device.json',pending)
milestone('Đã viết REPORT-AR-NOHAND.md UTF-8 sau đọc log thật/đối soát final. Đóng bàn giao vòng1 theo điều phối21:16, không tuyên bố chữa xong gameplay:354ARGesture/265release-required/0charging/0fire, readyCPU/1tay được log chứng minh. Nêu rõ log20:50–20:53 trước material build cuối và có426shader exception; không gán hash20a4... hoặc shader runtime đạt cho log đó. pm path hiệnrỗng ghi evidence, không chờ/reinstall. APK finalhash20a4..., unit55/0+AR25/0+mock1/1 giữ đúng1lượt; Android/Edit/SampleScene/Console0/save/protected sạch. pending-device cũ đã supersede. Vòng2 xử lý requireRelease theo brief riêng.')
print('Wrote',target.relative_to(ROOT),len(report),'UTF-8 characters')
