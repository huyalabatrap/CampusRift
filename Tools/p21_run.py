from pathlib import Path
import json,time,sys,shutil
from p13_cli import call
root=Path('task/p21');job=sys.argv[1]
def code(s):return call('execute_code',dict(action='execute',code=s))
def save(name,data):(root/name).write_text(json.dumps(data,indent=2),encoding='utf-8')
typ,done={'cinematic':('P21CinematicSmoke','task/p21/Cinematic-DONE.txt'),'sword':('HeavenSwordPlayTest','Artifacts/SkyBeast/HeavenSword-DONE.txt'),'sky':('SkyBeastPlayTest','Artifacts/SkyBeast/SkyBeast-DONE.txt')}[job]
call('manage_editor',dict(action='stop'))
old=code('return new {reload=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,bg=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior.ToString(),editor=UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode.ToString()};')['data']['result'];save(job+'-original.json',old)
code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return "ready";')
call('read_console',dict(action='clear'));start=time.time()
try:
    call('manage_editor',dict(action='play'));time.sleep(2)
    code('UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;return "temporary focus override";')
    setup='var harness=new UnityEngine.GameObject("P21 '+job+'").AddComponent<CampusRift.SkyBeast.'+typ+'>();'
    if job=='cinematic' and len(sys.argv)>2:setup+='harness.MeasureFps=false;'
    code(setup+'return "started";');print('START',job,flush=True)
    deadline=time.time()+140
    while time.time()<deadline:
        p=Path(done)
        if p.exists() and p.stat().st_mtime>=start:break
        time.sleep(.5)
    else:raise RuntimeError('No fresh completion '+job)
    errors=call('read_console',dict(action='get',types=['error'],count=30,format='detailed'));save(job+'-console.json',errors)
    save(job+'-run.json',dict(started=start,finished=time.time(),fresh=True,done=done,errors=errors))
    print(p.read_text(encoding='utf-8-sig'),flush=True)
    if job!='cinematic':
        name='HeavenSword' if job=='sword' else 'SkyBeast';shutil.copy2('Artifacts/SkyBeast/'+name+'.json',root/(name+'.json'))
finally:
    code('UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)System.Enum.Parse(typeof(UnityEngine.InputSystem.InputSettings.BackgroundBehavior),"'+old['bg']+'");UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)System.Enum.Parse(typeof(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode),"'+old['editor']+'");UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return "input restored";')
    call('manage_editor',dict(action='stop'))
    code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(old['reload']).lower()+';UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return "settings restored";')
