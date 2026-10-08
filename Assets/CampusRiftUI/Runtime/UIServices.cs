using UnityEngine;
namespace CampusRift.UI
{
    [DefaultExecutionOrder(-1000)]
    public sealed class UIServices : MonoBehaviour
    {
        public static UIServices Instance { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var prefab = Resources.Load<UIServices>("CampusRiftServices");
            if (prefab != null) Instantiate(prefab);
        }
        void Awake()
        {
            if (Instance != null && Instance != this) { gameObject.SetActive(false); Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
