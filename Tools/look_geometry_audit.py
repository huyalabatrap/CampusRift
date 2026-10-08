"""Compare serialized physical scene records, excluding material assignments only."""
from pathlib import Path
import re, json, hashlib
ROOT=Path(__file__).resolve().parents[1]
BACKUP=ROOT/(ROOT/'task/look/BACKUP.txt').read_text(encoding='utf-8-sig').strip()
CLASSES={4,23,33,54,64,65,135,136,143,195,196,208}
def records(path):
    text=path.read_text(encoding='utf-8-sig')
    out={}
    for kind,entity,body in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)',text,re.S):
        if int(kind) not in CLASSES: continue
        if int(kind)==23:
            body=re.sub(r'  m_Materials:\n(?:  - .*\n)*','',body)
        out[(kind,entity)]=body
    return out
if __name__=='__main__':
    relative=Path('Assets/Scenes/SampleScene.unity')
    before=records(BACKUP/relative);after=records(ROOT/relative)
    changed=[f'{k[0]}:{k[1]}' for k in before if before[k]!=after.get(k)]
    added=[f'{k[0]}:{k[1]}' for k in after if k not in before]
    fbx=Path('Assets/Models/Comic_Vibrant_Elevator_System_T77/Comic_Vibrant_Elevator_System_T77.fbx')
    digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
    result={'scenePhysicalRecords':len(before),'changed':changed,'added':added,'fbxSHA256':digest(ROOT/fbx),'fbxUnchanged':digest(ROOT/fbx)==digest(BACKUP/fbx)}
    for p in (ROOT/'Assets').rglob('*NavMesh*.asset'):
        r=p.relative_to(ROOT);old=BACKUP/r
        if old.exists():result[str(r)]=digest(p)==digest(old)
    (ROOT/'task/look/geometry-audit.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result,indent=2))
