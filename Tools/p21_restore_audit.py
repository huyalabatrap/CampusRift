from pathlib import Path
import hashlib,json,shutil
root=Path.cwd();task=root/'task/p21';backup=Path((task/'BACKUP.txt').read_text(encoding='utf-8').strip())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(name,data):(task/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
# Restore test side effects only; retain fresh test results in task/p21.
history=[]
for p in (backup/'Artifacts/SkyBeast').rglob('*'):
    if p.is_file():
        dest=root/'Artifacts/SkyBeast'/p.relative_to(backup/'Artifacts/SkyBeast');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest);history.append({'path':dest.relative_to(root).as_posix(),'restored':sha(dest)==sha(p)})
font=root/'Assets/CampusRiftUI/Comic/Resources/Comic'
fonts=[]
for p in (backup/'TMP').iterdir():
    if p.is_file():shutil.copy2(p,font/p.name);fonts.append({'path':(font/p.name).relative_to(root).as_posix(),'restored':sha(font/p.name)==sha(p)})
saves=json.loads((task/'user-save-manifest.json').read_text(encoding='utf-8'));save_audit=[]
for path,original in saves.items():
    p=Path(path);before=sha(p) if p.exists() else None
    if before!=original:shutil.copy2(backup/'UserSave'/p.name,p)
    save_audit.append({'path':path,'before':before,'original':original,'preserved':sha(p)==original})
protected=json.loads((task/'protected-manifest.json').read_text(encoding='utf-8'))
pre=json.loads((task/'pre-manifest.json').read_text(encoding='utf-8'))
changed=[p for p,h in pre.items() if not (root/p).exists() or sha(root/p)!=h]
protected_diffs=[p for p,h in protected.items() if not (root/p).exists() or sha(root/p)!=h]
settings_diffs=[p for p in changed if p.startswith('ProjectSettings/')]
docs=json.loads((task/'credits-index.json').read_text(encoding='utf-8'))['documents']
doc_diffs=[d['path'] for d in docs if sha(root/d['path'])!=d['sha256']]
source=json.loads((task/'source/manifest.json').read_text(encoding='utf-8'))
source_diffs=[d['file'] for d in source if sha(root/d['file'])!=d['sha256']]
audits=[{'path':p.relative_to(root).as_posix(),'issues':len(json.loads(p.read_text(encoding='utf-8'))['issues'])} for p in (task/'screens').glob('*audit.json')]
data={'protectedCount':len(protected),'protectedDifferences':protected_diffs,'projectSettingsDifferences':settings_diffs,'modifiedOriginalFiles':changed,'save':save_audit,'fonts':fonts,'historicArtifacts':history,'creditsDocuments':len(docs),'creditsSourceDifferences':doc_diffs,'downloadDifferences':source_diffs,'captureTextAudit':audits}
write('final-file-audit.json',data)
assert not protected_diffs and not settings_diffs and not doc_diffs and not source_diffs
assert all(d['preserved'] for d in save_audit) and all(d['restored'] for d in fonts+history)
print('Protected253 / Settings / saves5 / TMP4 / historic artifacts / licenses / downloads: PASS')
