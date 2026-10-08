"""Fresh sessions for the complete Shaban navigation/hunt regression group."""
import json,re,shutil,time,sys
from pathlib import Path
import unity_mcp as m
m.initialize();root=Path('Artifacts/P12/regressions');root.mkdir(parents=True,exist_ok=True)
summary_path=root.parent/'NavigationRegressions.json'
summary=json.loads(summary_path.read_text()) if len(sys.argv)>1 and summary_path.exists() else []
def selected(name):return len(sys.argv)==1 or name in sys.argv[1:]
def record(entry):
    global summary
    summary=[x for x in summary if x['test']!=entry['test']]+[entry];save()
def call(name,args):
    for attempt in range(30):
        r=m.call(name,args).get('result',{});d=r.get('structuredContent',r)
        if d.get('success') is not False:return d
        if 'not ready' not in str(d).lower() and 'please retry' not in str(d).lower():raise RuntimeError(str(d))
        time.sleep(2)
    raise RuntimeError(str(d))
def fresh():
    call('manage_editor',{'action':'stop'});time.sleep(1.5)
    call('execute_code',{'action':'execute','code':'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return "opened";'})
    call('read_console',{'action':'clear'})
    call('manage_editor',{'action':'play'});time.sleep(2)
    call('execute_code',{'action':'execute','code':'UnityEngine.Application.runInBackground=true;CampusRift.UI.UIStateManager.Instance.EnterScene(true);return "gameplay";'})
def save():
    summary_path.write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
for name,source,progress,timeout in [
 ('ShabanNavigationPlayTest','Assets/MonsterShaban/Validation/NavigationPlayMode.txt','Temp/shaban-nav-progress.txt',650),
 ('ShabanTraversalPlayTest','Assets/MonsterShaban/Validation/TraversalPlayMode.txt','Temp/shaban-traversal-progress.txt',1400)]:
    if not selected(name):continue
    fresh();started=time.time();call('execute_code',{'action':'execute','code':f'new UnityEngine.GameObject("P12 {name}").AddComponent<{name}>();return "started";'})
    print('START',name,flush=True);p=Path(progress);deadline=time.time()+timeout
    while time.time()<deadline:
        if p.exists() and p.stat().st_mtime>=started and p.read_text().startswith('DONE'):break
        time.sleep(1)
    done=p.exists() and p.stat().st_mtime>=started and p.read_text().startswith('DONE')
    raw=Path(source).read_text(encoding='utf-8-sig') if done else 'TIMEOUT'
    (root/(name+'.txt')).write_text(raw,encoding='utf-8')
    record(dict(test=name,completed=done,passed=[line for line in raw.splitlines() if ' PASS:' in line],failed=[line for line in raw.splitlines() if ' FAIL:' in line] if done else ['TIMEOUT'],capturedAt=time.strftime('%Y-%m-%dT%H:%M:%S')));print(name,summary[-1],flush=True)
for scenario in ['elevator','elevator-return','stairs','room','sprint']:
    if not selected('Hunt-'+scenario):continue
    fresh();started=time.time();p=root/('Hunt-'+scenario+'.txt')
    code='var c=new UnityEngine.GameObject("P12 hunt").AddComponent<ShabanHuntScenarioTest>();c.scenario='+json.dumps(scenario)+';c.output='+json.dumps(p.as_posix())+';return "started";'
    call('execute_code',{'action':'execute','code':code});print('START Hunt',scenario,flush=True);deadline=time.time()+600
    while time.time()<deadline:
        if p.exists() and p.stat().st_mtime>=started and p.read_text().rstrip().endswith('DONE'):break
        time.sleep(1)
    raw=p.read_text(encoding='utf-8-sig') if p.exists() and p.stat().st_mtime>=started else 'TIMEOUT'
    match=re.search(r'RESULT .*reacquiredAt=([-\d.]+)',raw);passed=bool(match and float(match[1])>=0)
    record(dict(test='Hunt-'+scenario,completed=bool(match),passed=passed,failed=[] if passed else ['No reacquisition'],result=next((line for line in raw.splitlines() if line.startswith('RESULT')),'TIMEOUT'),capturedAt=time.strftime('%Y-%m-%dT%H:%M:%S')));print(summary[-1],flush=True)
for group in ['Memory','Motion','Pursuit','Search','Graph','Interception','Belief','Lifts','NavigationRobustness']:
    if not selected('Hunter-'+group):continue
    fresh();d=call('execute_code',{'action':'execute','code':'return ShabanHunterValidation.'+group+'();'});print(d,flush=True)
    for source in Path('Assets/MonsterShaban/Validation/Hunter').glob('*.json'):
        if time.time()-source.stat().st_mtime<15:
            data=json.loads(source.read_text(encoding='utf-8-sig'));shutil.copy2(source,root/('Hunter-'+source.name));record(dict(test='Hunter-'+group,passed=data.get('passed',[]),failed=data.get('failed',[]),capturedAt=time.strftime('%Y-%m-%dT%H:%M:%S')))
call('manage_editor',{'action':'stop'});time.sleep(1.5)
