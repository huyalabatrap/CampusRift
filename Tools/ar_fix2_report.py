from pathlib import Path
import json
out=Path('task/ar/fix2');build=Path('task/ar/build-fix2')
assert (build/'DONE.txt').read_text().strip()=='Succeeded'
v=json.loads((build/'verification.json').read_text())
state=json.loads((out/'final-state.json').read_text())
assert state['target']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and not any(state[k] for k in ['playing','compiling','updating','building','dirty'])
assert json.loads((out/'final-console.json').read_text())['data']==[]
assert all(r['identical'] for r in json.loads((out/'protected.json').read_text()))
assert all(r['identical'] for r in json.loads((out/'orchestrators.json').read_text()))
assert all(r['identical'] for r in json.loads((out/'restoration.json').read_text())['files'])
assert json.loads((out/'diagnostics-input-timed.json').read_text())==dict(tripleClickShows=True,tripleClickHides=True)
assert json.loads((out/'convergence-after-spawn.json').read_text())['convergence']
assert json.loads((out/'text-audit.json').read_text())['issues']==0
assert all(line.endswith('(type 0x10)0x6') for line in v['screenOrientation'])
assert (out/'HubFlow/HubFlow-DONE.txt').read_text().strip()=='42 passed; 0 failed'
summary=(build/'build-summary.txt').read_text(encoding='utf-8-sig')
assert 'Errors: 0\n' in summary
report=f'''# AR fix2 — hoàn tất 04/10/2026

Đã build [CampusRift-AR-dev-20261004-fix2.apk](../../APK-Test/CampusRift-AR-dev-20261004-fix2.apk), **{v['bytes']:,} bytes**, ARM64/IL2CPP, API26+, development/debug, package `com.campusrift.game`, ARCore Optional. [Build summary](build-fix2/build-summary.txt), [APK verification](build-fix2/verification.json).

Build cuối thành công **18,49s / 0 lỗi / 1 cảnh báo** (incremental, sau hai build trước giữ evidence).

SHA256: `{v['sha256']}`.

## Kết luận từng nghi vấn

| Nghi vấn | Kết luận và bằng chứng |
|---|---|
| 1. Cho xoay dọc | **Đúng về cấu hình:** AutoRotation và hai portrait=true trái thiết kế ngang. Loader chưa khóa hướng trước XR. [Trước](fix2/configuration-before.json) → [sau](fix2/configuration.json). Chưa có máy thật để kết luận riêng rotation gây nửa màn hình đen. |
| 2. Vulkan/pre-transform | **Vulkan đứng đầu và pre-transform=true được xác nhận; Automatic là sai:** bản trên đĩa thực tế Manual Vulkan→GLES3. ARCore6.6.2 hỗ trợ Vulkan nhưng URP bắt buộc thêm `ARCommandBufferSupportRendererFeature`; `AR_Renderer` chỉ có ARBackground, thiếu feature này. Đây là cấu hình thiếu được xác định, phù hợp với CPU có ảnh/GPU nền đen. Chưa chứng minh riêng pre-transform là nguyên nhân trên máy. Chọn **GLES3-only**, bỏ Vulkan và tắt pre-transform để dùng đường nền camera đã hỗ trợ, giảm biến số display rotation. |
| 3. Sai renderer/background/URP | **Không thấy sai wiring:** renderer0 của bản sao AR_RPAsset là AR_Renderer; ARBackgroundRendererFeature active; ARCameraBackground có trong sessionComponents và bật sau Ready. Runtime backgroundRendering=true; consent Canvas inactive sau Accept. Camera rect=(0,0,1,1), clearFlags=Nothing khi AR background điều khiển. [Wiring](fix2/runtime-wiring.json). Thiếu feature cho Vulkan thuộc mục2. RenderScale1→0,85 vẫn giữ pixelRect đầy1600×720: [kiểm hẹp](fix2/render-scale-085.json). |
| 4. Canvas/safe area | **Canvas vốn đã Overlay + CanvasScaler, không phải nửa viewport.** Tuy nhiên nội dung dùng khung1920×1080 tuyệt đối và chưa xử lý notch/safeArea. Đã fit nội dung vào Screen.safeArea và giữ Canvas/viewport toàn màn hình; cập nhật mỗi frame khi kích thước/vùng an toàn thay đổi. [2400×1080](fix2/layout-2400.json), [1600×720](fix2/layout-1600.json). Kích thước Rect Canvas là đơn vị layout sau scaler, không phải số pixel vùng render. |

Đối chiếu yêu cầu Vulkan với [tài liệu chính thức ARCore6.6.2](https://docs.unity3d.com/Packages/com.unity.xr.arcore@6.6/manual/project-configuration-arcore.html#graphics-api) và Documentation~/project-configuration-arcore.md / Editor/ProjectValidation/ARCoreProjectValidationRules.cs của package cài trên đĩa. Lỗi máy thật vẫn cần người dùng xác nhận; không tuyên bố đã tái hiện hay chữa xong trên thiết bị.

## Những gì đã sửa / cấu hình cuối

- Player: **AutoRotation chỉ LandscapeLeft/Right**, Portrait/PortraitUpsideDown=false; Android **Manual [OpenGLES3]**, vulkanEnablePreTransform=false. MobileControlSetup và ARRiftSetup dùng cùng hàm cấu hình để tránh setup ghi đè. Build processor đặt activity sensorLandscape trong generated manifest vì Unity6 mặc định xuất userLandscape.
- ARXRLoaderControl: khóa **LandscapeLeft trong Awake**, đợi surface ngang trước InitializeLoader; giữ hướng trong phiên, khôi phục hướng trước đó sau Shutdown. Sau đổi pipeline, đặt lại renderer0, camera rect toàn màn hình và targetTexture=null.
- ARUI/ARScreenLayout: Canvas Overlay toàn màn hình, fit nội dung1920×1080 vào safeArea. Consent backdrop stretch theo Canvas và tắt cả Canvas sau đồng ý. GestureDebug: landmark phủ đúng toàn viewport camera; nút/metrics nằm trong safeArea.
- ARDeviceDiagnostics chỉ compile trong **Editor/development**; đếm frameReceived, overlay góc trên phải, ba lần chạm liên tiếp để bật/tắt. Debug.Log ra tag Unity mỗi2s với `[ARDiag]`, kể cả khi overlay ẩn. Không sửa combat hoặc harness cũ.

## Kiểm thử và giới hạn

- ARRiftPlayTest chạy **đúng1lượt:24 PASS/1 FAIL**, Console0. Fail còn trong [raw](fix2/results.json): Pulled+area=Convergence. Kiểm hẹp tại tâm chiến trường sau spawn **Convergence PASS**: [focused](fix2/convergence-after-spawn.json); không gọi kết quả này là harness25/25.
- ComicTextAudit đúng1lượt, VI1600×720,19text, **0issue**: [audit](fix2/text-audit.json).
- HubFlow đúng1lượt **42 PASS/0 FAIL**, [Console0](fix2/HubFlow/console.json); [kết quả](fix2/HubFlow/HubFlow.json). Editor dùng D3D11; APK đã biên dịch cho GLES3 nhưng game thường/AR chạy GLES3 trên Android thật vẫn chưa kiểm.
- Đã soi [HUD2400×1080](screens/fix2/hud-2400x1080.png), [HUD1600×720](screens/fix2/hud-1600x720.png), [overlay](screens/fix2/diagnostics-1600x720.png). Canvas/camera đầy màn hình, text không cắt. [Virtual Mouse triple-click](fix2/diagnostics-input-timed.json) show/hide đều đạt; chưa thử chạm thật. `Portrait` trong ảnh overlay là giá trị Screen.orientation của **Editor**, không phải orientation APK.
- QA local helper hotreload làm mất managed XR state; giữ [Console](fix2/qa-hotreload-console.json)/[state](fix2/qa-hotreload-state.json). Đã dựng fixture sạch; riêng collector recovery đồng bộ camera với Simulation pose, plane/anchor vẫn provider thật: [pose](fix2/recovery-pose.json). Focused checks trước recovery vô hiệu, raw vẫn giữ. HubFlow từng auto-pause do Editor chạy ẩn/focus loss; Resume để tiếp tục cùng lượt. Không sửa gameplay/harness để xử lý các lỗi collector này.

## Kiểm trên máy / bàn giao

Cài APK fix2, cầm máy ngang, vào AR và đồng ý camera: nền camera thật phải phủ toàn màn hình; ô xem tay có ảnh chưa đủ. Thử vào AR từ cả hai hướng ngang ở Sảnh rồi xoay máy trong phiên: AR giữ LandscapeLeft, không xoay lệch90° hay bị chia/nửa đen; thoát AR khôi phục hướng game. Chi tiết trong [DEVICE-TEST.md](DEVICE-TEST.md).

Chạm3lần góc trên phải trong safeArea, mỗi lần cách dưới0,6s. Chụp toàn bộ overlay và hai dòng log `[ARDiag]` cách2s: **Screen / orientation / safeArea / Display / pixelRect / GPU / ARSession / Background / BackgroundRendering / frames / renderer / pipeline / renderScale / ready**. Mong đợi Screen W>H LandscapeLeft; GPU=OpenGLES3; pixelRect=(0,0,W,H); Background/BackgroundRendering/ready=True; frames tăng; renderer=UniversalRenderer/AR_Renderer; renderScale1 hoặc0,85 đều phủ đầy màn hình. Lấy log bằng `adb logcat -s Unity` khi có adb. Gửi kèm model máy/Android và bước xoay nếu còn lỗi.

Merged manifest activity **sensorLandscape(0x6)**: [manifest APK](build-fix2/AndroidManifest.txt). ModelSTORED/hash, DEX Bridge/Listener/Recognizer và thư viện MediaPipeARM64 đã xác minh. Build lần1 thành công nhưng kiểmmanifest phát hiện userLandscape0xb; lần2 vẫn cũ vì Editor chạyẩn chưaimport sourceprocessor. Giữ [lần1](build-fix2/attempt1/build-summary.txt), [lần2](build-fix2/attempt2/manifest-requirement.json). Đã explicitRefresh/compile ởEditMode, [xác minh hookloaded](build-fix2/manifest-hook-imported.json) và build lần3 đạtmanifest. Không chạy lại harness/audit; xem summary cuối bên trên.

Unity cuối **Android / Edit Mode / SampleScene**, scene không dirty, loaderinactive/startupoff, không compiling/updating/building,3shaderAR0lỗi, [Console cuối0](fix2/final-console.json) và [final state](fix2/final-state.json). [38file bảo vệ](fix2/protected.json), [script điều phối + codex-accounts](fix2/orchestrators.json) nguyênhash; [save/settings/prefilter phục hồi](fix2/restoration.json). Backup: [FIX2-BACKUP.txt](FIX2-BACKUP.txt); [danh sách thay đổi](fix2/changes.json). PROGRESS đã cập nhật từng mốc; chưa kiểm Android thật, FPS/nhiệt hay cân bằng thực chiến.
'''
Path('task/ar/REPORT-AR-fix2.md').write_text(report,encoding='utf-8')
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — HOÀN TẤT / bàn giao\n- APK '+v['apk']+', '+str(v['bytes'])+'bytes, SHA256 '+v['sha256']+'. Build3PASS; build1/2 giữattempt1/2 vì userLandscape chưa đúng brief và Editor chưaimportprocessor; manifest cuối sensorLandscape; packaging/model/DEX/native verified.\n- Player landscape-only/GLES3-only/pretransformoff; ARLandscapeLeft trước XR. AR24/1+focusedConvergencePASS, audit0, HubFlow42/0; ảnh2resolution/diag và inputtoggle đạt. RawQAhotreload/focus recovery giữ.\n- Save/settings/prefilter phục hồi; protected/orchestrator hashes đạt. UnityAndroid/Edit/SampleScene/Console0/finalstate đạt. DEVICE-TEST cập nhật; REPORT-AR-fix2.md chỉ tạo sau hoàn tất. Máy thật còn chờ người dùng thử.\n')
progress=Path('task/ar/PROGRESS.md');text=progress.read_text(encoding='utf-8')
text=text.replace('## Trạng thái hiện tại — tiếp tục từ đây','## Trạng thái hiện tại — tiếp tục từ đây\n- **AR fix2 HOÀN TẤT:** REPORT-AR-fix2.md + APK fix2; sensorLandscape/GLES3-only/pretransformoff, AR fixedLandscapeLeft trướcXR. AR24/1+focusedConvergencePASS, audit0, HubFlow42/0. UnityAndroid/Edit/SampleScene/Console0; save/settings/protected/orchestrators phục hồi. Device test còn chờ người dùng; lịch sử các mốc ở cuối file.',1)
progress.write_text(text,encoding='utf-8')
print('Final report written after completion checks')
