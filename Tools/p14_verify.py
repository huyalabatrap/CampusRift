from pathlib import Path
import json,hashlib
root=Path.cwd();backup=Path('task/p14/BACKUP.txt').read_text(encoding='utf-8-sig').strip()
def digest(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
preserve={}
for name in ['Assets/Scenes/SampleScene.unity','ProjectSettings/TagManager.asset','ProjectSettings/EditorSettings.asset']:
 preserve[name]=dict(sha256=digest(name),sameAsBackup=digest(name)==digest(Path(backup)/name))
for folder in ['Assets/SkyBeast/Resources/P12','Assets/SkyBeast/Resources/P13','Assets/SkyBeast/Prefabs','Assets/Levels/Data']:
 files=[p for p in Path(folder).rglob('*') if p.is_file()]
 preserve[folder]=dict(files=len(files),changed=[str(p) for p in files if not (Path(backup)/p).exists() or digest(p)!=digest(Path(backup)/p)])
for name,reference,key in [('Assets/Models/Comic_Vibrant_Elevator_System_T77/Comic_Vibrant_Elevator_System_T77.fbx','task/p13/geometry-audit.json','fbxSHA256'),('Assets/MonsterShaban/CampusNavMesh.asset','task/p13/geometry-audit.json','navMeshSHA256')]:
 preserve[name]=dict(sha256=digest(name),sameAsP13=digest(name)==json.loads(Path(reference).read_text(encoding='utf-8-sig'))[key])
manifest=json.loads(Path('task/p14/source/manifest.json').read_text());audio=[dict(file=x['file'],sha256Matches=digest(x['file'])==x['sha256']) for x in manifest]
audits=[dict(file=str(p),issues=len(json.loads(p.read_text(encoding='utf-8-sig'))['issues'])) for p in Path('task/p14/screens').glob('*text-audit.json')]
summary=dict(preservation=preserve,audio=audio,audits=audits,sky=json.loads(Path('task/p14/SkyBeast.json').read_text(encoding='utf-8-sig')),fire=json.loads(Path('task/p14/FireBreath.json').read_text(encoding='utf-8-sig')),dev=json.loads(Path('task/p14/dev-smoke.json').read_text(encoding='utf-8-sig')))
Path('task/p14/preservation.json').write_text(json.dumps(preserve,indent=2),encoding='utf-8')
Path('task/p14/verification-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps(dict(sky=(summary['sky']['passed'],summary['sky']['failed']),fire=(summary['fire']['passed'],summary['fire']['failed']),dev=summary['dev']['pass'],auditIssues=sum(x['issues'] for x in audits),preservation=preserve,audio=audio),indent=2))
