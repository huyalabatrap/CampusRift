import hashlib, json, shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
backup = root / 'Backups/STABILIZE-pre-20261004'
folders = ['Assets/Controls', 'Assets/Combat', 'Assets/Skills', 'Assets/Levels',
           'Assets/Localization', 'Assets/CampusRiftUI', 'Assets/Enemies',
           'Assets/SkyBeast', 'Assets/MonsterShaban', 'Assets/Editor',
           'Assets/Scenes', 'ProjectSettings']
files = [p for folder in folders for p in (root / folder).rglob('*') if p.is_file()]
files += [root / 'task/README.md', root / 'task/polish-notes.md']
manifest = {}
for source in files:
    relative = source.relative_to(root).as_posix()
    target = backup / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        raise RuntimeError('Backup already exists: ' + str(target))
    shutil.copy2(source, target)
    manifest[relative] = hashlib.sha256(source.read_bytes()).hexdigest()
profile = Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift')
user_manifest = {}
for source in profile.glob('*'):
    if source.is_file():
        shutil.copy2(source, backup / ('USER-' + source.name))
        user_manifest[source.name] = hashlib.sha256(source.read_bytes()).hexdigest()
payload = {'backup': str(backup), 'files': manifest, 'user_profile': str(profile), 'user_files': user_manifest}
(root / 'task/stabilize/pre-manifest.json').write_text(json.dumps(payload, indent=2), encoding='utf-8')
(root / 'task/stabilize/BACKUP.txt').write_text(str(backup), encoding='utf-8')
print(f'Backed up {len(files)} project files and {len(user_manifest)} user files.')
