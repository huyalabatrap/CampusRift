"""P23 QA isolation: preserve the exact existing native prefs and user-save bytes.

Run file mutations only while Unity is in Edit Mode and native players are closed.
No registry tree deletion, and no removal of unrecorded pre-existing save files.
"""
from pathlib import Path
import base64, hashlib, json, shutil, sys, winreg

ROOT = Path.cwd()
STATE = ROOT / 'task/p23'
KEY = r'Software\DefaultCompany\Campus Rift'
SNAPSHOT = STATE / 'native-registry-before.json'
settings = json.loads((STATE / 'settings-before.json').read_text(encoding='utf-8-sig'))
user = Path(settings['path']).resolve()
backup = Path((STATE / 'BACKUP.txt').read_text(encoding='utf-8-sig').strip()) / 'UserSave'
assert user == Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift').resolve()
assert backup.is_dir(), backup

def values():
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, KEY) as key:
            return [winreg.EnumValue(key, i) for i in range(winreg.QueryInfoKey(key)[1])]
    except FileNotFoundError:
        return []

def snapshot_registry():
    if SNAPSHOT.exists():
        raise RuntimeError('Original registry snapshot already exists; refusing overwrite')
    rows = []
    for name, value, kind in values():
        rows.append({'name': name, 'kind': kind, 'value': base64.b64encode(value).decode() if isinstance(value, bytes) else value, 'binary': isinstance(value, bytes)})
    SNAPSHOT.write_text(json.dumps({'key': KEY, 'values': rows}, indent=2), encoding='utf-8')
    print('Snapshot native prefs:', len(rows), 'values')

def restore_files():
    original = {p.relative_to(backup).as_posix(): p for p in backup.rglob('*') if p.is_file()}
    qa = STATE / 'diagnostics/user-save-after-qa'
    for p in user.rglob('*'):
        if not p.is_file():
            continue
        rel = p.relative_to(user).as_posix()
        if rel not in original or p.read_bytes() != original[rel].read_bytes():
            q = qa / rel
            q.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(p, q)
        if rel not in original:
            assert p.resolve().is_relative_to(user)
            p.unlink()  # Only a QA-created file, now copied to diagnostic evidence.
    for rel, p in original.items():
        q = user / rel
        q.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(p, q)
    manifest = [{'path': rel, 'sha256': hashlib.sha256((user / rel).read_bytes()).hexdigest(), 'identical': (user / rel).read_bytes() == p.read_bytes()} for rel, p in original.items()]
    assert all(r['identical'] for r in manifest)
    (STATE / 'user-save-restored.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print('Restored exact original save files:', len(original))

def restore_registry():
    data = json.loads(SNAPSHOT.read_text(encoding='utf-8'))
    assert data['key'] == KEY
    original = {r['name']: r for r in data['values']}
    with winreg.CreateKey(winreg.HKEY_CURRENT_USER, KEY) as key:
        for name, _, _ in values():
            if name not in original:
                winreg.DeleteValue(key, name)  # Only QA-created values in this product key.
        for name, row in original.items():
            value = base64.b64decode(row['value']) if row['binary'] else row['value']
            winreg.SetValueEx(key, name, 0, row['kind'], value)
    restored = {n: (v, k) for n, v, k in values()}
    assert len(restored) == len(original)
    for name, row in original.items():
        value = base64.b64decode(row['value']) if row['binary'] else row['value']
        assert restored[name] == (value, row['kind'])
    (STATE / 'native-registry-restored.json').write_text(json.dumps({'key': KEY, 'values': len(original), 'identical': True}, indent=2), encoding='utf-8')
    print('Restored exact original native prefs:', len(original))

action = sys.argv[1]
if action == 'snapshot-registry':
    snapshot_registry()
elif action == 'restore':
    assert '--editor-stopped' in sys.argv
    restore_files()
    restore_registry()
elif action == 'prepare-native':
    assert '--editor-stopped' in sys.argv
    restore_files()
    restore_registry()
    fresh = STATE / 'native-fresh-profile.json'
    data = json.loads(fresh.read_text(encoding='utf-8-sig'))
    assert data['version'] == 2 and data['cultivation']['tier'] == 1
    for name in ('campusrift-v2.json', 'campusrift-v2.json.bak'):
        shutil.copy2(fresh, user / name)
    # Use the PlayerPrefs value name actually observed in the Editor product key.
    # Isolate QA settings; the original native product values are restored after both players close.
    editor_key = r'Software\Unity\UnityEditor\DefaultCompany\Campus Rift'
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, editor_key) as key:
        candidates = [winreg.EnumValue(key, i)[0] for i in range(winreg.QueryInfoKey(key)[1]) if winreg.EnumValue(key, i)[0].startswith('CampusRift.Settings.v1_')]
    assert len(candidates) == 1, candidates
    prefs = json.loads(settings['settings'])
    prefs.update(Quality=1, ResolutionWidth=1920, ResolutionHeight=1080, Fullscreen=False, VSync=False, TelemetryConsentAsked=True, LocalTelemetryEnabled=False, Subtitles=True, TextSize=0, AccessibleColors=0, SlowReading=False, ReduceCameraShake=False)
    with winreg.CreateKey(winreg.HKEY_CURRENT_USER, KEY) as key:
        winreg.SetValueEx(key, candidates[0], 0, winreg.REG_BINARY, json.dumps(prefs, separators=(',', ':')).encode('utf-8') + b'\0')
    (STATE / 'native-qa-settings.json').write_text(json.dumps(prefs, indent=2), encoding='utf-8')
    print('Native QA profile isolated; original bytes remain in P23-pre')
else:
    raise ValueError(action)
