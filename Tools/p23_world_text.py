"""Finish text-size coverage for comic world billboards after the running suite stops."""
from pathlib import Path
import shutil
root=Path.cwd();backup=root/'Backups/P23-resume-20261004'
changes={
 'Assets/Combat/Runtime/EnemyHealthBars.cs':[
  ('Mathf.Clamp(distance * WorldSize, 0.6f, 3f);','Mathf.Clamp(distance * WorldSize, 0.6f, 3f) * UI.Accessibility.TextScale;')],
 'Assets/Combat/Runtime/DamageNumberPool.cs':[
  ('*145+Random.Range(-3f,3f);','*145*UI.Accessibility.TextScale+Random.Range(-3f,3f);'),
  ('float y=ceiling-(index/columns)*64;','float y=ceiling-(index/columns)*64*UI.Accessibility.TextScale;'),
  ('WorldSize * e.scale * (1f + 0.3f * (1f - t));','WorldSize * e.scale * UI.Accessibility.TextScale * (1f + 0.3f * (1f - t));')]
}
for rel,edits in changes.items():
 p=root/rel;dest=backup/rel;dest.parent.mkdir(parents=True,exist_ok=True)
 if not dest.exists():shutil.copy2(p,dest)
 s=p.read_text(encoding='utf-8-sig')
 for old,new in edits:
  assert old in s,(rel,old);s=s.replace(old,new)
 p.write_text(s,encoding='utf-8')
with (root/'task/p23/PROGRESS.md').open('a',encoding='utf-8') as f:
 f.write('\n- Rà toàn bộ UI comic phát hiện hai billboard TMP ngoài Canvas còn cỡ cố định: bảng tên/thanh máu quái và số sát thương. Bổ sung TextScale trên root world UI (nền/viền/phụ tố cùng lớn), số sát thương và khoảng cách hàng/cột; cỡ100% giữ nguyên. Không đổi collider/model/combat. Kiểm tỷ lệ100/115/130% riêng trong closeout probe trước build.\n')
print('World comic text-size coverage completed')
