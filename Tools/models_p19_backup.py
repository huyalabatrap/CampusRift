from pathlib import Path
import hashlib, json, shutil, datetime
root=Path(__file__).resolve().parents[1]
out=root/'task/models'
stamp=datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
backup=root/f'Backups/MODELS-P19-pre-{stamp}'
paths=['Assets/Enemies','Assets/GameReadyModels','Assets/Scenes/SampleScene.unity','Assets/Scenes/SampleScene.unity.meta','ProjectSettings/EditorSettings.asset']
manifest={}
for rel in paths:
    src=root/rel; dst=backup/rel; dst.parent.mkdir(parents=True,exist_ok=True)
    if src.is_dir(): shutil.copytree(src,dst)
    else: shutil.copy2(src,dst)
    for f in src.rglob('*') if src.is_dir() else [src]:
        if f.is_file(): manifest[f.relative_to(root).as_posix()]=hashlib.sha256(f.read_bytes()).hexdigest()
protected={}
for folder in ['Assets/Levels','Assets/MonsterShaban','Assets/AI','Assets/SkyBeast']:
    src=root/folder
    if src.exists():
        for f in src.rglob('*'):
            if f.is_file(): protected[f.relative_to(root).as_posix()]=hashlib.sha256(f.read_bytes()).hexdigest()
for f in (root/'Assets').rglob('*'):
    if f.is_file() and ('navmesh' in f.name.lower() or f.suffix=='.unity'): protected[f.relative_to(root).as_posix()]=hashlib.sha256(f.read_bytes()).hexdigest()
(out/'baseline-hashes.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(out/'protected-hashes.json').write_text(json.dumps(protected,indent=2),encoding='utf-8')
(out/'BACKUP.txt').write_text(str(backup)+'\n',encoding='utf-8')
print(json.dumps({'backup':str(backup),'files':len(manifest),'protected':len(protected)}))
