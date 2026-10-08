"""Run the P11 mechanic and visual/audit harnesses in fresh sessions."""
import json,pathlib,time,subprocess,sys
import unity_mcp as m
m.initialize()
def call(name,args):
    answer=m.call(name,args);result=answer.get('result',{}).get('structuredContent',answer)
    if result.get('success') is False:raise RuntimeError(json.dumps(result))
    return result
cases=[('ReactionPlayTest','Tools/p11_start.cs','Artifacts/Reactions/DONE.txt','Artifacts/Reactions/Validation.json',300),
       ('ReactionVisualCapture','Tools/p11_capture.cs','Artifacts/Reactions/Visual-DONE.txt','Artifacts/Reactions/Visual.json',420)]
for name,script,done,report,timeout in cases:
    if len(sys.argv)>1 and name not in sys.argv[1:]:continue
    call('manage_editor',{'action':'stop'});call('execute_code',{'action':'execute','code':'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); return "opened";'})
    call('manage_editor',{'action':'play'});time.sleep(1);started=time.time();call('execute_code',{'action':'execute','code':pathlib.Path(script).read_text(encoding='utf-8')});print('START '+name,flush=True)
    path=pathlib.Path(done);deadline=time.time()+timeout
    while time.time()<deadline:
        if path.exists() and path.stat().st_mtime>=started:break
        time.sleep(1)
    if not path.exists() or path.stat().st_mtime<started:raise TimeoutError(name)
    result=path.read_text(encoding='utf-8-sig');print(name+': '+result,flush=True);data=json.loads(pathlib.Path(report).read_text(encoding='utf-8-sig'))
    call('manage_editor',{'action':'stop'})
    if data.get('failed') or data.get('error') or (name=='ReactionVisualCapture' and result.strip()!='PASS'):sys.exit(1)
subprocess.run([sys.executable,'Tools/p11_sheets.py'],check=True)
