using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.SkyBeast;
namespace CampusRift.UI
{
    public sealed class SkyBeastBarUI:MonoBehaviour
    {
        sealed class Row{public RectTransform root;public TMP_Text name,phase;public Image[] segments;}
        readonly Row[] rows=new Row[2];SkyBeastScheduler scheduler;Canvas canvas;TMP_Text fury;
        public const float SwordIntentReservedTop=122;
        public static void Attach(SkyBeastScheduler owner){var ui=owner.gameObject.AddComponent<SkyBeastBarUI>();ui.scheduler=owner;ui.Build();}
        static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.sizeDelta=size;r.anchoredPosition=position;return r;}
        static TMP_Text Text(Transform parent,string name,int size,Vector2 area,Vector2 position)
        {var r=Rect(parent,name,area,position);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=ComicTheme.Font;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=16;t.fontSizeMax=size;t.textWrappingMode=TextWrappingModes.NoWrap;t.alignment=TextAlignmentOptions.Center;t.color=ComicTheme.Paper;t.raycastTarget=false;ComicTheme.Text(t,true);return t;}
        void Build()
        {
            var go=new GameObject("Sky Beast Comic Bars",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=26;
            var scale=go.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=1;
            for(int i=0;i<2;i++)
            {
                var root=Rect(go.transform,"Beast bar "+i,new Vector2(406,110),new Vector2(0,-59));ComicTheme.Frame(root.gameObject);
                var row=new Row{root=root,name=Text(root,"Beast name",25,new Vector2(378,44),new Vector2(0,-28)),phase=Text(root,"Phase and segments",18,new Vector2(378,36),new Vector2(0,-77)),segments=new Image[2]};row.phase.margin=new Vector4(2,0,2,0);
                for(int s=0;s<2;s++){var r=Rect(root,"Sword segment "+s,new Vector2(179,16),new Vector2(-93+s*186,-53));var im=r.gameObject.AddComponent<Image>();im.sprite=ComicTheme.Sprite("round-mask");im.type=Image.Type.Sliced;im.color=ComicTheme.Orange;im.raycastTarget=false;row.segments[s]=im;}
                rows[i]=row;
            }
            // P15 owns the meter at 122px; this HUD leaves that strip empty.
            fury=Text(go.transform,"Dragon Fury alert",32,new Vector2(800,50),new Vector2(0,-177));fury.color=ComicTheme.Gold;ComicTheme.ReadabilityPlate(fury);
        }
        void Update()
        {
            var state=UIStateManager.Instance;canvas.enabled=scheduler!=null&&!scheduler.Completed&&(state==null||state.State==UIState.Gameplay||state.State==UIState.Modal);if(!canvas.enabled)return;
            int count=scheduler.Beasts.Count;bool vi=LevelHUD.Vietnamese;
            for(int i=0;i<2;i++)
            {
                var row=rows[i];row.root.gameObject.SetActive(i<count);if(i>=count)continue;
                row.root.anchoredPosition=new Vector2(count==1?0:i==0?-211:211,-59);
                var beast=scheduler.Beasts[i];var v=beast.GetComponent<SkyBeastVitality>();row.name.text=vi?beast.definition.nameVi:beast.definition.nameEn;
                row.phase.text=(vi?"PHA ":"PHASE ")+scheduler.Phase+" · "+v.RemainingSegments+"/"+v.TotalSegments+(vi?" KHÚC":" SEGMENTS");
                for(int s=0;s<2;s++){var im=row.segments[s];im.gameObject.SetActive(s<v.TotalSegments);im.rectTransform.sizeDelta=new Vector2(v.TotalSegments==1?366:179,16);im.rectTransform.anchoredPosition=new Vector2(v.TotalSegments==1?0:-93+s*186,-53);im.color=s<v.RemainingSegments?ComicTheme.Orange:ComicTheme.Navy;}
            }
            bool warn=scheduler.Fury!=null&&scheduler.Fury.Warning;fury.gameObject.SetActive(warn);
            if(warn)fury.text=(vi?"LONG NỘ — HÃY TRÚ ẨN! ":"DRAGON FURY — TAKE SHELTER! ")+Mathf.CeilToInt(scheduler.Fury.Remaining);
        }
    }
}
