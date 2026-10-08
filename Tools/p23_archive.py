"""Archive the complete reproducible project, excluding caches and recursive backups."""
from pathlib import Path
import hashlib,json,shutil,time
root=Path.cwd();dest=root/'Backups/V2-Release-2026-10-04';dest.mkdir(parents=True,exist_ok=True)
folders=['Assets','Content','Packages','ProjectSettings','UserSettings','Tools','task','Artifacts','Releases/2026-10-04-v1.0']
sources=[p for folder in folders for p in (root/folder).rglob('*') if p.is_file()]
sources += [p for p in root.iterdir() if p.is_file()]
manifest=[];total=0
for i,p in enumerate(sources):
 rel=p.relative_to(root);q=dest/rel;q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
 h=hashlib.sha256()
 with q.open('rb') as f:
  while data:=f.read(8*1024*1024):h.update(data)
 size=q.stat().st_size;manifest.append({'path':rel.as_posix(),'bytes':size,'sha256':h.hexdigest()});total+=size
 if i%1000==0:print(i,len(sources),flush=True)
(dest/'SOURCE-MANIFEST.json').write_text(json.dumps({'created':time.strftime('%Y-%m-%dT%H:%M:%S'),'files':manifest,'totalBytes':total,'exclusions':['Library','Temp','Logs','obj','.vs','Backups (recursive)','historical Releases outside current version'],'notes':'Reproducible source + full current evidence + four release builds; user persistent saves are preserved in P23-pre backup, not distributed.'},ensure_ascii=False,indent=2),encoding='utf-8')
print('ARCHIVED',len(manifest),total,dest,flush=True)
