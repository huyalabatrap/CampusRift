var ui=CampusRift.UI.LoadoutUI.Instance;
CampusRift.UI.LoadoutUI.Level=CampusRift.Levels.LevelCatalog.Instance.Get(10);
CampusRift.UI.UIStateManager.Instance.OpenLoadout();
ui.UseSuggested();
UnityEngine.Canvas.ForceUpdateCanvases();
foreach(var scroll in ui.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true))scroll.verticalNormalizedPosition=0;
UnityEngine.ScreenCapture.CaptureScreenshot("task/p18/screens/loadout-21-new.png");
int cards=0;foreach(var b in ui.GetComponentsInChildren<UnityEngine.UI.Button>(true))if(b.name.StartsWith("Skill ")&&b.name!="Skill List")cards++;
return "Loadout cards="+cards+"; selected="+string.Join(",",ui.Slots);
