using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CampusRift.UI
{
    public enum UIState { Gameplay, Paused, Menu, Settings, Course, Credits, Modal, GameOver, Loading, Victory, Hub, Loadout }
    [DefaultExecutionOrder(-200)]
    public sealed class UIStateManager : MonoBehaviour
    {
        public static UIStateManager Instance { get; private set; }
        public UIState State { get; private set; } = UIState.Menu;
        public UIState ReturnState { get; private set; } = UIState.Menu;
        public bool GameplayInputEnabled => State == UIState.Gameplay;
        public int EscapeConsumedFrame { get; private set; } = -1;
        public event Action<UIState> Changed;
        Action closeModal;
        void Awake() { if(Instance!=null && Instance!=this){enabled=false;return;} Instance = this; }
        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null; Time.timeScale = 1; AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void EnterScene(bool gameplay)
        {
            closeModal = null;
            ReturnState = gameplay ? UIState.Gameplay : UIState.Menu;
            Set(gameplay ? UIState.Gameplay : UIState.Menu);
        }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && SkyBeast.P21StoryCinematic.Active==null && !(SkyBeast.HeavenSwordCinematic.Active!=null&&SkyBeast.HeavenSwordCinematic.Active.Playing))
            { EscapeConsumedFrame = Time.frameCount; Back(); }
        }
        public void Pause() { if (State == UIState.Gameplay) Set(UIState.Paused); }
        public void Resume() { if (State == UIState.Paused) Set(UIState.Gameplay); }
        public void OpenSettings()
        {
            if (State != UIState.Menu && State != UIState.Paused && State != UIState.Hub) return;
            ReturnState = State; Set(UIState.Settings);
        }
        // The Library (study) opens only from the Hub, never during a level (P09-T01).
        public void OpenCourse()
        {
            if(State!=UIState.Hub)return;
            ReturnState=State;Set(UIState.Course);
        }
        public void OpenShrine()
        {
            if(State!=UIState.Gameplay || Learning.LearningShrine.Pending==null)return;
            ReturnState=UIState.Gameplay;Set(UIState.Course);
        }
#if UNITY_EDITOR
        // QA harnesses run inside the gameplay scene and need the study panel there.
        public void EditorForceCourse(){ReturnState=State==UIState.Paused?UIState.Paused:UIState.Gameplay;Set(UIState.Course);}
#endif
        // The Hub is the centre of everything outside a level; the title menu leads into it.
        public void OpenHub(){if(State==UIState.Menu||State==UIState.Loadout||State==UIState.Course||State==UIState.Loading||State==UIState.Settings)Set(UIState.Hub);}
        public void OpenLoadout(){if(State==UIState.Hub)Set(UIState.Loadout);}
        public void OpenCredits() { if (State == UIState.Menu) Set(UIState.Credits); }
        public void OpenModal(Action onClose)
        {
            if (State != UIState.Gameplay) return;
            closeModal = onClose; Set(UIState.Modal);
        }
        public void CloseModal()
        {
            closeModal = null;
            if (State == UIState.Modal) Set(UIState.Gameplay);
        }
        public void Defeat()
        {
            if (State == UIState.Loading || State == UIState.GameOver || State == UIState.Victory) return;
            closeModal?.Invoke(); closeModal = null; Set(UIState.GameOver);
        }
        public void BeginLoading() { closeModal?.Invoke(); closeModal = null; Set(UIState.Loading); }
        public void Win()
        {
            if(State==UIState.Loading || State==UIState.GameOver || State==UIState.Victory)return;
            closeModal?.Invoke();closeModal=null;Set(UIState.Victory);
        }
        public void Back()
        {
            if(State==UIState.Settings)
                foreach(var dropdown in FindObjectsByType<TMPro.TMP_Dropdown>())
                    if(dropdown.IsExpanded){dropdown.Hide();return;}
            switch (State)
            {
                case UIState.Gameplay: Pause(); break;
                case UIState.Paused: Resume(); break;
                case UIState.Settings: Set(ReturnState); break;
                case UIState.Course: Set(ReturnState); break;
                case UIState.Hub: Set(UIState.Menu); break;
                case UIState.Loadout: Set(UIState.Hub); break;
                case UIState.Credits: Set(UIState.Menu); break;
                case UIState.Modal: closeModal?.Invoke(); CloseModal(); break;
                default: return;
            }
            UIAudioManager.Instance?.Back();
        }
        void Set(UIState state)
        {
            var before = State;
            State = state;
            // Outcome jingles play once on entering the state (they ignore the audio pause of the frozen game).
            if (before != state)
            {
                if (state == UIState.Victory) Audio.GameSfx.PlayUI(Audio.SfxGroup.Victory);
                else if (state == UIState.GameOver) Audio.GameSfx.PlayUI(Audio.SfxGroup.Defeat);
            }
            // The existing elevator modal blocks control while allowing its cabin to travel.
            bool freeze = state == UIState.Course || state == UIState.Paused || state == UIState.GameOver || state == UIState.Victory ||
                (state == UIState.Settings && ReturnState == UIState.Paused);
            Time.timeScale = freeze ? 0 : 1;
            AudioListener.pause = freeze;
            ApplyCursor(); Changed?.Invoke(state);
        }
        public void RefreshCursor() => ApplyCursor();
        void ApplyCursor()
        {
            bool capture=GameplayInputEnabled && !Controls.CampusInput.Mobile;
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && State == UIState.Gameplay) Pause();
            else if (focused) ApplyCursor();
        }
    }
}
