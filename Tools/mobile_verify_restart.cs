UnityEngine.Application.runInBackground=true;UIValidation.SetResolution(1920,1080);
var settings=CampusRift.UI.SettingsManager.Instance.Current;
bool pass=settings.ControlMode==CampusRift.Controls.ControlMode.Mobile&&UnityEngine.Mathf.Abs(settings.TouchSensitivity-1.17f)<.001f;
System.IO.File.WriteAllText("Artifacts/MobileControls/Restart.txt",pass?"PASS: saved Mobile mode and 1.17 sensitivity survive a fresh Play Mode session.":"FAIL");
return new{pass,mode=settings.ControlMode.ToString(),settings.TouchSensitivity};
