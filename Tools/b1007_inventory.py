from pathlib import Path
import re,json
ROOT=Path.cwd();cases=[]
def add(name,component,report,done=None,timeout=360,contains=None,code=None,scene='SampleScene'):
    cases.append(dict(name=name,component=component,report=report,done=done or report,timeout=timeout,contains=contains,code=code,scene=scene))
src=(ROOT/'Tools/p23_regressions.py').read_text(encoding='utf-8-sig')
exec(src[src.index('# Reuse the complete'):src.index("(OUT/'inventory")])
# Duplicate aliases call the very same harness; count it once.
cases=[c for c in cases if c['name'] not in ('P18Legacy','P18Reactions','P18BaseSkills')]
# Use the current default functional Reaction smoke. Historical balance/rank
# matrices stay excluded by task/TEST-POLICY.md; the suite's assertions are intact.
for case in cases:
    if case['name']=='Reaction':case['code']=None
add('DevMode','CampusRift.Progression.DevModePlayTest','task/devmode/DevModePlayTest.json','task/devmode/DevModePlayTest-DONE.txt',400)
add('ComicTextAudit',None,None,code='return CampusRift.UI.ComicTextAudit.Scan("batch1007 Gameplay");')
order=['HubFlow','HubLayout','LookSmoke','MobileControls','BoostEnergy','LevelFlow','Level8to10PlayTest','EnemyRosterPlayTest','DevMode','ComicTextAudit','Economy','ExtendedLearning']
cases.sort(key=lambda c:order.index(c['name']) if c['name'] in order else len(order))
if __name__=='__main__':
    out=ROOT/'task/batch-1007/regression';out.mkdir(exist_ok=True)
    (out/'inventory.json').write_text(json.dumps(cases,indent=2),encoding='utf-8')
    print(len(cases),'suites');print('\n'.join(c['name']+' : '+str(c['component'] or c['code']) for c in cases))
