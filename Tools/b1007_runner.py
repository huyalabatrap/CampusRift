"""Invoke existing suites once, with a durable invocation ledger and isolated sessions."""
from pathlib import Path
import json,time,re,shutil,sys,traceback
import unity_mcp as m
from b1007_inventory import cases
ROOT=Path.cwd();OUT=ROOT/'task/batch-1007/regression';OUT.mkdir(exist_ok=True)
m.initialize()
def call(name,args):
    for attempt in range(35):
        raw=m.call(name,args);v=raw.get('result',{}).get('structuredContent',raw.get('result',raw))
        if isinstance(v.get('result'),dict):v=v['result']
        if v.get('success') is not False:return v
        if v.get('message') is not None and not any(s in str(v).lower() for s in ['no_unity_session','not ready','retry','compiling','domain reload','busy']):raise RuntimeError(json.dumps(v))
        time.sleep(2)
    raise RuntimeError(str(v))
def code(s):return call('execute_code',{'action':'execute','code':s})
def value(r):return r.get('data',{}).get('result',r)
def save(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf-8')
def progress(s):
    with (OUT.parent/'PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- Job7 hồi quy: '+s+'\n')
def stop():
    code('var x=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>();if(x!=null)x.Shutdown();return true;')
    call('manage_editor',{'action':'stop'})
def prepare(scene='SampleScene',edit=False):
    scene_path=scene if scene.startswith('Assets/') else 'Assets/Scenes/'+scene+'.unity'
    stop();code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("'+scene_path+'");return true;')
    call('read_console',{'action':'clear'})
    if edit:return
    call('manage_editor',{'action':'play'})
    for attempt in range(60):
        if value(code('return EditorApplication.isPlaying && !EditorApplication.isCompiling && CampusRift.UI.SettingsManager.Instance!=null && CampusRift.Progression.ProfileService.Instance!=null;')):break
        time.sleep(1)
    else:raise TimeoutError('Scene services did not become ready')
    code('Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.Subtitles=false;s.TextSize=0;s.AccessibleColors=CampusRift.UI.AccessiblePalette.Default;s.ReduceCameraShake=false;s.ReduceSkillFlashes=false;s.ControlMode=CampusRift.Controls.ControlMode.PC;s.ResolutionWidth=1920;s.ResolutionHeight=1080;s.Fullscreen=false;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;foreach(var device in UnityEngine.InputSystem.InputSystem.devices)UnityEngine.InputSystem.InputSystem.EnableDevice(device);UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);PlayerPrefs.SetInt("CampusRift.AR.HelpSeen",1);return true;')
def evaluate(data):
    if isinstance(data,str) and data.endswith('active controls within bounds; no truncated text.'):return 'PASS',[]
    if isinstance(data,str):
        totals=re.search(r'\b(\d+) pass / (\d+) fail\b',data,re.IGNORECASE)
        if totals:return ('PASS',[]) if int(totals[2])==0 else ('FAIL',[totals.group(0)])
    if not isinstance(data,dict):return 'PASS' if data is True or data==0 else 'DONE',[]
    # Numeric audit counters describe measured mismatches; the existing suite's
    # explicit accuracy threshold owns acceptance, rather than requiring 100%.
    if 'accuracy' in data and isinstance(data.get('passed'),(int,float)) and isinstance(data.get('failed'),(int,float)) and isinstance(data.get('pass'),bool):
        return ('PASS',[]) if data['pass'] else ('FAIL',['accuracy '+str(data['accuracy'])])
    failed=data.get('failed',data.get('issues',[]));passed=data.get('passed')
    failures=failed if isinstance(failed,list) else ([] if not failed else [str(failed)])
    failures += [str(x) for x in data.get('checks',[]) if isinstance(x,dict) and x.get('pass') is False]
    if failures or data.get('pass') is False or data.get('error'):return 'FAIL',failures
    if passed is not None or data.get('pass') is True or 'issues' in data:return 'PASS',[]
    bools=[v for k,v in data.items() if isinstance(v,bool)]
    if bools:return ('PASS' if all(bools) else 'FAIL'),[k for k,v in data.items() if v is False]
    return 'DONE',[]
def run(case):
    name=case['name'];folder=OUT/'runs'/name
    if (folder/'row.json').exists():return json.loads((folder/'row.json').read_text(encoding='utf-8'))
    if (folder/'invoked.json').exists():raise RuntimeError('Already invoked: '+name+'; collect existing result before continuing')
    folder.mkdir(parents=True,exist_ok=True);begin=time.time();row=dict(name=name,component=case['component'],status='ERROR')
    print('START '+name,flush=True)
    paths=[ROOT/p for p in [case.get('done'),case.get('report')] if p];original={}
    for p in paths:
        if p.exists():original[p]=p.read_bytes();dest=folder/'historical'/p.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(original[p])
    try:
        prepare(case['scene'],case.get('editOnly',False))
        if case.get('delay'):time.sleep(case['delay'])
        stamp=time.time();save(folder/'invoked.json',dict(utc=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),case=case))
        result=value(code(case['code'] or 'new GameObject("Batch1007 '+name+'").AddComponent<'+case['component']+'>();return "started";'))
        save(folder/'invocation-result.json',result)
        if not case['component'] and not name.startswith('ShabanHunt-'):
            row['summary']=result;row['status'],row['failures']=evaluate(result)
        else:
            path=ROOT/case['done'];deadline=time.time()+case['timeout']
            while time.time()<deadline:
                if path.exists() and path.stat().st_mtime>=stamp and (not case['contains'] or case['contains'] in path.read_text(encoding='utf-8-sig')):break
                time.sleep(1)
            else:raise TimeoutError('No fresh completion marker after '+str(case['timeout'])+'s')
            row['summary']=path.read_text(encoding='utf-8-sig')[:5000];shutil.copy2(path,folder/('DONE'+path.suffix));row['status']='DONE'
        report=ROOT/case['report'] if case['report'] else None
        if report and report.exists() and report.stat().st_mtime>=stamp:
            shutil.copy2(report,folder/('result'+report.suffix));row['report']=(folder/('result'+report.suffix)).relative_to(ROOT).as_posix()
            if report.suffix=='.json':row['status'],row['failures']=evaluate(json.loads(report.read_text(encoding='utf-8-sig')))
        if name.startswith('Hunter-'):
            for p in (ROOT/'Assets/MonsterShaban/Validation/Hunter').glob('*.json'):
                if p.stat().st_mtime>=stamp:shutil.copy2(p,folder/p.name);row['status'],row['failures']=evaluate(json.loads(p.read_text(encoding='utf-8-sig')))
        if name.startswith('ShabanHunt-'):
            # Hunt reports end with RESULT; the display summary is capped at 5 KB.
            match=re.search(r'reacquiredAt=(-?[\d.]+)',path.read_text(encoding='utf-8-sig'));row['status']='PASS' if match and float(match.group(1))>=0 else 'FAIL'
        if row['status']=='DONE':
            s=str(row['summary']);row['status']='FAIL' if re.search(r'\bFAIL\b|[1-9]\d* failed|[1-9]\d* errors|=False',s) else 'PASS' if re.search(r'\bPASS\b|0 failed|0 errors|=True',s) else 'REVIEW'
            if row['status']=='REVIEW':
                parsed,failures=evaluate(s)
                if parsed in ('PASS','FAIL'):row['status']=parsed;row['failures']=failures
        console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save(folder/'console.json',console)
        if console.get('data'):
            row['consoleErrors']=console['data'];row['status']='FAIL'
    except Exception as e:row['error']=str(e);save(folder/'error.json',dict(error=str(e),traceback=traceback.format_exc()))
    finally:
        row['seconds']=round(time.time()-begin,1);save(folder/'row.json',row)
        for p,b in original.items():p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b)
    progress(name+' → '+row['status']+'; evidence regression/runs/'+name+'/row.json.');print('END '+name+' '+row['status'],flush=True)
    return row
if __name__=='__main__':
    selected=set(sys.argv[1:]);rows=[]
    for case in cases:
        if selected and case['name'] not in selected:continue
        fresh=not (OUT/'runs'/case['name']/'row.json').exists()
        row=run(case);rows.append(row)
        allrows=[json.loads(p.read_text(encoding='utf-8')) for p in (OUT/'runs').glob('*/row.json')];save(OUT/'Summary.json',allrows)
        if (OUT/'STOP-AFTER-SUITE.txt').exists():break
        if fresh and row['status']!='PASS':break
    stop();print('FINISHED '+str(len(rows)),flush=True)
