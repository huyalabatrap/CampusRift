"""Compare existing backup to Assets and record project-bound comic deliverables."""
from pathlib import Path
import hashlib, json
backup=Path('Backups/ComicUI-20261001')
modified=[]
for old in backup.rglob('*'):
    if not old.is_file() or old.suffix=='.meta': continue
    rel=old.relative_to(backup)
    current=Path('Assets')/rel
    if current.exists() and current.read_bytes()!=old.read_bytes(): modified.append(current.as_posix())
created=[p.as_posix() for p in Path('Assets/CampusRiftUI/Comic').rglob('*') if p.is_file() and p.suffix!='.meta']
created += ['Assets/CampusRiftUI/Runtime/ComicTheme.cs','Assets/CampusRiftUI/Runtime/FitFrame.cs','Assets/CampusRiftUI/Editor/ComicUIBuilder.cs','Tools/build_comic_sprites.py','Tools/comic_mcp.py','Tools/polish_comic_sources.py','Tools/comic_inventory.py','Tools/comic_capture.py','Tools/comic_report.py','Tools/extract_fitframe.py','Tools/repair_fitframe_refs.py']
modified += ['Assets/Controls/Runtime/MobileControlsHUD.cs','Assets/Localization/Resources/LocalizationCatalog.asset']
record={'modified_against_backup':sorted(modified),'created':sorted(created),'screens':[p.as_posix() for p in Path('task/ui-comic/screens/round1').glob('*.png')]}
Path('task/ui-comic/FILES-round1.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(record,ensure_ascii=False,indent=2))
