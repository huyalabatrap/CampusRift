using UnityEngine;
namespace CampusRift.UI
{
    public sealed class MonsterWarningUI : MonoBehaviour
    {
        public CanvasGroup Indicator;
        public void SetWarning(bool active) {Indicator.gameObject.SetActive(active);}
        void Update() {if(Indicator.gameObject.activeSelf)Indicator.alpha=SettingsManager.Instance?.Current.ReduceSkillFlashes==true?.85f:.7f+.25f*Mathf.Sin(Time.unscaledTime*3);}
    }
}
