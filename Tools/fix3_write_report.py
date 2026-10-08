"""Only write the completion report after fix3 evidence passes all guards."""
from pathlib import Path
import json,subprocess,sys

subprocess.run([sys.executable,'Tools/fix3_verify.py'],check=True)
def read(p):return json.loads(Path(p).read_text(encoding='utf-8-sig'))
root=Path('Artifacts/Skills/fix3')
pc=read(root/'Performance-Player.json');rp=read('Artifacts/Reactions/fix3/Performance-Player.json')
pool=read(root/'regressions/SkillSet1Pool.json')
main=read(root/'regressions/SkillSet1.json')
tests=read(root/'Regressions.json')
names={'van-kiem-quyet':'Vạn Kiếm Quyết','than-kiem-ngu-loi':'Thần Kiếm Ngự Lôi','phat-no-hoa-lien':'Phật Nộ Hỏa Liên','tich-lich-nhat-thiem':'Tích Lịch Nhất Thiểm'}
rows=[]
for name,title in names.items():
    trial=next(t for t in pc['trials'] if t['skill']==name)
    metric=next(t for t in pool['metrics'] if t['skill']==name)
    visual=read(root/(name+'-visual.json'))
    pcs=[trial['peakParticles'],metric['peakPC'],visual['peakPC']]
    mobiles=[metric['peakMobile'],visual['peakMobile']]
    for m in main['measurements']:
        if m['skill']==name:
            pcs.append(m['peakPC'])
            mobiles.append(m['peakMobile'])
    rows.append(f"| {title} | {trial['baselineFps']:.2f} | {trial['castFps']:.2f} | {trial['lossPercent']:.2f}% | {trial['meanBaselineMs']:.3f} / {trial['meanCastMs']:.3f} | {trial['p95Ms']:.3f} | {max(pcs)} / {max(mobiles)} | {metric['before']}→{metric['after']} | {trial['maxVfxFrameGCBytes']} B |")
testrows=[]
for t in tests:
    result=t['result'].strip().replace('\n',' ')
    if t['test']=='PhantomDecoy':result='FAIL cũ, khớp cả 5 cờ baseline; 0 FAIL mới'
    testrows.append(f"| {t['test']} | {result} | [JSON](../../Artifacts/Skills/fix3/regressions/{t['test']}.json) |")
photos=[]
for name,title in names.items():
    photos.append(f"| {title} | [sáng](screens/fix3/{name}-sheet-light.png) | [tối](screens/fix3/{name}-sheet-dark.png) | [cận](screens/fix3/{name}-impact.png) | [mobile](screens/fix3/{name}-mobile.png) |")
reactions={'bang-loi-liet':'Băng Lôi Liệt','dien-luu':'Điện Lưu','bao-viem':'Bạo Viêm','tu-sat':'Tụ Sát','pha-giap':'Phá Giáp'}
reactionphotos=[]
for name,title in reactions.items():
    reactionphotos.append(f"| {title} | [sáng](../p11/screens/fix3/{name}-sheet-light.png) | [tối](../p11/screens/fix3/{name}-sheet-dark.png) | [cận](../p11/screens/fix3/{name}-impact.png) | [mobile](../p11/screens/fix3/{name}-mobile.png) |")
