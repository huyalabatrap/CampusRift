from ar_session import *
import shutil
out=Path('task/ar/fix1')
prior=out/'prior-m5';shutil.copytree('task/ar/m5',prior,dirs_exist_ok=True)
screens=list(Path('task/ar/screens').glob('m5-*.png'))
for p in screens:shutil.copy2(p,out/p.name)
done=Path('task/ar/m5/DONE.txt')
if done.exists():done.unlink()
print(code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.Shrine.Revive(1,0);return new {paused=f.Paused,actors=f.GetComponent<CampusRift.AR.ARMonsterDirector>().Actors.Count};'),flush=True)
code('new UnityEngine.GameObject("AR fix1 existing harness").AddComponent<CampusRift.AR.ARRiftPlayTest>();return true;')
for i in range(180):
 if done.exists():break
 time.sleep(1)
else:raise RuntimeError('harness timed out')
for name in ['results.json','DONE.txt']:shutil.copy2(Path('task/ar/m5')/name,out/name)
for p in screens:
 shutil.copy2(p,Path('task/ar/screens/fix1')/p.name)
 shutil.copy2(out/p.name,p)
shutil.copytree(prior,'task/ar/m5',dirs_exist_ok=True)
console(out/'harness-console.json')
print((out/'DONE.txt').read_text(),flush=True)
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n## AR fix1 — triển khai + gate chức năng\n- Thêm giới hạn render AR (mesh/line/particle), co vòng/portal/Vortex theo s + bán kính, số sát thương AR chỉ số; shrine đá tròn + pha lê vàng/tím + crack/flash/break. HUD M6 đã auto-hide; component placement cũ cũng được sửa.\n- ARRiftPlayTest chạy1lượt: '+(out/'DONE.txt').read_text()+'. Raw fix1/results.json, harness-console.json. Evidence M5 cũ được phục hồi, ảnh mới screens/fix1/. Tiếp soi hình, ComicTextAudit1lượt và build.\n')
