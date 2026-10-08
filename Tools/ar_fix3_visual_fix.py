from ar_fix3_local import *
state=code('return new {playing=UnityEditor.EditorApplication.isPlaying,building=UnityEditor.BuildPipeline.isBuildingPlayer};')
assert not any(state.values()),state
p=Path('Assets/ARRift/Runtime/RiftPlacementService.cs');text=p.read_text(encoding='utf-8-sig')
old='planes.requestedDetectionMode=PlaneDetectionMode.None;foreach(var p in planes.trackables)foreach(var r in p.GetComponentsInChildren<Renderer>())r.enabled=false;planes.enabled=false;'
assert old in text
text=text.replace(old,'planes.requestedDetectionMode=PlaneDetectionMode.None;SetPlaneVisuals(false);planes.enabled=false;')
text=text.replace('planes.enabled=true;if(points!=null)points.enabled=true;', 'planes.enabled=true;SetPlaneVisuals(true);if(points!=null)points.enabled=true;')
text=text.replace('        public void Reposition()', '''        void SetPlaneVisuals(bool visible)
        {
            foreach(var plane in planes.trackables)
            {
                // The visualizer updates renderer.enabled every frame independently of its manager.
                foreach(var visualizer in plane.GetComponentsInChildren<ARPlaneMeshVisualizer>(true))visualizer.enabled=visible;
                if(!visible)foreach(var renderer in plane.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            }
        }
        public void Reposition()''')
p.write_text(text,encoding='utf-8')
code('UnityEditor.AssetDatabase.Refresh();UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();return true;')
errors=console(out/'visual-fix-compile.json');assert errors['data']==[],errors
progress('Ẩn plane sau neo — sửa cuối\n- Soi ảnh phát hiện ARPlaneMeshVisualizer tự bật renderer mỗi Update dù manager tắt. Đã tắt cả visualizer khi neo và bật lại khi Đổi vị trí. Build1 giữ attempt1; cần kiểm hẹp visual và build lại vì source đổi. Không chạy lại harness/full audit.')
