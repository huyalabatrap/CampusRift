from pathlib import Path
import shutil, hashlib, json, datetime, re
out=Path('task/ar/fix3'); out.mkdir(exist_ok=True)
assert not (out/'manifest.json').exists(), 'Continue existing backup'
backup=Path('Backups')/('AR-fix3-pre-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'))
paths=set()
for folder in ['Assets/ARRift','Assets/Plugins/Android','Assets/Settings','ProjectSettings']:
    paths.update(p for p in Path(folder).rglob('*') if p.is_file())
paths.add(Path('Assets/Editor/ARFix2LocalRunner.cs'))
paths.update(Path('task').glob('run-*.ps1')); paths.add(Path('task/codex-accounts.json'))
rows=[]
for p in sorted(paths):
    q=backup/p; q.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(p,q)
    rows.append(dict(path=p.as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
shutil.copytree('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift',backup/'UserSave')
Path('task/ar/FIX3-BACKUP.txt').write_text(str(backup.resolve()),encoding='utf-8')
(out/'manifest.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
Path('task/ar/screens/fix3').mkdir(exist_ok=True)
log=Path('task/ar/device-logs/fix2-realme.txt').read_text(encoding='utf-8-sig')
(out/'device-log-review.json').write_text(json.dumps(dict(lines=len(log.splitlines()),obsoleteByteArray=log.count('AndroidJNIHelper: converting Byte array is obsolete'),diagnostics=re.findall(r'^.*\[ARDiag\].*$',log,re.M),exceptions=re.findall(r'^.*(?:Exception|E/Unity).*$',log,re.M)),indent=2),encoding='utf-8')
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix3 — tiếp quản / backup 05/10\n- Đọc PROMPT-fix3 đầy đủ, REPORT-fix2, SPEC, log Realme, PROGRESS và TEST-POLICY; chưa có code/evidence fix3. Không làm lại fix2. Backup FIX3-BACKUP.txt + fix3/manifest.json + UserSave; bảo vệ script điều phối.\n- MCP HTTP không chạy; Unity đang mở, dùng local runner sẵn có cho Editor. Chưa harness/build fix3.\n')
print(backup,len(rows))
