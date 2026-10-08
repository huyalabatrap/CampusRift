"""Manual mock smoke plus invocation of existing AR suites; no new runtime test class."""
from b1007_runner import *
def once(name,action):
    folder=OUT/'runs'/name;folder.mkdir(parents=True,exist_ok=True)
    if (folder/'row.json').exists():return
    if (folder/'invoked.json').exists():raise RuntimeError('Collect interrupted '+name+' before any rerun')
    save(folder/'invoked.json',dict(utc=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())))
    row={'name':name,'status':'ERROR'}
    try:
        row.update(action(folder));console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save(folder/'console.json',console)
        if console.get('data'):row.update(status='FAIL',consoleErrors=console['data'])
    except Exception as e:row['error']=str(e)
    save(folder/'row.json',row);progress(name+' → '+row['status']+'; evidence regression/runs/'+name);print(name+' '+row['status'],flush=True)
def wait_for(expr,seconds=40):
    end=time.time()+seconds
    while time.time()<end:
        r=value(code('return '+expr+';'))
        if r:return r
        time.sleep(1)
    raise TimeoutError(expr)
def setup(mode='training'):
    prepare('Assets/ARRift/Scenes/ARRiftBattle.unity')
    code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>();c.Continue();c.Continue();return true;')
    wait_for('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>()?.Ready==true')
    code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();var f=p.GetComponent<CampusRift.AR.ARBattlefield>();var selection=f.GetComponent<CampusRift.AR.ARModeSelectionHUD>();selection.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Select '+mode+'").onClick.Invoke();selection.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Start selected").onClick.Invoke();f.ModeSession.KnowledgeEnabled=false;var h=f.GetComponent<CampusRift.AR.ARBattleHUD>();h.SetHelp(false);h.SetMenu(false);p.SetFloor(false);p.enabled=false;return true;')
    wait_for('UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null')
    for yaw in [155,170,180,195,210,180]:
        code('var camera=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();camera.transform.position=new Vector3(.75f,1.5f,.1f);camera.transform.rotation=Quaternion.Euler(40,'+str(yaw)+',0);return true;')
        time.sleep(.55)
    deadline=time.time()+40
    while time.time()<deadline:
        if value(code((ROOT/'Tools/b1007_ar_center.cs').read_text(encoding='utf-8'))):break
        time.sleep(1)
    else:raise TimeoutError('No scanned polygon fits the minimum battlefield disc')
    time.sleep(.6)
    code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
    wait_for('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().ReticleValid')
    code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().Confirm();return true;')
    wait_for('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().Root!=null')
    code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.StartBattlefield();var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=p.Root.position+new Vector3(.4f,.8f,1);c.transform.LookAt(p.Root.position);return true;')
    wait_for('!UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().Paused')
    code('var m=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.MockGestureSource>();m.enabled=false;return true;')
def emit_for(labels,seconds=.5):
    # Emit in Editor updates so every D1 frame is fresh and monotonic during the smoke.
    labels_cs='new string[]{'+','.join(json.dumps(s) for s in labels)+'}'
    total=len(labels)*seconds
    code('var m=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.MockGestureSource>();var labels='+labels_cs+';double begin=UnityEditor.EditorApplication.timeSinceStartup,next=begin;UnityEditor.EditorApplication.CallbackFunction tick=null;tick=()=>{double now=UnityEditor.EditorApplication.timeSinceStartup;if(m==null||now-begin>='+str(total)+'){UnityEditor.EditorApplication.update-=tick;return;}if(now>=next){next=now+.05;m.Emit(labels[Mathf.Min(labels.Length-1,(int)((now-begin)/'+str(seconds)+'))],new Vector2(.5f,.5f),1,.3f);}};UnityEditor.EditorApplication.update+=tick;return true;')
    time.sleep(total+.3)
