from ar_fix2_local import *
import shutil
build=Path('task/ar/build-fix2');prior=build/'attempt2'
assert (build/'DONE.txt').read_text().strip()=='Succeeded'
assert not prior.exists(), 'Attempt 2 already archived; inspect before continuing'
prior.mkdir()
for p in list(build.iterdir()):
    if p.is_file():shutil.copy2(p,prior/p.name);p.unlink()
save(prior/'manifest-requirement.json',dict(passed=False,actual='userLandscape(0xb)',reason='Hidden Editor had not imported updated processor; old method IL length 83'))
hook=code(Path('task/ar/fix2-manifest-hook.cs').read_text(encoding='utf-8'))
assert hook['orientation']=='sensorLandscape' and hook['methodLength']>83,hook
save(build/'manifest-hook-imported.json',hook)
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — build2 manifest vẫn cũ; import xác minh / build3\n- Build2SUCCESS59s/0errors3warnings nhưng APK vẫn userLandscape0xb. Editor DLL17:40 cũ, script mới17:55 chưaimport trong Editor chạyẩn; xác minh callbackIL83(old), không phải ARCore override. Giữ attempt2/ với AAPT raw + yêu cầuFAIL.\n- Đã explicitAssetDatabase.Refresh+RequestScriptCompilation ởEditMode; DLL cập nhật17:58. Hook loadedIL269 và generatedmanifest=sensorLandscape. Queuebuild3 sau xác minh compiledhook, không chạy lại harness/audit.\n')
exec(Path('Tools/ar_fix2_build.py').read_text(encoding='utf-8'))
