from pathlib import Path
import hashlib,json
root=Path.cwd()
backup=Path((root/'task/p22/BACKUP.txt').read_text(encoding='utf-8-sig').strip())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((root/'task/p22/pre-manifest.json').read_text(encoding='utf-8-sig'))
for row in manifest:row['hash']=row['hash'].lower()
changed=[];missing=[];same=[];save=[]
for row in manifest:
 rel=row['path'];p=root/rel
 if rel.startswith('UserSave/'):
  p=Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift')/rel[len('UserSave/'):]
  save.append({'path':rel,'same':p.exists() and sha(p)==row['hash']});continue
 if not p.exists():missing.append(rel)
 elif sha(p)==row['hash']:same.append(rel)
 else:changed.append(rel)
new=[]
known={r['path'] for r in manifest}
for folder in ['Assets/Progression','Assets/Levels','Assets/CampusRiftUI','Assets/SkyBeast/Runtime']:
 for p in (root/folder).rglob('*'):
  if p.is_file() and p.relative_to(root).as_posix() not in known:new.append(p.relative_to(root).as_posix())
protected=[r for r in manifest if r['path'].startswith('Assets/Scenes/') or r['path'].startswith('ProjectSettings/') or r['path'].endswith('.fbx') or 'NavMesh' in r['path'] or r['path'].startswith('Assets/Levels/Resources/')]
report={'same':len(same),'changed':changed,'missing':missing,'new':new,'userSaves':save,'protected':[{'path':r['path'],'same':(root/r['path']).exists() and sha(root/r['path'])==r['hash']} for r in protected]}
(root/'task/p22/change-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
audits=[]
for p in (root/'task/p22/screens').glob('*-audit.json'):
 v=json.loads(p.read_text(encoding='utf-8-sig'));audits.append({'file':p.name,'issues':len(v['issues']),'width':v['width'],'height':v['height']})
(root/'task/p22/screens-audit.json').write_text(json.dumps(audits,indent=2),encoding='utf-8')
prior=json.loads((root/'task/p21/protected-manifest.json').read_text(encoding='utf-8-sig'))
historical=[{'path':k,'same':(root/k).exists() and sha(root/k)==v.lower()} for k,v in prior.items()]
(root/'task/p22/prior-model-nav-audit.json').write_text(json.dumps({'source':'task/p21/protected-manifest.json','files':len(historical),'differences':[r['path'] for r in historical if not r['same']],'results':historical},indent=2),encoding='utf-8')
print(json.dumps({'same':len(same),'changed':changed,'missing':missing,'newFiles':len(new),'saveMismatches':[r['path'] for r in save if not r['same']],'protectedMismatches':[r['path'] for r in report['protected'] if not r['same']],'screens':audits},ensure_ascii=False,indent=2))
