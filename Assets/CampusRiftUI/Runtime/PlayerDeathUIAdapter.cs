using CampusRift.Monsters;
using UnityEngine;
namespace CampusRift.UI
{
    [RequireComponent(typeof(PlayerMonsterHealth))]
    public sealed class PlayerDeathUIAdapter : MonoBehaviour
    {
        PlayerMonsterHealth source;
        void Awake(){source=GetComponent<PlayerMonsterHealth>();}
        void OnEnable(){source.Defeated.AddListener(OnDefeated);}
        void OnDisable(){source.Defeated.RemoveListener(OnDefeated);}
        void OnDefeated(){UIStateManager.Instance?.Defeat();}
    }
}
