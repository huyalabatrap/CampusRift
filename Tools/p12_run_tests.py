"""Fresh sessions, timestamp guards, raw archival; resume by explicitly naming tests."""
import json,time,sys,shutil
from pathlib import Path
import unity_mcp as m
m.initialize();root=Path('Artifacts/P12/tests');root.mkdir(parents=True,exist_ok=True)
names=sys.argv[1:] or ['EnemyRosterPlayTest','EnemyAnimationPlayTest','ShabanBossPlayTest','SkyBeastPresencePlayTest','Level3to7PlayTest']
def call(name,args):
    for attempt in range(30):
        r=m.call(name,args).get('result',{});d=r.get('structuredContent',r)
        if d.get('success') is not False:return d
        if 'not ready' not in str(d).lower() and 'please retry' not in str(d).lower():raise RuntimeError(str(d))
        time.sleep(2)
    raise RuntimeError(str(d))
for name in names:
    call('manage_editor',{'action':'stop'});time.sleep(1.5)
    call('execute_code',{'action':'execute','code':'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); return "opened";'})
    call('read_console',{'action':'clear'})
    call('manage_editor',{'action':'play'});time.sleep(2);start=time.time()
    call('execute_code',{'action':'execute','code':f'new UnityEngine.GameObject("P12 {name}").AddComponent<CampusRift.Validation.{name}>(); return "started";'})
    print('START',name,flush=True);done=root/(name+'-DONE.txt')
    deadline=time.time()+(1800 if name=="P12BalancePlayTest" else 650)
    while time.time()<deadline:
        if done.exists() and done.stat().st_mtime>=start:break
        time.sleep(1)
    complete=done.exists() and done.stat().st_mtime>=start
    if not complete:print('TIMEOUT',name,call('read_console',{'action':'get','types':['error'],'count':20}),flush=True);break
    print(name,done.read_text(),flush=True)
    archive=root/'runs'/time.strftime('%Y%m%d-%H%M%S');archive.mkdir(parents=True,exist_ok=True)
    for p in [done,root/(name+'.json')]:shutil.copy2(p,archive/p.name)
    (archive/'console.json').write_text(json.dumps(call('read_console',{'action':'get','types':['error','warning'],'count':30}),indent=2),encoding='utf-8')
call('manage_editor',{'action':'stop'});time.sleep(1.5)
