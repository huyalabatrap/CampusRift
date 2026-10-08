using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace CampusRift.UI
{
    public sealed class LoadingScreenUI : MonoBehaviour
    {
        public Image Fill;
        public TMP_Text Progress;
        public void SetProgress(float value)
        { value=Mathf.Clamp01(value); if(Fill!=null) Fill.fillAmount=value; if(Progress!=null) Progress.text=$"{Mathf.RoundToInt(value*100):00}%"; }
    }
}
