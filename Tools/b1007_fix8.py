"""Job 8 checkpoints. Runs only the five authorized existing suites, once each."""
from b1007_runner import *
import hashlib

FIX = ROOT / 'task/batch-1007/fix8'
FIX.mkdir(exist_ok=True)
BACKUP = ROOT / 'Backups/Fix-Remaining-pre-20261007'

def milestone(message):
    with (FIX.parent/'PROGRESS.md').open('a', encoding='utf-8') as f:
        f.write('\n- Job8: '+message+'\n')

def inspect():
    result=value(code('return new {playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,building=BuildPipeline.isBuildingPlayer,target=EditorUserBuildSettings.activeBuildTarget.ToString(),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};'))
    save(FIX/'resume-state.json', result)
    print(json.dumps(result))

def isolate_developer_prefs():
    stop()
    snapshot=FIX/'typed-developer-prefs.json'
    if not snapshot.exists():
        prefs=value(code('var keys=new[]{"CampusRift.DevMode","CampusRift.DevMode.Invincible","CampusRift.DevMode.NoCooldown"};return keys.Select(k=>new {key=k,exists=PlayerPrefs.HasKey(k),integer=PlayerPrefs.GetInt(k,0),text=PlayerPrefs.GetString(k,"")}).ToArray();'))
        save(snapshot,prefs)
        milestone('Chẩn đoán môi trường: snapshot8 cũ đọc DevMode bằng GetString, trong khi runtime dùng GetInt. Đã lưu typed-developer-prefs.json trước thay đổi; PlayerCombat layout 3/3 PASS nhưng 20 assertion combat bị DevMode làm sai, giữ nguyên FAIL và không chạy lại. Tắt 3 prefs dev tạm thời trong Edit trước các fixture còn lại.')
    code('PlayerPrefs.SetInt("CampusRift.DevMode",0);PlayerPrefs.SetInt("CampusRift.DevMode.Invincible",0);PlayerPrefs.SetInt("CampusRift.DevMode.NoCooldown",0);PlayerPrefs.Save();return true;')

def run_one(name):
    allowed={'PlayerCombat','SkillSet1Edges','P12AudioEndingPlayTest','P17Smoke','UIPlayAcceptance'}
    assert name in allowed
    case=next(c.copy() for c in cases if c['name']==name)
    folder=FIX/'runs'/name
    if (folder/'row.json').exists():
        print((folder/'row.json').read_text(encoding='utf-8'));return
    if (folder/'invoked.json').exists():
        raise RuntimeError('Already invoked; collect its existing output, do not rerun: '+name)
    # Full functional suite. Keep the existing policy's capture-matrix exclusion.
    case['code']=('var h=new GameObject("Job8 UI Acceptance").AddComponent<UIPlayValidation>();h.SkipCaptureMatrix=true;h.FailedItemsOnly=false;return "started";' if name=='UIPlayAcceptance' else None)
    isolate_developer_prefs()
    prepare(case['scene'])
    preflight=value(code('return new {dev=CampusRift.Progression.DevMode.Active,invincible=CampusRift.Progression.DevMode.Invincible,noCooldown=CampusRift.Progression.DevMode.NoCooldown,attack=UnityEngine.Object.FindAnyObjectByType<CampusRift.Combat.PlayerStats>()?.Attack};'))
    assert not preflight['dev'],preflight
    folder.mkdir(parents=True,exist_ok=True)
    save(folder/'preflight.json',preflight)
    historical={}
    for p in [case.get('report'),case.get('done')]:
        if p and (ROOT/p).exists():
            historical[p]=(ROOT/p).read_bytes()
            dest=folder/'historical'/p;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(historical[p])
    stamp=time.time()
    save(folder/'invoked.json',dict(utc=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),case=case))
    milestone(name+' đã gọi đúng một lần, full suite; đợi DONE mới chuyển fixture.')
    result=code(case['code'] or 'new GameObject("Job8 '+name+'").AddComponent<'+case['component']+'>();return "started";')
    save(folder/'invocation-result.json',result)
    deadline=time.time()+case['timeout']
    path=ROOT/case['done']
    while time.time()<deadline:
        if path.exists() and path.stat().st_mtime>=stamp and (not case['contains'] or case['contains'] in path.read_text(encoding='utf-8-sig')):break
        time.sleep(1)
    else:raise TimeoutError('Fixture has no fresh completion marker; preserve running session: '+name)
    report=json.loads((ROOT/case['report']).read_text(encoding='utf-8-sig'))
    status,failures=evaluate(report)
    save(folder/'result.json',report)
    shutil.copy2(path,folder/('DONE'+path.suffix))
    console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True})
    save(folder/'console.json',console)
    if console.get('data'):status='FAIL'
    row=dict(name=name,component=case['component'],status=status,failures=failures,passed=len(report.get('passed',[])),failed=len(report.get('failed',[])),seconds=round(time.time()-stamp,2))
    save(folder/'row.json',row)
    if name=='P17Smoke':
        for p in (ROOT/'task/p17/screens').glob('*'):
            if p.stat().st_mtime>=stamp:shutil.copy2(p,folder/p.name)
    for p,b in historical.items():(ROOT/p).write_bytes(b)
    milestone(name+' '+status+'; '+str(row['passed'])+' PASS / '+str(row['failed'])+' FAIL; evidence fix8/runs/'+name+'/row.json.')
    print(json.dumps(row),flush=True)
    stop()

if __name__=='__main__':
    if sys.argv[1]=='inspect':inspect()
    elif sys.argv[1]=='run':run_one(sys.argv[2])
    elif sys.argv[1]=='isolate':isolate_developer_prefs()
