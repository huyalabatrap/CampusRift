var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path != "Assets/Scenes/SampleScene.unity") return "Open SampleScene first";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
var start = UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
UnityEditor.SessionState.SetString("CombatOriginalPlayScene", start != null ? UnityEditor.AssetDatabase.GetAssetPath(start) : "");
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = null;
return "Saved gameplay scene and prepared direct play test";
