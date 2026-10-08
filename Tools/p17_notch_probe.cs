var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.ControlMode=CampusRift.Controls.ControlMode.Mobile;s.MobileControlScale=1.2f;CampusRift.UI.SettingsManager.Instance.Apply(s,false);
UIValidation.SetResolution(1920,1080);return "mobile max scale ready";
