"""Snapshot, isolate QA evidence and audit the P20 takeover (no deletes)."""
from pathlib import Path
import hashlib, json, shutil, sys
ROOT = Path(__file__).resolve().parents[1]
QA = ROOT / 'task/p20'
RESUME = ROOT / 'Backups/P20-resume-20261004'
USER = Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift')
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def copytree(source, target):
    if source.exists(): shutil.copytree(source, target, dirs_exist_ok=True)
def snapshot():
    if (RESUME/'manifest.json').exists(): raise RuntimeError('Resume snapshot already exists')
    for rel in ['Assets/Learning/Validation','Assets/CampusRiftUI/Validation','Assets/Progression/Validation','ProjectSettings','task/p20']:
        copytree(ROOT/rel, RESUME/rel)
    copytree(USER, RESUME/'UserProfile')
    for folder in ['Learning','UI']: copytree(ROOT/'Artifacts'/folder, RESUME/'HistoricalArtifacts'/folder)
    files = {str(p.relative_to(RESUME)):digest(p) for p in RESUME.rglob('*') if p.is_file()}
    (RESUME/'manifest.json').write_text(json.dumps(files,indent=2),encoding='utf-8')
    print('Resume snapshot:', len(files), 'files')
def archive(name, folder):
    copytree(ROOT/'Artifacts'/folder, QA/'runs'/name)
    print('Archived',name)
def finish():
    initial=ROOT/'Backups/P20-pre-20261004-021044'
    historical=ROOT/'ï»¿Backups/P20-pre-20261004-021044/HistoricalArtifacts'
    for folder in ['Learning','UI','Economy']:
        copytree(historical/folder,ROOT/'Artifacts'/folder)
    user_checks=[]
    qa_saves=QA/'runs/user-save-before-restore'
    qa_saves.mkdir(parents=True,exist_ok=True)
    for p in (initial/'UserProfile').glob('*'):
        if p.is_file():
            current=USER/p.name
            if current.exists() and digest(current)!=digest(p):
                shutil.copy2(current,qa_saves/p.name)
                shutil.copy2(p,current)
    for p in (initial/'UserProfile').glob('*'):
        if p.is_file():
            current=USER/p.name
            user_checks.append({'file':p.name,'matchesInitial':current.exists() and digest(current)==digest(p)})
    protected=json.loads((QA/'protected-before.json').read_text(encoding='utf-8-sig'))
    for row in protected: row['matches']=digest(Path(row['Path'])).upper()==row['Hash']
    history=[{'file':str(p.relative_to(historical)),'matches':digest(p)==digest(ROOT/'Artifacts'/p.relative_to(historical))} for p in historical.rglob('*') if p.is_file()]
    settings=[{'file':str(p.relative_to(initial/'ProjectSettings')),'matches':digest(p)==digest(ROOT/'ProjectSettings'/p.relative_to(initial/'ProjectSettings'))} for p in (initial/'ProjectSettings').rglob('*') if p.is_file()]
    audit={'protected':protected,'userProfile':user_checks,'historicalFiles':len(history),'historicalMismatches':[r for r in history if not r['matches']],'settingsMismatches':[r for r in settings if not r['matches']]}
    (QA/'final-disk-audit.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(audit,ensure_ascii=True))
if __name__=='__main__':
    if sys.argv[1]=='snapshot': snapshot()
    elif sys.argv[1]=='archive': archive(sys.argv[2],sys.argv[3])
    elif sys.argv[1]=='finish': finish()
