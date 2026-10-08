if(UnityEditor.EditorApplication.isPlaying) return "Stop Play first";
if(UnityEditor.EditorUserBuildSettings.activeBuildTarget==UnityEditor.BuildTarget.Android)
    return "Android already active";
return UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTargetAsync(UnityEditor.BuildTargetGroup.Android,UnityEditor.BuildTarget.Android);
