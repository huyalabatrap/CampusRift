from pathlib import Path
import shutil, hashlib, json, datetime
root=Path.cwd(); out=root/'task/p23'; out.mkdir(exist_ok=True)
stamp=datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
backup=root/('Backups/P23-pre-'+stamp); backup.mkdir()
folders=['Assets','Content','Packages','ProjectSettings','UserSettings','Tools','task','Artifacts']
manifest=[]
for folder in folders:
    src=root/folder
    shutil.copytree(src,backup/folder)
    for p in src.rglob('*'):
        if p.is_file(): manifest.append({'path':p.relative_to(root).as_posix(),'hash':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size})
save=Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift')
if save.exists():
    shutil.copytree(save,backup/'UserSave')
    for p in save.rglob('*'):
        if p.is_file():manifest.append({'path':'UserSave/'+p.relative_to(save).as_posix(),'hash':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size})
for name in ['KE_HOACH_V2_HOC_DE_THANG.md','GAME_DESIGN_PLAN.md']:
    shutil.copy2(root/name,backup/name)
(out/'BACKUP.txt').write_text(str(backup),encoding='utf-8')
(out/'pre-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(out/'PROGRESS.md').write_text('''# P23 — nhật ký tiếp quản · 04/10/2026

Đã đọc đầy đủ PROMPT-P23, TEST-POLICY, đặc tả P23/§11.5/§16/§18, các REPORT P17–P22, STABILIZE và báo cáo điều phối. Trước khi bắt đầu không có PROGRESS/REPORT P23, chỉ có brief và log runner 61 byte.

## Trạng thái / việc tiếp theo
- [x] Kiểm tra disk, Unity Android/Edit/SampleScene idle; lưu settings/play/input gốc. Sao lưu toàn bộ Assets/Content/Packages/ProjectSettings/UserSettings/Tools/task/Artifacts và save trước sửa; xem BACKUP.txt + pre-manifest.json.
- [ ] T01: tìm dữ liệu telemetry người thật; công cụ tổng hợp + Balance-Final (⏳ nếu chưa có dữ liệu).
- [ ] T02: gói duyệt P23 gồm P20 và tên P22; tự soát §11.5; biên bản ⏳ giảng viên.
- [ ] T03: phụ đề/3 cỡ chữ/bảng màu và ký hiệu; timer quiz/bia ×1,5, exam giữ nguyên; Settings/persist/audit.
- [ ] Polish đợt2: Linh Bia, mũi tên Indoor, Long Vương, Learning fixture; trước/sau.
- [ ] T04: mẫu nhanh Editor + Windows; hướng dẫn APK/Android ⏳.
- [ ] T05: mỗi suite chức năng P01–P22 một lượt; raw cách ly, bảng PASS/FAIL, sửa lỗi game thật, giữ baseline Shelter40.
- [ ] T06: 4 build, Windows release Sảnh→màn1; archive Releases và dự án; LICENSES/hash/ghi chú.
- [ ] Audit bảo toàn/save/settings, cập nhật checkbox/README, Android/Edit/SampleScene/Console0; chỉ viết REPORT khi xong.

Không chạy bot balance/50trial/ma trận ảnh. P23 cho phép riêng hồi quy toàn bộ và mẫu hiệu năng PC ngắn. Không công bố store/mạng. Mốc6 vẫn chờ dữ liệu người chơi/giảng viên/Android thật.
''',encoding='utf-8')
print(json.dumps({'backup':str(backup),'files':len(manifest),'bytes':sum(x['bytes'] for x in manifest)}))
