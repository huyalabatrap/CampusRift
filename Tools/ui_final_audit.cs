var reports=new System.Collections.Generic.List<string>();
foreach(var path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"})
{
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
int missing=0;foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))missing+=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
var ui=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.UIManager>();
int legacy=ui.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length;
var canvas=ui.GetComponent<UnityEngine.Canvas>();var scaler=ui.GetComponent<UnityEngine.UI.CanvasScaler>();
int events=UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length;
if(missing!=0||legacy!=0||events!=1||canvas.renderMode!=UnityEngine.RenderMode.ScreenSpaceOverlay||scaler.referenceResolution!=new UnityEngine.Vector2(1920,1080)||scaler.matchWidthOrHeight!=.5f)throw new System.Exception("Scene audit failed: "+path);
reports.Add(path+": 0 missing scripts; 0 legacy UI Text; 1 EventSystem; Overlay 1920x1080 0.5 scaler.");
}
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Scenes/MainMenu.unity");
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllLines("Artifacts/UI/SCENE_AUDIT.txt",reports.ToArray());return reports;
