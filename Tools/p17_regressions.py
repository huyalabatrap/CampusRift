"""P17: each existing functional suite once, fresh Play session, preserve raw failures."""
import json,pathlib,re,shutil,time,sys,traceback
import unity_mcp as m
ROOT=pathlib.Path('.');OUT=ROOT/'Artifacts/V2/P17-regression';OUT.mkdir(parents=True,exist_ok=True)
m.initialize()
def call(name,args):
    for attempt in range(35):
        raw=m.call(name,args);result=raw.get('result',{}).get('structuredContent',raw)
        if result.get('success') is not False:return result
        if not any(s in str(result).lower() for s in ('not ready','please retry','compiling','domain reload')):raise RuntimeError(json.dumps(result))
        time.sleep(2)
    raise RuntimeError(str(result))
cases=[]
def add(name,component,report,done=None,timeout=360,contains=None,code=None,scene='SampleScene'):
    cases.append(dict(name=name,component=component,report=report,done=done or report,timeout=timeout,contains=contains,code=code,scene=scene))
source=(ROOT/'Assets/Levels/Validation/V2RegressionRunner.cs').read_text(encoding='utf-8-sig')
for line in source.splitlines():
    hit=re.search(r'Name = "([^"]+)".*Harness = typeof\(([^)]+)\).*Done = "([^"]+)".*Report = "([^"]+)"',line)
    if not hit:continue
    name,kind,done,report=hit.groups()
    if kind.startswith(('Shaban','Controls.')):kind=kind if kind.startswith('Shaban') else 'CampusRift.'+kind
    else:kind='CampusRift.'+('Levels.' if '.' not in kind else '')+kind
    if name=='SkyVictory':kind='CampusRift.UI.SkyVictoryPlayTest'
    if name=='ShabanBehavior':report='Assets/MonsterShaban/Validation/BehaviorPlayMode.json'
    add(name,kind,report,done,900 if name=='ShabanTraversal' else 600 if name=='ShabanNavigation' else 420,'DONE' if done.startswith('Temp/') else 'Twenty' if name=='MobileChase' else None)
for scenario in ('elevator','elevator-return','stairs','room','sprint'):
    name='ShabanHunt-'+scenario;path='Temp/shaban-scenario-'+scenario+'.txt'
    add(name,None,path,path,360,'DONE','var t=new UnityEngine.GameObject("P17 '+name+'").AddComponent<ShabanHuntScenarioTest>();t.scenario="'+scenario+'";t.output="'+path+'";return "started";')
for name,typ,path,timeout in (
 ('SkillSet1','SkillSet1PlayTest','SkillSet1',900),('SkillSet1Pool','SkillSet1PoolPlayTest','SkillSet1-Pool',650),('SkillSet1Edges','SkillSet1EdgePlayTest','SkillSet1-Edges',300),('LightningFlash','LightningFlashPlayTest','LightningFlash',300)):
    add(name,'CampusRift.Skills.'+typ,'Artifacts/Skills/'+path+'.json','Artifacts/Skills/'+path+'-DONE.txt',timeout)
add('Reaction','CampusRift.Combat.ReactionPlayTest','Artifacts/Reactions/Validation.json','Artifacts/Reactions/DONE.txt')
add('Localization','CampusRift.Localization.LocalizationPlayTest','Artifacts/Localization/PlayMode.json','Artifacts/Localization/DONE.txt')
for name in ('EnemyAbilitiesPlayTest','EnemyAnimationPlayTest','EnemyReactionAnimationPlayTest','ShabanBossPlayTest','Level3to7PlayTest','P12StarEventPlayTest','P12AudioEndingPlayTest','P12SkyPosePlayTest','SkyBeastPresencePlayTest','P12SimpleSmoke','P12Fix1VisualSmoke','Level8to10PlayTest'):
    add(name,'CampusRift.Validation.'+name,'Artifacts/P12/tests/'+name+'.json','Artifacts/P12/tests/'+name+'-DONE.txt',720)
for name in ('FireBreath','SkyBeast','HeavenSword','ShelterAudit'):
    add(name,'CampusRift.SkyBeast.'+name+'PlayTest','Artifacts/SkyBeast/'+name+'.json','Artifacts/SkyBeast/'+name+'-DONE.txt',420)
add('P14DevSmoke','CampusRift.SkyBeast.P14DevSmoke','task/p14/dev-smoke.json','task/p14/dev-DONE.txt',180)
add('P13LayerSmoke','CampusRift.SkyBeast.P13LayerSmoke','Artifacts/SkyBeast/LayerSmoke.json','Artifacts/SkyBeast/LayerSmoke-DONE.txt')
# UIPlayValidation and ComicReview/ComicOutcome generate photo matrices: excluded by TEST-POLICY.
add('LookSmoke','CampusRift.Controls.LookSmoke','task/look/smoke.json','task/look/smoke-DONE.txt',180)
for name in ('Memory','Motion','Interception','Graph','Search','Belief','Lifts','NavigationRobustness','Pursuit'):
    add('Hunter-'+name,None,None,code='return ShabanHunterValidation.'+name+'();')
