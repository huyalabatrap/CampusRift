from ar_fix3_local import *
code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();typeof(CampusRift.AR.RiftPlacementService).GetProperty("Radius").SetValue(p,.18f);return true;')
time.sleep(.2)
state=code('var s=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSkillCaster>();typeof(CampusRift.AR.ARSkillCaster).GetMethod("Build",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(s,null);var c=s.Caster.GetComponent<UnityEngine.CharacterController>();return new {radius=s.field.placement.Radius,scale=s.field.Scale,rootScale=s.field.Root.lossyScale.ToString(),controllerEnabled=c.enabled,stepOffset=c.stepOffset,runtimes=s.Caster.GetComponents<CampusRift.Skills.SkillRuntime>().Length};')
assert state['runtimes']==5 and not state['controllerEnabled'] and state['stepOffset']==0 and abs(state['radius']-.18)<.001,state
time.sleep(.3)
save(out/'small-scale-controller.json',state)
errors=console(out/'small-scale-console.json');assert errors['data']==[],errors
code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();p.Reposition();p.InputBlocked=true;return true;')
time.sleep(.4)
visual=code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>();int count=0,visible=0,enabled=0;foreach(var plane in p.planes.trackables){count++;foreach(var r in plane.GetComponentsInChildren<UnityEngine.Renderer>(true))if(r.enabled)visible++;foreach(var v in plane.GetComponentsInChildren<UnityEngine.XR.ARFoundation.ARPlaneMeshVisualizer>(true))if(v.enabled)enabled++;}return new {count,visible,visualizersEnabled=enabled,managerEnabled=p.planes.enabled};')
assert visual['count']>0 and visual['visible']>0 and visual['visualizersEnabled']>0 and visual['managerEnabled'],visual
save(out/'plane-visual-restored.json',visual)
progress('Kiểm hẹp visual/scale đạt\n- Neo world: 5 plane, 0 renderer/visualizer bật sau nhiều frame, manager tắt. Đổi vị trí bật lại visual. Bán kính 0,18m / scale 0,054 có đủ 5 runtime, controller tắt/stepOffset0, Console0. 10 ảnh cuối đã chụp lại, kiểm text hẹp0; không chạy lại full audit/harness. Tiếp build2.')
print(state,visual,flush=True)
