from pathlib import Path
import json
root=Path.cwd();out=root/'task/ar/goiA';build=root/'task/ar/build-goiA'
def read(p):return json.loads(Path(p).read_text(encoding='utf-8-sig'))
v=read(build/'verification.json');disk=read(out/'disk-audit.json');restore=read(out/'restoration.json')
state=read(out/'final-state.json')['result']['structuredContent']['data']['result']
assert not any(state[k] for k in ['playing','compiling','updating','building','dirty','runeShaderError','shadowShaderError','planeShaderError'])
assert state['target']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and state['loaderInactive'] and state['simulationRays']==10
assert all(r['identical'] for r in disk['protected']+disk['orchestrators']+restore['files'])
console=read(out/'final-console.json')['result']['structuredContent'];assert console['success'] and not console['data']
u=read(out/'gesture-unit-tests.json');a=read(out/'ar-results.json');hub=read(out/'HubFlow/HubFlow.json')
mock=read(out/'mock-export-final.json');byte_count=len(('[ARCheck] '+(out/'mock-export-final.json').read_text(encoding='utf-8')).encode('utf-8'))
assert len(mock['cells'])==30 and byte_count<=3500 and not mock['complete']
(out/'mock-roundtrip-final.json').write_text(json.dumps(dict(cells=30,tupleLength=len(mock['cells'][0]),bytes=byte_count,complete=False,n=sum(c[3] for c in mock['cells']),correctUnique=sum(c[16] for c in mock['cells']),firstValidIntent=mock['latency']['firstValidIntent'],captureSpan=mock['latency']['captureSpan']),indent=2),encoding='utf-8')
summary=(build/'build-summary.txt').read_text(encoding='utf-8')
text=f'''# Báo cáo AR Gói A — hoàn tất 06/10/2026

Đã triển khai A1–A5, build và kiểm APK cuối, phục hồi save/settings. Chưa nghiệm thu các ngưỡng trên máy thật: {v['device']}.

## APK và bàn giao

- APK: `APK-Test/CampusRift-AR-dev-20261006-goiA.apk` — **{v['bytes']:,} byte**.
- SHA256: `{v['sha256']}`.
- Development / IL2CPP / ARM64 / GLES3; manifest activity **sensorLandscape (6)**. Model 8.373.440 byte STORED, SHA256 không đổi. DEX có `submit(ByteBuffer, …, epoch, frameId)`, `recover`, `selectDelegate`, `clockNanos`, listener world/categories/timestamps và `onState`; xem `build-goiA/verification.json`.
- Build1 thành công 4m44s/0errors/73warnings nhưng giữ ở `build-goiA/attempt1/` vì có sửa nguồn cuối cho Kiểm Ấn mở sau trận thua. Build2 là APK bàn giao; summary bên dưới. Không chạy lại full unit/combat/audit/Hub để làm đẹp raw.
- Hướng dẫn tiếng Việt: [DEVICE-TEST.md](DEVICE-TEST.md), khoảng 14 phút + 1 phút dự phòng; cắm cáp để người điều phối lấy `[ARCheck]`.

## A1 — ảnh, contract và phục hồi

`IGestureSource.GestureFrame`, `FrameSampler.Update/ConvertAsync`, `ARFrameDeadline`, `GestureRecognizerBridge.Start/Submit/Update/CalibrateClock`, Kotlin `GestureBridge.create/submit/recover/close`:

- World21×3 + categories/scores/capability, hand presence, epoch/frameId, sensor/acquire/convert/submit/consume/infer/result clocks. API chỉ winner thì không điền vector score giả.
- Deadline20Hz/512 hoặc12Hz/384, dedupe timestamp CPU, tối đa một acquire/Update, không upsample; conversion1/pending latest1/in-flight1. Pause/menu/background ngừng sampler, callback cũ không cast.
- GPU trước, lỗi về CPU; bỏ benchmark30+30/cache device và JNI hỏi busy/delegate mỗi frame. Selector dev AUTO/CPU/GPU và Thử lại.
- Watchdog1s; worker close/recreate tuần tự, buffer còn inference giữ đến close/callback; CPU retry tối đa2, backoff0,5/1s. Hết deadline2s khóa/báo lỗi nếu native close treo, không hứa phục hồi lỗi không recoverable.
- Governor một tier/visit: rolling5s frame>40ms chiếm>20% hoặc thermal≥3; unsupported=null. JNI handshake3mẫu chọn RTT nhỏ nhất/midpoint, giữ error bound; không trừ sensor clock với Unity clock.

## A2 — geometry world và D1

`GestureGeometry.Evaluate`, `GestureStateMachine.Process/Suspend/RequireRelease`, `GestureCoordinates`, `ARModeSettings`:

- Góc3D/basis giải phẫu, world thiếu/hỏng/suy biến→Neutral; pixel sau transform chỉ dùng framing/motion và hướng lên/xuống. Reflection không đổi luật handedness.
- Model≥0,6, ≥3 ảnh riêng + dwell100ms; switch200ms, global lock350ms. Một pose một intent; None/unmapped không nhả held. Vắng tay≥200ms/3ảnh mới nhả; gap>150ms reset candidate/absence, acquire→consume>250ms bỏ. Epoch/pause/recovery yêu cầu hạ tay.
- Framing: inset2% chiều ngắn, wrist/MCP/tips trong cả CPU và viewport; min8% chiều ngắn, max95% mỗi chiều viewport. Motion≤1,5 chiều ngắn/s.
- Extended150°/Folded120°, thumb140°/150° và khoảng cách, Victory tips>.35palm, Up/Down>.15palmPixel. Rescue mặc định off; cờ dành cho thay đổi có kiểm soát sau này, bản này không có đường rescue. Các field evidence/rescue cũ còn để tương thích asset, không dùng để quyết định.

## A3 — ngắm và phản hồi

`ARSkillCaster.Update/DiscAim/Fire`, `ARBattleHUD`, `ARSessionBootstrap`, marker `ARCombatContext.VfxStarted`, `SkillVfxPool.Spawn`, `GiantHandVisual.Begin`:

- Ray tâm màn hình trên disc thật, intersection phía trước và trong bán kính; bỏ chọn quái/plane fallback. Palm chỉ còn field dev. Reticle báo hợp lệ; lấy aim tại intent.
- Outcome typed Success/CD/Spirit/Aim/Paused/Unavailable; reject tiêu intent, giải thích tiếng Việt, phải hạ tay. Hand-dot TTL250ms và tắt khi invalidate.
- Hướng dẫn25–55cm, bắt đầu40cm; lòng/mu ngang nhau và ngón cầm máy khỏi ống kính.
- FirstVFX nối nơi spawn/enable CPU thực, không dùng flash/haptic làm VFX. Độ trễ vật lý/GPU-visible chưa đo; clockError đi cùng số đo JNI.

## A4 — placement, light và navigation

`RiftPlacementService.Evaluate/Confirm/TryAdjustment/Reposition`, `ARPlaneGeometry`, `ARBattlefield.LightFrame/Build`, `ARRiftSetup`, `ARRiftBattle.unity`, `ProjectSettings/NavMeshAreas.asset`:

- Chỉ polygon đủ extent confirm. Estimated/Depth/Feature preview mờ “Đang tìm mặt”, tap không confirm; polygon valid tap đặt ngay. Cache boundary/area/convexity, bỏ Incenter grid khỏi Evaluate. Radius=min(nominal,edge−1cm), min.18m; giữ area/tracking/distance/angle gates.
- Snapshot boundary anchor-local, kéo/pinch validate disc và revert vượt mép; offset local/smooth position+quaternion. Reset stable/observations khi mất tracking và counter mất neo khi tạo/đổi trận.
- Light request flags31 scene/runtime/setup, lọc .2s intensity/rotation/SH, fallback và trả ambient khi exit. `[ARDiag]` có requested/current + HasValue bitmask:1brightness/2direction/4intensity/8SH, −1 chưa có frame.
- Chỉ hai AR profiles: Bàn radius.0546/height.351m, Sàn .2184/1.404m. Ba fixture min/max PathComplete; Sàn min **không có mesh**, dùng steering với log theo R2, không coi case này nav pass. Chưa tune bake mỗi scale (B).

## A5 — Kiểm Ấn dev và T1

`ARGestureCheck`, `ARReviewMetrics/ARMetricHistogram`, `ARMonsterDirector.CheckLoad`, menu `ARBattleHUD` (Editor/development only):

- Consent phiên, tay/light tự chọn; cue tiếng Việt + hình pose + timer/skip; warm60s →10placement×12s →transition30s →120trial360s →negative180s →play60s →results. Sáu block cân bằng random,5s đổi điều kiện +20trial×2,75s; response2,25s/release.5s. Không cue mới khi release chưa đạt; vẫn ghi timeout/late/skip.
- Practice giữ CD/spirit ready qua cùng mock/native→D1→intent pipeline; không dùng cue quyết nhãn, không bỏ framing/aim. Tải cap6quái/giữ shrine sống; đã sửa mở sau Finished. Warm-up/prepare cũng bảo vệ trận để kiểm overlay; chỉ Trials→Play mới tính active/perf/accuracy.
- Negative exposure thật, duplicate hold/reject riêng; injected lý do có ghi chú, bỏ injection sau5s trong mỗi10s để kiểm giữ tay không tự retry. Play phát5VFX theo lịch5/15/25/35/45s; đây là tải dev riêng, không tính recognition intent hoặc latency clean.
- RAM scalar/histogram bounded, không lưu ảnh/landmark/room pose. Export explicit một dòng ASCII `[ARCheck]` v2≤3500byte, đủ30tuple23field; quá cap báo lỗi, không truncate. Tắt consent clearRAM.
- First-intent confusion+timeout, allIntent/correctIntent, correctUnique/multi/late/framing, skips, pooled gesture/lòng-mu; precision suy từ correctIntent/allIntent (mẫu số0→null). Nearest-rank n/p50/p95 clean practice, clocks từng stage, captureSpan, VFX per-skill, frame aggregate+rolling10s, uniqueHz, thermal/tier/drop/decision/outcome reasons.

**Lệch/giới hạn so R2:** cold placement để `null` vì chưa có mốc app→camera/cold-start đáng tin; sensor-age cũng chưa suy từ hai clock khác epoch. FPS dưới27 được ghi theo chuỗi frame liên tiếp >1/27s, không phải bộ profile FPS window riêng. Histogram latency/frame lượng tử hóa1ms và bão hòa2000ms. UI ưu tiên bảng recall/negative/placement/latency; precision đầy đủ trong aggregate để tính sau. Phiên bị pause/abort/thiếu matrix ghi incomplete, không bù thời gian ngầm. Không few-shot/lưu điểm tay.

## Kiểm thử và evidence

| Kiểm | Kết quả |
|---|---|
| Unit một lượt | {len(u['passed'])}PASS/{len(u['failed'])}FAIL raw: deadline tại đúng1s do floating point. Sửa epsilon; `deadline-focused.json` first/afterGaptrue,duplicatefalse,next1.05. |
| R2 traces | Cadence .6/.62/.9×10/12/15/20Hz→200/167/133/100ms, held1intent; None/mislabel/reject/switch/absence/gap/stale/duplicate/pause/epoch, anatomy fixture độc lập aspect/rotation/reflection, recovery gate/aim/metrics. |
| ARRiftPlayTest một lượt | {len(a['passed'])}PASS/{len(a['failed'])}FAIL raw Convergence. Sửa AR-only kiếm đầu SwordRain vào điểm ngắm; normal scatter/damage/CD giữ nguyên. Kiểm hẹp Convergence=true,pulled6,casts2,swordHits10. |
| HubFlow một lượt | {len(hub['passed'])}PASS/{len(hub['failed'])}FAIL, Console0; chạy vì3shared skill files đổi. Artifacts/UI gốc phục hồi. Không hồi quy Level/HubLayout khác. |
| UI/mock | Full text audit10state một lượt0issue; focused selector/play/negative0. Mock cuối2trial/1correct/1timeout,30cells,{byte_count}byte, incomplete; mock trước1trial+1skip vẫn giữ. Clean clock/captureSpan/VFX nối qua pipeline. Không dùng số này làm accuracy/perf điện thoại. |
| Dev load/metrics hẹp | Dead+Finished→mở KiểmẤn→alive6/deadfalse/Finishedfalse/paneloff; scheduled5VFX/allIntent0; warmupallIntent0; rolling10/JSONroundtrip đạt. |

Raw giữ trong `goiA/`: lỗi fixture combat thiếuactor trước assertion và command namespace UIValidation ban đầu không phải lỗi gameplay; không tính là lượt suite combat thứ hai. `ARRiftPlayTest` chỉ thêm chờactor và đường output, giữ assertions combat. `SwordRainRuntime.TickCast` là thay đổi ngoài danh sách R2 tối thiểu: rải RNG trên disc nhỏ khiến Pulled hết trước sword hit; kiếm AR đầu trúng aim giữ combo hoạt động.

Ảnh hai độ phân giải **2400×1080 /1600×720** tại [screens/goiA](screens/goiA): `placement-preview`, `placement-valid`, `battle-center`, `reject-feedback`, `check-prepare`, `check-warmup`, `check-trial`, `check-release`, `check-negative`, `check-play`, `check-results`. Ảnh là XR Simulation/Editor mock, không camera người dùng. Đã soi prepare/results/cue và ảnh placement corrected; initial preview2400 no-hit giữ rawstate, ảnh cuối đã thay bằng estimated preview thật.

## Khôi phục và còn kiểm máy thật

- Backup `Backups/AR-GoiA-pre-20261006/`; {len(restore['files'])}save byte-identical, settings/PlayerPrefs/Input/Editor options/devflag/scan10rays/prefilter phục hồi. {len(disk['protected'])}protected assets và {len(disk['orchestrators'])}file điều phối/accounts nguyên hash. Diff source có trong `goiA/disk-audit.json`.
- Unity **Android/Edit/SampleScene/không dirty/XRloaderinactive**, Console0, ba AR shader0error; xem `goiA/final-state.json`, `final-console.json`.
- ADB: {v['device']}. Không tuyên bố đã cài/mở/kiểm consent trên Realme khi thiết bị không có.
- Cần phiên DEVICE-TEST thật để đánh giá lòng/mu25/40/55cm, framing/world veto/handedness, full-score capability thực tế, GPU/CPU JNI, latency/clock uncertainty, warm/cold placement/light/drift, nhiệt/FPS10phút, background/menu và recovery lỗi native. Chưa nghiệm thu các mục tiêu ≥3/4/cell, pooled≥90%,0wrong/duplicate/retry,p95cast≤200ms/VFX≤50ms,placement≥7/8/median≤2s hoặc frame33,5/40ms trên điện thoại.

### Build cuối

```text
{summary.strip()}
```
'''
(root/'task/ar/REPORT-AR-GOI-A.md').write_text(text,encoding='utf-8')
print('Final report written after all completion gates')
