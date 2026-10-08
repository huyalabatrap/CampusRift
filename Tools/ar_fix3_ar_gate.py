from ar_fix3_local import *
import shutil
assert not (out/'AR-DONE.txt').exists(),'Do not repeat completed AR harness'
prior=out/'prior-m5';shutil.copytree('task/ar/m5',prior)
screens=list(Path('task/ar/screens').glob('m5-*.png'))
for p in screens:shutil.copy2(p,out/p.name)
done=Path('task/ar/m5/DONE.txt');done.unlink()
progress('ARRiftPlayTest bắt đầu\n- Chạy đúng1lượt harness combat gốc, không đổi assertions. Theo dõi m5/DONE timestamp; output sẽ copy fix3 rồi phục hồi M5. Chưa HubFlow/build.')
code('new UnityEngine.GameObject("Fix3 AR harness").AddComponent<CampusRift.AR.ARRiftPlayTest>();return true;')
for i in range(140):
    if done.exists():break
    time.sleep(1)
else:raise TimeoutError('AR harness')
shutil.copy2(done,out/'AR-DONE.txt');shutil.copy2('task/ar/m5/results.json',out/'ar-results.json')
for p in screens:shutil.copy2(out/p.name,p)
shutil.copytree(prior,'task/ar/m5',dirs_exist_ok=True)
console(out/'ar-harness-console.json')
progress('ARRiftPlayTest hoàn tất\n- '+(out/'AR-DONE.txt').read_text()+'. Raw ar-results.json/console; M5 evidence phục hồi. Không chạy lại harness. Tiếp HubFlow1lượt/APK.')
print((out/'AR-DONE.txt').read_text())
