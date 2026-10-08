using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusRift
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CampusExplorer))]
    public sealed class ElevatorInteraction : MonoBehaviour
    {
        public CampusElevator[] elevators;
        public float interactionDistance = 2.3f;
        CampusExplorer player;
        CampusElevator nearby;
        CampusElevator panelElevator;
        int nearbyFloor;
        int selectedFloor;
        bool inside;
        float nextScan;
        float nextAutomaticCall;
        [System.NonSerialized] GUIStyle heading;
        [System.NonSerialized] GUIStyle label;
        [System.NonSerialized] GUIStyle button;

        public bool PanelOpen => panelElevator != null;
        public CampusElevator PanelElevator => panelElevator;
        public bool CanInteract => nearby != null;
        public string InteractionLabel => inside ? "SELECT FLOOR" : "CALL LIFT";
        public void Interact()
        {
            FindNearby();if(nearby==null)return;
            if(inside)OpenPanel(nearby);else nearby.RequestFloor(nearbyFloor);
        }

        void Awake()
        {
            player = GetComponent<CampusExplorer>();
            if (elevators == null || elevators.Length == 0)
                elevators = FindObjectsByType<CampusElevator>();
        }

        void Update()
        {
            var ui = UI.UIStateManager.Instance;
            if (ui != null && (ui.EscapeConsumedFrame == Time.frameCount ||
                (!ui.GameplayInputEnabled && !(ui.State == UI.UIState.Modal && PanelOpen)))) return;
            if (Time.unscaledTime >= nextScan)
            {
                FindNearby();
                nextScan = Time.unscaledTime + 0.1f;
                if (!inside && nearby != null && Time.unscaledTime >= nextAutomaticCall)
                {
                    // Calls remain subject to the lift's cabin/door interlocks.
                    foreach (var entry in nearby.floors[nearbyFloor].entrances)
                    {
                        Vector3 delta = transform.position - entry;
                        delta.y = 0;
                        if (delta.sqrMagnitude > 1.8f * 1.8f) continue;
                        nearby.RequestFloor(nearbyFloor);
                        nextAutomaticCall = Time.unscaledTime + 0.75f;
                        break;
                    }
                }
            }
            var keyboard = Keyboard.current;
            if(Controls.CampusInput.Mobile)return; // touch routes through the same Interact/SelectFloor methods
            if (keyboard == null) return;
            if (PanelOpen)
            {
                if (!panelElevator.IsInside(transform.position)) { ClosePanel(); return; }
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame) { ClosePanel(); return; }
                if (keyboard.upArrowKey.wasPressedThisFrame) selectedFloor = Mathf.Min(selectedFloor + 1, panelElevator.FloorCount - 1);
                if (keyboard.downArrowKey.wasPressedThisFrame) selectedFloor = Mathf.Max(selectedFloor - 1, 0);
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                    SelectFloor(selectedFloor);
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked || nearby == null || !GetComponent<Controls.CampusInput>().Pressed(Controls.CampusAction.Interact)) return;
            Interact();
        }

        void FindNearby()
        {
            nearby = null;
            inside = false;
            float best = interactionDistance * interactionDistance;
            foreach (var elevator in elevators)
            {
                if (elevator == null || !elevator.isActiveAndEnabled) continue;
                if (elevator.IsInside(transform.position)) { nearby = elevator; inside = true; return; }
                for (int f = 0; f < elevator.FloorCount; f++)
                {
                    if (Mathf.Abs(transform.position.y - elevator.floors[f].height) > 1.2f) continue;
                    foreach (var entry in elevator.floors[f].entrances)
                    {
                        Vector3 delta = transform.position - entry;
                        if (Vector3.Dot(delta, elevator.outward) < -0.3f) continue;
                        delta.y = 0f;
                        if (delta.sqrMagnitude < best) { best = delta.sqrMagnitude; nearby = elevator; nearbyFloor = f; }
                    }
                }
            }
        }

        public void OpenPanel(CampusElevator elevator)
        {
            if (UI.UIStateManager.Instance != null && !UI.UIStateManager.Instance.GameplayInputEnabled) return;
            if (elevator == null || !elevator.IsInside(transform.position)) return;
            panelElevator = elevator;
            selectedFloor = elevator.IsMoving ? elevator.TargetFloor : elevator.CurrentFloor;
            player.SetInterfaceOpen(true);
        }

        public void SelectFloor(int floor)
        {
            if (panelElevator == null) return;
            if (panelElevator.RequestFloor(floor)) ClosePanel();
        }

        public void ClosePanel()
        {
            panelElevator = null;
            if (player != null) player.SetInterfaceOpen(false);
        }

        void OnDisable()
        {
            if (PanelOpen) ClosePanel();
        }

        void OnGUI()
        {
            if(Controls.CampusInput.Mobile)return;
            var ui = UI.UIStateManager.Instance;
            if (ui != null && !ui.GameplayInputEnabled && !(ui.State == UI.UIState.Modal && PanelOpen)) return;
            if (nearby == null && !PanelOpen) return;
            if (heading == null || heading.fontSize != 21)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                heading.normal.textColor = UI.ComicTheme.Gold;
                if (UI.ComicTheme.Font != null) heading.font = UI.ComicTheme.Font.sourceFontFile;
                label = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                label.normal.textColor = Color.white;
                if (UI.ComicTheme.Font != null) label.font = UI.ComicTheme.Font.sourceFontFile;
                button = UI.ComicTheme.LegacyButton();
            }
            if (PanelOpen)
            {
                float width = Mathf.Min(420, Screen.width - 28);
                float height = 222 + Mathf.Ceil(panelElevator.FloorCount / 3f) * 74;
                var rect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
                DrawPanel(rect);
                GUI.Label(new Rect(rect.x + 12, rect.y + 12, width - 24, 32), Localization.LocalizationService.T("ELEVATOR")+" " + panelElevator.building, heading);
                GUI.Label(new Rect(rect.x + 12, rect.y + 48, width - 24, 44), Status(panelElevator), label);
                float cell = (width - 40) / 3;
                for (int i = 0; i < panelElevator.FloorCount; i++)
                {
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = i == selectedFloor ? UI.ComicTheme.Gold
                        : panelElevator.IsQueued(i) ? UI.ComicTheme.Green : Color.white;
                    if (GUI.Button(new Rect(rect.x + 16 + (i % 3) * (cell + 4), rect.y + 98 + (i / 3) * 74, cell, 68),
                        (i + 1).ToString("00"), button)) { SelectFloor(i); GUI.backgroundColor = old; return; }
                    GUI.backgroundColor = old;
                }
                GUI.Label(new Rect(rect.x + 12, rect.yMax - 122, width - 24, 32), Localization.LocalizationService.T("Select floor with arrows; Enter to confirm"), label);
                if (GUI.Button(new Rect(rect.x + 16, rect.yMax - 82, width - 32, 68), Localization.LocalizationService.T("Close panel [Esc]"), UI.ComicTheme.LegacyButton("button-red"))) ClosePanel();
                return;
            }
            float promptWidth = Mathf.Min(550, Screen.width - 28);
            var prompt = new Rect((Screen.width - promptWidth) / 2, 20, promptWidth, 78);
            DrawPanel(prompt);
            string text = inside ? "[E]  "+Localization.LocalizationService.T("SELECT FLOOR")+" — "+Localization.LocalizationService.T("LIFT")+" " + nearby.building
                : "[E]  "+Localization.LocalizationService.T("CALL LIFT")+" " + nearby.building + " — "+Localization.LocalizationService.T("Floor")+" " + (nearbyFloor + 1);
            GUI.Label(new Rect(prompt.x + 8, prompt.y + 4, prompt.width - 16, 34), text, heading);
            GUI.Label(new Rect(prompt.x + 8, prompt.y + 40, prompt.width - 16, 30), Status(nearby), label);
        }

        static string Status(CampusElevator elevator)
        {
            if (elevator.IsMoving) return Localization.LocalizationService.T("Moving to floor")+" " + (elevator.TargetFloor + 1) + " · "+Localization.LocalizationService.T("Please wait");
            if (elevator.State == CampusElevator.LiftState.Open || elevator.State == CampusElevator.LiftState.Opening)
                return Localization.LocalizationService.T("Floor")+" " + (elevator.CurrentFloor + 1) + " · "+Localization.LocalizationService.T("Doors opening");
            if (elevator.State == CampusElevator.LiftState.Closing) return Localization.LocalizationService.T("Doors closing / Stand inside the cabin");
            return Localization.LocalizationService.T("Cabin at floor")+" " + (elevator.CurrentFloor + 1);
        }

        static void DrawPanel(Rect rect)
        {
            UI.ComicTheme.LegacyPanel(rect);
        }
    }
}
