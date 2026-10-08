"""One fresh-scene direct guard probe, not a rerun of Hunter-Lifts assertions."""
import json,time,pathlib,sys
import unity_mcp as m
m.initialize()
def call(name,args):
    r=m.call(name,args).get('result',{})
    d=r.get('structuredContent',r)
    if d.get('success') is False:raise RuntimeError(d)
    return d
if '--resume' not in sys.argv:
    call('execute_code',{'action':'execute','code':'UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return "fresh reload";'})
    call('manage_editor',{'action':'play'})
    time.sleep(12)
call('execute_code',{'action':'execute','code':'Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.UI.UIStateManager.Instance.EnterScene(true);return "QA transient; wait carving";'})
time.sleep(3)
code='''var brain=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterBrain>();var belief=brain.GetComponent<CampusRift.Monsters.MonsterBelief>();belief.EnsureGraph();var agent=brain.GetComponent<UnityEngine.AI.NavMeshAgent>();var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=UnityEngine.AI.NavMesh.AllAreas};int closed=0,count=0,walkable=0;var path=new UnityEngine.AI.NavMeshPath();var sb=new System.Text.StringBuilder();foreach(var info in belief.Graph.Lifts){var el=info.Elevator;if(el.IsMoving||el.DoorAmount>.3f||info.Landing[0]<0)continue;var inside=el.cabinSpaces[0].center;inside.y=el.floors[0].height;var lobby=info.Lobby[0];UnityEngine.AI.NavMeshHit hit;bool startOnMesh=UnityEngine.AI.NavMesh.SamplePosition(lobby,out hit,.3f,filter);if(startOnMesh)walkable++;count++;bool complete=UnityEngine.AI.NavMesh.CalculatePath(lobby,inside,filter,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete&&Vector3.Distance(path.corners[path.corners.Length-1],inside)<.5f;if(!complete)closed++;sb.AppendLine(el.building+": blocked="+(!complete)+" lobbyOnMesh="+startOnMesh+" door="+el.DoorAmount+" status="+path.status);}sb.AppendLine("Blocked="+closed+"/"+count+" walkableStarts="+walkable+" guards="+CampusRift.Monsters.ElevatorShaftGuard.GuardCount+" time="+UnityEngine.Time.time+" frame="+UnityEngine.Time.frameCount);return sb.ToString();'''
probe=call('execute_code',{'action':'execute','code':code})
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True})
probe_path=pathlib.Path('task/p17/lift-direct-probe.json')
if probe_path.exists():probe_path.rename(probe_path.with_name('lift-direct-probe-first-derived-lobby.json'))
probe_path.write_text(json.dumps({'probe':probe,'console':console,'scope':'fresh reload; same graph lobby query as Hunter-Lifts; verify walkable query origins; carving after several seconds; does not replace raw Hunter-Lifts0/12'},ensure_ascii=False,indent=2),encoding='utf-8')
print(probe.get('data',{}).get('result'))
call('manage_editor',{'action':'stop'})
call('execute_code',{'action':'execute','code':'UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;return "original Enter Play Options restored";'})
