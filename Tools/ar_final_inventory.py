"""Record final changes against the earliest available AR backups."""
from pathlib import Path
import hashlib,json
root=Path.cwd();out=root/'task/ar/final';out.mkdir(exist_ok=True)
original={}
for marker in ['BACKUP.txt','M4-BACKUP.txt','M5-BACKUP.txt','M6-BACKUP.txt','M7-BACKUP.txt','M7-FIX-BACKUP.txt']:
    folder=Path((root/'task/ar'/marker).read_text(encoding='utf-8-sig').strip())
    for prefix in ['Assets','Packages','ProjectSettings']:
        for p in (folder/prefix).rglob('*'):
            if p.is_file():original.setdefault(p.relative_to(folder).as_posix(),p)
changed=[]
for rel,before in original.items():
    current=root/rel
    if rel.startswith(('Assets/ARRift/','Assets/XR/')):continue
    if current.is_file() and before.read_bytes()!=current.read_bytes():
        changed.append({'path':rel,'before':hashlib.sha256(before.read_bytes()).hexdigest(),'after':hashlib.sha256(current.read_bytes()).hexdigest()})
new=[]
for prefix in ['Assets/ARRift','Assets/XR']:
    new += [p.relative_to(root).as_posix() for p in (root/prefix).rglob('*') if p.is_file()]
for rel in ['Assets/Plugins/Android/GestureBridge.kt','Assets/Plugins/Android/mainTemplate.gradle','Assets/Plugins/Android/launcherTemplate.gradle','Assets/StreamingAssets/gesture_recognizer.task']:
    if rel not in original:new.append(rel)
protected=json.loads((root/'task/ar/protected-manifest.json').read_text(encoding='utf-8-sig'))
checks=[dict(r,identical=hashlib.sha256((root/r['path']).read_bytes()).hexdigest()==r['sha256']) for r in protected]
assert all(r['identical'] for r in checks),[r['path'] for r in checks if not r['identical']]
(out/'protected.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
(out/'changes.json').write_text(json.dumps({'modified':sorted(changed,key=lambda r:r['path']),'new':sorted(new)},indent=2),encoding='utf-8')
print('Protected:',len(checks),'unchanged; modified:',len(changed),'new:',len(new))
