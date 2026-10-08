"""P13 read-only geometry/NavMesh preservation check against its pre-change scene."""
from pathlib import Path
import json,hashlib
from look_geometry_audit import records
root=Path(__file__).resolve().parents[1]
backup=Path((root/'task/p13/BACKUP.txt').read_text(encoding='utf-8-sig').strip())
scene=Path('Assets/Scenes/SampleScene.unity')
before,after=records(backup/scene),records(root/scene)
changed=[f'{k[0]}:{k[1]}' for k in before if before[k]!=after.get(k)]
added=[f'{k[0]}:{k[1]}' for k in after if k not in before]
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
fbx=root/'Assets/Models/Comic_Vibrant_Elevator_System_T77/Comic_Vibrant_Elevator_System_T77.fbx'
nav=root/'Assets/MonsterShaban/CampusNavMesh.asset'
result={'existingPhysicalRecords':len(before),'changedExisting':changed,'addedPhysicalRecords':added,
 'addedReason':'P13 root/IndoorVolume shaft transforms and BoxCollider triggers only; no solid campus geometry',
 'fbxSHA256':digest(fbx),'fbxUnchangedFromLOOK':digest(fbx)==json.loads((root/'task/look/geometry-audit.json').read_text())['fbxSHA256'],
 'navMeshSHA256':digest(nav),'navMeshUnchangedFromLOOK':digest(nav)==json.loads((root/'task/look/navmesh-audit.json').read_text())['sha256']}
(root/'task/p13/geometry-audit.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
