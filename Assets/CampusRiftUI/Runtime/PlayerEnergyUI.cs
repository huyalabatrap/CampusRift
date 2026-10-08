using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusRift.UI
{
    [DisallowMultipleComponent]
    public sealed class PlayerEnergyUI : MonoBehaviour
    {
        public CampusExplorer Source { get; private set; }
        public Image Fill { get; private set; }
        public TMP_Text Value { get; private set; }
        public TMP_Text Label { get; private set; }
        static readonly Color Ready=new Color(.25f,.85f,1f),Exhausted=new Color(1f,.48f,.22f);
        public void Bind(CampusExplorer source,Transform vitals,Sprite sprite)
        {
            Source=source;
            if(vitals==null)return;
            Fill=vitals.Find("EnergyPlaceholderFill")?.GetComponent<Image>();
            Value=vitals.Find("EnergyPlaceholder")?.GetComponent<TMP_Text>();
            Label=vitals.Find("EnergyLabel")?.GetComponent<TMP_Text>();
            var track=vitals.Find("EnergyTrack") as RectTransform;
            if(Fill!=null && track!=null)
            {
                Fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,track.rect.width);
                Fill.sprite=sprite;Fill.type=Image.Type.Filled;Fill.fillMethod=Image.FillMethod.Horizontal;Fill.fillOrigin=0;
                Fill.raycastTarget=false;
            }
            Refresh();
            ComicTheme.ClipBar(Fill);
        }
        void LateUpdate()=>Refresh();
        void Refresh()
        {
            if(Source==null || Fill==null || Value==null || Label==null)return;
            Fill.fillAmount=Source.EnergyFraction;
            Fill.color=Source.BoostExhausted?Exhausted:Ready;
            string value=Mathf.CeilToInt(Source.EnergyFraction*100)+"%";
            if(Value.text!=value)Value.text=value;
            string label=Source.BoostExhausted?"RECOVERING":Source.IsSprinting?"BOOST":"ENERGY";
            if(Label.text!=label)Label.text=label;
            Label.color=Fill.color;
        }
    }
}
