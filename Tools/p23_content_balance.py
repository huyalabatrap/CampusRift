from pathlib import Path
import json,re,csv,hashlib,html
source=Path('Tools/p20_export_content.py').read_text(encoding='utf-8-sig')
# Same current-source export; new destination preserves all P17/P20 review evidence.
source=source.replace("Path('Artifacts/Content/P20')","Path('Artifacts/Content/P23')").replace('Review-P20-2026-10-04.md','Review-Final-2026-10-04.md').replace('P20 ·','P23 ·').replace('(P20/','(P23/')
exec(compile(source,'P23 current content export','exec'))
out=Path('Artifacts/Content/P23');names=[]
for kind,path,pattern in [('achievement/title','Assets/Progression/Runtime/EndgameService.cs',r'new AchievementDefinition\("([^"]+)","([^"]+)","([^"]+)"'),('costume','Assets/CampusRiftUI/Runtime/PlayerCostume.cs',r'new CostumeDefinition\("([^"]+)","([^"]+)","([^"]+)"')]:
 for identity,vn,en in re.findall(pattern,Path(path).read_text(encoding='utf-8-sig')):names.append(dict(kind=kind,id=identity,name_vn=vn,name_en=en,path=path))
with (out/'p22-names.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=list(names[0]));w.writeheader();w.writerows(names)
packet=out/'review-print.html';s=packet.read_text(encoding='utf-8').replace('</body></html>','<h2>Thành tựu / danh hiệu / trang phục P22</h2>'+''.join('<p><b>'+html.escape(r['kind']+' / '+r['id'])+'</b>: '+html.escape(r['name_vn']+' / '+r['name_en'])+'</p>' for r in names)+'</body></html>');packet.write_text(s,encoding='utf-8')
manifest=json.loads((out/'export-manifest.json').read_text(encoding='utf-8'));manifest.update(p22AchievementsAndTitles=20,p22Costumes=5,files=[{'path':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in out.iterdir() if p.is_file() and p.name!='export-manifest.json']);(out/'export-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'SELF-REVIEW.md').write_text('''# Tự soát P23 · §11.5 — chưa thay thế duyệt giảng viên

- Gói từ Content CSV hiện hành: 6 chương, 21 bài, 93 trang, 449 câu / 6 dạng, 93 thẻ. Mọi nguồn trang có trong manifest; thêm 9 câu P20 được chỉ rõ bằng ID, giữ 440 câu cũ.
- Thẻ lấy từ Ghi nhớ có nguồn, không thưởng sức mạnh bằng thẻ. Quiz / Linh Bia và 3 dạng mới sử dụng nội dung giáo trình nghiêm túc, font đứng; hiệu ứng không phủ phần học.
- Tên 21 kỹ năng, 16 vật phẩm, 5 pháp bảo, quái/cự thú/cảnh giới nằm ở fantasy-names.csv; thêm đúng 20 thành tựu/danh hiệu và 5 trang phục tại p22-names.csv. Không phát hiện tên nhân vật lịch sử hoặc trích dẫn môn học làm tên chiến đấu qua screening thủ công + chuỗi.
- Không thêm hình ảnh nhân vật lịch sử vào combat. Các lớp hư cấu/gameplay và môn học vẫn phân biệt tại màn học, giới thiệu/credits.
- Linh Thạch/Tu Vi thưởng nỗ lực học, không gán sức mạnh siêu nhiên cho tư tưởng/nhân vật lịch sử.
- Nội dung tiếng Việt; UI Việt/Anh. Giữ nguyên dữ liệu học thuật, ID và câu gốc trong P23.
- Chưa có xác nhận giảng viên, chưa chứng nhận tính đúng học thuật/legal. Biên bản cuối để trống chữ ký; không công bố lên store/mạng.

Giảng viên cần đối chiếu từng ID câu/thẻ/trang với bản giáo trình 2021, kiểm đáp án/giải thích và 9 câu mới P20; xác nhận tên hư cấu P22. Góp ý phải có ID, nguồn trang, đề nghị sửa và người xác nhận. Sau sửa: nguồn→CSV→Import Course CSV→Validate Content→xuất gói→build lại.
''',encoding='utf-8')
p=Path('Artifacts/Content/Review-Final-2026-10-04.md');s=p.read_text(encoding='utf-8');s=s.replace('| Ghi công nội dung, hình ảnh, âm thanh | Chờ | |','| 20 thành tựu / danh hiệu + 5 trang phục P22 | Chờ | |\n| Ghi công nội dung, hình ảnh, âm thanh | Chờ | |').replace('Tools/p20_export_content.py','Tools/p23_content_balance.py');s+='\nTên P22: [CSV](P23/p22-names.csv) · [Tự soát AI](P23/SELF-REVIEW.md). Chưa có chữ ký hoặc dữ liệu duyệt người thật.\n';p.write_text(s,encoding='utf-8')
Path('Artifacts/V2/Balance-Final.md').write_text('''# Cân bằng cuối P23 — ⏳ dữ liệu người chơi thật

04/10/2026: chỉ có telemetry smoke P17 trong task/p17; không có gói telemetry người chơi cung cấp hoặc phiên người thật được xác nhận. Không dùng QA/DEV lethal/đồng hồ giả để cân bằng. **Không đổi tham số balance P23; chưa đo thực chiến.**

Gom JSONL tự nguyện từ Settings → Tiện nghi & dữ liệu → Ghi dữ liệu chơi cục bộ. Windows: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Campus Rift/telemetry`; Android xem Perf-Final.md. Chép vào một thư mục riêng, giữ file gốc; chạy `python Tools/p23_telemetry_summary.py <folder> --out Artifacts/V2/Telemetry-Players`.

P23 thêm mode Normal/Tower/Nightmare và towerFloor vào schema1. File cũ không có mode xếp Normal; không thể phân biệt các lượt P22 cũ. Tổng hợp không có PII hoặc mạng. Cần người chơi xác nhận nguồn, bao phủ 10 màn + Tháp + Ác Mộng và các câu học; mẫu thiếu/nhỏ chỉ là tín hiệu, không kết luận.

| Mục tiêu / tham số cần xem | Nguồn điều chỉnh | Dữ liệu cần |
|---|---|---|
| Tiểu Yêu 2–3 nhát ở đúng cảnh giới; thường 2–4s | EnemyArchetype, LevelDefinition.Scaling, PlayerStats/cultivation | Quan sát hit/kill và cảnh giới; JSONL chưa có số nhát |
| Elite10–15s; boss5 60–90s, boss7 90–120s | BossController, EliteAffix / authored LevelData | Thời gian combat boss thật, tránh DEV lethal |
| Thời lượng / deaths / Thiên Hỏa từng màn | Spawn/wave/rest, HP/damage, FireBreathProfile | runs.csv, ghi nguyên nhân kẹt/chết, xem parTime theo màn |
| Câu đúng60–80% | Content/questions.csv; không chỉnh đáp án đúng để dễ | questions.csv + giảng viên xác nhận sửa độ rõ |
| 1 buổi học ≈ 1 lượt thử màn khó | StudyRewards, cultivation thresholds, Economy / giá | Nhật ký buổi học45–60phút + lượt thử; không suy từ QA |
| Tháp +8%/tầng trần×5; cap thưởng100/ngày | EndgameFactory/EndgameService | mode/floor, thời gian, thất bại và cảm nhận người chơi |
| Ác Mộng×1,5, Fire cycle×0,8 | EndgameFactory / FireBreathCycle | Mỗi màn đã mở riêng, build mới, người chơi thật |

Mẫu ghi quyết định: tham số · trước · dữ liệu/bao phủ · lý do · sau · người chấp nhận · phiên kiểm lại. Mốc6 cân bằng cuối giữ ⏳ cho đến khi số nằm mục tiêu hoặc được người phụ trách chấp nhận có lý do.
''',encoding='utf-8')
print('P23 review packet and pending balance prepared')
