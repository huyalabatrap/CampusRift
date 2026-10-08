"""Refuse completion without fresh raw QA, captures, regressions and a valid native benchmark."""
import datetime,json,pathlib
def read(path):return json.loads(pathlib.Path(path).read_text(encoding='utf-8-sig'))
qa=read('Artifacts/Reactions/Validation.json');visual=read('Artifacts/Reactions/Visual.json');perf=read('Artifacts/Reactions/Performance-Player.json');regressions=read('Artifacts/Reactions/Regressions.json')
assert not qa['failed'] and not qa['error']
assert visual['issues']==0 and not visual['error'] and visual['iceCombo'] and visual['convergenceCombo']
assert len(visual['audits'])>=73 and len(visual['captured'])==15
assert perf['passed'] and not perf['error'] and perf['lossPercent']<=10
assert perf['objectsBefore']==perf['objectsAfter'] and perf['exhausted']==0
assert all(perf[k] for k in ('postProcessing','bloom','hdr','comicInk'))
assert perf['width']==1920 and perf['height']==1080 and perf['quality']=='PC'
assert perf['renderedFrames']>perf['samples']>0
assert len(regressions)==14 and all(r['completed'] and not r['failed'] for r in regressions)
vfx=read('Artifacts/Reactions/Visual-Vfx.json')['measurements'];assert len(vfx)==15 and all(v['exhausted']==0 for v in vfx)
assert next(r for r in regressions if r['test']=='PhantomDecoy')['baselineKnownFail']
started=datetime.datetime.fromisoformat(visual['capturedAt'].replace('Z','+00:00')).timestamp()
root=pathlib.Path('task/p11/screens');rules=read('Assets/Combat/Audio/Reactions/sources.json')
for name in ('bang-loi-liet','dien-luu','bao-viem','tu-sat','pha-giap'):
    for mode in ('light','dark'):
        assert (root/f'{name}-sheet-{mode}.png').exists()
        for index in range(8):assert (root/'frames'/f'{name}-{mode}-{index}.png').stat().st_mtime>=started
        assert (root/f'{name}-impact-{mode}.png').stat().st_mtime>=started
    assert (root/f'{name}-mobile.png').stat().st_mtime>=started
assert len(rules)==11
print('P11 artifact guards PASS:',len(qa['passed']),'QA checks;',len(visual['audits']),'UI screens;',len(regressions),'regressions; native loss',round(perf['lossPercent'],2),'%; pool',perf['objectsBefore'],'->',perf['objectsAfter'])
