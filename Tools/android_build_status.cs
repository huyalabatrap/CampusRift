return new {
    target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(),
    playing=UnityEditor.EditorApplication.isPlaying,
    compiling=UnityEditor.EditorApplication.isCompiling,
    supported=UnityEditor.BuildPipeline.IsBuildTargetSupported(UnityEditor.BuildTargetGroup.Android,UnityEditor.BuildTarget.Android),
    scenes=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(UnityEditor.EditorBuildSettings.scenes,s=>new {s.path,s.enabled})),
    version=UnityEditor.PlayerSettings.bundleVersion,
    versionCode=UnityEditor.PlayerSettings.Android.bundleVersionCode,
    dirtyScenes=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount),i=>new {path=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path,dirty=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty}))
};
