"""Fresh compact smoke or framebuffer capture; restore reload/input even on failure."""
from pathlib import Path
import json, time, sys
from p13_cli import call

root=Path('task/p15'); job=sys.argv[1]
typ,done={'capture':('P15Fix1Capture','task/p15/fix1-capture-DONE.txt'),
          'sword':('HeavenSwordPlayTest','Artifacts/SkyBeast/HeavenSword-DONE.txt'),
          'sky':('SkyBeastPlayTest','Artifacts/SkyBeast/SkyBeast-DONE.txt')}[job]
def code(s): return call('execute_code',{'action':'execute','code':s})
def save(name,data): (root/name).write_text(json.dumps(data,indent=2),encoding='utf-8')
call('manage_editor',{'action':'stop'})
old=code('return new {reload=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled, bg=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior.ToString(), editor=UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode.ToString()};')['data']['result']
save('fix1-'+job+'-original.json',old)
code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return "ready";')
call('read_console',{'action':'clear'}); start=time.time()
try:
    call('manage_editor',{'action':'play'});time.sleep(1.5)
    if job=='sword':
        code('UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;return "temporary focus override";')
    setup='var h=new UnityEngine.GameObject("P15 fix1 '+job+'").AddComponent<CampusRift.SkyBeast.'+typ+'>();'
    if job=='capture' and len(sys.argv)>2 and sys.argv[2]=='visual':setup+='h.MeasureFps=false;'
    if job=='capture' and len(sys.argv)>2 and sys.argv[2]=='pc':setup+='h.MeasureMobileFps=false;'
    code(setup+'return "started";');print('START',job,flush=True)
    deadline=time.time()+100
    while time.time()<deadline:
        p=Path(done)
        if p.exists() and p.stat().st_mtime>=start:break
        time.sleep(.5)
    else:raise RuntimeError('No fresh completion: '+job)
    errors=call('read_console',{'action':'get','types':['error'],'count':20,'format':'detailed'})
    save('fix1-'+job+'-console.json',errors)
    print(p.read_text(encoding='utf-8-sig'),flush=True);print('ERRORS',errors,flush=True)
    save('fix1-'+job+'-run.json',dict(started=start,finished=time.time(),done=done,fresh=True,errors=errors))
    if job in ('sword','sky'):
        name='HeavenSword' if job=='sword' else 'SkyBeast';(root/('fix1-'+name+'.json')).write_bytes(Path('Artifacts/SkyBeast/'+name+'.json').read_bytes())
finally:
    code('UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)System.Enum.Parse(typeof(UnityEngine.InputSystem.InputSettings.BackgroundBehavior),"'+old['bg']+'");UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)System.Enum.Parse(typeof(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode),"'+old['editor']+'");UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return "input restored";')
    call('manage_editor',{'action':'stop'})
    code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(old['reload']).lower()+';UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return "restored";')
