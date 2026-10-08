"""Summarize saved evidence; no gameplay, benchmark, or regression reruns."""
from pathlib import Path
import json, re
root=Path('task/p15')
def read(name):return json.loads((root/name).read_text(encoding='utf-8-sig'))
state=read('fix1-final-state.json')['data']['result'];console=read('fix1-final-console.json')
smokes={name:read('fix1-'+name+'.json') for name in ('HeavenSword','SkyBeast')}
preservation=read('fix1-preservation.json');door=read('fix1-door-probe.json')
audits=[json.loads(p.read_text(encoding='utf-8-sig')) for p in (root/'screens/fix1').glob('*-text-audit.json')]
fps=read('fix1-fps.json')
checks={
    'smoke_once_each_pass':all(r['failed']==0 for r in smokes.values()),
    'final_android_edit_clean':state['target']=='Android' and not any(state[k] for k in ('playing','compiling','updating','dirty','inputDirty')) and state['scene']=='Assets/Scenes/SampleScene.unity',
    'console_empty':not console['data'],
    'shaders_no_errors':all(not s['errors'] and not s['messages'] for s in state['shaders']),
    'nine_images_audited':len(audits)==9 and all(not a['issues'] for a in audits),
    'door_label_at_zero_verified':door['verified'] and door['distance']<=1 and not door['marker'],
    'no_files_lost_or_core_changed':not preservation['missing'] and all(x['sameAsP15'] for x in preservation['preservedCore']) and preservation['qualitySettingsSame'],
    'correct_two_accepted_fps_pipelines':{x['sample']['pipeline'] for x in fps['samples']}=={'PC_RPAsset','Mobile_RPAsset'},
    'input_options_restored':state['reload'] and state['inputBg']=='ResetAndDisableNonBackgroundDevices' and state['inputEditor']=='PointersAndKeyboardsRespectGameViewFocus'
}
report_path=root/'REPORT-P15-fix1.md'
if report_path.exists():
    links=re.findall(r'\]\(([^)]+)\)',report_path.read_text(encoding='utf-8-sig'))
    missing=[link for link in links if not (root/link).exists()]
    checks['report_links_exist']=not missing
    print(json.dumps({'report_links':len(links),'missing':missing},ensure_ascii=False))
summary={'complete':all(checks.values()),'checks':checks,'smokes':{n:{'passed':r['passed'],'failed':r['failed']} for n,r in smokes.items()},'screens':9,'fps':fps['samples'],'preservation':{'baseline':preservation['baseline'],'unchanged':preservation['unchanged'],'changed':preservation['changed'],'missing':preservation['missing']},'limits':'Smoke only. HeavenSword precedes final rune-light polish, which was compiled and directly captured without runtime errors. FPS samples precede that cosmetic polish. No native/device or balance measurements; no full regression.'}
(root/'fix1-verification-summary.json').write_text(json.dumps(summary,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'complete':summary['complete'],'checks':checks},ensure_ascii=False))
if not summary['complete']:raise SystemExit('Saved evidence gate incomplete')
