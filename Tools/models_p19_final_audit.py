from pathlib import Path
import hashlib,json,re,datetime
root=Path(__file__).resolve().parents[1]; out=root/'task/models'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
baseline=json.loads((out/'baseline-hashes.json').read_text());protected=json.loads((out/'protected-hashes.json').read_text())
changed=[p for p,h in baseline.items() if not (root/p).exists() or digest(root/p)!=h]
protected_changes=[p for p,h in protected.items() if not (root/p).exists() or digest(root/p)!=h]
user=[p for p in baseline if p.startswith('Assets/GameReadyModels/')]
user_changes=[p for p in user if p in changed]
backup=Path((out/'BACKUP.txt').read_text().strip())
def blocks(p):return {m.group(2):(m.group(1),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\s*\n(.*?)(?=^--- !u!|\Z)',p.read_text(encoding='utf-8'),re.M|re.S)}
prefabs=[]
for name in ['AnhYeu','TrieuHonSu','DucYeu','HoaLinh']:
 rel=f'Assets/Enemies/Prefabs/{name}.prefab';old=blocks(backup/rel);new=blocks(root/rel)
 # Root GameObject, Transform, LODGroup and animation-profile are visual ownership.
 root_id=next(re.search(r'm_GameObject: \{fileID: (\d+)\}',body).group(1) for typ,body in old.values() if typ=='4' and 'm_Father: {fileID: 0}' in body)
 visual={k for k,(typ,body) in old.items() if typ in ['1','4','205'] or ('  profile:' in body)}
 gameplay={k:(typ,body) for k,(typ,body) in old.items() if k not in visual and f'm_GameObject: {{fileID: {root_id}}}' in body}
 changes=[k for k,v in gameplay.items() if new.get(k)!=v]
 prefabs.append({'prefab':name,'gameplayComponentsChecked':len(gameplay),'changedGameplayComponentIDs':changes,'guidUnchanged':digest(root/(rel+'.meta'))==baseline[rel+'.meta']})
report={'at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'protectedFiles':len(protected),'protectedChanges':protected_changes,'userModelFiles':len(user),'userModelChanges':user_changes,'changedExistingFiles':changed,'prefabs':prefabs,'passed':not protected_changes and not user_changes and all(not p['changedGameplayComponentIDs'] and p['guidUnchanged'] for p in prefabs)}
(out/'final-disk-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2))
