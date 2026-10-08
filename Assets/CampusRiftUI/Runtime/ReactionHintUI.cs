using TMPro;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Progression;
namespace CampusRift.UI
{
    public sealed class ReactionHintUI : MonoBehaviour
    {
        Canvas canvas; RectTransform card; TMP_Text title,hint,plus;
        ComboGraphic first,second; TMP_Text firstName,secondName;
        readonly ReactionType[] queued=new ReactionType[9];int count;float until;
        ReactionType current;ReactionConfig config;
        public bool Visible=>card!=null&&card.gameObject.activeSelf;
        public float Remaining=>Mathf.Max(0,until-Time.time);
        public ReactionType Current=>current;
        void Awake()
        {
            config=ReactionConfig.Current;canvas=ComboUIFactory.Canvas("First reaction tutorial",transform,27);
            card=ComboUIFactory.Rect("Three second comic hint",canvas.transform,new Vector2(700,126),new Vector2(0,-96));
            card.anchorMin=card.anchorMax=new Vector2(.5f,1);card.pivot=new Vector2(.5f,1);card.anchoredPosition=new Vector2(0,-35);
            ComicTheme.Frame(card.gameObject,"round-paper");
            first=ComboUIFactory.Graphic("First element icon",card,new Vector2(62,62),new Vector2(-292,0),ComboGraphic.Shape.Disc,ComicTheme.Purple);
            second=ComboUIFactory.Graphic("Second element icon",card,new Vector2(62,62),new Vector2(-192,0),ComboGraphic.Shape.Disc,ComicTheme.Gold);
            firstName=ComboUIFactory.Text("First element",first.transform,new Vector2(46,38),Vector2.zero,12);secondName=ComboUIFactory.Text("Second element",second.transform,new Vector2(46,38),Vector2.zero,12);
            firstName.fontStyle=secondName.fontStyle=FontStyles.Bold;firstName.fontSizeMin=secondName.fontSizeMin=8;firstName.margin=secondName.margin=new Vector4(2,0,2,0);
            plus=ComboUIFactory.Text("Plus",card,new Vector2(30,36),new Vector2(-242,0),28);plus.text="+";plus.color=ComicTheme.Ink;
            title=ComboUIFactory.Text("First encounter",card,new Vector2(440,44),new Vector2(66,24),26);title.color=ComicTheme.Purple;
            hint=ComboUIFactory.Text("Reaction recipe",card,new Vector2(440,54),new Vector2(66,-21),20);hint.textWrappingMode=TextWrappingModes.Normal;hint.color=ComicTheme.Ink;
            card.gameObject.SetActive(false);
        }
        public void Encounter(ReactionType type)
        {
            if(config==null||ProfileService.Instance==null)return;
            var profile=ProfileService.Instance;if(profile.Data.seenReactions==null)profile.Data.seenReactions=new System.Collections.Generic.List<string>();
            string id=config.Rule(type).id;if(profile.Data.seenReactions.Contains(id))return;
            profile.Data.seenReactions.Add(id);profile.MarkDirty();
            if(count<queued.Length)queued[count++]=type;
            if(!Visible)Next();
        }
        void Next()
        {
            if(count==0){card.gameObject.SetActive(false);return;}
            current=queued[0];for(int i=1;i<count;i++)queued[i-1]=queued[i];count--;
            until=Time.time+3;card.gameObject.SetActive(true);Refresh();
        }
        static string ElementName(Element element)
        {
            switch(element){case Element.Thuy:return LevelHUD.Vietnamese?"THỦY":"WATER";case Element.Loi:return LevelHUD.Vietnamese?"LÔI":"BOLT";case Element.Hoa:return LevelHUD.Vietnamese?"HỎA":"FIRE";case Element.Kim:return LevelHUD.Vietnamese?"KIM":"METAL";case Element.Tho:return LevelHUD.Vietnamese?"THỔ":"EARTH";case Element.Moc:return LevelHUD.Vietnamese?"MỘC":"WOOD";case Element.Am:return LevelHUD.Vietnamese?"ÂM":"YIN";case Element.KhongGian:return LevelHUD.Vietnamese?"KHÔNG GIAN":"SPACE";default:return LevelHUD.Vietnamese?"VÔ HỆ":"NONE";}
        }
        void Refresh()
        {
            var rule=config.Rule(current);title.text=(LevelHUD.Vietnamese?rule.nameVI:rule.nameEN).ToUpperInvariant();hint.text=LevelHUD.Vietnamese?rule.hintVI:rule.hintEN;
            first.tint=ElementChart.ColorOf(rule.first);second.tint=ElementChart.ColorOf(rule.second);first.Refresh();second.Refresh();firstName.text=ElementName(rule.first);secondName.text=ElementName(rule.second);
        }
        void LateUpdate()
        {
            canvas.enabled=UIStateManager.Instance!=null&&(UIStateManager.Instance.State==UIState.Gameplay||UIStateManager.Instance.State==UIState.Modal);
            if(Visible){if(Time.time>=until)Next();else Refresh();}
        }
        public void Clear(){count=0;if(card!=null)card.gameObject.SetActive(false);}
    }
}
