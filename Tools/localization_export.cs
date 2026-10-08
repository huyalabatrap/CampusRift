var result=new System.Collections.Generic.HashSet<string>();
foreach(var path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}){
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
 foreach(var t in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(UnityEngine.FindObjectsInactive.Include))if(!string.IsNullOrWhiteSpace(t.text))result.Add(t.text);
}
System.IO.File.WriteAllLines("Artifacts/Localization/SceneText.txt",result);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");return result.Count;
