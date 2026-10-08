var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();
var sim=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();
var session=p.GetComponent<CampusRift.AR.ARBattlefield>().ModeSession;
var hits=new System.Collections.Generic.List<UnityEngine.XR.ARFoundation.ARRaycastHit>();
p.raycasts.Raycast(new Vector2(Screen.width*.5f,Screen.height*.5f),hits,UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon);
var planes=new System.Collections.Generic.List<string>();foreach(var x in p.planes.trackables)planes.Add(x.alignment+":"+x.transform.position+":"+x.size+":"+x.trackingState);
return new{p.Message,p.HitType,p.ReticleValid,p.Adjusting,p.InputBlocked,enabled=p.enabled,root=p.Root!=null,state=UnityEngine.XR.ARFoundation.ARSession.state.ToString(),p.FirstPlaneAt,p.ValidAt,floor=p.settings.Floor,selection=session.SelectionConfirmed,simPosition=sim.transform.position.ToString(),simRotation=sim.transform.eulerAngles.ToString(),viewPosition=p.view.transform.position.ToString(),viewRotation=p.view.transform.eulerAngles.ToString(),screen=Screen.width+"x"+Screen.height,planes,hits=hits.Select(x=>x.pose.position.ToString()).ToArray()};
