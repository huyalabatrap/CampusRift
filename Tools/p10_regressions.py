import json,pathlib,time,sys
import unity_mcp as m
m.initialize()
cases=[
 ('GiantHandSeal','CampusRift.Skills.GiantHandPlayTest','Artifacts/GiantHandSeal/Validation.json','Artifacts/GiantHandSeal/DONE.txt'),
 ('PhantomDecoy','CampusRift.Skills.PhantomDecoyWorldPlayTest','Artifacts/PhantomDecoy/WorldValidation.json','Artifacts/PhantomDecoy/WorldValidation-DONE.txt'),
 ('VoidWall','CampusRift.Skills.VoidWallPlayTest','Artifacts/VoidWall/Validation.json','Artifacts/VoidWall/DONE.txt'),
 ('VoidWallQuickCast','CampusRift.Skills.VoidWallQuickCastPlayTest','Artifacts/VoidWall/QuickCast.json','Artifacts/VoidWall/QuickCast-DONE.txt'),
 ('SkillLoadout','CampusRift.Skills.SkillLoadoutPlayTest','Artifacts/Skills/Loadout.json','Artifacts/Skills/Loadout-DONE.txt'),
 ('DamagePipeline','CampusRift.Combat.DamagePipelinePlayTest','Artifacts/Combat/Pipeline.json','Artifacts/Combat/Pipeline-DONE.txt'),
 ('PlayerCombat','CampusRift.Combat.PlayerCombatPlayTest','Artifacts/Combat/PlayerCombat.json','Artifacts/Combat/PlayerCombat-DONE.txt'),
 ('LightningFlash','CampusRift.Skills.LightningFlashPlayTest','Artifacts/Skills/LightningFlash.json','Artifacts/Skills/LightningFlash-DONE.txt'),
 ('HubFlow','CampusRift.UI.HubFlowPlayTest','Artifacts/UI/HubFlow.json','Artifacts/UI/HubFlow-DONE.txt'),
 ('HubLayout','CampusRift.UI.HubLayoutPlayTest','Artifacts/UI/HubLayout.json','Artifacts/UI/HubLayout-DONE.txt')]
output=pathlib.Path('Artifacts/Skills/P10-Regressions.json')
summary=json.loads(output.read_text(encoding='utf-8')) if len(sys.argv)>1 and output.exists() else []
def call(name,args):
    answer=m.call(name,args)
    result=answer.get('result',{}).get('structuredContent',answer)
    if result.get('success') is False: raise RuntimeError(json.dumps(result))
    return result
for name,component,report,done in cases:
    if len(sys.argv)>1 and name not in sys.argv[1:]:continue
    call('manage_editor',{'action':'stop'})
    call('execute_code',{'action':'execute','code':'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); return "opened";'})
    call('manage_editor',{'action':'play'});time.sleep(2)
    started=time.time()
    call('execute_code',{'action':'execute','code':'if(!UnityEditor.EditorApplication.isPlaying) return "Play required"; new UnityEngine.GameObject("P10 regression '+name+'").AddComponent<'+component+'>(); return "started";'})
    print('START '+name,flush=True)
    deadline=time.time()+300
    path=pathlib.Path(done)
    while time.time()<deadline:
        if path.exists() and path.stat().st_mtime>=started:break
        time.sleep(1)
    completed=path.exists() and path.stat().st_mtime>=started
    data=json.loads(pathlib.Path(report).read_text(encoding='utf-8-sig')) if completed else {}
    message=path.read_text(encoding='utf-8-sig') if completed else 'TIMEOUT'
    failed=list(data.get('failed',[]))
    baseline_known_fail=False
    if completed and message.strip()=='FAIL':
        if name=='PhantomDecoy':
            baseline=json.loads(pathlib.Path('Artifacts/Skills/P10-baseline/PhantomDecoy-WorldValidation.json').read_text(encoding='utf-8-sig'))
            checks=('doorCast','crossedEntrance','openedEntrance','stairCast','descendedStair')
            baseline_known_fail=all(data.get(key)==baseline.get(key) for key in checks)
        if not baseline_known_fail:failed.append('DONE=FAIL')
    summary=[s for s in summary if s['test']!=name]
    summary.append(dict(test=name,completed=completed,result=message,report=report,failed=failed,baselineKnownFail=baseline_known_fail,capturedAt=time.strftime('%Y-%m-%dT%H:%M:%S')))
    pathlib.Path('Artifacts/Skills/P10-Regressions.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
    print(name+': '+message,flush=True)
call('manage_editor',{'action':'stop'})
