"""Existing distinct functional harnesses omitted by the inherited inventory."""
from b1007_runner import *
jobs = [
    dict(name='P23BaseSkills', component='CampusRift.UI.P23BaseSkillRegression', report='Artifacts/V2/P23-base-skills/result.json', done='Artifacts/V2/P23-base-skills/DONE.txt', timeout=600, contains=None, code=None, scene='SampleScene'),
    dict(name='Accessibility', component='CampusRift.UI.AccessibilityPlayTest', report='task/p23/accessibility.json', done='task/p23/accessibility-DONE.txt', timeout=420, contains=None, code=None, scene='MainMenu'),
    dict(name='P23Gameplay', component='CampusRift.UI.P23GameplaySmoke', report='task/p23/gameplay.json', done='task/p23/gameplay-DONE.txt', timeout=300, contains=None, code=None, scene='SampleScene'),
]
if __name__ == '__main__':
    selected=set(sys.argv[1:])
    for case in jobs:
        if not selected or case['name'] in selected:
            run(case)
            save(OUT/'Summary.json', [json.loads(p.read_text(encoding='utf-8')) for p in (OUT/'runs').glob('*/row.json')])
    stop()
