EditorSettings.enterPlayModeOptionsEnabled=false;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");
foreach(var b in UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
{
    if(b is CampusRift.AR.ARSessionBootstrap||b is CampusRift.AR.ARXRLoaderControl||b is CampusRift.AR.ARBattlefield||b is CampusRift.AR.RiftPlacementService||b is CampusRift.AR.FrameSampler||b is CampusRift.AR.GestureRecognizerBridge||b is CampusRift.AR.ARSkillCaster||b is CampusRift.AR.ARMonsterDirector||b is CampusRift.AR.ARAdaptiveQuality||b is CampusRift.AR.ARBattleEvents||b is UnityEngine.XR.ARFoundation.ARSession||b is UnityEngine.XR.ARFoundation.ARCameraManager||b is UnityEngine.XR.ARFoundation.ARCameraBackground||b is UnityEngine.XR.ARFoundation.ARPlaneManager||b is UnityEngine.XR.ARFoundation.ARRaycastManager||b is UnityEngine.XR.ARFoundation.AROcclusionManager||b.GetType().Name.Contains("TrackedPose"))b.enabled=false;
}
UIValidation.SetResolution(2400,1080);
return "Temporary photo scene prepared; no gameplay/recognition/XR session";
