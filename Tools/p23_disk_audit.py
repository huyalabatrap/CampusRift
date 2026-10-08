"""Compare release source and protected data against the P23 pre-task backup."""
from pathlib import Path
import hashlib, json

root = Path.cwd()
out = root / 'task/p23/disk-audit.json'
original = json.loads((root / 'task/p23/pre-manifest.json').read_text(encoding='utf-8-sig'))
def digest(p):
    h = hashlib.sha256()
    with p.open('rb') as f:
        while data := f.read(8 * 1024 * 1024):
            h.update(data)
    return h.hexdigest()

changed, missing, protected = [], [], []
for row in original:
    rel = row['path']
    p = root / rel
    if rel.startswith('UserSave/'):
        continue  # Separate exact-byte user-save audit.
    if not p.is_file():
        missing.append(rel)
        continue
    same = digest(p) == row['hash']
    if not same:
        changed.append(rel)
    # P23 is runtime/code/UI polish. Existing scene/prefab/navigation/content/model data is protected.
    guard = (rel.startswith('Content/') or (rel.startswith('Assets/') and p.suffix.lower() in ('.unity', '.prefab', '.fbx', '.glb', '.obj', '.blend', '.navmesh')) or 'NavMesh' in rel or 'GameReadyModels/' in rel)
    if guard:
        protected.append({'path': rel, 'identical': same})
known = {r['path'] for r in original}
new = []
for folder in ('Assets', 'Content', 'Packages', 'ProjectSettings', 'UserSettings', 'Tools', 'task', 'Artifacts'):
    for p in (root / folder).rglob('*'):
        if p.is_file() and p.relative_to(root).as_posix() not in known and '__pycache__' not in p.parts:
            new.append(p.relative_to(root).as_posix())
report = {'changed': changed, 'missing': missing, 'new': new, 'protectedFiles': len(protected), 'protectedChanges': [r['path'] for r in protected if not r['identical']], 'protected': protected}
out.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print('Changed', len(changed), 'new', len(new), 'missing', len(missing), 'protected', len(protected), 'protected changes', report['protectedChanges'])
print('Asset changes:', [p for p in changed if p.startswith('Assets/')])
