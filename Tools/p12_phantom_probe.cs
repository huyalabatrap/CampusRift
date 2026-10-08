var player=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();
var decoy=player.GetComponent<CampusRift.Skills.PhantomDecoySkill>().Decoy;
System.IO.Directory.CreateDirectory("Artifacts/P12/regressions");
System.IO.File.WriteAllText("Artifacts/P12/regressions/Phantom-door-probe.txt","");
decoy.Dissolved+=point=>{
 var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
 var corners=(UnityEngine.Vector3[])decoy.GetType().GetField("corners",flags).GetValue(decoy);
 int index=(int)decoy.GetType().GetField("corner",flags).GetValue(decoy);
 var heading=index<corners.Length?UnityEngine.Vector3.ProjectOnPlane(corners[index]-point,UnityEngine.Vector3.up).normalized:decoy.transform.forward;
 UnityEngine.AI.NavMeshHit from,to,edge;
 bool on=UnityEngine.AI.NavMesh.SamplePosition(point,out from,.9f,UnityEngine.AI.NavMesh.AllAreas);
 bool ahead=UnityEngine.AI.NavMesh.SamplePosition(point+heading*7.4f*UnityEngine.Mathf.Min(UnityEngine.Time.deltaTime,.05f),out to,.65f,UnityEngine.AI.NavMesh.AllAreas);
 bool ray=on&&ahead&&UnityEngine.AI.NavMesh.Raycast(from.position,to.position,out edge,UnityEngine.AI.NavMesh.AllAreas);
 var doors=UnityEngine.Object.FindObjectsByType<CampusRift.CampusAutomaticDoor>(UnityEngine.FindObjectsSortMode.None).Where(d=>UnityEngine.Vector3.Distance(d.doorway.center,point)<8).Select(d=>d.name+" center="+d.doorway.center+" open="+d.OpenAmount);
 UnityEngine.RaycastHit wall;bool obstacle=UnityEngine.Physics.Raycast(point+UnityEngine.Vector3.up,heading,out wall,2,UnityEngine.Physics.DefaultRaycastLayers,UnityEngine.QueryTriggerInteraction.Ignore);
 System.IO.File.AppendAllText("Artifacts/P12/regressions/Phantom-door-probe.txt","at="+point+" time="+(UnityEngine.Time.time-decoy.SpawnedAt)+" corner="+index+"/"+corners.Length+" path="+string.Join(";",corners.Select(v=>v.ToString()).ToArray())+"\nnav="+on+"/"+ahead+" ray="+ray+" from="+from.position+" to="+to.position+" physical="+(obstacle?wall.collider.name:"none")+" doors="+string.Join(";",doors.ToArray())+"\n");
};
return "Phantom diagnostic attached";