text='''# REPORT-P10-fix3 — Hoàn tất P10/P11 vòng sửa 3

Ngày 02/10/2026 · Unity 6000.6.2f1. Đã tiếp quản từ `RESUME-fix3.md` và phần sửa trên đĩa, đọc đầy đủ brief/context, hoàn tất cả 5 mục fix3. Sao lưu trước sửa: `Backups/P10-fix3-pre-20261002-081306/`; sao lưu các sửa khi tiếp quản: `Backups/P10-fix3-resume-20261002/`. Nhật ký từng mốc và checklist: [PROGRESS-fix3.md](PROGRESS-fix3.md).

Giữ nguyên số liệu gameplay, capacity **112 node VFX + 30 FlyingSword**, ngân sách **PC 1500 / mobile 400 hạt**. **Không thêm kiếm ảo**: đúng 30 kiếm gây hit, launch/hitbox/damage giữ nguyên. [SHA-256 17 tệp](../../Artifacts/Skills/fix3/GameplayDataAudit.json) xác nhận definition/config gameplay, Hắc Động runtime, resolver/chain/status P11 giống backup. Hắc Động không được làm lại; pool/regression kiểm lại khả năng dùng chung tài nguyên.

## 1. Vạn Kiếm Quyết

Kiếm dài **2,8275 m**, lưỡi vàng HDR, viền hull mực dày 0,03, chuôi vàng đậm; trail **0,15 s**. Mỗi lần cắm có **sao vàng 90 ms**, vòng nhỏ, tia lửa và dấu đất đồng/nâu vàng có nứt đen. Kiếm giữ kích thước lúc cắm 0,5 s rồi fade 0,5 s và tan bụi vàng. Năm cổng tăng sáng và độ dày trong giây cuối; lịch 30 lần rơi dồn dần đã có được giữ nguyên.

Bằng chứng: sheet sáng/tối **F1–2 cổng**, **F3 bắt đầu rơi**, **F4–7 trail/kiếm cắm/flash sao**, **F8 dấu đất vàng đồng**; impact và mobile đọc được thân kiếm, chắn/chuôi. Không có đất đỏ/tím. Local hold **65 ms**, impulse **0,50**.

## 2. Thần Kiếm Ngự Lôi

Tăng bề rộng glow vàng và alpha viền tím, giữ filament trắng mảnh, nhánh phụ/jitter mỗi 2 frame và giảm độ dày theo mỗi lần nảy. Flash nhỏ tại mỗi mục tiêu giữ tâm đọc được. Tint impact nhẹ giữ **65 ms**; đồng thời sửa tint hiển thị **Shock** của nạn nhân Ngự Lôi để không kéo dài hình đen đặc trong 0,5 s. Thời lượng Shock và logic status không đổi. Reset tint khi pool reuse.

Bằng chứng: sheet sáng/tối **F2–6** đọc được tia vàng/tím; **F2–4** và impact cho thấy chi tiết quái tại điểm trúng, **F7–8** dư âm. Local hold **65 ms**, impulse **0,45**; overlay tối/speed lines ở dưới HUD.

## 3. Phật Nộ Hỏa Liên

Bỏ mesh vòm của FireBloom. Nổ dùng flash trắng-vàng ngắn, **flipbook smoke atlas với shader lửa cuộn**, các lobe bốc lên từ thân hẹp ra tán rộng trong **0,6 s**; vòng sóng lan **R7**, khói/tàn và vùng cháy giữ thời gian gameplay. Flash rim chỉ **66 ms**. Cánh sen có UV theo cánh, gân giữa mảnh màu vàng/cam, mép đỏ sẫm; không còn vạch trắng dạng lưới.

Bằng chứng: sheet sáng/tối **F1–3 cánh/gân**, **F4 ribbon bay**, **F5–6 hai nhịp khối lửa bốc lên và sóng R7**, **F7–8 vùng cháy/fade**; impact/mobile giữ hình quái. Local hold **65 ms**, impulse **1,00**.

## 4. Tích Lịch Nhất Thiểm

LineRenderer là cung cong (độ lệch giữa 2,2 m), width curve dày giữa/vuốt nhọn hai đầu, lõi trắng–viền vàng–mép tím; có tia điện nhỏ tách ở mép. Giữ delay/nở/damage của gameplay; giữ hình nét chém 0,23 s rồi tan đến 0,30 s, phần sau chỉ là hạt điện. Không có texture nứt đá.

Sửa nguyên nhân nét nứt giả: **stencil mask 0x08 (bit 3)** cho các bề mặt năng lượng đã tác giả hóa; ComicInk fullscreen có depth/stencil attachment và bỏ nét depth/normal của nền trong các pixel này. Cánh sen, billow, lightning và flash phản ứng dùng cùng cơ chế; viền shader/hull mực vẫn được vẽ. Phép tính nét mực môi trường được trả về nguyên bản, không lọc trắng toàn cảnh bằng ngưỡng sáng. Không tạo thêm render texture/node mỗi frame.

Bằng chứng: sheet sáng/tối **F2 điện quanh người, còn model**, **F3–4 dash/camera**, **F5 chờ nét chém**, **F6 cung nhọn lõi sạch**, **F7 hạt điện**, **F8 rãnh đất**. Camera gameplay được xoay yaw250 sau dash để thấy cung, vẫn distance3,4/FOV60/pitch14. Local hold **65 ms**, impulse **0,75**.

## 5. Phản ứng P11 — đủ 4 mục review

**VFX phản ứng:** dùng material/render order ưu tiên và reset trên pool reuse. Băng Lôi Liệt làm mờ/co nhẹ gai băng quanh vùng phản ứng trong **0,55 s**, vỏ băng trên mục tiêu fade **80 ms** rồi văng mảnh cyan, flash nâng tại thân quái và tia vàng-tím lan **3 m**. Điện Lưu dùng bốn arc nhóm và điện bám thân, giữ nguyên gameplay Shock/splash; giảm arc hình để giữ pool. Bạo Viêm có khối lửa cuộn, vòng **R4** và tàn; Tụ Sát có ba vòng tím co siết/flash đỏ nổi trên Hắc Động + Hỏa Liên; Phá Giáp có mảnh vàng/xám và icon khiên nứt trong ArmorBreak **8 s**.

Tự soi năm sheet sáng/tối: Băng **F1 vỏ/gai → F2–4 mảnh cyan, tia 3m và quái lộ ra**; Điện **F2–5 arc/người bị Shock**, khác tia chiêu gốc; Viêm **F2–5 flash/billow/vòng R4**; Tụ **F1 chiêu nền → F2–5 vòng tím siết trước chiêu nền**; Giáp **F2–4 mảnh vàng/xám, F8 icon còn giữ**. Các impact/mobile cùng thư mục. **15 capture / 0 pool exhaustion**, hai combo kỹ năng thật đều phát event.

**Thẻ gợi ý:** THỦY/WATER co vừa vùng 46×38 trong vòng hệ 62 px, autosize và margin; đủ dấu VI/EN. **Số phản ứng:** scale **1,45×**, riêng Tụ Sát **1,55×**, mặt trắng/viền màu phản ứng/underlay đen, khoảng cột 145 và hàng 64; gộp/stagger cũ giữ nguyên. [12 quái/ba phản ứng](../p11/screens/fix3/crowd-three-reactions.png) cho thấy burst ×4 và số phản ứng khác số thường. **TƯƠNG SINH!** có burst giấy/viền mực, punch/fade cùng phong cách nhãn phản ứng: [PC](../p11/screens/fix3/generation-3-Vietnamese-PC-1920.png), [mobile](../p11/screens/fix3/generation-3-Vietnamese-Mobile-1920.png).

[Thẻ THỦY PC](../p11/screens/fix3/hint-bang-loi-liet-Vietnamese-PC-1920.png), [mobile](../p11/screens/fix3/hint-bang-loi-liet-Vietnamese-Mobile-1920.png). **ComicTextAudit P11: 73 màn / 0 issues**, bao gồm crowd và 5 thẻ + 0/1/2/3 chấm × VI/EN × PC/mobile × 720p/1080p. [Raw audit](../../Artifacts/Reactions/fix3/Visual.json), [hạt/pool từng capture](../../Artifacts/Reactions/fix3/Visual-Vfx.json). Sửa thêm Count/Key của ItemBar khi audit phát hiện truncation: inset/height/autosize/Unicode × và cập nhật mode PC/mobile; không đổi cơ chế dùng vật phẩm.

## Kiểm thử cuối

Mỗi suite chạy trong Play session mới trên bản cuối. **14 suite đạt; 1 FAIL cũ PhantomDecoy**, không có FAIL mới. ReactionPlayTest kiểm mechanics/ICD/chain/save/pool; SkillSet1/Pool/Edge kiểm damage/timing/range/rank/mobile, 10 cast, budget/GC, camera và cleanup. ComicTextAudit P10 **22 màn / 0 issues**; tổng hai audit **95 màn / 0 issues**. [Tổng hợp fresh](../../Artifacts/Skills/fix3/Regressions.json).

| Suite | Kết quả | Bằng chứng |
|---|---|---|
'''+ '\n'.join(testrows)+'''

PhantomDecoy giữ lỗi qua Entrance E đã có trước P10: `doorCast/stairCast/descendedStair=true`, `crossedEntrance/openedEntrance=false`; đối chiếu đủ 5 cờ với baseline fix2, không gọi FAIL này là PASS.

## FPS native mới / pool

Windows standalone Mono release, **1920×1080**, '''+pc['gpu']+' / '+pc['cpu']+''', PC_RPAsset, HDR/Bloom/ComicInk/HUD và kiếm thụ động bật. Warmup trước sampling, 240 mẫu idle và 3 cast/chiêu, baseline Tích Lịch ghép hai góc origin/destination theo số frame active. Không gọi screenshot/MCP trong sampling. [Raw 4 chiêu](../../Artifacts/Skills/fix3/Performance-Player.json), [lệnh/SHA-256 assembly](../../Artifacts/Skills/fix3/Native-run.json).

| Chiêu | Idle FPS | Cast FPS | Giảm | Mean idle/cast ms | P95 cast ms | Peak PC/mobile | Objects 10 cast | VFX GC/frame |
|---|---:|---:|---:|---:|---:|---:|---|---:|
'''+ '\n'.join(rows)+f'''

Cả 4 chiêu giảm **≤10% theo median FPS**, cùng phương pháp suite đang có; pool không cạn, không tăng object sau 10 cast. Riêng Tích Lịch mean frame time tăng **13,27%** (21,819→24,715 ms), dù median FPS giảm 1,42%; bảng giữ cả mean/P95 để thể hiện dao động frame, không kết luận mọi frame đều nằm trong 10%. GC chỉ vòng VFX LateUpdate, không phải toàn game. Peak lấy max của Main/Pool/Visual/Player, không chọn riêng lượt nhẹ nhất.

P11 crowd native, 10 nhóm Băng Lôi Liệt+Bạo Viêm+Tụ Sát đồng thời / 12 quái: **{rp['idleFPS']:.2f} → {rp['activeFPS']:.2f} FPS**, giảm **{rp['lossPercent']:.2f}%**, mean **{rp['meanIdleMs']:.3f} → {rp['meanActiveMs']:.3f} ms**, P95 **{rp['p95Ms']:.3f} ms**, {rp['samples']} mẫu; object **{rp['objectsBefore']}→{rp['objectsAfter']}**, cạn **{rp['exhausted']}**, peak **{rp['peakParticles']}**, VFX GC **{rp['maxVfxFrameGCBytes']} B**. [Raw P11](../../Artifacts/Reactions/fix3/Performance-Player.json), [lệnh/assembly](../../Artifacts/Reactions/fix3/Native-run.json).

Hai benchmark dùng URP StandardRequest render **toàn bộ camera stack vào GPU target HDR**, D3D12 fence hoàn tất mỗi frame; HUD Overlay được route sang camera chỉ trong benchmark. Số frame render: **P10 {pc['renderedFrames']} / P11 {rp['renderedFrames']}**. FPS này đo chi phí frame CPU/GPU trên máy hiện tại, **không gồm display Present**, không quy thành FPS desktop hoặc Android. Lượt vượt ngưỡng (nếu có) được giữ riêng; kết quả cuối dùng đầy đủ 4 chiêu cùng bản build, không chắp trial tốt nhất từ nhiều lượt.

## Ảnh cuối và bàn giao

Đã trực tiếp soi 8 sheet P10, 10 sheet P11, impact/mobile, crowd và thẻ/Tương Sinh; ảnh gốc là capture Unity, sheet chỉ ghép/ghi frame, impact cận chỉ crop, không tô lại. Camera gameplay/EnemyPool thật; kiếm thụ động chỉ ẩn trong QA và khôi phục, benchmark bật lại. Checklist đọc sáng/tối, bốn nhịp, impact, hold/impulse, dư âm, popup và che quái <0,3 s nằm trong PROGRESS.

| P10 | Sheet sáng 8F | Sheet tối 8F | Impact cận | Mobile |
|---|---|---|---|---|
'''+ '\n'.join(photos)+'''

| P11 | Sheet sáng 8F | Sheet tối 8F | Impact cận | Mobile |
|---|---|---|---|---|
'''+ '\n'.join(reactionphotos)+'''

Raw frame ở `task/p10/screens/fix3/frames/` và `task/p11/screens/fix3/frames/`; impact-light/dark giữ đầy đủ HUD. Tài nguyên mới là code/mesh/shader procedural của project, dùng atlas/âm thanh/font đã có; [LICENSES](../../Assets/Skills/Core/LICENSES.md), không tải asset ngoài, không tạo lại icon.

Giữ lượt chưa đạt trong `Artifacts/Skills/fix3/visual-before-billows-soft-hit/` và `Artifacts/Reactions/fix3/before-itembar-audit-fix/` để đối chiếu. Không dùng raw FAIL cạn pool/chữ cắt của lượt cũ làm bằng chứng đạt. Tiến trình capture/test bị ngắt khi đổi script đã chạy lại fresh trên bản cuối.

Trạng thái bàn giao: **Android active, Edit Mode, SampleScene, không compiling/updating, 0 lỗi Console**. [Editor state](../../Artifacts/Skills/fix3/FinalEditorState.json), [Console lỗi](../../Artifacts/Skills/fix3/FinalConsoleErrors.json), [Console đầy đủ](../../Artifacts/Skills/fix3/FinalConsoleAll.json); không tuyên bố warning bằng 0. Log native còn thông báo khởi tạo agent chưa có NavMesh, D3D12 info queue, DOF/Panini inactive bị strip và shadow atlas tự hạ độ phân giải; không có exception hay lỗi compile shader mới. Fixture tạm dừng AI/combat của quái để so chi phí VFX, không đo toàn trận đấu có AI. HUD trong GPU snapshot chịu ComicInk do route sang camera; ảnh QA gameplay dùng Overlay HUD bình thường. Chưa đo FPS/hình trên thiết bị Android thật; mobile ở đây là control mode/mức VFX trong Editor.

Tái kiểm: `python Tools/p10_fix3_run_visuals.py`; `python Tools/p11_fix3_finish.py ReactionVisualCapture`; `python Tools/p10_fix3_tests.py`; build qua `p10_fix3_build.cs` / `p11_fix3_build.cs`, chạy `p10_fix3_native.ps1` / `p11_fix3_native.ps1`, trả Android. `python Tools/fix3_verify.py` đã đạt trước khi script ghi báo cáo này.
'''
Path('task/p10/REPORT-P10-fix3.md').write_text(text,encoding='utf-8')
print('Wrote task/p10/REPORT-P10-fix3.md after successful verification.')
