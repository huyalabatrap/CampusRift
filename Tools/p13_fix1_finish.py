"""Read-only final gate for the visual fix, after its one gameplay smoke."""
import json,hashlib
from pathlib import Path
from p13_cli import call
root=Path(__file__).resolve().parents[1]
def read(p):return json.loads((root/p).read_text(encoding='utf-8-sig'))
fire=read('task/p13/fix1-FireBreath.json')
assert (fire['passed'],fire['failed'])==(42,0)
audits=[read(str(p.relative_to(root))) for p in (root/'task/p13/screens/fix1').glob('*-text-audit.json')]
assert len(audits)==8 and all(not a['issues'] for a in audits)
mobileWarning=read('task/p13/fix1-mobile-warning.json')
assert mobileWarning['visible'] and mobileWarning['node']>=0 and mobileWarning['auditIssues']==0
shots={s['image']:s for s in read('task/p13/fix1-capture.json')['shots']}
assert shots['hud-mobile']['meteors']<=36 and shots['hud-mobile']['scorches']<=12 and shots['hud-mobile']['plumes']<=3
assert shots['breath-indoor-window']['shelter']=='Indoor' and shots['breath-indoor-window']['lowPass']==650
assert shots['level9-dragon-breath']['dragonPose']=='Spell_Loop'
backup=Path((root/'task/p13/BACKUP-fix1.txt').read_text(encoding='utf-8-sig').strip())
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
unchanged=['Assets/Scenes/SampleScene.unity','Assets/SkyBeast/Runtime/FireBreathCycle.cs','Assets/SkyBeast/Runtime/FireBreathProfile.cs','ProjectSettings/EditorSettings.asset']
unchanged += [str(p.relative_to(root)) for p in (root/'Assets/SkyBeast/Resources/P13').glob('Fire*.asset')]
assert all(digest(root/p)==digest(backup/p) for p in unchanged)
old=read('task/p13/geometry-audit.json')
assert digest(root/'Assets/MonsterShaban/CampusNavMesh.asset')==old['navMeshSHA256']
assert digest(root/'Assets/Models/Comic_Vibrant_Elevator_System_T77/Comic_Vibrant_Elevator_System_T77.fbx')==old['fbxSHA256']
state=call('execute_code',{'action':'execute','code':(root/'task/p13/fix1-final-state.cs.txt').read_text(encoding='utf-8-sig')})
current=state['data']['result']
assert current['target']=='Android' and current['scene']=='Assets/Scenes/SampleScene.unity'
assert not any(current[k] for k in ['playing','compiling','updating','dirty','shaderErrors','savedSmoke','savedCapture'])
assert current['volumes']==12 and current['reloadOptionsEnabled']
console=call('read_console',{'action':'get','types':['error'],'count':30,'format':'detailed'})
assert console['data']==[]
(root/'task/p13/fix1-final-state.json').write_text(json.dumps({'editor':state,'console':console},indent=2),encoding='utf-8')
summary={'firePass':42,'fireFail':0,'imageTextAudits':8,'mobileWarningAuditIssues':0,'textIssues':0,'preservedHashes':unchanged,'geometryNavMeshUnchanged':True,'consoleErrors':0,'editor':current,'scope':'one relevant smoke; visual QA; one preliminary Editor FPS comparison; no full regression/device build'}
(root/'task/p13/fix1-verification-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps(summary,indent=2))
