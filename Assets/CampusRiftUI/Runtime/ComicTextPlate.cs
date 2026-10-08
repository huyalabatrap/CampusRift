using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusRift.UI
{
    // Keeps authored HUD label references and animations intact; the plate follows the label.
    public sealed class ComicTextPlate : MonoBehaviour
    {
        public Image Plate;
        TMP_Text label;string previousText;Vector2 previousSize;float previousFont;
        void OnDisable(){if(Plate!=null)Plate.enabled=false;}
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            var text=label!=null?label:label=GetComponent<TMP_Text>();if(text==null||Plate==null)return;
            var source=text.rectTransform;var target=Plate.rectTransform;
            if(previousText!=text.text||previousSize!=source.sizeDelta||previousFont!=text.fontSize)
            {previousText=text.text;previousSize=source.sizeDelta;previousFont=text.fontSize;
                target.anchorMin=source.anchorMin;target.anchorMax=source.anchorMax;target.pivot=new Vector2(.5f,source.pivot.y);
                target.anchoredPosition=source.anchoredPosition+new Vector2(source.rect.center.x,0);
                float width=Mathf.Min(source.rect.width+12,text.GetPreferredValues().x+32);target.sizeDelta=new Vector2(Mathf.Max(30,width),source.sizeDelta.y+6);
            }
            Plate.enabled=text.isActiveAndEnabled&&text.color.a>.01f&&!string.IsNullOrWhiteSpace(text.text);
        }
    }
}
