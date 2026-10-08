var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();p.enabled=false;var cc=p.GetComponent<UnityEngine.CharacterController>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var move=(System.Action<UnityEngine.Vector2,bool,bool,float>)System.Delegate.CreateDelegate(typeof(System.Action<UnityEngine.Vector2,bool,bool,float>),p,typeof(CampusRift.CampusExplorer).GetMethod("MoveCharacter",flags));
var doors=UnityEngine.Object.FindObjectsByType<CampusRift.CampusAutomaticDoor>();var probe=p.GetComponent<CampusRift.CampusTraversalProbe>()??p.gameObject.AddComponent<CampusRift.CampusTraversalProbe>();
var report=new CampusTraversalValidation.Report();
foreach(var door in doors.Where(d=>d.name.Contains("FireDoor")||d.name.Contains("GndExit")||d.name.Contains("SecDoor")||d.name.Contains("A7N")))
foreach(int side in new[]{-1,1})foreach(int edge in new[]{-1,1})
{
 var tangent=UnityEngine.Vector3.Cross(door.normal,UnityEngine.Vector3.up);float width=UnityEngine.Vector3.Dot(door.doorway.extents,new UnityEngine.Vector3(UnityEngine.Mathf.Abs(tangent.x),0,UnityEngine.Mathf.Abs(tangent.z)));
 var start=door.doorway.center+door.normal*(side*.8f)+tangent*(edge*UnityEngine.Mathf.Max(0,width-cc.radius-.03f));start.y=door.doorway.min.y+.08f;
 var target=start-door.normal*(side*1.6f);var travel=-door.normal*side;
 cc.enabled=false;p.transform.position=start;cc.enabled=true;typeof(CampusRift.CampusExplorer).GetField("planarVelocity",flags).SetValue(p,UnityEngine.Vector3.zero);typeof(CampusRift.CampusExplorer).GetField("verticalVelocity",flags).SetValue(p,-2f);typeof(CampusRift.CampusExplorer).GetField("yaw",flags).SetValue(p,0f);UnityEngine.Physics.SyncTransforms();probe.blockers.Clear();bool pass=false;
 for(int i=0;i<120;i++){foreach(var d in doors)if((d.doorway.center-p.transform.position).sqrMagnitude<30)d.Tick(1f/60);move(new UnityEngine.Vector2(travel.x,travel.z),false,false,1f/60);UnityEngine.Physics.SyncTransforms();if(UnityEngine.Vector3.Dot(p.transform.position-start,travel)>1.45f&&UnityEngine.Mathf.Abs(p.transform.position.y-target.y)<.65f){pass=true;break;}}
 report.trials.Add(new CampusTraversalValidation.Trial{source=door.name,direction=side+" edge "+edge,start=start,target=target,actual=p.transform.position,pass=pass,blockers=probe.blockers.ToArray()});if(pass)report.passed++;else report.failed++;
}
System.IO.File.WriteAllText("Artifacts/MobileControls/DoorEdges.json",UnityEngine.JsonUtility.ToJson(report,true));
return new{report.passed,report.failed,failures=report.trials.Where(t=>!t.pass).Take(12).ToArray()};

