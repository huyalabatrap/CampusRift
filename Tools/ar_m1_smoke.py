"""One M1 normal-game smoke, preserving prior LevelFlow evidence."""
from pathlib import Path
import json, time, shutil, sys
import unity_mcp as m
m.initialize()
out=Path('task/ar/m1-normal'); out.mkdir(exist_ok=True)
def call(name,args):
    for attempt in range(15):
        raw=m.call(name,args); r=raw.get('result',{}).get('structuredContent',raw)
        if isinstance(r.get('result'),dict): r=r['result']
        if r.get('success') is not False: return r
        if not any(x in str(r).lower() for x in ['no_unity_session','not ready','domain reload','busy','compiling']): raise RuntimeError(str(r))
        time.sleep(2)
    raise RuntimeError(str(r))
def code(s): return call('execute_code',{'action':'execute','code':s})['data']['result']
def save(n,r): (out/n).write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')
if '--level-only' not in sys.argv:
    call('manage_editor',{'action':'stop'})
    code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");return "opened";')
    call('read_console',{'action':'clear'})
    call('manage_editor',{'action':'play'})
    time.sleep(3)
    save('menu.json',code('UnityEngine.Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());var m=UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager;return new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, xrInactive=m.activeLoader==null, ui=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.UIManager>()!=null};'))
    code('UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.UIManager>().Play();return "clicked normal Play";')
    time.sleep(2)
    save('hub.json',code('return new { state=CampusRift.UI.UIStateManager.Instance.State.ToString(), xrInactive=UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader==null };'))
    code('UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");return "loading normal gameplay";')
    time.sleep(5)
code('UnityEngine.Application.runInBackground=true;CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.ControlMode=CampusRift.Controls.ControlMode.PC;CampusRift.UI.SettingsManager.Instance.Apply(s,false);UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return "isolated";')
files=[Path('Artifacts/Levels/LevelFlow.json'),Path('Artifacts/Levels/LevelFlow-DONE.txt')]
prior={p:p.read_bytes() if p.exists() else None for p in files}
for p,b in prior.items():
    if b is not None:(out/('prior-'+p.name)).write_bytes(b)
save('fixture-preflight.json',code('var h=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.GameplayHUD>(UnityEngine.FindObjectsInactive.Include);return new { state=CampusRift.UI.UIStateManager.Instance.State.ToString(), hudExists=h!=null, hudActive=h!=null&&h.gameObject.activeInHierarchy, health=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.PlayerMonsterHealth>().CurrentHealth };'))
code('CampusRift.UI.UIStateManager.Instance.EnterScene(true);return "gameplay state for documented sandbox fixture";')
stamp=time.time()
try:
    code('new UnityEngine.GameObject("AR M1 LevelFlow smoke").AddComponent<CampusRift.Levels.LevelFlowPlayTest>();return "started once";')
    print('LevelFlow started',flush=True)
    until=time.time()+300
    while time.time()<until:
        if files[1].exists() and files[1].stat().st_mtime>=stamp:break
        time.sleep(2)
    else:raise TimeoutError('No fresh LevelFlow DONE in 300 s')
    for p in files:shutil.copy2(p,out/p.name)
    save('after.json',code('return new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,xrInactive=UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader==null,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name};'))
    save('console.json',call('read_console',{'action':'get','types':['error'],'count':100,'format':'detailed'}))
    print(files[1].read_text(),flush=True)
finally:
    call('manage_editor',{'action':'stop'})
    for p,b in prior.items():
        if b is not None:p.write_bytes(b)
