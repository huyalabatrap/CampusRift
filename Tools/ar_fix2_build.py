from ar_fix2_local import *
build=Path('task/ar/build-fix2');build.mkdir(exist_ok=True)
assert not (build/'DONE.txt').exists(), 'Do not repeat completed build'
state=code('return new {playing=UnityEditor.EditorApplication.isPlaying,compiling=UnityEditor.EditorApplication.isCompiling,updating=UnityEditor.EditorApplication.isUpdating,building=UnityEditor.BuildPipeline.isBuildingPlayer,target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,loaderInactive=UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader==null};')
save(build/'prebuild-state.json',state)
assert not any(state[k] for k in ['playing','compiling','updating','building','dirty']),state
assert state['target']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and state['loaderInactive'],state
errors=console(build/'prebuild-console.json');assert errors['data']==[],errors
print(code(Path('task/ar/fix2-build.cs').read_text(encoding='utf-8')),flush=True)
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — build đang chạy\n- Queue1build development APK-Test/CampusRift-AR-dev-20261004-fix2.apk. Theo dõi build-fix2/DONE.txt/build-summary.txt; không queue lại khi đang chạy. Còn verify APK/mergedmanifest, phục hồi save/settings/build-prefilter, Console/finalstate vàREPORT.\n')
