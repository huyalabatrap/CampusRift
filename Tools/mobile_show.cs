UnityEngine.Application.runInBackground=true;CampusRift.UI.UIStateManager.Instance.EnterScene(true);
var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();p.ReturnToSpawn();
var monster=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterBrain>();monster.gameObject.SetActive(false);
var settings=CampusRift.UI.SettingsManager.Instance.Current.Copy();settings.ControlMode=CampusRift.Controls.ControlMode.Mobile;CampusRift.UI.SettingsManager.Instance.Apply(settings,false);
return new{mode=CampusRift.Controls.CampusInput.Mobile,player=p.name};
