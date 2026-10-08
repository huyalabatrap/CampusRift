using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CampusRift.UI
{
    public sealed class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }
        public SceneFlowConfig Config;
        public bool IsLoading { get; private set; }
        // Continue resumes with the saved profile (world position is not saved).
        public bool CanContinue => Progression.ProfileService.Instance!=null && Progression.ProfileService.Instance.HasProgress;
        void Awake() { if(Instance!=null && Instance!=this){enabled=false;return;} Instance=this; }
        void OnDestroy() { if(Instance==this) Instance=null; }
        // After a level the player lands in the Hub, not on the title menu.
        public bool OpenHubOnLoad { get; private set; }
        public void LoadMainMenu() { OpenHubOnLoad=true; Load(Config.MainMenuScene); }
        // A new game always begins at level 1; Continue resumes at the first level not yet cleared.
        public void StartNewGame() { StartLevel(1); }
        public void ContinueGame() { if(CanContinue) StartLevel(Levels.LevelSession.NextToPlay); }
        // Plain gameplay scene without a level: the LevelDirector stays out (QA harnesses, sandbox).
        public void StartSandbox() { if(IsLoading) return; Levels.LevelSession.Clear(); Load(Config.GameplayScene); }
        public string LastLockReason { get; private set; }
        public void StartLevel(int index)
        {
            if(IsLoading) return;
            // A locked level cannot be entered, however this is called (P06-T06).
            if(!Progression.LevelProgressService.CanPlay(index,out var reason)){LastLockReason=reason;Debug.Log("Level "+index+" is locked: "+reason);return;}
            LastLockReason=null;
            if(!Levels.LevelSession.Select(index)) return;
            Levels.LevelSession.Loadout=null;
            Load(Config.GameplayScene);
        }
        // Start a level with the skills chosen on the preparation screen.
        public void StartLevel(int index,string[] skills)
        {
            if(IsLoading) return;
            if(!Progression.LevelProgressService.CanPlay(index,out var reason)){LastLockReason=reason;return;}
            LastLockReason=null;
            if(!Levels.LevelSession.Select(index)) return;
            Levels.LevelSession.Loadout=skills;
            Load(Config.GameplayScene);
        }
        public void StartEndgame(Levels.LevelDefinition definition,string[] skills=null)
        {
            if(IsLoading||definition==null)return;
            var p=Progression.ProfileService.Ensure().Data;
            bool open=definition.runMode==Levels.EndgameMode.Tower?Progression.EndgameService.TowerOpen(p):definition.runMode==Levels.EndgameMode.Nightmare&&Progression.EndgameService.NightmareOpen(p,definition.index);
            if(!open){LastLockReason="Locked endgame mode";Levels.EndgameFactory.Dispose(definition);return;}
            LastLockReason=null;Levels.LevelSession.Select(definition);Levels.LevelSession.Loadout=skills;Load(Config.GameplayScene);
        }
        // Retry keeps the selected level and the skills the player had equipped.
        public void RetryLevel()
        {
            if(IsLoading) return;
            if(Levels.LevelSession.Current!=null) Levels.LevelSession.Select(Levels.LevelSession.Current);
            Load(Config.GameplayScene);
        }
        public void NextLevel()
        {
            if(IsLoading || Levels.LevelSession.Current==null) return;
            var current=Levels.LevelSession.Current;
            if(current.runMode==Levels.EndgameMode.Tower){StartEndgame(Levels.EndgameFactory.Tower(current.towerFloor+1,System.DateTime.UtcNow),Levels.LevelSession.Loadout);return;}
            if(current.runMode==Levels.EndgameMode.Nightmare){int n=current.index+1;if(n<=10&&Progression.EndgameService.NightmareOpen(Progression.ProfileService.Instance.Data,n))StartEndgame(Levels.EndgameFactory.Nightmare(n),Levels.LevelSession.Loadout);else LoadMainMenu();return;}
            int next=Levels.LevelSession.Current.index+1;
            string reason=null;
            if(next>Levels.LevelSession.LastIndex || !Progression.LevelProgressService.CanPlay(next,out reason)){LastLockReason=reason;LoadMainMenu();return;}
            var skills=Levels.LevelSession.Loadout;
            if(!Levels.LevelSession.Select(next)) return;
            Levels.LevelSession.Loadout=skills;
            Load(Config.GameplayScene);
        }
        public void ReloadCurrentScene() { Load(SceneManager.GetActiveScene().path); }
        public void QuitGame()
        {
            #if UNITY_EDITOR
            Debug.Log("Quit requested");
            #else
            Application.Quit();
            #endif
        }
        void Load(string scene)
        {
            if(IsLoading) return;
            if(!Application.CanStreamedLevelBeLoaded(scene)) { Debug.LogError("Scene missing from build settings: "+scene); return; }
            StartCoroutine(LoadRoutine(scene));
        }
        IEnumerator LoadRoutine(string scene)
        {
            IsLoading=true; UIStateManager.Instance.BeginLoading();
            var overlay=Instantiate(Config.LoadingPrefab); DontDestroyOnLoad(overlay.gameObject); overlay.SetProgress(0);
            yield return null; // Draw overlay before beginning expensive scene work.
            var operation=SceneManager.LoadSceneAsync(scene);
            operation.allowSceneActivation=false;
            float elapsed=0;
            while(operation.progress<.9f || elapsed<.35f)
            { elapsed+=Time.unscaledDeltaTime;overlay.SetProgress(operation.progress/.9f);yield return null; }
            overlay.SetProgress(1); yield return null;
            operation.allowSceneActivation=true;
            while(!operation.isDone) yield return null;
            IsLoading=false;
            UIStateManager.Instance.EnterScene(SceneManager.GetActiveScene().path==Config.GameplayScene);
            if(OpenHubOnLoad && SceneManager.GetActiveScene().path==Config.MainMenuScene) UIStateManager.Instance.OpenHub();
            OpenHubOnLoad=false;
            Destroy(overlay.gameObject);
        }
    }
}
