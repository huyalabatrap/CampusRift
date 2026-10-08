using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CampusRift.UI
{
    public sealed class SkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Icon, CooldownOverlay;
        public TMP_Text KeyLabel, CooldownText, UpgradeLabel, Tooltip;
        public GameObject LockedOverlay;
        public bool IsLocked {get;private set;}=true;
        TMP_Text empty;
        Image frame;
        Image elementMark;
        Combat.Element element;
        public void SetElement(Combat.Element value)
        {
            element=value;
            if(elementMark==null){var go=new GameObject("Element shape",typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);elementMark=go.GetComponent<Image>();elementMark.raycastTarget=false;var r=elementMark.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.anchoredPosition=new Vector2(16,-16);r.sizeDelta=new Vector2(30,30);}
            elementMark.sprite=Accessibility.SymbolSprite(value);elementMark.enabled=!IsLocked&&Icon!=null&&Icon.enabled;
        }
        int shownRank=-1,shownSeconds=-1;
        static readonly string[] RankLabels={"","I","II","III","IV","V"};
        void Awake()
        {
            frame=ComicTheme.Frame(gameObject,"round-panel");
            var go=new GameObject("Empty slot",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(transform,false);
            empty=go.GetComponent<TMP_Text>();ComicTheme.Text(empty);empty.text="+";empty.fontSize=42;empty.alignment=TextAlignmentOptions.Center;empty.color=new Color(.7f,.8f,.9f,.55f);empty.raycastTarget=false;
            var r=empty.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;go.SetActive(false);
            ComicSkillLayout.Apply(this);
        }
        public void SetLocked(bool locked) { IsLocked=locked;LockedOverlay.SetActive(locked);if(elementMark!=null)elementMark.enabled=!locked&&Icon!=null&&Icon.enabled; }
        public void SetCooldown(float remaining,float duration)
        {
            float value=duration>0?Mathf.Clamp01(remaining/duration):0;CooldownOverlay.fillAmount=value;
            int seconds=remaining>0?Mathf.CeilToInt(remaining):0;if(seconds!=shownSeconds){shownSeconds=seconds;if(seconds>0)CooldownText.SetText("{0}",seconds);else CooldownText.SetText("");}
        }
        public void Configure(Sprite icon,string key,string tooltip,int level=0)
        {Icon.sprite=icon;Icon.enabled=icon!=null;KeyLabel.text=key;Tooltip.text=tooltip;SetRank(level);bool vacant=icon==null&&level==0;if(empty!=null)empty.gameObject.SetActive(vacant);if(frame!=null)frame.color=vacant?new Color(.65f,.75f,.9f,.55f):Color.white;}
        public void SetRank(int level){level=Mathf.Clamp(level,0,5);if(shownRank==level)return;shownRank=level;if(UpgradeLabel!=null)UpgradeLabel.text=RankLabels[level];}
        public void OnPointerEnter(PointerEventData e) { Tooltip.gameObject.SetActive(true); }
        public void OnPointerExit(PointerEventData e) { Tooltip.gameObject.SetActive(false); }
        void OnDisable() { if(Tooltip!=null) Tooltip.gameObject.SetActive(false); }
    }
}
