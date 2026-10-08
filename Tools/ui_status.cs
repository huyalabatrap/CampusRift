var ui=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.UIManager>();
var s=CampusRift.UI.SettingsManager.Instance;
return new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,state=CampusRift.UI.UIStateManager.Instance.State.ToString(),settings=s!=null,settingsUI=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.SettingsUI>()!=null,player=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>()!=null,manager=ui!=null,timescale=UnityEngine.Time.timeScale};
