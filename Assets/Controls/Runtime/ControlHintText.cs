using TMPro;
using UnityEngine;
using CampusRift.UI;
namespace CampusRift.Controls
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class ControlHintText : MonoBehaviour
    {
        [TextArea] public string PCText,MobileText;
        TMP_Text label;
        void Awake(){label=GetComponent<TMP_Text>();}
        void OnEnable(){if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed+=Apply;Refresh();}
        void OnDisable(){if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed-=Apply;}
        void Apply(GameSettings _)=>Refresh();
        void Refresh(){if(label==null)label=GetComponent<TMP_Text>();label.text=CampusInput.Mobile?MobileText:PCText;}
    }
}
