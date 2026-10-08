"""Apply the remaining HUD resize correction between regression sessions."""
from pathlib import Path
import shutil

p = Path('Assets/Controls/Runtime/MobileControlsHUD.cs')
backup = Path('Backups/Regression-APK-pre-20261007-resume') / p
backup.parent.mkdir(parents=True, exist_ok=True)
if not backup.exists():
    shutil.copy2(p, backup)
s = p.read_text(encoding='utf-8-sig')
s = s.replace('[DisallowMultipleComponent]', '[DefaultExecutionOrder(100)]\n    [DisallowMultipleComponent]', 1)
s = s.replace('float lastCanvasScale;Vector2 lastRootSize;', 'float lastCanvasScale;Vector2 lastRootSize;Matrix4x4 lastRootTransform;', 1)
s = s.replace('lastCanvasScale=canvas.scaleFactor;lastRootSize=SafeRoot.rect.size;', 'lastCanvasScale=canvas.scaleFactor;lastRootSize=SafeRoot.rect.size;lastRootTransform=SafeRoot.localToWorldMatrix;', 1)
s = s.replace('// CanvasScaler and the safe-root geometry may settle after our Update\n            // on a resize. Reapply screen positions once that geometry changes.', '// FitFrame can change an ancestor scale/position while the canvas scale\n            // and local safe-root size stay identical. Run after it and include\n            // the complete transform when deciding to reapply screen positions.', 1)
s = s.replace('SafeRoot.rect.size!=lastRootSize))Layout();', 'SafeRoot.rect.size!=lastRootSize||SafeRoot.localToWorldMatrix!=lastRootTransform))Layout();', 1)
p.write_text(s, encoding='utf-8')
with Path('task/batch-1007/PROGRESS.md').open('a', encoding='utf-8') as f:
    f.write('\n- Mốc sửa resize sau focused FAIL: FitFrame thay scale/position tổ tiên trong LateUpdate nhưng canvas.scaleFactor và SafeRoot.rect.size có thể không đổi. MobileControlsHUD chạy sau FitFrame và theo dõi localToWorldMatrix. Không chạy PlayerCombat lần thứ3; giữ FAIL gốc/retest, compile và ảnh visual riêng sau sửa. Backup nguồn tiếp quản: Regression-APK-pre-20261007-resume.\n')
