"""Run the final P10 mechanics, pool, edge and UI checks in fresh Play sessions."""
import json,pathlib,time,sys
import unity_mcp as m
m.initialize()
cases=[
 ('SkillSet1','CampusRift.Skills.SkillSet1PlayTest','Artifacts/Skills/SkillSet1.json','Artifacts/Skills/SkillSet1-DONE.txt',900),
 ('SkillSet1Pool','CampusRift.Skills.SkillSet1PoolPlayTest','Artifacts/Skills/SkillSet1-Pool.json','Artifacts/Skills/SkillSet1-Pool-DONE.txt',600),
 ('SkillSet1Edges','CampusRift.Skills.SkillSet1EdgePlayTest','Artifacts/Skills/SkillSet1-Edges.json','Artifacts/Skills/SkillSet1-Edges-DONE.txt',180),
 ('ComicTextAudit','CampusRift.Skills.SkillSet1UiAudit','Artifacts/Skills/P10-ComicTextAudit.json','Artifacts/Skills/P10-ComicTextAudit-DONE.txt',180)]
out=pathlib.Path('Artifacts/Skills/P10-FinalTests.json')
summary=json.loads(out.read_text(encoding='utf-8')) if len(sys.argv)>1 and out.exists() else []
def call(name,args):
    answer=m.call(name,args);result=answer.get('result',{}).get('structuredContent',answer)
    if result.get('success') is False:raise RuntimeError(json.dumps(result))
    return result
for name,component,report,done,timeout in cases:
    if len(sys.argv)>1 and name not in sys.argv[1:]:continue
    call('manage_editor',{'action':'stop'})
    call('execute_code',{'action':'execute','code':'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); return "opened";'})
    call('manage_editor',{'action':'play'});time.sleep(2)
    started=time.time()
    call('execute_code',{'action':'execute','code':'new UnityEngine.GameObject("P10 final '+name+'").AddComponent<'+component+'>(); return "started";'})
    print('START '+name,flush=True);path=pathlib.Path(done);deadline=time.time()+timeout
    while time.time()<deadline:
        if path.exists() and path.stat().st_mtime>=started:break
        time.sleep(1)
    completed=path.exists() and path.stat().st_mtime>=started
    data=json.loads(pathlib.Path(report).read_text(encoding='utf-8-sig')) if completed else {}
    entry=dict(test=name,completed=completed,result=path.read_text(encoding='utf-8-sig') if completed else 'TIMEOUT',failed=data.get('failed',[]),error=data.get('error'),report=report,capturedAt=time.strftime('%Y-%m-%dT%H:%M:%S'))
    summary=[s for s in summary if s['test']!=name]+[entry]
    out.write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8');print(name+': '+entry['result'],flush=True)
call('manage_editor',{'action':'stop'})
