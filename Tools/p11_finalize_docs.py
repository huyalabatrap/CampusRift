"""Publish completion only after raw artifact guards and final Editor checks pass."""
import json,pathlib,runpy,shutil
runpy.run_path('Tools/p11_verify_artifacts.py')
def read(path):return json.loads(pathlib.Path(path).read_text(encoding='utf-8-sig'))
state=read('Artifacts/Reactions/FinalEditorState.json')
assert state['target']=='Android' and not any(state[k] for k in ('playing','compiling','updating'))
assert state['scene']=='Assets/Scenes/SampleScene.unity' and 'Succeeded' in state['build']
console=read('Artifacts/Reactions/FinalConsole.json')
assert console['success'] and console['data']==[]
reg=read('Artifacts/Reactions/Regressions.json');perf=read('Artifacts/Reactions/Performance-Player.json')
visual=read('Artifacts/Reactions/Visual.json');qa=read('Artifacts/Reactions/Validation.json')
report_path=pathlib.Path('task/p11/REPORT-P11.md');report=report_path.read_text(encoding='utf-8')
report=report.replace('Báo cáo đang hoàn tất kiểm chứng cuối; chưa đánh dấu phase ✅.','**P11 hoàn tất: T01–T04 ✅.** Kiểm chứng mechanics, hình ảnh, hồi quy và hiệu năng dưới đây đã có artifact thực.')
report=report.replace('| Băng Lôi Liệt | 463 | 146 | 0 |','| Băng Lôi Liệt | 459 | 146 | 0 |').replace('| Tụ Sát | 299 | 153 | 0 |','| Tụ Sát | 299 | 152 | 0 |')
report=report.replace('khoảng cách125 ở hệ tọa độ1080p','khoảng cách 125 ở hệ tọa độ 1080p')
old='Chờ ghi kết quả 13 suite hồi quy còn lại và phép đo native cuối sau chỉnh spacing; raw artifacts giữ trong [Artifacts/Reactions](../../Artifacts/Reactions/).'
rows=[]
for r in reg:
    result=r['result'].strip().replace('|','/')
    if r['baselineKnownFail']:result='FAIL cũ, khớp nguyên baseline P10/fix2; không có FAIL mới'
    rows.append('| '+r['test']+' | '+result+' | [raw](../../'+r['report'].replace('\\','/')+') |')
