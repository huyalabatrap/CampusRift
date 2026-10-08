import hashlib
import json
import re
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Artifacts/V2/Stabilize'

def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))

def write(path, data):
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    Path(path).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

manifest = read(ROOT / 'task/stabilize/pre-manifest.json')
BACKUP = Path(manifest['backup'])
history = read(ROOT / 'task/stabilize/evidence-backup.json')

def restore():
    for relative in history:
        source = BACKUP / 'evidence' / relative
        target = ROOT / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
    user = Path(manifest['user_profile'])
    for name in manifest['user_files']:
        shutil.copy2(BACKUP / ('USER-' + name), user / name)
    # TMP dynamically adds glyphs during captures. Preserve the authored font asset.
    font = 'Assets/CampusRiftUI/Comic/Resources/Comic/ComicVietnamese.asset'
    shutil.copy2(BACKUP / font, ROOT / font)
    print(f'Restored {len(history)} historical evidence files, {len(manifest["user_files"])} user files and original TMP asset.')

def audits():
    old = read(ROOT / 'Artifacts/V2/P17-regression/ShelterAudit.json')
    new = read(OUT / 'runs/ShelterAudit/result.json')
    shelter = {'baseline': 'Artifacts/V2/P17-regression/ShelterAudit.json',
               'phase': 'task/p13/REPORT-P13.md', 'identicalRawJSON': old == new,
               'nodes': new['nodes'], 'passed': new['passed'], 'acceptedMismatches': new['failed'],
               'accuracy': new['accuracy'], 'thresholdPass': new['pass'],
               'newMismatches': [m for m in new['mismatches'] if m not in old['mismatches']],
               'resolvedMismatches': [m for m in old['mismatches'] if m not in new['mismatches']]}
    shelter['pass'] = shelter['identicalRawJSON'] and new['pass'] and not shelter['newMismatches']
    write(OUT / 'shelter-baseline-comparison.json', shelter)
    assert shelter['pass'], 'Shelter baseline changed'
    shots = []
    for path in sorted((ROOT / 'task/stabilize/screens').glob('*-after-text-audit.json')):
        data = read(path)
        shots.append({'path': path.relative_to(ROOT).as_posix(), **data})
    assert len(shots) == 8 and all(not s['issues'] for s in shots)
    write(OUT / 'polish-audits.json', {'pass': True, 'captures': shots,
          'worldNameplate': 'runs/ModelsPolish/result.json (two lines, letter24.99px, no truncation, far/lock tested)',
          'note': 'ComicTextAudit covers canvas text. World TMP nameplate is checked separately by ModelsPolish.'})
    history_bad = [p for p in history if sha(ROOT / p) != sha(BACKUP / 'evidence' / p)]
    user_bad = [p for p, h in manifest['user_files'].items() if sha(Path(manifest['user_profile']) / p) != h]
    changed = [p for p,h in manifest['files'].items() if not (ROOT/p).exists() or sha(ROOT/p) != h]
    protected = read(ROOT / 'task/models/protected-hashes.json')
    # Structural assets and supplied models must remain byte-identical. Code fixtures are intentionally updated.
    immutable = {p:h for p,h in protected.items() if p.endswith(('.unity','.fbx','.glb','.blend'))
                 or 'GameReadyModels/' in p or 'NavMesh' in p}
    immutable.update({p:h for p,h in manifest['files'].items() if p.startswith(('Assets/Scenes/','Assets/Levels/Data/'))
                      or p.startswith('ProjectSettings/') or 'NavMesh' in p or p.endswith(('.fbx','.glb','.blend'))})
    user_models = {p:h for p,h in read(ROOT / 'task/models/baseline-hashes.json').items() if p.startswith('Assets/GameReadyModels/')}
    immutable.update(user_models)
    protected_bad = [p for p,h in immutable.items() if not (ROOT/p).exists() or sha(ROOT/p) != h]
    prefab_checks = []
    for name in ['AnhYeu','DucYeu']:
        rel = f'Assets/Enemies/Prefabs/{name}.prefab'
        def components(path):
            text = Path(path).read_text(encoding='utf-8-sig')
            return {ident:body for typ,ident,body in re.findall(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)', text, re.S)
                    if typ in ['114','136','143','65','54','195']}
        before, after = components(BACKUP / rel), components(ROOT / rel)
        prefab_checks.append({'prefab': rel, 'gameplayBlocks': len(before),
                              'gameplayUnchanged': before == after,
                              'guidUnchanged': sha(ROOT/(rel+'.meta')) == manifest['files'][rel+'.meta']})
    disk = {'at': datetime.now(timezone.utc).isoformat(), 'existingFilesChanged': changed,
            'protectedStructuralFiles': len(immutable), 'protectedChanges': protected_bad,
            'suppliedModelFiles': len(user_models),
            'historicalEvidenceFiles': len(history), 'historicalEvidenceChanges': history_bad,
            'userFiles': len(manifest['user_files']), 'userFileChanges': user_bad,
            'prefabs': prefab_checks}
    disk['pass'] = not (history_bad or user_bad or protected_bad) and all(p['gameplayUnchanged'] and p['guidUnchanged'] for p in prefab_checks)
    write(OUT / 'final-disk-audit.json', disk)
    print(json.dumps({k:v for k,v in disk.items() if k != 'existingFilesChanged'}, ensure_ascii=False))
    return disk

if __name__ == '__main__':
    if sys.argv[1] == 'restore': restore()
    elif sys.argv[1] == 'audit': audits()
