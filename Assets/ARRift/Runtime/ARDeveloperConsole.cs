using UnityEngine;

namespace CampusRift.AR
{
    // Suppress Unity's automatic on-screen error console; errors still reach logcat.
    [DefaultExecutionOrder(32000)]
    public sealed class ARDeveloperConsole : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            Debug.developerConsoleEnabled = false;
            Debug.developerConsoleVisible = false;
        }
        void Awake() { Debug.developerConsoleVisible = false; }
        void LateUpdate() { if (Debug.developerConsoleVisible) Debug.developerConsoleVisible = false; }
    }
}
