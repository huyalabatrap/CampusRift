"""Close progress only after the guarded fix2 report has been written."""
from pathlib import Path
import json,re
report=Path('task/p10/REPORT-P10-fix2.md'); assert report.is_file()
perf=json.loads(Path('Artifacts/Skills/fix2/SkillSet1-Performance-Player.json').read_text(encoding='utf-8-sig'))
assert len(perf['trials'])==7 and all(x['withinTenPercent'] for x in perf['trials'])
progress=Path('task/p10/PROGRESS-fix2.md'); s=progress.read_text(encoding='utf-8')
s=s.replace('# P10 fix2 — Tiến độ\n', '# P10 fix2 — Tiến độ\n\n**Trạng thái cuối 02/10/2026: hoàn tất fix2.** [REPORT-P10-fix2.md](REPORT-P10-fix2.md) trả lời từng mục review và liên kết bằng chứng mới. Các đoạn “đang/chờ/chưa đạt” bên dưới giữ làm lịch sử thử/sửa; kết quả cuối nằm ở cuối file.\n',1)
s=s.replace('ảnh cũ screens không đổi','ảnh chiêu cũ trong screens không đổi')
s+='''
### Chốt fix2 — 06:08, 02/10/2026

- Main269/0, Pool121/0, Edge29/0, ComicTextAudit22screens/0issues;10suite hồi quy fresh không có FAIL mới. HubFlow42/0 và HubLayout24/0. Phantom rawFAIL đúng baseline EntranceE, được nêu riêng trong report, không đổi thànhPASS.
- Native Windows64 build Succeeded635392498bytes; PC1920×1080, PC_RPAsset, Bloom0,25/HDR/ComicInk/PostProcessing đều true. Bảy trial3cast đềuPASS, giảmFPS lần lượt2,88%;2,16%;0,47%;5,88%;2,83%;3,40%;6,30%. Max6,30% ở Vạn Kiếm <10%; raw fix1 giữ trong backup, raw fix2 đóng băng trong Artifacts/Skills/fix2/.
- Peak max qua Main/Pool/Visual/Player PC/mobile:350/125,134/75,470/146,261/87,48/8,172/86,277/109. Mỗi chiêu10PCcast1122→1122objects;0exhaustion,0B VFX LateUpdateGC, finite/naturallyidle. Không nói toàn bộ game0GC; chưa đo FPS Android vật lý.
-14sheet light/dark+7impact crop+14impact đủHUD+7mobile+112frame gốc/Bell-expiry đã tự soi.22ảnh UI audit mới sao lưu riêng screens/fix2/ui/. Ảnh kỹ năng cũ giữ để so sánh. Checklist từngchiêu ở trên và từngmục review trong REPORT-P10-fix2.
- Editor cuối Android, SampleScene, Stop, compiling/updating=false; console sau refresh0error. Console trướcclear/Playerlog giữ warning thật; không sửa xóa rawFAIL/JSON lịch sử.
-16file dữ liệu/config/catalog/rank/base-runtime giốngSHA256trướcfix2. Giấy phép nguồn/reuse/procedural bổ sung Core/LICENSES.md. Master PROGRESS/README/P10spec gắn report mới; các task/checkbox P10 vốn hoàn tất giữ trạng thái✅.

Không còn hạng mục fix2 đang chờ. Giới hạn còn lại: traversal Phantom đã lỗi trướcP10, không đo Android vật lý; đây không phải mục review VFX chưa sửa.
'''
progress.write_text(s,encoding='utf-8')
master=Path('task/p10/PROGRESS.md'); s=master.read_text(encoding='utf-8')
s=s.replace('**Fix2 đang được thực hiện theo yêu cầu mới từ04:49, 02/10/2026.** Xem [PROGRESS-fix2.md](PROGRESS-fix2.md). Báo cáo/trạng thái chốt fix1 bên dưới là lịch sử trước vòng sửa này; chưa kết luận fix2 đạt.', '**Fix2 hoàn tất 02/10/2026.** Xem [REPORT-P10-fix2.md](REPORT-P10-fix2.md) và [PROGRESS-fix2.md](PROGRESS-fix2.md): Main269/0, Pool121/0, Edge29/0, UI22/0, HubFlow42/0, HubLayout24/0; native PC max giảmFPS6,30%,0B VFX GC, pool1122→1122. Báo cáo/trạng thái fix1 bên dưới giữ làm lịch sử.')
master.write_text(s,encoding='utf-8')
old=Path('task/p10/REPORT-P10.md'); s=old.read_text(encoding='utf-8')
s=s.replace('Fix2 đang tiếp tục theo brief mới; xem [PROGRESS-fix2.md](PROGRESS-fix2.md).','Fix2 đã hoàn tất; xem [REPORT-P10-fix2.md](REPORT-P10-fix2.md).')
old.write_text(s,encoding='utf-8')
spec=Path('task/P10-ky-nang-dot-1.md'); s=spec.read_text(encoding='utf-8')
s+='\n## Kiểm chứng vòng sửa fix2\n\n02/10/2026: xem [REPORT-P10-fix2](p10/REPORT-P10-fix2.md) và [PROGRESS-fix2](p10/PROGRESS-fix2.md). Main269/0, Pool121/0, Edge29/0, HubFlow42/0, HubLayout24/0; không có FAIL hồi quy mới. PC giảmFPS tối đa6,30%; pool1122→1122 và0B VFXGC/frame.14sheet mới sáng/tối cùng impact/mobile trong p10/screens/fix2/.\n'
spec.write_text(s,encoding='utf-8')
readme=Path('task/README.md'); s=readme.read_text(encoding='utf-8')
s+='\nP10: vòng sửa VFX fix2 đã hoàn tất02/10/2026; [báo cáo và bằng chứng](p10/REPORT-P10-fix2.md). Trạng thái P10✅ giữ nguyên sau Main/Pool/Edge/UI, HubFlow/HubLayout và benchmark PC mới.\n'
readme.write_text(s,encoding='utf-8')
s=report.read_text(encoding='utf-8').replace('Ảnh cũ giữ nguyên trong `screens/`','Ảnh kỹ năng cũ giữ nguyên trong `screens/`')
s=s.replace('và [Player log](../../Artifacts/Skills/fix2/P10-Player.log) giữ cảnh báo thật', ', [console trước clear](../../Artifacts/Skills/fix2/Console-before-final-clear.json) và [Player log](../../Artifacts/Skills/fix2/P10-Player.log) giữ cảnh báo thật')
s=s.replace('/0failed','/ 0 failed').replace('/0issues','/ 0 issues').replace('Unity0lỗi','Unity 0 lỗi').replace('|0B|','| 0 B |')
s=s.replace('Lượt Edge mới phải xác nhận có giao cắt','Lượt Edge mới PASS xác nhận có giao cắt')
s=s.replace('Checklist có/không theo từng chiêu', '[22 ảnh UI audit](screens/fix2/ui/) cũng được lưu riêng. Checklist có/không theo từng chiêu')
report.write_text(s,encoding='utf-8')
missing=[]
for doc in (report,progress,master,old,spec,readme):
    for target in re.findall(r'\]\(([^)]+)\)',doc.read_text(encoding='utf-8')):
        if target.startswith(('http','app:','#')): continue
        target=target.split('#')[0]
        if not (doc.parent/target).exists(): missing.append(str(doc)+': '+target)
print('Docs closed; missing links:',missing)
assert not missing
