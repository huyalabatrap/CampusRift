"""One deterministic related branch: real targets, current skill, controlled tick cadence."""
from pathlib import Path
import json,time,sys
import unity_mcp as m
root=Path.cwd();phase=sys.argv[1];out=root/'task/p23/chain-probe';out.mkdir(parents=True,exist_ok=True)
m.initialize()
def call(name,args):
    for _ in range(35):
        raw=m.call(name,args);v=raw.get('result',{}).get('structuredContent',raw.get('result',raw))
        if v.get('success') is not False:return v
        transient=v.get('data',{}).get('reason') in ('no_unity_session','unity_session_not_ready')
        if not transient and not any(s in str(v.get('message','')).lower() for s in ('retry','not ready','compiling','domain reload','busy')):raise RuntimeError(v)
        time.sleep(2)
    raise RuntimeError(v)
def code(s):
    return call('execute_code',{'action':'execute','code':s}).get('data',{}).get('result')
call('manage_editor',{'action':'stop'})
code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return "opened";')
call('read_console',{'action':'clear'})
call('manage_editor',{'action':'play'});time.sleep(3)
code('Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.LocalTelemetryEnabled=false;s.TelemetryConsentAsked=true;s.Subtitles=false;s.TextSize=0;s.ControlMode=CampusRift.Controls.ControlMode.PC;CampusRift.UI.SettingsManager.Instance.Apply(s,false);return "QA";')
rows=[]
for rank,step in [(4,1/60),(4,.3),(1,.3)]:
    snippet='''
var w=new CampusRift.Skills.SkillSet1TestWorld{GameplayCamera=true};w.Begin();
CampusRift.Skills.P18TestSupport.Prepare(w,false);
var skill=w.player.GetComponent<CampusRift.Skills.ChainLightningRuntime>();
CampusRift.Skills.P18TestSupport.SetRank(skill,RANK);w.player.GetComponent<CampusRift.Skills.SkillLoadout>().Equip(2,skill.Id);
var extras=new System.Collections.Generic.List<CampusRift.Enemies.EnemyInstance>();
for(int i=0;i<2;i++){var e=CampusRift.Enemies.EnemyPool.Instance.Spawn(w.victims[0].GetComponent<CampusRift.Enemies.EnemyInstance>().archetype,w.origin+new Vector3(8+i,0,1),CampusRift.Enemies.EnemyScaling.Default,false);e.Vitality.SetMaxHealth(10000,true);e.Vitality.Element=CampusRift.Combat.Element.None;e.Brain.enabled=false;e.Motor.Stop();extras.Add(e);}
Physics.SyncTransforms();
bool cast=skill.CastAt(w.origin+Vector3.right*8);
Time.timeScale=0;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var tick=typeof(CampusRift.Skills.ChainLightningRuntime).GetMethod("TickCast",flags);
var elapsed=typeof(CampusRift.Skills.Set1SkillRuntime).GetField("elapsed",flags);
var progress=new System.Collections.Generic.List<int>();int frames=0;
while(skill.IsCasting && frames<200){frames++;elapsed.SetValue(skill,frames*STEPf);tick.Invoke(skill,new object[]{STEPf});progress.Add(skill.BounceCount);}
int unique=0;foreach(var victim in w.victims)if(victim.Health<10000)unique++;foreach(var e in extras)if(e.Vitality.Health<10000)unique++;
var result=new {rank=RANK,step=STEPf,cast,frames,bounces=skill.BounceCount,uniqueHits=unique,completed=!skill.IsCasting,damage=skill.BounceDamage,progress};
foreach(var e in extras)CampusRift.Enemies.EnemyPool.Instance.Release(e);Time.timeScale=1;w.End();return result;
'''.replace('RANK',str(rank)).replace('STEP',format(step,'.9f'))
    row=code(snippet);row['pass']=row['cast'] and row['completed'] and row['bounces']==(9 if rank==4 else 6) and row['uniqueHits']==row['bounces'];rows.append(row)
console=call('read_console',{'action':'get','types':['error','warning'],'count':100,'format':'detailed','include_stacktrace':True})
(out/(phase+'-console.json')).write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
errors=[x for x in console.get('data',[]) if isinstance(x,dict) and x.get('type') in ('Error','Exception','Assert')]
(out/(phase+'.json')).write_text(json.dumps({'phase':phase,'method':'Same SkillSet2 mastery arrangement, nine real enemy prefabs, real damage and runtime TickCast; controlled elapsed/dt. No modified test thresholds. Only Chain Lightning branch; not full SkillSet2 rerun.','checks':rows,'consoleErrors':len(errors)},ensure_ascii=False,indent=2),encoding='utf-8')
call('manage_editor',{'action':'stop'})
print(phase,[(r['rank'],r['step'],r['bounces'],r['uniqueHits'],r['pass']) for r in rows], 'Console errors',len(errors),flush=True)
if phase=='after':assert all(r['pass'] for r in rows) and not errors
