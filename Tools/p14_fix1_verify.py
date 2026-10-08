from pathlib import Path
import hashlib, json
from PIL import Image

root=Path('task/p14')
def read(name): return json.loads((root/name).read_text(encoding='utf-8-sig'))
def digest(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
baseline=read('fix1-baseline.json')
changes=[name for name,h in baseline.items() if not Path(name).exists() or digest(name)!=h]
expected=['Assets/SkyBeast/Runtime/Attacks/SkyStrikePool.cs','Assets/SkyBeast/Runtime/FireBreathVisuals.cs','Assets/SkyBeast/SkyStrike.shader']
preservation=dict(files=len(baseline),unchanged=len(baseline)-len(changes),changed=changes,onlyExpectedChanges=sorted(changes)==sorted(expected),geometryUnchanged=all(digest(n)==baseline[n] for n in ['Assets/Models/Comic_Vibrant_Elevator_System_T77/Comic_Vibrant_Elevator_System_T77.fbx','Assets/MonsterShaban/CampusNavMesh.asset']))
(root/'fix1-preservation.json').write_text(json.dumps(preservation,indent=2),encoding='utf-8')
capture=read('fix1-capture.json'); samples=capture['samples'];rest,active=samples
fps=dict(rest=rest['fps'],active=active['fps'],dropPercent=(rest['fps']-active['fps'])/rest['fps']*100,sameCamera=rest['camera']==active['camera'] and rest['angle']==active['angle'],sameQuality=rest['quality']==active['quality'] and rest['pipeline']==active['pipeline'],sixStrikesThroughout=active['minStrikes']==6 and active['maxStrikes']==6,samples=len(samples))
images=[]
for shot in capture['shots']:
    p=root/'screens/fix1'/(shot['image']+'.png')
    audit=json.loads(p.with_name(p.stem+'-text-audit.json').read_text(encoding='utf-8-sig'))
    with Image.open(p) as im: dimensions=list(im.size)
    images.append(dict(path=p.as_posix(),dimensions=dimensions,issues=len(audit['issues']),sha256=digest(p)))
sky=read('fix1-SkyBeast.json');fire=read('fix1-FireBreath.json')
state=read('fix1-final-state.json');console=read('fix1-final-console.json')
summary=dict(preservation=preservation,fps=fps,images=images,sky=dict(passed=sky['passed'],failed=sky['failed']),fire=dict(passed=fire['passed'],failed=fire['failed']),editor=state,console=console)
summary['allPassed']=preservation['onlyExpectedChanges'] and preservation['geometryUnchanged'] and fps['sameCamera'] and fps['sameQuality'] and fps['sixStrikesThroughout'] and len(images)==6 and all(x['issues']==0 and x['dimensions']==[1920,1080] for x in images) and sky['failed']==0 and fire['failed']==0 and state['target']=='Android' and not any(state[k] for k in ['playing','dirty','compiling','updating']) and not state['shaderErrors'] and state['fixtures']==0 and not console['data']
(root/'fix1-verification-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps(dict(allPassed=summary['allPassed'],preservation=preservation,fps=fps,sky=summary['sky'],fire=summary['fire'],editor=state),indent=2))
assert summary['allPassed']
