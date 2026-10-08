using UnityEngine;
using CampusRift.Controls;
namespace CampusRift.Look
{
    public static class CampusSurfaceQuality
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start(){var go=new GameObject("Campus surface quality");Object.DontDestroyOnLoad(go);go.AddComponent<CampusSurfaceQualityDriver>();}
    }
    public sealed class CampusSurfaceQualityDriver : MonoBehaviour
    {
        void Update(){Shader.SetGlobalFloat("_CampusLowQuality",Application.isMobilePlatform||CampusInput.Mobile||QualitySettings.GetQualityLevel()==0?1:0);}
    }
}
