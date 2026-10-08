using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CampusRift.UI
{
    // The V2 pause menu (P09-T01): Continue, Settings, Leave level. Studying, restarting and quitting live elsewhere now (the Hub, the
    // result screen, the title menu). Leaving needs a second click, and items already used stay used. Applied at runtime to the pause
    // card the scene already has, so the scene file does not change.
    public sealed class PauseMenuAdapter : MonoBehaviour
    {
        RiftButton leave;
        bool armed; float armedUntil;
        string idleText;
        public bool Armed => armed;
        public RiftButton LeaveButton => leave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Attach(); }
        static void OnLoaded(Scene scene, LoadSceneMode mode) { Attach(); }
        static void Attach()
        {
            var manager = FindAnyObjectByType<UIManager>();
            if (manager == null || !manager.IsGameplay || manager.PauseMenu == null || manager.PauseMenu.GetComponent<PauseMenuAdapter>() != null) return;
            manager.PauseMenu.gameObject.AddComponent<PauseMenuAdapter>();
        }

        void Start()
        {
            var buttons = GetComponentsInChildren<RiftButton>(true);
            foreach (var b in buttons) if (b.name == "COURSES" || b.name == "RESTART" || b.name == "QUIT GAME") b.gameObject.SetActive(false);
            leave = buttons.FirstOrDefault(b => b.name == "MAIN MENU");
            if (leave == null) return;
            // The scene wired this button to "main menu" with a persistent listener; a fresh event drops it.
            leave.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            leave.onClick.AddListener(OnLeave);
            SetLabel(false);
        }

        void SetLabel(bool confirm)
        {
            bool vn = LevelHUD.Vietnamese;
            leave.Label.text = confirm ? (vn ? "XÁC NHẬN RỜI MÀN" : "CONFIRM LEAVING") : (vn ? "RỜI MÀN" : "LEAVE LEVEL");
            var hint = transform.Find("Pause Card/Hint");
            var text = hint != null ? hint.GetComponent<TMPro.TMP_Text>() : null;
            if (text != null) { if (idleText == null) idleText = text.text; text.text = confirm ? (vn ? "Vật phẩm đã dùng sẽ mất. Bấm lần nữa để rời màn." : "Items already used are lost. Click again to leave the level.") : idleText; }
        }

        void OnLeave()
        {
            if (!armed) { armed = true; armedUntil = Time.unscaledTime + 5f; SetLabel(true); return; }
            armed = false;
            GameSceneManager.Instance.LoadMainMenu();
        }

        void Update()
        {
            if (armed && Time.unscaledTime > armedUntil) { armed = false; if (leave != null) SetLabel(false); }
        }
        void OnDisable() { if (armed) { armed = false; if (leave != null) SetLabel(false); } }
    }
}
