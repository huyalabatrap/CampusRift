from pathlib import Path
import hashlib,json
root=Path.cwd();backup=root/'Backups/AR-GoiA-pre-20261006';out=root/'task/ar/goiA'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
rows=json.loads((root/'task/ar/protected-manifest.json').read_text(encoding='utf-8-sig'))
protected=[dict(path=r['path'],identical=digest(root/r['path'])==r['sha256'],sha256=digest(root/r['path'])) for r in rows]
orchestrators=json.loads((backup/'orchestrators.json').read_text(encoding='utf-8-sig'))
orchestrators.append(json.loads((backup/'accounts.json').read_text(encoding='utf-8-sig')))
control=[dict(path=str(Path(r['Path']).relative_to(root)),identical=digest(Path(r['Path'])).lower()==r['Hash'].lower()) for r in orchestrators]
changes=[];new=[]
for directory in ['Assets','ProjectSettings']:
    for p in (backup/directory).rglob('*'):
        if p.is_file():
            rel=p.relative_to(backup);q=root/rel
            if not q.exists() or digest(p)!=digest(q):changes.append(str(rel))
for directory in ['Assets/ARRift','Assets/Plugins/Android']:
    for p in (root/directory).rglob('*'):
        if p.is_file() and not (backup/p.relative_to(root)).exists():new.append(str(p.relative_to(root)))
result=dict(protected=protected,orchestrators=control,changedBackedFiles=changes,newFiles=new)
(out/'disk-audit.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(dict(protectedMatches=sum(r['identical'] for r in protected),protectedCount=len(protected),controlMatches=sum(r['identical'] for r in control),controlCount=len(control),changes=changes,newFiles=new),indent=2))
assert all(r['identical'] for r in protected+control)
