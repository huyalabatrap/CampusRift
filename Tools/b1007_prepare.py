from pathlib import Path
import json, shutil
root=Path.cwd(); backup=root/'Backups/Regression-APK-pre-20261006'
backup.mkdir(parents=True,exist_ok=True)
for folder in ['Assets/ARRift','Assets/Controls','Assets/CampusRiftUI/Validation','ProjectSettings','Docs']:
    if not (backup/folder).exists(): shutil.copytree(root/folder,backup/folder)
snap=json.loads((root/'task/batch-1007/7-original-editor.json').read_text(encoding='utf-8'))['data']['result']
if not (backup/'save').exists(): shutil.copytree(snap['savePath'],backup/'save')
shutil.copy2(root/'task/batch-1007/7-original-editor.json',backup/'editor.json')
with (root/'task/batch-1007/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n# Job 7 — Regression / APK — 2026-10-06\n## Mốc 1: tiếp quản, đọc và sao lưu\n- Đã đọc PLAN, toàn bộ brief7, REPORT1–6, PROGRESS; chưa có kết quả hồi quy/REPORT7 trên disk. Đã xem ảnh mode, Kết Ấn, rồng, Tri Thức và Luyện Ấn.\n- Unity Android/Edit/SampleScene không dirty. Snapshot7-original-editor.json; backup Backups/Regression-APK-pre-20261006 gồm nguồn dự kiến sửa, Docs, ProjectSettings và save.\n- Không sửa run-*.ps1/accounts. Mỗi suite một lượt, chỉ các mục fail được chạy lại một lần sau sửa; không thêm test mới. Chuẩn bị sửa HUD theo brief trước hồi quy.\n')
print(backup)
