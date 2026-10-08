var ui=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.UIManager>();
var state=CampusRift.UI.UIStateManager.Instance;
if(ui==null || state==null)throw new System.Exception("UI/services missing");
ui.Courses();if(state.State!=CampusRift.UI.UIState.Course)throw new System.Exception("Courses failed");
ui.Back();ui.OpenCredits();if(state.State!=CampusRift.UI.UIState.Credits)throw new System.Exception("Credits failed");
ui.Back();ui.OpenSettings();if(state.State!=CampusRift.UI.UIState.Settings)throw new System.Exception("Settings failed");
ui.Back();ui.Quit();
if(state.State!=CampusRift.UI.UIState.Menu)throw new System.Exception("Back failed");
return "Phase 2: Main Menu/Courses/Credits/Settings navigation and Quit Editor log PASS. Continue disabled.";
