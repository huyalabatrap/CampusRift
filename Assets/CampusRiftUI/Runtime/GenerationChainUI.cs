using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Combat;
using CampusRift.Controls;
namespace CampusRift.UI
{
    public sealed class GenerationChainUI : MonoBehaviour
    {
        Canvas canvas;
        GenerationChainTracker tracker;
        TMP_Text title, timer, complete;
        ComboGraphic ring;
        ComboGraphic completionShape;
        readonly ComboGraphic[] dots=new ComboGraphic[3], motes=new ComboGraphic[6];
        readonly Image[] dotShapes=new Image[3];
        float completedAt=-10;
        RectTransform panel,completionBurst;CanvasGroup completionGroup;
        void Start()
        {
            tracker=FindAnyObjectByType<GenerationChainTracker>();
            canvas=ComboUIFactory.Canvas("Generation chain HUD",tracker!=null?tracker.transform:transform,25);
            panel=ComboUIFactory.Rect("Tương Sinh",canvas.transform,new Vector2(350,76),Vector2.zero);
            panel.anchorMin=panel.anchorMax=new Vector2(1,0);panel.pivot=new Vector2(1,0);panel.anchoredPosition=new Vector2(-38,265);
            ComicTheme.Frame(panel.gameObject,"round-panel");
            title=ComboUIFactory.Text("Chain title",panel,new Vector2(202,38),new Vector2(-52,10),24);
            title.margin=new Vector4(5,4,7,12);
            timer=ComboUIFactory.Text("Chain countdown",panel,new Vector2(194,34),new Vector2(-52,-9),15);
            ring=ComboUIFactory.Graphic("Six second rim",panel,new Vector2(112,54),new Vector2(110,0),ComboGraphic.Shape.Ring,ComicTheme.Gold);
            for(int i=0;i<3;i++)dots[i]=ComboUIFactory.Graphic("Element dot "+i,panel,new Vector2(24,24),new Vector2(75+i*35,0),ComboGraphic.Shape.Disc,ComicTheme.Muted);
            for(int i=0;i<3;i++){var r=ComboUIFactory.Rect("Element shape",dots[i].transform,new Vector2(24,24),Vector2.zero);dotShapes[i]=r.gameObject.AddComponent<Image>();dotShapes[i].raycastTarget=false;}
            completionBurst=ComboUIFactory.Rect("Generation comic burst",canvas.transform,new Vector2(470,105),new Vector2(555,-125));
            completionShape=ComboUIFactory.Graphic("Generation ink burst",completionBurst,completionBurst.sizeDelta,Vector2.zero,ComboGraphic.Shape.Burst,ComicTheme.Paper);
            completionGroup=completionBurst.gameObject.AddComponent<CanvasGroup>();completionGroup.blocksRaycasts=false;
            complete=ComboUIFactory.Text("Generation completion",completionBurst,new Vector2(375,68),Vector2.zero,34);completionBurst.gameObject.SetActive(false);
            for(int i=0;i<motes.Length;i++){motes[i]=ComboUIFactory.Graphic("Spirit return mote "+i,canvas.transform,new Vector2(17,17),Vector2.zero,ComboGraphic.Shape.Disc,new Color(.25f,1,.75f));motes[i].gameObject.SetActive(false);}
            if(tracker!=null)tracker.ChainCompleted+=Completed;
        }
        void Completed(){completedAt=Time.time;completionBurst.gameObject.SetActive(true);foreach(var mote in motes)mote.gameObject.SetActive(true);}
        void LateUpdate()
        {
            if(panel!=null)
            {
                float textScale=Accessibility.TextScale;
                panel.sizeDelta=new Vector2(350*textScale,76*textScale);
                title.rectTransform.sizeDelta=new Vector2(202,38)*textScale;
                title.rectTransform.anchoredPosition=new Vector2(-52,10)*textScale;
                title.margin=new Vector4(5,4,7,12)*textScale;
                timer.rectTransform.sizeDelta=new Vector2(194,34)*textScale;
                timer.rectTransform.anchoredPosition=new Vector2(-52,-9)*textScale;
                timer.margin=new Vector4(3,0,3,0)*textScale;
                ring.rectTransform.sizeDelta=new Vector2(112,54)*textScale;
                ring.rectTransform.anchoredPosition=new Vector2(110,0)*textScale;
                for(int i=0;i<3;i++)
                {
                    dots[i].rectTransform.sizeDelta=Vector2.one*24*textScale;
                    dots[i].rectTransform.anchoredPosition=new Vector2(75+i*35,0)*textScale;
                    dotShapes[i].rectTransform.sizeDelta=Vector2.one*24*textScale;
                }
                completionBurst.sizeDelta=new Vector2(470,105)*textScale;
                completionShape.rectTransform.sizeDelta=completionBurst.sizeDelta;
                complete.rectTransform.sizeDelta=new Vector2(375,68)*textScale;
                panel.anchorMin=panel.anchorMax=CampusInput.Mobile?new Vector2(.5f,0):new Vector2(1,0);
                panel.pivot=CampusInput.Mobile?new Vector2(.5f,0):new Vector2(1,0);
                panel.anchoredPosition=CampusInput.Mobile?new Vector2(-40,150):new Vector2(-38,265);
            }
            if(canvas==null||tracker==null)return;
            canvas.enabled=UIStateManager.Instance!=null&&(UIStateManager.Instance.State==UIState.Gameplay||UIStateManager.Instance.State==UIState.Modal);
            bool mobile=SettingsManager.Instance!=null&&SettingsManager.Instance.Current.ControlMode==ControlMode.Mobile;
            completionBurst.anchoredPosition=mobile?new Vector2(-540,-25):new Vector2(555,-125);
            title.text=LevelHUD.Vietnamese?"TƯƠNG SINH":"GENERATION";
            timer.text=tracker.Count>0&&tracker.Count<3?tracker.Remaining.ToString("0.0")+" s":tracker.Count==3?(LevelHUD.Vietnamese?"+20 LINH LỰC":"+20 SPIRIT"):(LevelHUD.Vietnamese?"3 HỆ LIÊN TIẾP":"3 LINKED ELEMENTS");
            ring.progress=tracker.Count==0?0:tracker.Count==3?1:tracker.Remaining/tracker.Window;
            ring.tint=tracker.Count==3?ComicTheme.Paper:ElementChart.ColorOf(tracker.Next);ring.Refresh();
            for(int i=0;i<3;i++)
            {
                dots[i].tint=i<tracker.Count?ElementChart.ColorOf(tracker.Used(i)):i==tracker.Count&&tracker.Next!=Element.None?new Color(ElementChart.ColorOf(tracker.Next).r,ElementChart.ColorOf(tracker.Next).g,ElementChart.ColorOf(tracker.Next).b,.35f):new Color(.14f,.2f,.28f);
                dots[i].Refresh();
                dotShapes[i].enabled=Accessibility.ColorBlind;dotShapes[i].sprite=Accessibility.SymbolSprite(i<tracker.Count?tracker.Used(i):i==tracker.Count?tracker.Next:Element.None);
            }
            float t=Time.time-completedAt;
            complete.text=LevelHUD.Vietnamese?"TƯƠNG SINH!":"GENERATION!";
            completionBurst.gameObject.SetActive(t<1.1f);completionGroup.alpha=Mathf.Clamp01((1.1f-t)/.3f);complete.color=ComicTheme.Purple;
            completionBurst.localScale=Vector3.one*(t<.15f?1+Mathf.Sin(t/.15f*Mathf.PI)*.25f:1);
            var fill=FindSpiritFill();Vector2 finish=fill!=null?new Vector2(fill.position.x/Screen.width*1920-960,fill.position.y/Screen.height*1080-540):new Vector2(-725,310);
            var panelCenter=panel.TransformPoint(panel.rect.center);
            Vector2 start=new Vector2(panelCenter.x/Screen.width*1920-960,panelCenter.y/Screen.height*1080-540);
            for(int i=0;i<motes.Length;i++)
            {
                float travel=Mathf.Clamp01((t-i*.035f)/.85f);bool live=t>=i*.035f&&t<1.15f;
                motes[i].gameObject.SetActive(live);if(!live)continue;
                motes[i].rectTransform.anchoredPosition=Vector2.Lerp(start,finish,travel)+Vector2.up*Mathf.Sin(travel*Mathf.PI)*(140+i*9);
                motes[i].color=new Color(1,1,1,1-travel*.7f);
            }
        }
        RectTransform spiritFill;
        RectTransform FindSpiritFill(){if(spiritFill!=null)return spiritFill;var ui=FindAnyObjectByType<PlayerSpiritUI>();if(ui!=null&&ui.Fill!=null)spiritFill=ui.Fill.rectTransform;return spiritFill;}
        void OnDestroy(){if(tracker!=null)tracker.ChainCompleted-=Completed;if(canvas!=null)Destroy(canvas.gameObject);}
    }
}
