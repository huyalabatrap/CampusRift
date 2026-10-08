if(UnityEditor.EditorApplication.isPlaying) return "Stop Play first";
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
var boot=new UnityEngine.GameObject("P10 standalone benchmark boot").AddComponent<CampusRift.Skills.SkillSet1StandaloneBench>();
boot.enemyTemplate=UnityEditor.AssetDatabase.LoadAssetAtPath<CampusRift.Enemies.EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");
System.IO.Directory.CreateDirectory("Assets/Skills/Core/Validation/Benchmark");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Assets/Skills/Core/Validation/Benchmark/SampleScene.unity");
var options=new UnityEditor.BuildPlayerOptions();options.scenes=new string[]{"Assets/Skills/Core/Validation/Benchmark/SampleScene.unity","Assets/Scenes/MainMenu.unity"};options.locationPathName="Builds/P10Benchmark/CampusRift.exe";options.target=UnityEditor.BuildTarget.StandaloneWindows64;options.options=UnityEditor.BuildOptions.None;options.extraScriptingDefines=new string[]{"P10_BENCH"};
System.IO.File.WriteAllText("Artifacts/Skills/P10-Build-status.txt","Queued");
UnityEditor.EditorApplication.delayCall += () => {
    try {
        System.IO.File.WriteAllText("Artifacts/Skills/P10-Build-status.txt","Building");
        var report=UnityEditor.BuildPipeline.BuildPlayer(options);
        System.IO.File.WriteAllText("Artifacts/Skills/P10-Build-status.txt",report.summary.result.ToString()+" "+report.summary.totalSize+" bytes");
    } catch(System.Exception ex) { System.IO.File.WriteAllText("Artifacts/Skills/P10-Build-status.txt",ex.ToString()); }
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
};
return "Build queued; poll P10-Build-status.txt";
