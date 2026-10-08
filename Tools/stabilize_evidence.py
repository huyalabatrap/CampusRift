from pathlib import Path
import json, shutil
root=Path(__file__).resolve().parents[1]
backup=root/'Backups/STABILIZE-pre-20261004/evidence'
folders=['Artifacts/MobileControls','Artifacts/BoostEnergy','Artifacts/Combat','Artifacts/Skills',
         'Artifacts/SkyVictory','Artifacts/Localization','Artifacts/P12/tests','Artifacts/Levels',
         'Artifacts/PhantomDecoy','Assets/MonsterShaban/Validation','task/p10/screens/frames',
         'task/p12/screens/fix1']
files=[]
for folder in folders:
    for p in (root/folder).rglob('*'):
        if p.is_file():
            rel=p.relative_to(root); target=backup/rel
            target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target);files.append(rel.as_posix())
(root/'task/stabilize/evidence-backup.json').write_text(json.dumps(files,indent=2),encoding='utf-8')
print(f'Historical evidence backup: {len(files)} files.')
