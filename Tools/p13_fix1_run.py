"""Run only the fix1 capture or the existing relevant FireBreath smoke, restore Editor settings."""
import json,time,sys
from pathlib import Path
from p13_cli import call
job=sys.argv[1]
typ,done=('CampusRift.SkyBeast.P13Fix1Capture','task/p13/fix1-capture-DONE.txt') if job=='capture' else ('CampusRift.SkyBeast.FireBreathPlayTest','Artifacts/SkyBeast/FireBreath-DONE.txt')
root=Path('task/p13')
call('manage_editor',{'action':'stop'})
result=call('execute_code',{'action':'execute','code':'bool old=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled; UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false; UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); return old;'})
(root/'fix1-editor-reload-original.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
# The authored setting is reload disabled; the runner enables fresh fixture lifecycle only for Play.
call('read_console',{'action':'clear'})
begin=time.time()
try:
    call('manage_editor',{'action':'play'})
    time.sleep(1)
    setup='var harness=new UnityEngine.GameObject("P13 fix1 '+job+'").AddComponent<'+typ+'>(); '
    if job=='capture' and len(sys.argv)>2 and sys.argv[2]=='dragon':setup+='harness.DragonOnly=true; '
    if job=='capture' and len(sys.argv)>2 and sys.argv[2]=='mobile':setup+='harness.MobileOnly=true; '
    if job=='capture' and len(sys.argv)>2 and sys.argv[2]=='warning':setup+='harness.WarningOnly=true; '
    call('execute_code',{'action':'execute','code':setup+'return "started";'})
    print('START',job,flush=True)
    deadline=time.time()+90
    while time.time()<deadline:
        p=Path(done)
        if p.exists() and p.stat().st_mtime>=begin:break
        time.sleep(.5)
    else:raise RuntimeError('No fresh completion: '+job)
    errors=call('read_console',{'action':'get','types':['error'],'count':30,'format':'detailed'})
    (root/('fix1-'+job+'-console.json')).write_text(json.dumps(errors,indent=2),encoding='utf-8')
    print(p.read_text(),flush=True);print('ERRORS',errors,flush=True)
    if job=='fire':(root/'fix1-FireBreath.json').write_bytes(Path('Artifacts/SkyBeast/FireBreath.json').read_bytes())
    (root/('fix1-'+job+'-run.json')).write_text(json.dumps({'started':begin,'finished':time.time(),'done':done,'fresh':True,'errors':errors},indent=2),encoding='utf-8')
finally:
    call('manage_editor',{'action':'stop'})
    call('execute_code',{'action':'execute','code':'UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true; return "restored authored reload setting";'})
