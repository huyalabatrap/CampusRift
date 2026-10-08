var doors=new System.Collections.Generic.List<object>();
foreach(var d in UnityEngine.Object.FindObjectsByType<CampusRift.CampusAutomaticDoor>())
if(d.doorway.center.y<3 && doors.Count<35)doors.Add(new {d.name,center=d.doorway.center.ToString(),size=d.doorway.size.ToString()});
var clips=new System.Collections.Generic.List<object>();
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Skills/VoidWall/Audio"}))
{var a=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));clips.Add(new{a.name,a.length,a.samples,a.channels});}
var agent=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterNavigation>().GetComponent<UnityEngine.AI.NavMeshAgent>();
return new{doors,clips,agentRadius=agent.radius,agentHeight=agent.height,stoppingDistance=agent.stoppingDistance};
