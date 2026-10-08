"""Fresh Play sessions; retain P12 regression evidence separately from historical P10 results."""
import json,pathlib,shutil,time,sys
import unity_mcp as m
m.initialize()
out=pathlib.Path('Artifacts/P12/regressions');out.mkdir(parents=True,exist_ok=True)
cases=[
('MinionCombat','CampusRift.Enemies.MinionCombatPlayTest','Artifacts/Enemies/MinionCombat.json','Artifacts/Enemies/MinionCombat-DONE.txt',400),
('ShabanCombat','ShabanCombatPlayTest','Artifacts/Combat/Validation.json','Artifacts/Combat/DONE.txt',350),
('ShabanPressure','ShabanPressurePlayTest','Artifacts/ShabanPressure/Validation.json','Artifacts/ShabanPressure/DONE.txt',400),
('ShabanBehavior','ShabanBehaviorPlayTest','Assets/MonsterShaban/Validation/BehaviorPlayMode.json','Temp/shaban-behavior-progress.txt',450),
('ReactionPlayTest','CampusRift.Combat.ReactionPlayTest','Artifacts/Reactions/Validation.json','Artifacts/Reactions/DONE.txt',300),
('SkillSet1','CampusRift.Skills.SkillSet1PlayTest','Artifacts/Skills/SkillSet1.json','Artifacts/Skills/SkillSet1-DONE.txt',900),
('SkillSet1Pool','CampusRift.Skills.SkillSet1PoolPlayTest','Artifacts/Skills/SkillSet1-Pool.json','Artifacts/Skills/SkillSet1-Pool-DONE.txt',650),
('SkillSet1Edges','CampusRift.Skills.SkillSet1EdgePlayTest','Artifacts/Skills/SkillSet1-Edges.json','Artifacts/Skills/SkillSet1-Edges-DONE.txt',240),
('GiantHandSeal','CampusRift.Skills.GiantHandPlayTest','Artifacts/GiantHandSeal/Validation.json','Artifacts/GiantHandSeal/DONE.txt',300),
('PhantomDecoy','CampusRift.Skills.PhantomDecoyWorldPlayTest','Artifacts/PhantomDecoy/WorldValidation.json','Artifacts/PhantomDecoy/WorldValidation-DONE.txt',300),
('VoidWall','CampusRift.Skills.VoidWallPlayTest','Artifacts/VoidWall/Validation.json','Artifacts/VoidWall/DONE.txt',300),
('VoidWallQuickCast','CampusRift.Skills.VoidWallQuickCastPlayTest','Artifacts/VoidWall/QuickCast.json','Artifacts/VoidWall/QuickCast-DONE.txt',300),
('SkillLoadout','CampusRift.Skills.SkillLoadoutPlayTest','Artifacts/Skills/Loadout.json','Artifacts/Skills/Loadout-DONE.txt',300),
('DamagePipeline','CampusRift.Combat.DamagePipelinePlayTest','Artifacts/Combat/Pipeline.json','Artifacts/Combat/Pipeline-DONE.txt',300),
('PlayerCombat','CampusRift.Combat.PlayerCombatPlayTest','Artifacts/Combat/PlayerCombat.json','Artifacts/Combat/PlayerCombat-DONE.txt',300),
('LightningFlash','CampusRift.Skills.LightningFlashPlayTest','Artifacts/Skills/LightningFlash.json','Artifacts/Skills/LightningFlash-DONE.txt',300),
('HubFlow','CampusRift.UI.HubFlowPlayTest','Artifacts/UI/HubFlow.json','Artifacts/UI/HubFlow-DONE.txt',300),
('HubLayout','CampusRift.UI.HubLayoutPlayTest','Artifacts/UI/HubLayout.json','Artifacts/UI/HubLayout-DONE.txt',300)]
summary_path=out.parent/'Regressions.json'
summary=json.loads(summary_path.read_text()) if len(sys.argv)>1 and summary_path.exists() else []
def call(name,args):
    for attempt in range(30):
        answer=m.call(name,args);result=answer.get('result',{}).get('structuredContent',answer)
        if result.get('success') is not False:return result
        if 'not ready' not in str(result).lower() and 'please retry' not in str(result).lower():raise RuntimeError(json.dumps(result))
        time.sleep(2)
    raise RuntimeError(json.dumps(result))
for name,component,report,done,timeout in cases:
    if len(sys.argv)>1 and name not in sys.argv[1:]:continue
    call('manage_editor',{'action':'stop'});time.sleep(1.5)
    call('execute_code',{'action':'execute','code':'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); return "opened";'})
    call('read_console',{'action':'clear'})
    call('manage_editor',{'action':'play'});time.sleep(2)
    if name=='PhantomDecoy':call('execute_code',{'action':'execute','code':pathlib.Path('Tools/p12_phantom_probe.cs').read_text(encoding='utf-8-sig')})
    started=time.time();call('execute_code',{'action':'execute','code':'foreach(var device in UnityEngine.InputSystem.InputSystem.devices) UnityEngine.InputSystem.InputSystem.EnableDevice(device); new UnityEngine.GameObject("P12 regression '+name+'").AddComponent<'+component+'>(); return "started";'})
    print('START '+name,flush=True);deadline=time.time()+timeout;path=pathlib.Path(done)
    while time.time()<deadline:
        if path.exists() and path.stat().st_mtime>=started and (name!='ShabanBehavior' or path.read_text().startswith('DONE')):break
        time.sleep(1)
    completed=path.exists() and path.stat().st_mtime>=started
    data=json.loads(pathlib.Path(report).read_text(encoding='utf-8-sig')) if completed else {}
    result=path.read_text(encoding='utf-8-sig') if completed else 'TIMEOUT';failed=list(data.get('failed',[]));known=False
    if data.get('error'):failed.append(data['error'])
    if completed and result.strip()=='FAIL' and name=='PhantomDecoy':
        base=json.loads(pathlib.Path('Artifacts/Skills/fix2/regressions/PhantomDecoy-baseline.json').read_text(encoding='utf-8-sig'))
        known=all(data.get(key)==base.get(key) for key in ('doorCast','crossedEntrance','openedEntrance','stairCast','descendedStair'))
    if result.strip()=='FAIL' and not known:failed.append('DONE=FAIL')
    if completed:shutil.copy2(report,out/(name+'.json'));shutil.copy2(done,out/(name+'-DONE.txt'))
    console=call('read_console',{'action':'get','types':['error','warning'],'count':50})
    (out/(name+'-console.json')).write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
    summary=[entry for entry in summary if entry['test']!=name]+[dict(test=name,completed=completed,result=result,failed=failed,baselineKnownFail=known,report=str(out/(name+'.json')),capturedAt=time.strftime('%Y-%m-%dT%H:%M:%S'))]
    summary_path.write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8');print(name+': '+result,flush=True)
call('manage_editor',{'action':'stop'});time.sleep(1.5)
