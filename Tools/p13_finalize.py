"""Collect final evidence; no additional gameplay tests or benchmark."""
import json,hashlib
from pathlib import Path
from p13_cli import call
root=Path(__file__).resolve().parents[1]
def read(p):return json.loads((root/p).read_text(encoding='utf-8-sig'))
fire=read('Artifacts/SkyBeast/FireBreath.json');layer=read('Artifacts/SkyBeast/LayerSmoke.json');audit=read('Artifacts/SkyBeast/ShelterAudit.json');geometry=read('task/p13/geometry-audit.json')
assert (fire['passed'],fire['failed'])==(42,0)
assert (layer['passed'],layer['failed'])==(11,0)
assert audit['nodes']==2126 and audit['accuracy']>=98 and all(m['reason'] for m in audit['mismatches'])
assert not geometry['changedExisting'] and geometry['fbxUnchangedFromLOOK'] and geometry['navMeshUnchangedFromLOOK']
texts=[read('task/p13/look-text-audit.json')]+[read(str(p.relative_to(root))) for p in (root/'task/p13/screens').glob('*-text-audit.json')]
assert len(texts)==7 and all(not t['issues'] for t in texts)
for entry in read('task/p13/source/manifest.json'):
    assert hashlib.sha256((root/entry['file']).read_bytes()).hexdigest()==entry['sha256']
state=call('execute_code',{'action':'execute','code':(root/'task/p13/final-state.cs.txt').read_text(encoding='utf-8-sig')})
current=state['data']['result'];assert current['target']=='Android' and current['scene']=='Assets/Scenes/SampleScene.unity'
assert not any(current[k] for k in ['playing','compiling','updating','dirty','shaderErrors','savedSmoke','savedCapture'])
assert current['volumes']==12 and current['reloadOptionsOriginal']
errors=call('read_console',{'action':'get','types':['error'],'count':30,'format':'detailed'})
assert errors['data']==[]
(root/'task/p13/final-state.json').write_text(json.dumps({'editor':state,'console':errors},indent=2),encoding='utf-8')
summary={'firePass':42,'layerPass':11,'auditPassed':audit['passed'],'auditNodes':2126,'auditAccuracy':audit['accuracy'],'acceptedMismatches':40,'textAudits':len(texts),'textIssues':0,'sourceSHA256Verified':5,'geometryUnchanged':True,'finalEditor':current,'consoleErrors':0,'scope':'smoke only; no FPS/benchmark/full regression/device build'}
(root/'task/p13/verification-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps(summary,indent=2))
