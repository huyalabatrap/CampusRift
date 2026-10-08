from pathlib import Path
import hashlib, shutil, json
root=Path('Backups/ComicUI-round1-pre-round2')
groups=['Assets/CampusRiftUI','Assets/Settings','Assets/Scenes','Assets/Resources/ContentImages','Assets/Controls/Runtime','Assets/Learning/Runtime','Assets/Localization','Assets/Progression/Runtime','Assets/Scripts/ElevatorInteraction.cs','Tools/build_comic_sprites.py']
manifest={}
for group in groups:
    source=Path(group)
    for path in ([source] if source.is_file() else source.rglob('*')):
        if not path.is_file(): continue
        manifest[path.as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
        target=root/path
        if not target.exists():
            target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,target)
root.mkdir(parents=True,exist_ok=True)
record=root/'manifest.json'
if not record.exists(): record.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Round 1 snapshot preserved:',len(manifest),'files')
