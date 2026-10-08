var result=new {
    capturedAt=System.DateTime.UtcNow.ToString("o"),
    target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(),
    playing=UnityEditor.EditorApplication.isPlaying,
    compiling=UnityEditor.EditorApplication.isCompiling,
    updating=UnityEditor.EditorApplication.isUpdating,
    scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    build=System.IO.File.ReadAllText("Artifacts/Reactions/Build-status.txt")
};
System.IO.File.WriteAllText("Artifacts/Reactions/FinalEditorState.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
