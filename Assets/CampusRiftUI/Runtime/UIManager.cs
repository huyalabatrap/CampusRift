using UnityEngine;
namespace CampusRift.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        public bool IsGameplay;
        public PanelTransition MainMenu, PauseMenu, Settings, Course, Credits, GameOver, Victory;
        Monsters.MonsterVitality monster;
        public GameObject HUD;
        void Start()
        {
            UIStateManager.Instance.Changed+=Refresh;
            if(IsGameplay)
            {
                monster=FindAnyObjectByType<Monsters.MonsterVitality>();
                if(monster!=null)monster.DefeatedOnce+=MonsterDefeated;
            }
            if(!GameSceneManager.Instance.IsLoading) UIStateManager.Instance.EnterScene(IsGameplay);
            Refresh(UIStateManager.Instance.State);
        }
        // In a V2 level the LevelDirector decides victory; the old rule (first monster down = win) only applies
        // to scenes opened without a level (QA harnesses).
        void MonsterDefeated(){if(Levels.LevelDirector.Instance!=null)return;UIStateManager.Instance?.Win();}
        void OnDestroy()
        {
            if(UIStateManager.Instance!=null) UIStateManager.Instance.Changed-=Refresh;
            if(monster!=null)monster.DefeatedOnce-=MonsterDefeated;
        }
        void Refresh(UIState state)
        {
            if(MainMenu!=null)
            {
                foreach(var button in MainMenu.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    if(button.name=="CONTINUE")
                    {
                        button.interactable=GameSceneManager.Instance.CanContinue;
                        var hint=button.transform.Find("NoSave");if(hint!=null)hint.gameObject.SetActive(!button.interactable);
                    }
            }
            if(state==UIState.Settings && Settings!=null) Settings.GetComponent<SettingsUI>()?.BeginEditing();
            Show(MainMenu,state==UIState.Menu); Show(PauseMenu,state==UIState.Paused);
            Show(Settings,state==UIState.Settings); Show(Course,state==UIState.Course);
            if(state==UIState.Course&&Course!=null&&Learning.LearningUI.PendingScreen!=null)Course.GetComponent<Learning.LearningUI>()?.OpenPendingScreen();
            Show(Credits,state==UIState.Credits);Show(GameOver,state==UIState.GameOver);
            Show(Victory,state==UIState.Victory);
            if(HUD!=null) HUD.SetActive(state==UIState.Gameplay || state==UIState.Modal);
        }
        static void Show(PanelTransition panel,bool visible) { if(panel!=null) panel.Show(visible); }
        public void Play() { UIStateManager.Instance.OpenHub(); }
        public void Continue() { GameSceneManager.Instance.ContinueGame(); }
        public void Courses() { UIStateManager.Instance.OpenHub(); }
        public void OpenSettings() { UIStateManager.Instance.OpenSettings(); }
        public void OpenCredits() { UIStateManager.Instance.OpenCredits(); }
        public void Back() { UIStateManager.Instance.Back(); }
        public void Resume() { UIStateManager.Instance.Resume(); }
        public void Restart() { if(Levels.LevelSession.Current!=null) GameSceneManager.Instance.RetryLevel(); else GameSceneManager.Instance.ReloadCurrentScene(); }
        public void Main() { GameSceneManager.Instance.LoadMainMenu(); }
        public void Quit() { GameSceneManager.Instance.QuitGame(); }
    }
}
