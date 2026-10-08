from ar_session import *
import shutil
out=Path('task/ar/build-fix1');out.mkdir(exist_ok=True)
assert not (out/'DONE.txt').exists(),'Do not repeat a completed build'
state=code('return new {playing=UnityEditor.EditorApplication.isPlaying,compiling=UnityEditor.EditorApplication.isCompiling,updating=UnityEditor.EditorApplication.isUpdating,building=UnityEditor.BuildPipeline.isBuildingPlayer,target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,loaderInactive=UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader==null};')
save(out/'prebuild-state.json',state)
assert not any(state[k] for k in ['playing','compiling','updating','building','dirty']),state
assert state['target']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and state['loaderInactive'],state
errors=console(out/'prebuild-console.json');assert errors['data']==[],errors
if Path('APK-Test/build-summary.txt').exists():shutil.copy2('APK-Test/build-summary.txt',out/'prior-apk-folder-summary.txt')
print(code(Path('task/ar/fix1-build.cs').read_text(encoding='utf-8')),flush=True)
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n## AR fix1 — build đang chạy\n- Ảnh pha đặt cuối xác nhận nút comic không bị combo che, silhouette tím mờ; rune shader0lỗi. Collector dùng Shutdown trước Stop, cleanup Console0.\n- Đã queue đúng1build development tại APK-Test/CampusRift-AR-dev-20261004-fix1.apk. Theo dõi task/ar/build-fix1/DONE.txt và build-summary.txt; không queue lại khi build chạy. Chưa REPORT; còn verify APK, phục hồi save/settings/URP prefilter và final state.\n')