def state():
    return value(code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var c=f.GetComponent<CampusRift.AR.ARSkillCaster>();var s=f.GetComponent<CampusRift.AR.ARSpaceModes>();var d=f.GetComponent<CampusRift.AR.ARMonsterDirector>();var r=f.GetComponent<CampusRift.AR.ARSealPractice>();return new{mode=f.Mode.id,placed=f.Root!=null,f.Paused,d.Wave,actors=d.Actors.Count,c.Fired,c.Seal,s.SwordReady,s.SwordProgress,practiceRunning=r.Running,r.Elapsed,r.Points,delegateName=c.source.DelegateName,profileTransient=CampusRift.Progression.ProfileService.Instance.Transient};'))
def mode_smoke(mode,folder):
    setup(mode);save(folder/'runtime-start.json',{'mode':mode});save(folder/'before.json',state())
    emit_for(['None','Open_Palm','None','Closed_Fist','None','Pointing_Up','None','Victory','None','Thumb_Down','None','Thumb_Up','None'])
    save(folder/'after.json',state())
    code('UIValidation.SetResolution(1600,720);return true;');time.sleep(.5)
    code('CampusRift.Controls.LookCapture.Image("'+(folder/'view.png').relative_to(ROOT).as_posix()+'");return true;')
    return dict(status='PASS',summary='Short mock gesture session completed without exceptions; real polygon/anchor in XR Simulation; no physical device claim')
def unit(folder):
    prepare(edit=True);r=value(code('return CampusRift.AR.ARGestureUnitTests.Run();'));save(folder/'result.json',r);status,failed=evaluate(r);return dict(status=status,failures=failed,summary=r)
def rift(folder):
    setup();wait_for('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARMonsterDirector>().Actors.Count>0')
    stamp=time.time();save(folder/'runtime-start.json',{'utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())});code('new GameObject("batch1007 original AR suite").AddComponent<CampusRift.AR.ARRiftPlayTest>();return true;')
    p=ROOT/'task/ar/goiA/AR-DONE.txt';end=time.time()+200
    while time.time()<end:
        if p.exists() and p.stat().st_mtime>=stamp:break
        time.sleep(1)
    else:raise TimeoutError('ARRiftPlayTest completion')
    data=json.loads((ROOT/'task/ar/goiA/ar-results.json').read_text(encoding='utf-8-sig'));save(folder/'result.json',data);shutil.copy2(p,folder/'DONE.txt');status,failed=evaluate(data);return dict(status=status,failures=failed,summary=p.read_text(encoding='utf-8-sig'))
def check_smoke(folder):
    setup();save(folder/'runtime-start.json',{'utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())});code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();c.NewSession();c.Open();c.PreviewPhase(CampusRift.AR.ARGestureCheck.Phase.Trials,0);return true;')
    emit_for(['None','Open_Palm','None'])
    save(folder/'state.json',value(code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();return new{c.Opened,phase=c.Stage.ToString(),c.TrialNumber,c.RequestedGesture,delegateName=c.GetComponent<CampusRift.AR.GestureRecognizerBridge>().DelegateName};')))
    code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>().Close();return true;');return dict(status='PASS',summary='Kiểm Ấn opened, one mock trial, closed without exceptions; no 14-minute device session')
if __name__=='__main__':
    selected=set(sys.argv[1:])
    jobs=[('ARGestureUnitTests',unit),('ARRiftPlayTest',rift),('ARGestureCheckMock',check_smoke)]
    jobs += [('ARMode-'+mode,lambda folder,mode=mode:mode_smoke(mode,folder)) for mode in ['training','defense','rift-hunt','dragon-duel','seal-practice']]
    for name,action in jobs:
        if not selected or name in selected:once(name,action)
    if not selected or 'ARNavigationPlayTest' in selected:
        run(dict(name='ARNavigationPlayTest',component='CampusRift.AR.ARNavigationPlayTest',report='task/ar/m7/navigation.json',done='task/ar/m7/DONE.txt',timeout=240,contains=None,code=None,scene='MainMenu'))
    stop();save(OUT/'Summary.json',[json.loads(p.read_text(encoding='utf-8')) for p in (OUT/'runs').glob('*/row.json')])
