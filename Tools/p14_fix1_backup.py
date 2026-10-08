from pathlib import Path
from datetime import datetime
import hashlib, json, shutil

root = Path.cwd()
backup = Path('Backups') / ('P14-fix1-pre-' + datetime.now().strftime('%Y%m%d-%H%M%S'))
folders = ['Assets/SkyBeast', 'Assets/CampusLook', 'Assets/UI/Runtime', 'Assets/Settings', 'Assets/Levels/Data', 'ProjectSettings']
files = {p for folder in folders for p in Path(folder).rglob('*') if p.is_file()}
files.add(Path('Assets/Scenes/SampleScene.unity'))
baseline = {}
for p in sorted(files):
    dest = backup / p
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(p, dest)
    baseline[p.as_posix()] = hashlib.sha256(p.read_bytes()).hexdigest()
for name in ['Assets/Models/Comic_Vibrant_Elevator_System_T77/Comic_Vibrant_Elevator_System_T77.fbx', 'Assets/MonsterShaban/CampusNavMesh.asset']:
    baseline[name] = hashlib.sha256(Path(name).read_bytes()).hexdigest()
Path('task/p14/BACKUP-fix1.txt').write_text(backup.as_posix(), encoding='utf-8')
Path('task/p14/fix1-baseline.json').write_text(json.dumps(baseline, indent=2), encoding='utf-8')
print(json.dumps(dict(backup=str(backup), files=len(baseline))))
