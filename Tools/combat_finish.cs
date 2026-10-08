var path = UnityEditor.SessionState.GetString("CombatOriginalPlayScene", "");
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path) ? null : UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path);
var brain=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterBrain>();
var player=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();
var feedback=player.GetComponent<CampusRift.Monsters.PlayerBloodFeedback>();
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab");
var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/MonsterShaban/Combat/Shaban_ClawAttack.anim");
var material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/MonsterShaban/Combat/BloodDroplet.mat");
return new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, sceneBlood=feedback!=null && feedback.bloodPrefab!=null,prefabBlood=prefab.GetComponent<CampusRift.Monsters.PlayerBloodFeedback>().bloodPrefab!=null,clipLength=clip.length,shaderSupported=material.shader.isSupported,shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(material.shader),playStart=path,testObjects=UnityEngine.Object.FindObjectsByType<ShabanCombatPlayTest>().Length};
