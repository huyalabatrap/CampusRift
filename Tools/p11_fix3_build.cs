if(UnityEditor.EditorApplication.isPlaying) return "Stop Play first";
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
var boot=new UnityEngine.GameObject("P11 standalone benchmark boot").AddComponent<CampusRift.Combat.ReactionStandaloneBench>();
boot.enemyTemplate=UnityEditor.AssetDatabase.LoadAssetAtPath<CampusRift.Enemies.EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");
System.IO.Directory.CreateDirectory("Assets/Combat/Validation/Benchmark");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Assets/Combat/Validation/Benchmark/SampleScene.unity");
var options=new UnityEditor.BuildPlayerOptions();options.scenes=new string[]{"Assets/Combat/Validation/Benchmark/SampleScene.unity","Assets/Scenes/MainMenu.unity"};options.locationPathName="Builds/P11Fix3Benchmark/CampusRift.exe";options.target=UnityEditor.BuildTarget.StandaloneWindows64;options.options=UnityEditor.BuildOptions.None;options.extraScriptingDefines=new string[]{"P10_BENCH","P11_BENCH"};
System.IO.Directory.CreateDirectory("Artifacts/Reactions/fix3");System.IO.File.WriteAllText("Artifacts/Reactions/fix3/Build-status.txt","Queued");
UnityEditor.EditorApplication.delayCall += () => {
    try {System.IO.File.WriteAllText("Artifacts/Reactions/fix3/Build-status.txt","Building");var report=UnityEditor.BuildPipeline.BuildPlayer(options);System.IO.File.WriteAllText("Artifacts/Reactions/fix3/Build-status.txt",report.summary.result.ToString()+" "+report.summary.totalSize+" bytes");}
    catch(System.Exception ex){System.IO.File.WriteAllText("Artifacts/Reactions/fix3/Build-status.txt",ex.ToString());}
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
};
return "Build queued; poll Build-status.txt";
