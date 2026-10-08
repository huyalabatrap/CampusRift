using UnityEngine;
namespace CampusRift.UI
{
    [CreateAssetMenu(menuName="Campus Rift/Scene Flow")]
    public sealed class SceneFlowConfig : ScriptableObject
    {
        public string MainMenuScene;
        public string GameplayScene;
        public LoadingScreenUI LoadingPrefab;
    }
}
