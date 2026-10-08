CampusRift.UI.GameSceneManager.Instance.StartNewGame();
UnityEditor.EditorApplication.CallbackFunction ready=null;
ready=()=>{
 if(CampusRift.UI.GameSceneManager.Instance.IsLoading)return;
 CampusRift.UI.UIStateManager.Instance.EnterScene(true);
 var h=UnityEngine.Object.FindAnyObjectByType<CampusRift.Controls.MobileControlsHUD>();if(h==null || h.SafeRoot==null)return;
 UnityEditor.EditorApplication.update-=ready;
 new UnityEngine.GameObject("Pressure QA isolated").AddComponent<ShabanPressurePlayTest>();
};
UnityEditor.EditorApplication.update+=ready;return "Pressure retest queued after clean scene load";
