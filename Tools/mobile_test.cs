UnityEngine.Application.runInBackground=true;UIValidation.SetResolution(1920,1080);UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
var old=UnityEngine.Object.FindAnyObjectByType<CampusRift.Controls.MobileControlPlayTest>();if(old!=null)UnityEngine.Object.Destroy(old.gameObject);
new UnityEngine.GameObject("Mobile Control QA (runtime only)").AddComponent<CampusRift.Controls.MobileControlPlayTest>();
return "Mobile input and UI integration test started.";
