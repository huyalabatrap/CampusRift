var state=new {
    capturedAt=System.DateTime.UtcNow.ToString("o"),
    target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(),
    scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    playing=UnityEditor.EditorApplication.isPlaying,
    compiling=UnityEditor.EditorApplication.isCompiling,
    updating=UnityEditor.EditorApplication.isUpdating
};
System.IO.File.WriteAllText("Artifacts/Skills/fix3/FinalEditorState.json",Newtonsoft.Json.JsonConvert.SerializeObject(state,Newtonsoft.Json.Formatting.Indented));
return state;
