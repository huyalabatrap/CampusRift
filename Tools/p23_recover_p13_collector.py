"""Recover one completed 11/0 suite from a collector schema error; never rerun it."""
from pathlib import Path
import json, shutil

root=Path.cwd().resolve();out=root/'Artifacts/V2/P23-regression'
summary=out/'Summary.json';rows=json.loads(summary.read_text(encoding='utf-8'))
row=next(r for r in rows if r['name']=='P13LayerSmoke')
assert row['status']=='ERROR' and 'has no attribute' in row['exception']
result=json.loads((out/'runs/P13LayerSmoke/result.json').read_text(encoding='utf-8-sig'))
assert result['passed']==11 and result['failed']==0
assert all(isinstance(c,str) and c.startswith('PASS ') for c in result['checks'])
diag=root/'task/p23/diagnostics/p13-collector';diag.mkdir(parents=True,exist_ok=True)
(diag/'original-row.json').write_text(json.dumps(row,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(out/'runs/P13LayerSmoke/exception.txt',diag/'exception.txt')
row.pop('exception');row['status']='PASS';row['passed']=11;row['failed']=0
row['reviewNote']='Raw11/0; collector nhầm checks dạng chuỗi, lỗi/row gốc giữ trong diagnostics/p13-collector. Console riêng bị bỏ trước bước thu; không chạy lại. Look/Hunter Memory liền sau có Console0errors.'
summary.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
flag=out/'STOP-AFTER-SUITE.txt';dest=diag/'STOP-AFTER-SUITE.txt'
assert flag.resolve().is_relative_to(root) and dest.resolve().is_relative_to(root)
assert not dest.exists();flag.rename(dest)
with (root/'task/p23/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## Mốc 12 — sửa collector, không chạy lại suite\n- P13Layer raw11/0 bị evaluate nhầm checks string→dict. Giữ original-row/trace ở diagnostics/p13-collector; phục hồi PASS từ cùng raw11/0. Không giả có Console riêng vì collector lỗi trước bước thu; Look25/0 và Hunter-Memory8/0 kế tiếp có Console0errors.\n- Dừng worker sau Hunter-Memory (flag đặt lúc chuyển suite), nạp evaluate hỗ trợ hai schema và retry chỉ trạng thái kết nối/compile đang chạy, không retry runtime exception. Đã xử lý58/92nhóm; tiếp tục Hunter-Motion, không rerun nhóm hoàn tất.\n')
print('Recovered P13LayerSmoke 11/0; completed',len(rows))
