using UnityEngine;
using CampusRift.Controls;
namespace CampusRift.Enemies
{
    public sealed class EnemyQuality : MonoBehaviour
    {
        float prior; bool changed;
        int quality;
        void Start(){prior=QualitySettings.lodBias;quality=QualitySettings.GetQualityLevel();if(UI.SettingsManager.Instance!=null)UI.SettingsManager.Instance.Changed+=Apply;Apply(null);}
        void Apply(UI.GameSettings settings){int q=QualitySettings.GetQualityLevel();if(q!=quality){quality=q;prior=QualitySettings.lodBias;}QualitySettings.lodBias=CampusInput.Mobile?Mathf.Min(prior,.55f):prior;changed=CampusInput.Mobile;}
        void OnDestroy(){if(UI.SettingsManager.Instance!=null)UI.SettingsManager.Instance.Changed-=Apply;if(changed)QualitySettings.lodBias=prior;}
    }
}
