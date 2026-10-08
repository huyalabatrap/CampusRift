from pathlib import Path
import hashlib,json
out=Path('task/ar/fix3');digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((out/'manifest.json').read_text(encoding='utf-8-sig'))
checks=[dict(row,after=digest(Path(row['path']))) for row in manifest]
changed=[r for r in checks if r['sha256']!=r['after']]
protected=json.loads(Path('task/ar/protected-manifest.json').read_text(encoding='utf-8-sig'))
protected=[dict(row,identical=digest(Path(row['path']))==row['sha256']) for row in protected]
orchestrators=[dict(row,identical=row['sha256']==row['after']) for row in checks if row['path'].startswith('task/run-') or row['path']=='task/codex-accounts.json']
assert all(r['identical'] for r in protected),[r for r in protected if not r['identical']]
assert all(r['identical'] for r in orchestrators),orchestrators
tracked={r['path'] for r in manifest}
new=[p.as_posix() for p in Path('Assets/ARRift').rglob('*') if p.is_file() and p.as_posix() not in tracked]
(out/'changes.json').write_text(json.dumps(dict(modified=changed,new=new),ensure_ascii=False,indent=2),encoding='utf-8')
(out/'protected.json').write_text(json.dumps(protected,indent=2),encoding='utf-8')
(out/'orchestrators.json').write_text(json.dumps(orchestrators,indent=2),encoding='utf-8')
print('Protected',len(protected),'unchanged; orchestrators',len(orchestrators),'unchanged;',len(changed),'modified and',len(new),'new assets')
