from ar_fix2_local import *
console(out/'qa-hotreload-console.json')
save(out/'qa-hotreload-state.json',code(Path('task/ar/fix2-state.cs').read_text(encoding='utf-8')))
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>();if(c!=null)c.Shutdown();foreach(var a in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.ARFoundation.ARAnchor>(UnityEngine.FindObjectsSortMode.None))a.enabled=false;var xr=UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager;if(xr.activeLoader!=null){xr.StopSubsystems();xr.DeinitializeLoader();}UnityEditor.EditorApplication.isPlaying=false;return true;')
time.sleep(3)
for i in range(40):
    if not code('return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;'):break
    time.sleep(1)
clear()
script=Path('Tools/ar_fix2_scene.py').read_text(encoding='utf-8')
start="code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene"
script=script[script.index(start):]
# Keep the original compile/runtime evidence. This pass is just the recovered fixture.
script=script.replace("'runtime-wiring.json'","'recovered-wiring-initial.json'").replace("'scene-console.json'","'recovered-scene-console.json'")
exec(script)
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — QA fixture recovery\n- Local Editor runner đã recompile khi Play Mode, mất managed XR refs/Session=None; focusedcombo/input trước đó vô hiệu do fixture bị hotreload (raw giữ qa-hotreload-*). Đã Shutdown/deinitializeXR→Stop, clear sau khi lưu lỗi và mở fixture mới. Không sửa gameplay/shrine/reaction để che lỗi QA này.\n- ARRiftPlayTest24/1 và ComicTextAudit0 issue trướchotreload vẫn giữ, không chạy lại. Tiếp chỉ focused Convergence/input với fixture sạch rồi HubFlow/build.\n')
