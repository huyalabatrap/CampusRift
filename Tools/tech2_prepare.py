from pathlib import Path
import json,shutil,urllib.request,zipfile,hashlib
root=Path.cwd(); backup=root/'Backups/AR-Tech2-pre-20261006'
backup.mkdir(parents=True,exist_ok=True)
for name in ['Assets/ARRift','Assets/Plugins/Android','Assets/Skills/SwordRain','Assets/Skills/IceSeal','Assets/Skills/ChainLightning','Assets/Skills/GiantHandSeal']:
    src=root/name
    if src.exists() and not (backup/name).exists():shutil.copytree(src,backup/name)
snapshot=json.loads((root/'task/batch-1007/6-original-editor.json').read_text(encoding='utf-8'))['data']['result']
if not (backup/'save').exists():shutil.copytree(snapshot['savePath'],backup/'save')
with (root/'task/batch-1007/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n# Job 6 — AR Tech2 — 2026-10-06\n## Mốc 1: tiếp quản / sao lưu\n- Đọc PLAN/brief6, REPORT2–5, đề xuất4.2/4.3/6 và GóiA; Job6 chưa có source/report. Không có AGENTS.md áp dụng.\n- Unity Android/Edit/SampleScene không dirty. Snapshot6-original-editor.json; backup Backups/AR-Tech2-pre-20261006 gồm AR/native/shared skills dự kiến sửa/save.\n- Không chạy/viết test/harness/hồi quy/kiểm gameplay nhỏ/APK; chỉ compile và ảnh pose. Không sửa run-*.ps1/accounts.\n- Thiết kế: contract2tay theo batch + association/D1 riêng; depth gate/budget10ray; VoskVN local keyword mặc địnhtắt; MediaProjection explicit consent/MediaStore; probe có switch; giaiđoạn3 chỉ tài liệu.\n')
print('Backup and milestone saved')
