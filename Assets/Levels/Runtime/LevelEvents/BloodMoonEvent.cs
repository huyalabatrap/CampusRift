using UnityEngine;
namespace CampusRift.Levels
{
    public sealed class BloodMoonEvent : MonoBehaviour
    {public const float SpeedBoost=1.15f;public void Apply(){UI.SettingsManager.Instance?.Sky?.SetPreset(UI.SkyPreset.BloodMoon);}}
}
