var state=CampusRift.UI.UIStateManager.Instance;
state.EnterScene(true);state.Back();
if(state.State!=CampusRift.UI.UIState.Paused || UnityEngine.Time.timeScale!=0 || state.GameplayInputEnabled || UnityEngine.Cursor.visible!=true)throw new System.Exception("Pause failed");
state.OpenSettings();state.Back();if(state.State!=CampusRift.UI.UIState.Paused)throw new System.Exception("Settings back origin failed");
state.Back();if(state.State!=CampusRift.UI.UIState.Gameplay || UnityEngine.Time.timeScale!=1)throw new System.Exception("Resume failed");
bool closed=false;state.OpenModal(delegate{closed=true;});state.Back();if(!closed || state.State!=CampusRift.UI.UIState.Gameplay)throw new System.Exception("Modal ESC failed");
state.Pause();return "Phase 5 PASS: pause, input gate, Settings return, resume and modal precedence.";
