using UnityEngine;
namespace CampusRift.UI
{
    public sealed class GameOverUI : MonoBehaviour
    {
        public void ShowDefeated(){UIStateManager.Instance?.Defeat();}
        public void Retry(){GameSceneManager.Instance.RetryLevel();}
        public void MainMenu(){GameSceneManager.Instance.LoadMainMenu();}
    }
}
