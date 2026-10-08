"""P23 one functional pass per suite. Fresh scene/domain; archive and restore historical outputs."""
from pathlib import Path
import json,time,re,shutil,sys,traceback,collections
import unity_mcp as m
ROOT=Path.cwd();OUT=ROOT/'Artifacts/V2/P23-regression';OUT.mkdir(parents=True,exist_ok=True)
BACKUP=Path((ROOT/'task/p23/BACKUP.txt').read_text(encoding='utf-8-sig').strip())
m.initialize()
def call(name,args):
 for attempt in range(35):
  raw=m.call(name,args);v=raw.get('result',{}).get('structuredContent',raw.get('result',raw))
  if v.get('success') is not False:return v
  transient=v.get('data',{}).get('reason') in ('no_unity_session','unity_session_not_ready')
  if not transient and v.get('message') and not any(s in str(v.get('message','')).lower() for s in ['not ready','please retry','compiling','domain reload','busy']):raise RuntimeError(json.dumps(v))
  time.sleep(2)
 raise RuntimeError(str(v))
# Reuse the complete P17 functional inventory; composite runners and benchmarks are not duplicate suites.
src=(ROOT/'Tools/p17_regressions.py').read_text(encoding='utf-8-sig')
inventory=src[src.index('cases=[]'):src.index('summary_path=')].replace('"P17"','"P23"')
exec(inventory)
def code(s):return call('execute_code',{'action':'execute','code':s})
add('EnemyRosterPlayTest','CampusRift.Validation.EnemyRosterPlayTest','Artifacts/P12/tests/EnemyRosterPlayTest.json','Artifacts/P12/tests/EnemyRosterPlayTest-DONE.txt',240)
add('P18BaseSkills','CampusRift.UI.P23BaseSkillRegression','Artifacts/V2/P23-base-skills/result.json','Artifacts/V2/P23-base-skills/DONE.txt',400)
add('SkillSet2','CampusRift.Skills.SkillSet2PlayTest','Artifacts/Skills/Set2/SkillSet2.json','Artifacts/Skills/Set2/SkillSet2-DONE.txt',600)
add('P19','CampusRift.Validation.P19PlayTest','Artifacts/P12/tests/P19PlayTest.json','Artifacts/P12/tests/P19PlayTest-DONE.txt',300)
add('ExtendedLearning','CampusRift.Learning.ExtendedLearningPlayTest','task/p20/smoke.json','task/p20/DONE.txt',300)
add('Cinematic','CampusRift.SkyBeast.P21CinematicSmoke','task/p21/Cinematic.json','task/p21/Cinematic-DONE.txt',240)
add('Endgame','CampusRift.Levels.EndgamePlayTest','task/p22/EndgamePlayTest.json','task/p22/SMOKE-DONE.txt',300)
add('EndgameVisual','CampusRift.Levels.EndgameVisualSmoke','task/p22/visual-smoke.json','task/p22/VISUAL-DONE.txt',200)
add('P17Smoke','CampusRift.Validation.P17Smoke','task/p17/smoke.json','task/p17/smoke-DONE.txt',240)
add('P17Related','CampusRift.Validation.P17RelatedSmoke','task/p17/related-smoke.json','task/p17/related-smoke-DONE.txt',180)
add('P17Touch','CampusRift.Validation.P17TouchSmoke','task/p17/touch-smoke.txt','task/p17/touch-smoke.txt',100)
add('EnemyData',None,None,code='return EnemiesSetup.Validate();')
add('LearningContent',None,'LEARNING_VALIDATION_REPORT.md',code='var r=LearningContentValidation.Run(Resources.Load<CampusRift.Learning.LearningCatalog>("LearningCatalog"));return r.errors.Count;')
add('P20LearningFlow','CampusRift.Learning.P20LearningFlowPlayTest','task/p20/learning-flow.json','task/p20/LEARNING-FLOW-DONE.txt',220)
add('P20Shop','CampusRift.UI.P20ShopPlayTest','task/p20/shop-smoke.json','task/p20/SHOP-DONE.txt',200,scene='MainMenu')
for name in ('ModelsP19PlayTest','StabilizeModelsSmoke','SquadTacticsPlayTest','SquadIntegrationSmoke'):
 add(name,'CampusRift.Validation.'+name,'Artifacts/P12/tests/'+name+'.json','Artifacts/P12/tests/'+name+'-DONE.txt',480)
