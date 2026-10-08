var saved=System.IO.File.ReadAllText("Artifacts/MobileControls/Settings-before.json");
if(string.IsNullOrEmpty(saved))UnityEngine.PlayerPrefs.DeleteKey("CampusRift.Settings.v1");else UnityEngine.PlayerPrefs.SetString("CampusRift.Settings.v1",saved);UnityEngine.PlayerPrefs.Save();
var manager=CampusRift.UI.SettingsManager.Instance;if(manager!=null)manager.Apply(manager.ReadSaved(),false);
return "Original user settings restored.";
