"""Collect the shipped asset attributions verbatim and hash the four-build release."""
from pathlib import Path
import hashlib,json
root=Path.cwd();release=root/'Releases/2026-10-04-v1.0';release.mkdir(parents=True,exist_ok=True)
licenses=sorted(p for p in (root/'Assets').rglob('*') if p.is_file() and p.suffix.lower() in ('.md','.txt') and any(k in p.name.lower() for k in ('license','ofl','copyright','notice')))
text='# Campus Rift 1.0 — Ghi công và giấy phép tài nguyên\n\nCác bản ghi dưới đây được gom nguyên văn từ dự án hiện hành. Mỗi nguồn giữ tên tác giả, nguồn và điều kiện giấy phép; không thay giấy phép bằng tuyên bố CC0 chung. Tài nguyên P23 (ký hiệu hình/texture đá) tạo bằng mã trong dự án.\n\n'
index=[]
for p in licenses:
 rel=p.relative_to(root).as_posix();raw=p.read_bytes();index.append({'path':rel,'sha256':hashlib.sha256(raw).hexdigest()});text+='\n---\n\n## '+rel+'\n\n'+raw.decode('utf-8-sig')+'\n'
(release/'LICENSES.md').write_text(text,encoding='utf-8');(release/'license-index.json').write_text(json.dumps(index,indent=2),encoding='utf-8')
hashes=[]
for p in sorted(release.rglob('*')):
 if not p.is_file() or p.name in ('SHA256SUMS.txt','release-manifest.json'):continue
 h=hashlib.sha256()
 with p.open('rb') as f:
  while data:=f.read(8*1024*1024):h.update(data)
 hashes.append({'path':p.relative_to(release).as_posix(),'bytes':p.stat().st_size,'sha256':h.hexdigest()})
(release/'release-manifest.json').write_text(json.dumps({'version':'1.0.0','files':hashes,'licenseSources':len(licenses)},ensure_ascii=False,indent=2),encoding='utf-8');(release/'SHA256SUMS.txt').write_text(''.join(r['sha256']+'  '+r['path']+'\n' for r in hashes),encoding='utf-8');print('License sources',len(licenses),'hashed files',len(hashes))
