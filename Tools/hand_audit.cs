var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();
var m=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterBrain>();
var hud=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.GameplayHUD>();
return new {playing=UnityEditor.EditorApplication.isPlaying,scene=scene.path,dirty=scene.isDirty,player=p==null?null:p.transform.position.ToString(),monster=m==null?null:m.transform.position.ToString(),slots=hud==null?0:hud.Skills.Slots.Length,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name,monsters=UnityEngine.Object.FindObjectsByType<CampusRift.Monsters.MonsterBrain>().Length};
