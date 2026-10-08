from ar_fix2_local import *
import shutil
build=Path('task/ar/build-fix2');prior=build/'attempt1'
assert (build/'DONE.txt').read_text().strip()=='Succeeded'
assert not prior.exists(), 'Attempt 1 already archived; inspect before continuing'
prior.mkdir()
for p in list(build.iterdir()):
    if p.is_file():shutil.copy2(p,prior/p.name);p.unlink()
save(prior/'manifest-requirement.json',dict(passed=False,actual='userLandscape(0xb)',required=['landscape(0)','sensorLandscape(6)']))
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — build1 PASS, manifest yêu cầu chưa đạt / build2\n- Build1SUCCESS2m33/0errors57warnings, APK548527081bytes/SHA89e7150ed498d0c2f46d13d307f9135ea78f7ea107d8e8d4926569315f0257a7. AAPT phát hiện screenOrientation=userLandscape0xb. Unity6 mặc định xuất giá trị này cho landscape-only AutoRotation, chưa đúng literal yêu cầu landscape/sensorLandscape.\n- Giữ toàn bộ build1evidence ở build-fix2/attempt1. Sửa ARAndroidBuildProcessor để ghi sensorLandscape trong generatedunityLibrarymanifest. Sửa kiểm AAPT parse giá trị cuối tránh nhầm attributeID0x0101001e. Queue build2 vì thay đổi manifest, không chạy lại harness/audit.\n')
exec(Path('Tools/ar_fix2_build.py').read_text(encoding='utf-8'))
