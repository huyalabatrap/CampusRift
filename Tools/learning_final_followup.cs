CampusRift.UI.GameSceneManager.Instance.StartNewGame();
UnityEditor.EditorApplication.CallbackFunction ready=null;
ready=()=>{
 if(CampusRift.UI.GameSceneManager.Instance.IsLoading)return;
 CampusRift.UI.UIStateManager.Instance.EnterScene(true);
 var h=UnityEngine.Object.FindAnyObjectByType<CampusRift.Controls.MobileControlsHUD>();if(h==null || h.SafeRoot==null)return;
 UnityEditor.EditorApplication.update-=ready;
 new UnityEngine.GameObject("Learning final QA").AddComponent<CampusRift.Learning.LearningFollowupPlayTest>();
};
UnityEditor.EditorApplication.update+=ready;return "Learning followup queued after clean scene load";
