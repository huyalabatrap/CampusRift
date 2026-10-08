"""Archive two attempts that executed no validation trials, then permit first valid runs."""
from pathlib import Path
import json

root=Path.cwd().resolve();out=root/'Artifacts/V2/P23-regression';p=out/'Summary.json'
rows=json.loads(p.read_text(encoding='utf-8'));names={'CampusStairs','Cultivation'}
diag=root/'task/p23/diagnostics/invalid-context';diag.mkdir(parents=True,exist_ok=True)
old=[r for r in rows if r['name'] in names];assert len(old)==2
stairs=next(r for r in old if r['name']=='CampusStairs')
cultivation=next(r for r in old if r['name']=='Cultivation')
assert stairs['passed']==0 and stairs['failed']==0 and stairs['consoleErrors']>0
assert cultivation['status']=='ERROR' and 'Exit Play Mode first' in cultivation['exception']
(diag/'original-rows.json').write_text(json.dumps(old,ensure_ascii=False,indent=2),encoding='utf-8')
for name in names:
    src=out/'runs'/name;dest=diag/name
    assert src.resolve().is_relative_to(root) and dest.resolve().is_relative_to(root)
    assert not dest.exists();src.rename(dest)
rows=[r for r in rows if r['name'] not in names]
p.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
flag=out/'STOP-AFTER-SUITE.txt';dest=diag/'STOP-AFTER-SUITE.txt'
assert flag.resolve().is_relative_to(root) and dest.resolve().is_relative_to(root)
assert not dest.exists();flag.rename(dest)
with (root/'task/p23/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## Mốc 13 — sửa điều kiện chạy validator\n- CampusStairs attempt0/0 do mesh không readable trongPlay (Console chỉ thu tối đa100,95error trong phần thu). Không có trial nào chạy. Cultivation bị guardExitPlay từ đầu, chưa chạy check. Raw/rows giữ diagnostics/invalid-context; không tính hai attempt này là lượt hợp lệ.\n- Backup stair-reader trước sửa. EditorMeshUtility snapshot đọc cùng vertices/triangles mà không bậtReadWrite/đổimesh/collider/NavMesh, giữ nguyên thuật toán/232trial/ngưỡng. Cultivation+LevelData gọi Edit đúng guard. Sau compile chạy hai nhóm chưa thực thi này một lượt hợp lệ, rồi tiếp tục phần còn lại. Doors1012/0, Element11/0 đã đạt.\n')
print('Archived invalid-context attempts; valid completed groups',len(rows))
