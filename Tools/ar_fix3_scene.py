from ar_fix3_local import *
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.Floor",0);UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.HelpSeen",1);UIValidation.SetResolution(1600,720);return true;')
clear();code('UnityEditor.EditorApplication.isPlaying=true;return true;');time.sleep(1);background()
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>();c.Continue();c.Continue();return true;')
for i in range(30):
    if code('return UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>()!=null;'):break
    time.sleep(.2)
else:raise RuntimeError('Simulation provider not ready')
code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,180,0);return true;')
for i in range(30):
    time.sleep(.2)
    r=code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();var c=UnityEngine.Camera.main;var provider=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();var planes=new System.Collections.Generic.List<object>();foreach(var x in p.planes.trackables)planes.Add(new {x.trackableId,position=x.transform.position,area=CampusRift.AR.ARPlaneScoring.Area(x.boundary.ToArray()),x.trackingState});return new {state=UnityEngine.XR.ARFoundation.ARSession.state.ToString(),p.ReticleValid,p.Message,p.HitType,p.Radius,p.EnteredAt,p.FirstPlaneAt,p.ValidAt,p.AnchoredAt,p.AnchorMethod,root=p.Root!=null,adjusting=p.Adjusting,shr ine=p.GetComponent<CampusRift.AR.ARBattlefield>().Shrine!=null,camera=c.transform.position,rotation=c.transform.eulerAngles,provider=provider.transform.position,planes};'.replace('shr ine','shrine'))
    save(out/'placement-observed.json',r)
    if r['shrine']:break
else:raise RuntimeError('Placement not complete: '+str(r))
save(out/'placement-timing.json',r)
console(out/'placement-console.json')
progress('A simulation gate\n- Placement đã neo/bắt đầu trong fixture XR Simulation; xem placement-timing.json (thời gian thực được ghi, không giả mục tiêu PASS). Tiếp kiểm plane drift/ảnh HUD/harness1lượt.')
print(json.dumps(r,ensure_ascii=True))
