"""Read-only final evidence gate. The final report is written only after this succeeds."""
import json,hashlib
from pathlib import Path
from p13_cli import call
root=Path(__file__).resolve().parents[1]
out=root/'task/perf'
def read(p):return json.loads((out/p).read_text(encoding='utf-8-sig'))
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
backup=Path((out/'BACKUP.txt').read_text(encoding='utf-8-sig').strip())
b=read('before-pc.json');a=read('after-pc.json')
before={s['name']:s for s in b['samples']};after={s['name']:s for s in a['samples']}
for name in ('Rest','Warning','Breath','Afterfire','Dragon9-Rest','Dragon9-Breath','Mobile-Rest','Mobile-Breath'):
 x,y=before[name],after[name]
 assert (x['cameraPosition'],x['cameraEuler'],x['width'],x['height'])==(y['cameraPosition'],y['cameraEuler'],y['width'],y['height']),name
 assert x['width']==1920 and x['height']==1080
 assert x['seconds']>=5 and y['seconds']>=5
 if not name.startswith('Mobile'):
  assert x['pipeline']==y['pipeline']=='PC_RPAsset' and x['qualityIndex']==y['qualityIndex']==1 and x['comicInk'] and y['comicInk']
 else:assert x['pipeline']==y['pipeline']=='Mobile_RPAsset' and x['qualityIndex']==y['qualityIndex']==0
pc_loss=100*(1-after['Breath']['fps']/after['Rest']['fps'])
dragon_loss=100*(1-after['Dragon9-Breath']['fps']/after['Dragon9-Rest']['fps'])
mobile_loss=100*(1-after['Mobile-Breath']['fps']/after['Mobile-Rest']['fps'])
low_loss=100*(1-after['PC-Low-Breath']['fps']/after['PC-Low-Rest']['fps'])
assert pc_loss<=10 and dragon_loss<=10
assert after['Breath']['budget']==72 and after['Mobile-Breath']['budget']==36 and after['PC-Low-Breath']['budget']==48
fire=read('FireBreath.json');assert fire['passed']==42 and fire['failed']==0
for name in ('before-pc-console.json','after-pc-console.json','smoke-console.json','others-mobile-console.json'):assert read(name)['data']==[]
others={s['name']:s for s in read('others-mobile.json')['samples']}
assert others['Mobile-empty']['visibleEnemies']==0 and others['Mobile-seven-visible-TieuYeu-two-sided']['visibleEnemies']==7
assert all(s['pipeline']=='Mobile_RPAsset' and s['qualityIndex']==0 and s['seconds']>=5 for s in others.values())
unchanged=[]
for directory in ('Assets/CampusLook','Assets/Settings','Assets/CampusRiftUI/Runtime'):
 for old in (backup/directory).rglob('*'):
  if old.is_file():
   rel=old.relative_to(backup);assert digest(root/rel)==digest(old),str(rel);unchanged.append(str(rel))
for rel in ('Assets/Scenes/SampleScene.unity','Assets/SkyBeast/Runtime/FireBreathCycle.cs','Assets/SkyBeast/Runtime/FireBreathProfile.cs','Assets/SkyBeast/Runtime/ShelterDetector.cs','ProjectSettings/EditorSettings.asset'):
 assert digest(root/rel)==digest(backup/rel),rel;unchanged.append(rel)
for old in (backup/'Assets/SkyBeast/Resources/P13').glob('Fire*.asset'):
 rel=old.relative_to(backup);assert digest(root/rel)==digest(old),str(rel);unchanged.append(str(rel))
geometry=json.loads((root/'task/p13/geometry-audit.json').read_text(encoding='utf-8-sig'))
assert digest(root/'Assets/MonsterShaban/CampusNavMesh.asset')==geometry['navMeshSHA256']
assert digest(root/'Assets/Models/Comic_Vibrant_Elevator_System_T77/Comic_Vibrant_Elevator_System_T77.fbx')==geometry['fbxSHA256']
for name in ('before-pc-courtyard-breath.png','after-pc-courtyard-breath.png','before-pc-dragon9-breath.png','after-pc-dragon9-breath.png'):
 assert (out/'screens'/name).is_file()
state=call('execute_code',{'action':'execute','code':(out/'source/final-state.cs.txt').read_text(encoding='utf-8-sig')})['data']['result']
assert state['target']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity'
for key in ('playing','compiling','updating','dirty','shaderErrors','savedSmoke','savedPerf','savedProfiler','savedP13Capture'):assert not state[key],key
assert state['reloadOptionsEnabled'] and state['qualityOverride']=='Automatic'
console=call('read_console',{'action':'get','types':['error'],'count':30,'format':'detailed'});assert console['data']==[]
summary={'pcBreathLossPercent':pc_loss,'dragonBreathLossPercent':dragon_loss,'mobileEditorBreathLossPercent':mobile_loss,'pcLowFireLossPercent':low_loss,'passed':42,'failed':0,'consoleErrors':0,'shaderErrors':0,'sameCamerasAndVerifiedQuality':True,'visibleP12EnemiesInMobileProbe':7,'unchangedFiles':len(unchanged),'geometryNavMeshUnchanged':True,'frameDebuggerEventsBefore':len(read('before-pc-frame-debugger.json')),'frameDebuggerEventsAfter':len(read('after-pc-frame-debugger.json')),'editor':state,'scope':'One valid 5s sample/state before and after; preliminary diagnostic/fixture correction samples retained. One relevant FireBreath smoke. No native/device/full regression.'}
(out/'preservation.json').write_text(json.dumps({'unchangedFiles':unchanged,'fbxSHA256':geometry['fbxSHA256'],'navMeshSHA256':geometry['navMeshSHA256']},indent=2),encoding='utf-8')
(out/'final-state.json').write_text(json.dumps({'editor':state,'console':console},indent=2),encoding='utf-8')
(out/'verification-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k!='editor'},indent=2))
