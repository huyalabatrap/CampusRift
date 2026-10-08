from ar_fix3_local import *
assert not (out/'visual-DONE.txt').exists(),'Do not repeat visual/audit gate'
code('new UnityEngine.GameObject("Fix3 visual and drift smoke").AddComponent<CampusRift.AR.ARFix3VisualSmoke>();return true;')
for i in range(90):
    time.sleep(1)
    if (out/'visual-DONE.txt').exists():break
else:raise TimeoutError('visual smoke')
console(out/'visual-console.json')
progress('A/C ảnh + drift/audit\n- VisualSmoke1lượt hoàn tất: plane pose/subsumption drift trong plane-drift.json; coverage hai độ phân giải, menu pause/resume/explicit pause,10ảnh screens/fix3. ComicTextAudit1lượt lưu text-audit.json. Chưa AR/Hub harness/build.')
for path in ['plane-drift.json','coverage-2400.json','coverage-1600.json','text-audit.json']:print(path,(out/path).read_text(encoding='utf-8')[:1800])
