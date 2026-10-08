from pathlib import Path
import shutil, datetime, json, hashlib
stamp=datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
root=Path('Backups')/('P10-fix3-pre-'+stamp)
for name in ['Assets/Skills','Assets/Combat','Assets/CampusRiftUI/Runtime','Assets/Scenes','Assets/Characters/SchoolGirl/Prefabs','task/p10','task/p11','Artifacts/Skills','Artifacts/Reactions','Tools']:
    source=Path(name)
    shutil.copytree(source,root/name,ignore=shutil.ignore_patterns('__pycache__','codex-log*'))
data={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for folder in ['Assets/Skills','Assets/Combat/Data'] for p in Path(folder).rglob('*.asset') if '/Data/' in p.as_posix()}
data['Assets/Skills/BlackHole/Runtime/BlackHoleRuntime.cs']=hashlib.sha256(Path('Assets/Skills/BlackHole/Runtime/BlackHoleRuntime.cs').read_bytes()).hexdigest() if Path('Assets/Skills/BlackHole/Runtime/BlackHoleRuntime.cs').exists() else 'find-path'
Path('Artifacts/Skills/fix3').mkdir(parents=True,exist_ok=True)
Path('Artifacts/Reactions/fix3').mkdir(parents=True,exist_ok=True)
Path('Artifacts/Skills/fix3/PreEdit.json').write_text(json.dumps({'backup':str(root),'data':data},indent=2),encoding='utf-8')
Path('task/p10/PROGRESS-fix3.md').write_text('# P10/P11 — Fix3, đang thực hiện\n\nĐã đọc đầy đủ PROMPT-P10-fix3, chuẩn fix1, REPORT-P11/review-notes và code P11 trước sửa. Kiểm tra REPORT/PROGRESS fix2 và P11; chưa có triển khai fix3. Editor Android/SampleScene/Stop. Backup: `'+str(root)+'`.\n\nPhạm vi: Vạn Kiếm lớn/trail/impact/đất/cổng; Ngự Lôi glow/viền và silhouette; Hỏa Liên nổ billow/gân cánh; Tích Lịch nét cong taper; bốn mục review P11. Hắc Động giữ nguyên. Chưa kết luận hình, test hay FPS mới đạt.\n',encoding='utf-8')
print(root)
