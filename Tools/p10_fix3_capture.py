import json,pathlib,time,sys
import unity_mcp as m
m.initialize()
def call(name,args):
    answer=m.call(name,args);r=answer.get('result',{}).get('structuredContent',answer)
    if r.get('success') is False:raise RuntimeError(r)
    return r
call('manage_editor',{'action':'stop'})
call('execute_code',{'action':'execute','code':'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); return "opened";'})
call('manage_editor',{'action':'play'});time.sleep(2)
started=time.time();id=sys.argv[1]
call('execute_code',{'action':'execute','code':'var c=new UnityEngine.GameObject("Fix3 visual").AddComponent<CampusRift.Skills.SkillVisualCapture>();c.skillId="'+id+'";return "started";'})
p=pathlib.Path('Artifacts/Skills/fix3')/(id+'-visual.json')
deadline=time.time()+120
while time.time()<deadline:
    if p.exists() and p.stat().st_mtime>started:break
    time.sleep(1)
else:raise TimeoutError(id)
print(p.read_text(),flush=True)
call('manage_editor',{'action':'stop'})
