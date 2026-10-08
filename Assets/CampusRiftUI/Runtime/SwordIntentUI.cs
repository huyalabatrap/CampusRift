using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.Controls;
using CampusRift.SkyBeast;
namespace CampusRift.UI
{
    public sealed class SwordIntentUI : MonoBehaviour,ICanvasRaycastFilter
    {
        HeavenSwordUltimate owner;Canvas canvas;Image fill;TMP_Text meter,status,pcState,pcName;RectTransform pc,root;MobileArc progress;RiftGraphic edge;
        public static void Attach(HeavenSwordUltimate ultimate){var ui=new GameObject("Sword Intent comic HUD").AddComponent<SwordIntentUI>();ui.transform.SetParent(ultimate.transform,false);ui.owner=ultimate;ui.Build();}
        public static string StateLabel(HeavenSwordUltimate.ButtonState s,bool vi)=>s==HeavenSwordUltimate.ButtonState.Ready?(vi?"SẴN SÀNG":"READY"):s==HeavenSwordUltimate.ButtonState.NeedOutdoor?(vi?"RA NGOÀI TRỜI":"GO OUTDOORS"):s==HeavenSwordUltimate.ButtonState.Blocked?(vi?"LONG NỘ · KHÓA":"FURY · BLOCKED"):s==HeavenSwordUltimate.ButtonState.Channeling?(vi?"ĐANG NIỆM":"CHANNELING"):(vi?"TÍCH KIẾM Ý":"CHARGING");
        public static string Hint(HeavenSwordUltimate u,bool vi)
        {
            if(u.State==HeavenSwordUltimate.ButtonState.NeedOutdoor)return vi?"Ra ngoài trời để triệu hồi Thiên Kiếm":"Go outdoors to summon the Heaven Sword";
            if(u.State==HeavenSwordUltimate.ButtonState.Blocked)return vi?"Long Nộ — trú ẩn cho tới khi kết thúc":"Dragon Fury — take shelter until it ends";
            if(u.State==HeavenSwordUltimate.ButtonState.Channeling)return vi?"VẠN KIẾM QUY TÔNG · Giữ vững kiếm tâm":"TEN THOUSAND SWORDS · Hold your focus";
            if(u.State==HeavenSwordUltimate.ButtonState.Ready&&u.FireWarning)return vi?"Sẽ bị ngắt nếu không có hộ thể":"Fire will interrupt you without protection";
            return "";
        }
        void Build()
        {
            canvas=ComboUIFactory.Canvas("P15 sword intent",transform,27);canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=1;canvas.gameObject.AddComponent<GraphicRaycaster>();
            root=ComboUIFactory.Rect("Kiếm Ý",canvas.transform,new Vector2(560,74),Vector2.zero);root.anchorMin=root.anchorMax=new Vector2(.5f,1);root.anchoredPosition=new Vector2(0,-159);ComicTheme.Frame(root.gameObject);
            var track=ComboUIFactory.Rect("Weight progress",root,new Vector2(525,12),new Vector2(0,-15));var bg=track.gameObject.AddComponent<Image>();bg.color=ComicTheme.Navy;bg.raycastTarget=false;
            fill=ComboUIFactory.Rect("Sword intent fill",track,new Vector2(525,12),Vector2.zero).gameObject.AddComponent<Image>();fill.sprite=ComicTheme.Sprite("round-mask");fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.color=ComicTheme.Gold;fill.raycastTarget=false;
            meter=ComboUIFactory.Text("Meter label",root,new Vector2(520,40),new Vector2(0,3),22);
            status=ComboUIFactory.Text("Sword intent hint",canvas.transform,new Vector2(980,45),Vector2.zero,24);status.rectTransform.anchorMin=status.rectTransform.anchorMax=new Vector2(.5f,1);status.rectTransform.anchoredPosition=new Vector2(0,-221);ComicTheme.ReadabilityPlate(status);
            pc=ComboUIFactory.Rect("Heaven Sword V",canvas.transform,Vector2.one*145,Vector2.zero);pc.anchorMin=pc.anchorMax=new Vector2(1,0);pc.anchoredPosition=new Vector2(-115,500);
            var disc=pc.gameObject.AddComponent<RiftGraphic>();disc.Form=RiftGraphic.Shape.Disc;disc.color=new Color(.06f,.045f,.025f,.95f);disc.raycastTarget=true;
            edge=ComboUIFactory.Rect("Gold edge",pc,Vector2.one*141,Vector2.zero).gameObject.AddComponent<RiftGraphic>();edge.Form=RiftGraphic.Shape.Ring;edge.Thickness=5;edge.color=ComicTheme.Gold;edge.raycastTarget=false;
            progress=ComboUIFactory.Rect("Charge circle",pc,Vector2.one*127,Vector2.zero).gameObject.AddComponent<MobileArc>();progress.color=ComicTheme.Gold;progress.raycastTarget=false;
            var glyph=ComboUIFactory.Text("Sword glyph",pc,new Vector2(100,100),Vector2.zero,58);glyph.rectTransform.sizeDelta=new Vector2(100,100);glyph.text="V";
            pc.gameObject.AddComponent<Button>().onClick.AddListener(()=>owner.TryChannel());pc.gameObject.AddComponent<SwordRoundRaycast>();
            pcName=ComboUIFactory.Text("Heaven Sword",pc,new Vector2(195,42),new Vector2(0,-98),22);pcName.text="THIÊN KIẾM";ComicTheme.ReadabilityPlate(pcName);
            pcState=ComboUIFactory.Text("Ultimate status",pc,new Vector2(205,40),new Vector2(0,-135),19);ComicTheme.ReadabilityPlate(pcState);
        }
        void Update()
        {
            float textScale=Accessibility.TextScale;
            root.sizeDelta=new Vector2(560*textScale,74);
            meter.rectTransform.sizeDelta=new Vector2(520*textScale,40);
            meter.margin=new Vector4(4,0,4,0);
            ((RectTransform)fill.transform.parent).sizeDelta=new Vector2(525*textScale,12);
            fill.rectTransform.sizeDelta=new Vector2(525*textScale,12);
            canvas.enabled=owner!=null&&owner.Visible&&(UIStateManager.Instance==null||UIStateManager.Instance.State==UIState.Gameplay)&&!(HeavenSwordCinematic.Active!=null&&HeavenSwordCinematic.Active.Playing);if(!canvas.enabled)return;
            bool vi=LevelHUD.Vietnamese;var intent=owner.Intent;float value=intent!=null?intent.Fraction:0;fill.fillAmount=value;fill.color= intent!=null&&intent.Full&&SettingsManager.Instance?.Current.ReduceSkillFlashes!=true?Color.Lerp(ComicTheme.Gold,Color.white,(Mathf.Sin(Time.unscaledTime*5)+1)*.3f):ComicTheme.Gold;
            meter.text=intent==null||intent.TotalWeight==0?(vi?"KIẾM Ý · CHỜ ĐỢT QUÁI":"SWORD INTENT · WAITING FOR WAVE"):(vi?"KIẾM Ý":"SWORD INTENT")+" · "+Mathf.FloorToInt(value*100)+"%  · "+intent.DefeatedWeight+"/"+intent.TotalWeight;
            status.text=Hint(owner,vi);status.gameObject.SetActive(status.text.Length>0);pc.gameObject.SetActive(!CampusInput.Mobile);
            pcName.text=vi?"THIÊN KIẾM":"HEAVEN SWORD";
            pcState.text=StateLabel(owner.State,vi);progress.SetFill(owner.Channeling?owner.ChannelProgress:value);edge.color=owner.State==HeavenSwordUltimate.ButtonState.Ready?fill.color:owner.State==HeavenSwordUltimate.ButtonState.Blocked?ComicTheme.Red:ComicTheme.Gold;
        }
        public bool IsRaycastLocationValid(Vector2 point,Camera eventCamera)=>true;
    }
    public sealed class SwordRoundRaycast : MonoBehaviour,ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 screen,Camera camera){var r=(RectTransform)transform;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(r,screen,camera,out var point))return false;float radius=Mathf.Min(r.rect.width,r.rect.height)*.5f;return (point-r.rect.center).sqrMagnitude<=radius*radius;}
    }
}
