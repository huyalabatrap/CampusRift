UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Scenes/MainMenu.unity");
var ui=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.UIManager>();
ui.MainMenu.gameObject.SetActive(true);
var group=ui.MainMenu.GetComponent<UnityEngine.CanvasGroup>();group.alpha=1;group.interactable=true;group.blocksRaycasts=true;
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.AssetDatabase.SaveAssets();UIValidation.SetResolution(1920,1080);
return new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,playing=UnityEditor.EditorApplication.isPlaying,timeScale=UnityEngine.Time.timeScale,canvasCount=UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>().Length,buildScenes=UnityEditor.EditorBuildSettings.scenes.Length};