add('StabilizeLift','StabilizeLiftSmoke','Artifacts/V2/Stabilize/lifts/DONE.txt','Artifacts/V2/Stabilize/lifts/DONE.txt',160)
add('P23ReadingVariants','CampusRift.UI.P23ReadingVariantsSmoke','task/p23/reading-variants/result.json','task/p23/reading-variants/DONE.txt',300)
add('P18Legacy','CampusRift.Skills.SkillSet1PlayTest','Artifacts/Skills/SkillSet1.json','Artifacts/Skills/SkillSet1-DONE.txt',240)
add('P18Reactions','CampusRift.Combat.ReactionPlayTest','Artifacts/Reactions/Validation.json','Artifacts/Reactions/DONE.txt',240)
add('UIPlayAcceptance','UIPlayValidation','Artifacts/UI/UI_TEST_REPORT.json','Artifacts/UI/UI_TEST_REPORT.json',180,code='var h=new GameObject("P23 original UI acceptance").AddComponent<UIPlayValidation>();h.SkipCaptureMatrix=true;return "functional assertions only; resolution/photo matrix skipped";',scene='MainMenu')
cases.sort(key=lambda c: c['name']!='P23ReadingVariants')
for case in cases:
 if case['name'] in ('Cultivation','LevelData'):case['editOnly']=True
 if case.get('report'):case['report']=case['report'].replace('/P17-','/P23-')
 if case['name']=='EnemyAbilitiesPlayTest':case['code']='var h=new GameObject("P23 abilities").AddComponent<CampusRift.Validation.EnemyAbilitiesPlayTest>();h.SkipStatistics=true;return "functional assertions; statistics skipped per TEST-POLICY";'
 if case['name']=='Cinematic':case['code']='var h=new GameObject("P23 cinematic").AddComponent<CampusRift.SkyBeast.P21CinematicSmoke>();h.MeasureFps=false;h.CaptureScreens=false;return "started";'
 if case['name']=='Hunter-Lifts':case['delay']=1
 if case['name']=='Endgame':case['report']='task/p22/EndgamePlayTest.json'
 if case['name']=='SkillSet1':case['code']='var h=new GameObject("P23 full legacy skills").AddComponent<CampusRift.Skills.SkillSet1PlayTest>();h.smokeOnly=false;return "full legacy functional mode; assertions unchanged";'
 if case['name']=='Reaction':case['code']='var h=new GameObject("P23 full legacy reactions").AddComponent<CampusRift.Combat.ReactionPlayTest>();h.smokeOnly=false;return "full legacy functional mode; assertions unchanged";'
(OUT/'inventory.json').write_text(json.dumps(cases,indent=2),encoding='utf-8')
summary_path=OUT/'Summary.json';summary=json.loads(summary_path.read_text(encoding='utf-8')) if summary_path.exists() else [];finished={r['name'] for r in summary}
selected=set(sys.argv[1:]);historical=[p for folder in ['Artifacts','task','Assets/MonsterShaban/Validation','Assets/TraversalFixes'] for p in (BACKUP/folder).rglob('*') if p.is_file()]
def evidence(stamp,folder):
 files=[]
 for root in ['Artifacts','task','Assets/MonsterShaban/Validation','Assets/TraversalFixes']:
  for p in (ROOT/root).rglob('*'):
   if not p.is_file() or p.is_relative_to(OUT) or p.is_relative_to(ROOT/'task/p23') or p.stat().st_mtime<stamp:continue
   rel=p.relative_to(ROOT);target=folder/'outputs'/rel;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target);files.append(rel.as_posix())
   original=BACKUP/rel
   if original.exists():shutil.copy2(original,p)
 (folder/'outputs.json').write_text(json.dumps(files,indent=2),encoding='utf-8')
 return files
def evaluate(row,data):
 p=data.get('passed');f=data.get('failed');row['passed']=len(p) if isinstance(p,list) else p;row['failed']=len(f) if isinstance(f,list) else f;row['failures']=f if isinstance(f,list) else [x for x in data.get('checks',[]) if isinstance(x,dict) and x.get('pass') is False]
 if f or data.get('pass') is False or data.get('error'):return 'FAIL'
 if p is not None or data.get('pass') is True:return 'PASS'
 bools=[v for k,v in data.items() if isinstance(v,bool)]
 if bools:row['passed']=sum(bools);row['failed']=len(bools)-sum(bools);return 'FAIL' if not all(bools) else 'PASS'
 return 'DONE'
