using TMPro;
using UnityEngine;
namespace CampusRift.Localization
{
    // Preserve canonical text separately so EN/VN switches never translate a translation.
    [DefaultExecutionOrder(10000),DisallowMultipleComponent]
    public sealed class LocalizedText : MonoBehaviour
    {
        public string Source {get;private set;}
        TMP_Text target;
        TMP_FontAsset font;
        Material material;
        string rendered;
        GameLanguage language;
        bool initialized,autoSize;
        float size,min,max;
        void Awake(){Initialize();}
        void Initialize()
        {
            if(initialized)return;target=GetComponent<TMP_Text>();if(target==null)return;
            Source=target.text;font=target.font;material=target.fontSharedMaterial;
            autoSize=target.enableAutoSizing;size=target.fontSize;min=target.fontSizeMin;max=target.fontSizeMax;
            rendered=null;initialized=true;
        }
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            Initialize();if(!initialized || LocalizationService.Instance==null)return;
            var service=LocalizationService.Instance;
            if(target.text==rendered && language==service.Language)return;
            if(target.text!=rendered)Source=target.text;
            rendered=service.Translate(Source);language=service.Language;
            bool translated=language==GameLanguage.Vietnamese && rendered!=Source;
            var wanted=translated && service.VietnameseFont!=null?service.VietnameseFont:font;
            if(target.font!=wanted){target.font=wanted;if(wanted==font)target.fontSharedMaterial=material;}
            if(translated)
            {target.enableAutoSizing=true;target.fontSizeMax=autoSize?max:size;target.fontSizeMin=autoSize?min:Mathf.Max(12,size*.65f);}
            else{target.enableAutoSizing=autoSize;target.fontSize=size;target.fontSizeMin=min;target.fontSizeMax=max;}
            target.text=rendered;
            var layout=target.GetComponent<UnityEngine.UI.LayoutElement>();
            if(layout!=null && target.transform.parent!=null)
            {
                var parent=target.transform.parent as RectTransform;
                if(parent!=null)layout.preferredHeight=target.GetPreferredValues(rendered,Mathf.Max(100,parent.rect.width-24),0).y+24;
            }
        }
        public static string Canonical(TMP_Text text)=>text.GetComponent<LocalizedText>()?.Source??text.text;
    }
}
