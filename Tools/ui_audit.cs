var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var players = UnityEngine.Object.FindObjectsByType<CampusRift.CampusExplorer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
var health = UnityEngine.Object.FindObjectsByType<CampusRift.Monsters.PlayerMonsterHealth>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
var canvases = UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
var events = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
var names = new System.Collections.Generic.List<string>();
foreach(var p in players) names.Add(p.name + " | camera=" + (p.followCamera != null ? p.followCamera.name : "null") + " | sensitivity=" + p.mouseSensitivity);
return new {scene=s.path,dirty=s.isDirty,playing=UnityEditor.EditorApplication.isPlaying,players=names,healthCount=health.Length,canvasCount=canvases.Length,eventSystemCount=events.Length, rootCount=s.rootCount};