new='''**Hồi quy: 13 suite PASS, 1 FAIL đã có từ P10/fix2, 0 FAIL mới.** Mỗi suite chạy trong Play session mới; giữ riêng kết quả cũ tại `Artifacts/Reactions/pre-regression-baseline`. PhantomDecoy vẫn `doorCast=true`, `crossedEntrance=false`, `openedEntrance=false`, `stairCast=true`, `descendedStair=true`, khớp cả năm cờ baseline. Không tính FAIL này thành PASS.

| Suite | Kết quả cuối | Artifact |
|---|---|---|
'''+ '\n'.join(rows)+'''

**PC native: PASS**, mã cuối sau sửa spacing. Windows standalone Mono release, i9-12900H / RTX 3060 Laptop, PC_RPAsset, 1920×1080 HDR, Bloom/ComicInk/post-processing bật; camera gameplay, HUD và kiếm thụ động, 12 quái thật. Sau warmup, đo 10 nhóm **Băng Lôi Liệt + Bạo Viêm + Tụ Sát đồng thời**, không gọi screenshot/MCP trong mẫu. Median **58,74 → 55,89 FPS**, giảm **4,85%** (ngưỡng 10%); mean frame **16,789 → 17,649 ms**, p95 active **19,735 ms**, 630 mẫu active. Pool **1301 → 1301**, cạn **0**, peak particles **293**, max VFX-frame GC **0 B**. [Raw số đo](../../Artifacts/Reactions/Performance-Player.json), [arguments + SHA256 assembly](../../Artifacts/Reactions/Native-run.json), [ảnh crowd từ GPU target](../../Artifacts/Reactions/Player-crowd-render.png).

Phương pháp: player chạy ẩn nên vòng render màn hình thông thường không đáng tin. Helper **chỉ trong benchmark** dùng URP `StandardRequest` render toàn bộ camera stack vào GPU target HDR 1920×1080; đợi GPU fence D3D12 hoàn tất mỗi frame (1.310 frame render). HUD Overlay được đưa sang ScreenSpaceCamera để có mặt trong GPU target. Vì thế HUD của ảnh benchmark chịu ComicInk khác ảnh gameplay; **FPS này không gồm display Present**, đo chi phí frame CPU/GPU của cấu hình PC trên máy này, không tuyên bố FPS cửa sổ desktop hay Android. Các lượt render ẩn không có GPU, batch không render, fence D3D11 không hỗ trợ đều lưu riêng và bị loại khỏi kết quả. Lượt hợp lệ trước spacing cũng giữ riêng; dùng lượt cuối 00:47:22Z, không chọn lượt tốt nhất.

Trạng thái cuối: SampleScene, Android active, dừng Play, không compiling/updating, **0 lỗi Console**. [Editor state](../../Artifacts/Reactions/FinalEditorState.json), [Console lỗi](../../Artifacts/Reactions/FinalConsole.json). Script `Tools/p11_verify_artifacts.py` kiểm tra freshness ảnh, 108 QA, 73 audit, 14 suite và render/pool trước khi script chốt tài liệu cho phép đánh dấu ✅.'''
assert old in report
report=report.replace(old,new)
report=report.replace('Lỗi PhantomDecoy traversing door đã có từ P10/fix2 cần so đúng baseline;','Lỗi PhantomDecoy traversing door đã có từ P10/fix2, đã đối chiếu khớp baseline;')
report_path.write_text(report,encoding='utf-8')
progress=pathlib.Path('task/p11/PROGRESS.md')
shutil.copy2(progress,'Artifacts/Reactions/PROGRESS-before-completion.md')
progress.write_text('''# P11 — Tiến độ hoàn tất

02/10/2026 · **T01–T04 ✅**. Đọc đầy đủ prompt P11, đặc tả và P10 REPORT/REPORT-fix2/PROGRESS/SURVEY/PROMPT-fix1 trước sửa. Lúc bắt đầu P11 chưa có triển khai hay PROGRESS/REPORT. Sao lưu trước sửa tại `Backups/P11-pre-20261002-061521/`, bổ sung prefab người chơi thật trước cài đặt. Nhật ký các lượt trung gian giữ tại `Artifacts/Reactions/PROGRESS-before-completion.md`.

Tham chiếu GAME_DESIGN_PLAN §7.2/§7.5 là sự kiện đêm/tòa nhà; số liệu phản ứng nằm trong KE_HOACH_V2_HOC_DE_THANG §7.2/§7.5 và đặc tả P11. Wet/Stun/ArmorBreak/Pulled đã có, không thêm enum mới. Kim Thân P19 chưa có; đã đặt hook phá phụ tố.

- **T01:** config Reactions.asset, resolver trước phòng thủ, năm phản ứng đúng số liệu, ICD mỗi loại/mục tiêu 1 s, nguồn Reaction chống đệ quy, reset khi pool reuse. Đại Thủ Ấn gây Stun lúc impact để có nguồn Phá Giáp thực; boss giữ cap 1 s.
- **T02:** commit cast thành công, ba hệ theo Generates trong 6 s, snapshot damage ×1,30 và +20 Linh Lực; bonus không tăng shield. HUD countdown, màu tiếp theo, chớp/chữ/motes; owner trên player giữ cập nhật khi thanh PC ẩn trên mobile.
- **T03:** burst gradient, punch/hold/fade, merge 0,2 s; số phía dưới và tick muộn không vượt nhãn. Mobile sáu cột cách 125, đọc được số bốn chữ số. Năm VFX hai lớp dùng pool P10 112 node, 11 OGG Kenney CC0 / ba lớp âm, local hold 75–80 ms, camera impulse, ReduceSkillFlashes, thẻ 3 s lưu vào profile.
- **T04:** ReactionPlayTest **108/0**, ComicTextAudit **73/0**, 15 capture không cạn pool; hai combo yêu cầu chạy runtime thật trong SampleScene. 14 suite: **13 PASS + 1 FAIL cũ PhantomDecoy khớp P10/fix2**, không FAIL mới. Native PC giảm **4,85%**, pool **1301 → 1301** sau 10 nhóm ba phản ứng; full GPU HDR/Bloom/ComicInk, không tính display Present. Console 0 lỗi; Editor dừng tại SampleScene, Android active.

## Tự soi ảnh theo PROMPT-P10-fix1

Đã xem cả 10 sheet sáng/tối, năm impact và năm mobile, cảnh 12 quái, HUD 0/1/2/3 và thẻ. Sheet ghép từ ảnh Unity; impact chỉ crop. Camera gameplay mặc định; quái tiểu yêu thật. Từng tiêu chí dưới đây là **Có**, kèm frame; không dùng audit chữ để thay tự soi.

| Phản ứng | Nền sáng / tối | Bốn nhịp | Impact / hit-stop / impulse | Dư âm đất | Số đọc được / không che toàn bộ quái quá 0,3 s |
|---|---|---|---|---|---|
| Băng Lôi Liệt | Có / Có, F2–F5 | Có: F1 vỏ băng; F2–4 vỡ/tia; F5–7 mảnh/tia; F8 tan gai | Có: impact riêng, F5; 80 ms / 0,70 | Có: frost/mark 2,3 s, F7–8 | Có: số vàng nhiều hàng; flash ngắn. Vỏ Freeze còn trên quái chưa bị Lôi thuộc skill P10 |
| Điện Lưu | Có / Có, F2–F5 | Có: F1 Chill; F2–4 zigzag Shock; F5–7 bolt tan; F8 mark | Có: impact riêng, F5; 75 ms / 0,45 | Có: mark 2,3 s, bolt bám 0,5 s | Có: số tách hàng; đầu/mô hình quái đọc được sau impact |
| Bạo Viêm | Có / Có, F2–F6 | Có: F1 Burn; F2–4 nấm/vòng; F5–6 tàn; F7–8 smoke/mark | Có: impact riêng; 80 ms / 0,75 | Có: smoke 1,3 s / mark 2,3 s | Có: số đỏ/cam outline; nấm trong dần, mô hình còn nhận ra |
| Tụ Sát | Có / Có, F2–F6 | Có: F1 pull; F2–4 vòng siết/flash đỏ; F5–6 vòng tan; F7–8 mark | Có: impact riêng; 80 ms / 0,65 | Có: mark 2,3 s cùng dư âm P10 | Có: số lớn ×1,25 tách cột, tick muộn nằm dưới nhãn; xác nhận lại ảnh mobile cuối. Quái nhìn được qua vòng |
| Phá Giáp | Có / Có, F2–F5 | Có: F1 Stun; F2–4 vỡ vàng/xám; F5–7 mảnh tan; F8 khiên nứt (4,5 s) | Có: impact riêng; 80 ms / 0,60 | Có: mark 2,3 s, icon 8 s | Có: số Kim outline; mảnh không phủ kín nhóm quái kéo dài |

## Sửa từ bằng chứng chưa đạt

Các lượt chưa đạt được giữ trong `Artifacts/Reactions/visual-before-*`: CanvasRenderer/canvas lồng, 125 issues clipping chữ, popup depth, pool cạn 6 node vì shatter băng trùng, burst crowd, HUD mobile ngừng update, tick muộn và số mobile sát nhau. Sửa bằng dùng lại vỏ băng P10, giữ capacity 112; tách owner HUD; dành slot nhãn và reflow popup chung. Capture cuối mới là bằng chứng PASS.

Benchmark ẩn không render / batch không render / fence D3D11 không hỗ trợ đều bị loại và lưu lịch sử; helper GPU chỉ bật trong benchmark. Android chưa đo trên thiết bị thật. P19 chỉ có hook, PhantomDecoy vẫn là lỗi baseline. Số liệu, nguồn asset/giấy phép và toàn bộ ảnh/JSON liên kết trong [REPORT-P11](REPORT-P11.md).
''',encoding='utf-8')
spec=pathlib.Path('task/P11-phan-ung-combo.md')
spec.write_text(spec.read_text(encoding='utf-8').replace('| ⬜ |','| ✅ |').replace('- [ ]','- [x]')+'\nHoàn tất 02/10/2026: [báo cáo, số đo và ảnh kiểm chứng](p11/REPORT-P11.md).\n',encoding='utf-8')
index=pathlib.Path('task/README.md');text=index.read_text(encoding='utf-8');lines=text.splitlines()
for i,line in enumerate(lines):
    if 'P11-phan-ung-combo.md' in line:lines[i]=line.replace('⬜','✅')
index.write_text('\n'.join(lines)+'\n\nP11: hoàn tất 02/10/2026; [báo cáo và bằng chứng](p11/REPORT-P11.md). ReactionPlayTest 108/0, ComicTextAudit 73/0, không FAIL hồi quy mới; native PC giảm 4,85%, pool ổn định.\n',encoding='utf-8')
print('P11 completion documents and phase checkboxes updated after all guards PASS.')