for case in cases:
 name=case['name']
 if name in finished or (selected and name not in selected):continue
 folder=OUT/'runs'/name;folder.mkdir(parents=True,exist_ok=True);start=time.time();row=dict(name=name,status='ERROR',started=time.strftime('%Y-%m-%dT%H:%M:%S'))
 print('START '+name,flush=True);(OUT/'PROGRESS.txt').write_text('Running '+name,encoding='utf-8')
 try:
  call('manage_editor',{'action':'stop'});code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/'+case['scene']+'.unity");return "opened";');call('read_console',{'action':'clear'})
  if not case.get('editOnly'):
   call('manage_editor',{'action':'play'});time.sleep(3)
   code('Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.Subtitles=false;s.TextSize=0;s.AccessibleColors=CampusRift.UI.AccessiblePalette.Default;s.ReduceCameraShake=false;s.ReduceSkillFlashes=false;s.ControlMode=CampusRift.Controls.ControlMode.PC;s.ResolutionWidth=1920;s.ResolutionHeight=1080;s.Fullscreen=false;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;foreach(var device in UnityEngine.InputSystem.InputSystem.devices)UnityEngine.InputSystem.InputSystem.EnableDevice(device);UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return "ready";')
  # P17 fixtures read the first row; each run must start with its own telemetry, not history.
  if name in ['P17Smoke','P17Related']:
   for d in ['task/p17/telemetry-smoke','task/p17/related-telemetry']:
    for p in (ROOT/d).glob('*.jsonl'):
     q=folder/'pre-existing'/p.relative_to(ROOT);q.parent.mkdir(parents=True,exist_ok=True);shutil.move(p,q)
  if case.get('delay'):time.sleep(case['delay'])
  stamp=time.time();result=code(case['code'] or 'new GameObject("P23 '+name+'").AddComponent<'+case['component']+'>();return "started";')
  if not case['component'] and not name.startswith('ShabanHunt-'):
   row['summary']=result.get('data',{}).get('result',result);row['status']='DONE'
  else:
   path=ROOT/case['done'];deadline=time.time()+case['timeout']
   while time.time()<deadline:
    if path.exists() and path.stat().st_mtime>=stamp and (not case['contains'] or case['contains'] in path.read_text(encoding='utf-8-sig')):break
    time.sleep(1)
   else:raise TimeoutError('No fresh completion after '+str(case['timeout'])+'s')
   row['summary']=path.read_text(encoding='utf-8-sig')[:5000];row['status']='DONE';shutil.copy2(path,folder/('DONE'+path.suffix))
  report=ROOT/case['report'] if case['report'] else None
  if report and report.exists() and report.stat().st_mtime>=stamp:
   target=folder/('result'+report.suffix);shutil.copy2(report,target);row['report']=target.relative_to(ROOT).as_posix()
   if report.suffix=='.json':row['status']=evaluate(row,json.loads(report.read_text(encoding='utf-8-sig')))
  if name.startswith('Hunter-'):
   for p in (ROOT/'Assets/MonsterShaban/Validation/Hunter').glob('*.json'):
    if p.stat().st_mtime>=stamp:row['status']=evaluate(row,json.loads(p.read_text(encoding='utf-8-sig')))
  if row['status']=='DONE':
   text=str(row.get('summary',''));row['status']='FAIL' if re.search(r'\bFAIL\b|[1-9]\d* failed|[1-9]\d* errors|=False',text) else 'PASS' if re.search(r'\bPASS\b|0 failed|0 errors|=True',text) else 'DONE'
  if name.startswith('ShabanHunt-'):
   full=path.read_text(encoding='utf-8-sig');hit=re.search(r'reacquiredAt=(-?[\d.]+)',full);row['status']='PASS' if hit and float(hit.group(1))>=0 else 'FAIL';row['reacquiredAt']=float(hit.group(1)) if hit else None
  if name=='LearningContent':
   errors=int(row['summary']);row['status']='FAIL' if errors else 'PASS';row['passed']='validation';row['failed']=errors
  if name=='ShelterAudit':
   data=json.loads((folder/'result.json').read_text(encoding='utf-8-sig'));baseline=json.loads((BACKUP/'Artifacts/SkyBeast/ShelterAudit.json').read_text(encoding='utf-8-sig'))
   row['baselineIdentical']=data==baseline
   if data==baseline:row['status']='ACCEPTED BASELINE';row['newFailures']=0
  console=call('read_console',{'action':'get','types':['error','warning'],'count':100,'include_stacktrace':True,'format':'detailed'});(folder/'console.json').write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
  logs=console.get('data',[])
  if isinstance(logs,list):row['consoleErrors']=len([r for r in logs if isinstance(r,dict) and r.get('type') in ('Error','Exception','Assert')])
  if row.get('consoleErrors'):row['status']='FAIL'
  call('manage_editor',{'action':'stop'});evidence(stamp,folder)
  for p in (folder/'pre-existing').rglob('*'):
   if p.is_file():dest=ROOT/p.relative_to(folder/'pre-existing');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
 except Exception as e:
  row['exception']=str(e);row['status']='TIMEOUT' if isinstance(e,TimeoutError) else 'ERROR';(folder/'exception.txt').write_text(traceback.format_exc(),encoding='utf-8');print(traceback.format_exc(),flush=True)
  try:call('manage_editor',{'action':'stop'});evidence(start,folder)
  except Exception:pass
 row['seconds']=round(time.time()-start,1);summary.append(row);summary_path.write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8');print(name+': '+row['status']+' '+str(row.get('passed',''))+'/'+str(row.get('failed','')),flush=True)
 if (OUT/'STOP-AFTER-SUITE.txt').exists() or (name=='P23ReadingVariants' and row['status']!='PASS'):break
 with (ROOT/'task/p23/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- T05 '+name+': '+row['status']+' ('+str(row['seconds'])+'s); raw cách ly Artifacts/V2/P23-regression/runs/'+name+'.\n')
call('manage_editor',{'action':'stop'});(OUT/'DONE.txt').write_text(str(len(summary))+' suites processed; no automatic rerun; statistics/bench/photo matrices skipped per TEST-POLICY.',encoding='utf-8')
print(collections.Counter(r['status'] for r in summary),flush=True)
