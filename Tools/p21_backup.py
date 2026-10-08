from pathlib import Path
import hashlib, json, shutil, datetime
root=Path.cwd()
out=root/'Backups'/('P21-pre-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'))
folders=['Assets/SkyBeast','Assets/GameReadyDragons','Assets/Audio','Assets/Scenes','Assets/CampusRiftUI/Runtime','Assets/Levels/Runtime','Assets/Progression/Runtime','ProjectSettings']
manifest={}
for folder in folders:
    src=root/folder
    if not src.exists(): raise RuntimeError(folder)
    shutil.copytree(src,out/folder)
    for p in src.rglob('*'):
        if p.is_file(): manifest[p.relative_to(root).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
protected={}
for folder in ['Assets/Scenes','Assets/GameReadyDragons','Assets/Levels/Data','Assets/Resources/P12','Assets/Resources/P13','Assets/Resources/P14','Assets/SkyBeast/Resources/P12','Assets/SkyBeast/Resources/P13','Assets/SkyBeast/Resources/P14']:
    for p in (root/folder).rglob('*'):
        if p.is_file():protected[p.relative_to(root).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
for p in (root/'Assets').rglob('*'):
    if p.is_file() and ('navmesh' in p.name.lower() or p.suffix.lower()=='.fbx'):protected[p.relative_to(root).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
for folder in ['Artifacts/SkyBeast','task/p21']:
    if (root/folder).exists():shutil.copytree(root/folder,out/folder)
save=Path.home()/'AppData/LocalLow'
savefiles={}
(out/'UserSave').mkdir(parents=True,exist_ok=True)
for p in save.rglob('campusrift-v2.json'):
    base=p.parent
    for q in base.iterdir():
        if q.is_file():
            dest=out/'UserSave'/q.name;shutil.copy2(q,dest);savefiles[str(q)]=hashlib.sha256(q.read_bytes()).hexdigest()
(root/'task/p21/BACKUP.txt').write_text(str(out),encoding='utf-8')
(root/'task/p21/pre-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(root/'task/p21/protected-manifest.json').write_text(json.dumps(protected,indent=2),encoding='utf-8')
(root/'task/p21/user-save-manifest.json').write_text(json.dumps(savefiles,indent=2),encoding='utf-8')
(root/'task/p21/PROGRESS.md').write_text('''# P21 — PROGRESS · 04/10/2026

## M0 · tiếp quản và đọc bối cảnh
- Đã đọc toàn bộ PROMPT-P21, P21-cu-thu-dien-anh, TEST-POLICY, P14/P14fix1, P15/P15fix1, P12 handoff, PERF-FIRE, STABILIZE và polish-notes; §4.1/5.3/12 kế hoạch V2.
- Không có P21 PROGRESS/REPORT hoặc triển khai trước. Các log chain chỉ là lần khởi động. Unity SampleScene/Edit Mode.
- STABILIZE đã xử lý camera campus và impact bạc màu; giữ những sửa đó. Giữ model020/023/026, animation/rig/LOD, geometry/NavMesh và dữ liệu combat.
- Sao lưu đầy đủ trước sửa: BACKUP.txt; pre-manifest/protected-manifest/user-save-manifest ghi SHA256. Sao lưu thêm runtime Level/Progression, ProjectSettings và evidence SkyBeast cũ.

## Việc còn lại
- T01 accents nhận diện theo socket; T02 reveal020 7s/repeat/skip/VN-EN.
- T03 Timeline camera8/9/10 và finale dài hơn; giữ luật P15, pause cục bộ.
- T04 dawn20s/credits từ tất cả LICENSES/title Phá Rift; T05 nhạc riêng/pha/mixer/duck.
- T06 harness gọn + HeavenSword/SkyBeast mỗi1lượt; capture/tự soi; audit và Android/Edit/SampleScene/Console0error.
''',encoding='utf-8')
print(out, len(manifest), 'backed up;',len(protected),'protected;',len(savefiles),'user save files')
