var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/SampleScene.unity" || CampusRift.UI.GameSceneManager.Instance.IsLoading) throw new System.Exception("PLAY scene flow failed");
if(UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterBrain>()==null)throw new System.Exception("Monster missing");
return new {phase="3 PASS",scene=scene.path,services=UnityEngine.Object.FindObjectsByType<CampusRift.UI.UIServices>(UnityEngine.FindObjectsSortMode.None).Length, timescale=UnityEngine.Time.timeScale};
