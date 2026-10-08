from ar_session import *
print(code(Path('task/ar/m3-install.cs').read_text()),flush=True)
code('UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.Floor",0);UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;return true;')
call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});background()
for attempt in range(90):
    if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
    time.sleep(1)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=false;return true;')
for yaw in [155,170,180,195,210,180]:
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,'+str(yaw)+',0);return true;');time.sleep(.5)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
for i in range(35):
    time.sleep(1)
    if code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();return f.Shrine!=null;'):break
else:raise RuntimeError('Placement failed')
code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);return true;')
time.sleep(9)
state=code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var d=f.GetComponent<CampusRift.AR.ARMonsterDirector>();int strikes=0;bool campusOff=true;foreach(var e in d.Actors){strikes+=e.GetComponent<CampusRift.AR.ARMinionBrain>().Strikes;campusOff&=!e.Brain.enabled&&!e.Motor.enabled;}return new {spawned=d.Spawned,nav=f.NavigationReady,strikes=strikes,health=f.Shrine.CurrentHealth,campusOff=campusOff,squadAbsent=CampusRift.Enemies.EnemyDirector.Instance.Squad==null,paused=f.Paused};')
save('task/ar/m3-battle.json',state);print(state,flush=True)
assert state['spawned']==3 and state['strikes']>0 and state['campusOff'] and state['squadAbsent'],state
for name,pos in [('low','new UnityEngine.Vector3(.4f,.48f,.8f)'),('top','new UnityEngine.Vector3(0,1.25f,.2f)')]:
    code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=f.Root.position+'+pos+';c.transform.LookAt(f.Root.position+UnityEngine.Vector3.up*.08f);return true;');time.sleep(1)
    code('UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/m3-battle-'+name+'.png");return true;');time.sleep(.5)
if '--visual-only' in __import__('sys').argv:
    console('task/ar/m3-final-console.json')
    call('manage_editor',{'action':'stop'})
    raise SystemExit(0)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().UserPaused=true;return true;')
before=code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();return new {clock=f.Clock,health=f.Shrine.CurrentHealth};');time.sleep(1)
after=code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();return new {clock=f.Clock,health=f.Shrine.CurrentHealth,global=UnityEngine.Time.timeScale};');save('task/ar/m3-pause.json',{'before':before,'after':after});assert before=={k:after[k] for k in before} and after['global']==1
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().UserPaused=false;return true;')
for wave in range(3):
    # Real death/dissolve/recycle path; direct damage is only this smoke fixture's input.
    code('foreach(var e in CampusRift.Enemies.EnemyDirector.Instance.Active.ToArray())e.Vitality.ApplyDamage(CampusRift.Combat.DamageInfo.Create(100000,CampusRift.Combat.Element.None,CampusRift.Combat.DamageSource.Skill,e.transform.position,UnityEngine.Vector3.forward));return true;')
    time.sleep(9)
done=code('var d=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARMonsterDirector>();return new {won=d.Won,finished=d.Finished,spawned=d.Spawned,killed=d.Killed,active=CampusRift.Enemies.EnemyPool.Instance.ActiveCount};');save('task/ar/m3-waves.json',done);print(done,flush=True)
assert done['won'] and done['spawned']==13 and done['active']==0,done
console('task/ar/m3-console.json')
