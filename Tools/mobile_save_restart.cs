UnityEngine.Application.runInBackground=true;
var manager=CampusRift.UI.SettingsManager.Instance;var settings=manager.Current.Copy();settings.ControlMode=CampusRift.Controls.ControlMode.Mobile;settings.TouchSensitivity=1.17f;manager.Apply(settings);
return new{savedMode=manager.ReadSaved().ControlMode.ToString(),touch=manager.ReadSaved().TouchSensitivity};