for name,code,report in (
 ('CampusStairs','return CampusTraversalValidation.RunStairs("P17");','Assets/TraversalFixes/P17-Stairs.json'),
 ('CampusDoors','return CampusTraversalValidation.RunDoors("P17");','Assets/TraversalFixes/P17-Doors.json'),
 ('ElementChart','return CombatValidation.ElementChartAndCalculator();','Artifacts/Combat/ElementChart.json'),
 ('Cultivation','return CultivationValidation.Validate();','Artifacts/Progression/Cultivation.txt'),
 ('LevelData','return LevelValidation.Validate();','Artifacts/Levels/LevelData.txt'),
 ('UILayout','return UIValidation.AuditLayout();',None)):
    add(name,None,report,code=code)
summary_path=OUT/'Summary.json'
summary=json.loads(summary_path.read_text(encoding='utf-8')) if summary_path.exists() else []
finished={x['name'] for x in summary}
selected=set(sys.argv[1:])
for case in cases:
    name=case['name']
    if name in finished or (selected and name not in selected):continue
    row=dict(name=name,started=time.strftime('%Y-%m-%dT%H:%M:%S'),status='ERROR')
    print('START '+name,flush=True);(OUT/'PROGRESS.txt').write_text('Running '+name,encoding='utf-8')
    begin=time.time()
    try:
        call('manage_editor',{'action':'stop'});time.sleep(1)
        call('execute_code',{'action':'execute','code':'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/'+case['scene']+'.unity"); return "opened";'})
        call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});time.sleep(1)
        setup='Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.ReduceCameraShake=false;s.ReduceSkillFlashes=false;s.ControlMode=CampusRift.Controls.ControlMode.PC;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;foreach(var device in UnityEngine.InputSystem.InputSystem.devices) UnityEngine.InputSystem.InputSystem.EnableDevice(device);'
        setup+='UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);'
        call('execute_code',{'action':'execute','code':setup+'return "ready";'})
        stamp=time.time();report=pathlib.Path(case['report']) if case['report'] else None
        if report and report.exists():
            historical=OUT/'historical';historical.mkdir(exist_ok=True);shutil.copy2(report,historical/(name+report.suffix))
        result=call('execute_code',{'action':'execute','code':case['code'] or 'new UnityEngine.GameObject("P17 '+name+'").AddComponent<'+case['component']+'>(); return "started";'})
        if case['code'] and not case['component'] and not name.startswith('ShabanHunt-'):
            row['summary']=str(result.get('data',{}).get('result',result));row['status']='DONE'
        else:
            path=pathlib.Path(case['done']);deadline=time.time()+case['timeout'];complete=False
            while time.time()<deadline:
                if path.exists() and path.stat().st_mtime>=stamp and (not case['contains'] or case['contains'] in path.read_text(encoding='utf-8-sig')):complete=True;break
                time.sleep(1)
            row['summary']=path.read_text(encoding='utf-8-sig')[:4000] if complete else 'TIMEOUT'
            row['status']='DONE' if complete else 'TIMEOUT'
            if complete:shutil.copy2(path,OUT/(name+'-DONE'+path.suffix))
        if report and report.exists() and report.stat().st_mtime>=stamp:
            shutil.copy2(report,OUT/(name+report.suffix));row['report']=str(OUT/(name+report.suffix))
            if report.suffix=='.json':
                data=json.loads(report.read_text(encoding='utf-8-sig'));row['passed']=data.get('passed');row['failed']=data.get('failed',[]);row['raw']=data if name=='PhantomDecoy' else None
                if row['failed'] or data.get('pass') is False or data.get('error'):row['status']='FAIL'
                elif row['passed'] is not None or data.get('pass') is True:row['status']='PASS'
        if name.startswith('Hunter-'):
            folder=ROOT/'Assets/MonsterShaban/Validation/Hunter'
            for p in folder.glob('*.json'):
                if p.stat().st_mtime>=stamp:shutil.copy2(p,OUT/(name+'-'+p.name));row['hunterReport']=json.loads(p.read_text(encoding='utf-8-sig'))
        if row['status']=='DONE':
            text=row['summary'];row['status']='FAIL' if re.search(r'\bFAIL\b|[1-9]\d* failed|[1-9]\d* errors',text) else 'PASS' if re.search(r'\bPASS\b|0 failed|0 errors',text) else 'DONE'
        console=call('read_console',{'action':'get','types':['error','warning'],'count':100,'include_stacktrace':True})
        (OUT/(name+'-console.json')).write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
    except Exception as e:row['exception']=str(e);print(traceback.format_exc(),flush=True)
    row['seconds']=round(time.time()-begin,1);summary.append(row);summary_path.write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8');print(name+': '+row['status']+' '+str(row.get('summary',''))[:180],flush=True)
    with (ROOT/'task/p17/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- T08 '+name+': '+row['status']+' ('+str(row['seconds'])+' s), raw trong Artifacts/V2/P17-regression.\n')
call('manage_editor',{'action':'stop'})
(OUT/'DONE.txt').write_text(str(len(summary))+' functional suites processed once. Excluded benchmark/balance/photo matrices per TEST-POLICY.',encoding='utf-8')
