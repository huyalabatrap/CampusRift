"""Fail closed on stale, incomplete or failed fix3 delivery evidence."""
from pathlib import Path
from datetime import datetime
import json

def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))

skills=['van-kiem-quyet','than-kiem-ngu-loi','phat-no-hoa-lien','tich-lich-nhat-thiem']
reactions=['bang-loi-liet','dien-luu','bao-viem','tu-sat','pha-giap']
root=Path('Artifacts/Skills/fix3')
source=[Path(p) for p in [
    'Assets/Skills/Core/Runtime/SkillVfxPool.cs',
    'Assets/Skills/Core/P10Surface.shader',
    'Assets/Skills/Core/P10LayeredStroke.shader',
    'Assets/Skills/Core/P10FireBillow.shader',
    'Assets/CampusRiftUI/Comic/ComicInk.shader',
    'Assets/CampusRiftUI/Runtime/ComicInkFeature.cs',
    'Assets/Combat/Runtime/ReactionFeedback.cs',
    'Assets/Combat/Runtime/DamageNumberPool.cs',
    'Assets/Skills/GiantHandSeal/Runtime/MonsterVitality.cs',
    'Assets/CampusRiftUI/Runtime/ItemBarUI.cs',
    'Assets/CampusRiftUI/Runtime/GenerationChainUI.cs',
    'Assets/CampusRiftUI/Runtime/ReactionHintUI.cs']]
fresh=max(p.stat().st_mtime for p in source)
for name in skills:
    d=read(root/(name+'-visual.json'))
    assert datetime.fromisoformat(d['capturedAt'].replace('Z','+00:00')).timestamp()>fresh
    assert d['flashCaptured'] and d['poolObjectsBefore']==d['poolObjectsAfter']
    assert d['peakPC']<=1500 and d['peakMobile']<=400
    for suffix in ['sheet-light','sheet-dark','impact','impact-light','impact-dark','mobile']:
        p=Path('task/p10/screens/fix3')/(name+'-'+suffix+'.png')
        assert p.exists() and p.stat().st_mtime>fresh,p
    for mode in ['light','dark']:
        for frame in range(8):
            assert (Path('task/p10/screens/fix3/frames')/f'{name}-{mode}-{frame}.png').stat().st_mtime>fresh
d=read('Artifacts/Reactions/fix3/Visual.json')
assert d['issues']==0 and not d['error'] and len(d['audits'])==73
assert d['iceCombo'] and d['convergenceCombo'] and len(d['captured'])==15
assert datetime.fromisoformat(d['capturedAt'].replace('Z','+00:00')).timestamp()>fresh
for m in read('Artifacts/Reactions/fix3/Visual-Vfx.json')['measurements']:
    assert m['exhausted']==0 and m['peak']<=(400 if m['mobile'] else 1500)
for name in reactions:
    for suffix in ['sheet-light','sheet-dark','impact','impact-light','impact-dark','mobile']:
        p=Path('task/p11/screens/fix3')/(name+'-'+suffix+'.png')
        assert p.exists() and p.stat().st_mtime>fresh,p
for name in ['crowd-three-reactions','generation-3-Vietnamese-PC-1920','generation-3-Vietnamese-Mobile-1920','hint-bang-loi-liet-Vietnamese-PC-1920']:
    assert (Path('task/p11/screens/fix3')/(name+'.png')).stat().st_mtime>fresh
tests=read(root/'Regressions.json');assert len(tests)==15
for t in tests:
    assert t['completed'] and not t['failed'],t
    assert Path(t['report']).stat().st_mtime>fresh,t
    if t['test']=='PhantomDecoy':assert t['baselineKnownFail'] and t['result'].strip()=='FAIL'
    else:
        assert not t['baselineKnownFail']
        raw=read(t['report'])
        assert not raw.get('error') and not raw.get('failed'),t
        if t['test']=='ComicTextAudit':assert raw['issues']==0 and len(raw['screens'])==22
        else:assert 'PASS' in t['result'] or '0 failed' in t['result'],t
assert read(root/'GameplayDataAudit.json')['passed']
for r in [root,Path('Artifacts/Reactions/fix3')]:
    p=r/'Performance-Player.json';d=read(p)
    assert p.stat().st_mtime>fresh and not d.get('error') and d['renderedFrames']>0
    assert all(d[k] for k in ['hdr','bloom','comicInk','postProcessing'])
    assert d['width']==1920 and d['height']==1080
    assert d['quality']=='PC' and d['pipeline']=='PC_RPAsset'
    assert (r/'Performance-Player-DONE.txt').read_text(encoding='utf-8-sig').strip()=='PASS'
    if 'trials' in d:
        assert {t['skill'] for t in d['trials']}==set(skills)
        assert all(t['withinTenPercent'] and t['lossPercent']<=10 and t['maxVfxFrameGCBytes']==0 and t['peakParticles']<=1500 for t in d['trials'])
    else:
        assert d['passed'] and d['lossPercent']<=10 and d['exhausted']==0 and d['objectsBefore']==d['objectsAfter']
        assert d['maxVfxFrameGCBytes']==0 and d['peakParticles']<=1500
state=read(root/'FinalEditorState.json')
assert state['target']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity'
assert not state['playing'] and not state['compiling'] and not state['updating']
assert not read(root/'FinalConsoleErrors.json')['data']
print('All fix3 evidence verified; 4 skills, 5 reactions, 95 text audits, 15 regression suites, both native benchmarks, Android/Edit/SampleScene/0 errors.')
