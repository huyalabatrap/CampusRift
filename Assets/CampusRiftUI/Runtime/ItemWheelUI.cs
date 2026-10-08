using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CampusRift.Controls;
using CampusRift.Progression;

namespace CampusRift.UI
{
    // A captured item touch owns this wheel until release. The wheel itself never
    // raycasts, pauses time, or steals a second thumb's movement/skill pointer.
    [DefaultExecutionOrder(-100)]
    public sealed class ItemWheelUI : MonoBehaviour
    {
        public const float HoldSeconds = .25f;
        const float Radius = 210, Inner = 72;
        public bool IsOpen => wheel != null && wheel.gameObject.activeSelf;
        public int QuickSlot { get; private set; }
        public int Highlight { get; private set; } = -1;
        public RectTransform WheelRect => wheel;
        MobileControlsHUD hud;
        PlayerItems items;
        LevelBag bag;
        RectTransform button, wheel;
        Image quickIcon;
        TMP_Text quickLabel, quickCount, centerText;
        MobileArc quickArc;
        CanvasGroup group;
        bool pressed, pc, cursorOwned;
        float pressedAt;
        Vector2 pointer;
        CursorLockMode oldLock;
        bool oldVisible;
        readonly List<Slice> slices = new List<Slice>();
        sealed class Slice { public RectTransform root; public ItemWheelSector sector; public Image icon; public TMP_Text count; public MobileArc arc; }

        public void Initialize(MobileControlsHUD owner, RectTransform quickButton, RectTransform glyph, TMP_Text label, MobileArc arc)
        {
            hud=owner;button=quickButton;quickLabel=label;quickArc=arc;
            quickIcon=glyph.gameObject.AddComponent<Image>();quickIcon.raycastTarget=false;quickIcon.preserveAspect=true;
            quickCount=Text(button,"Quantity","",20,new Vector2(76,38),new Vector2(0,button.rect.height*.33f));
            var layer=Rect(transform,"Item wheel overlay",Vector2.zero,Vector2.zero);
            layer.anchorMin=Vector2.zero;layer.anchorMax=Vector2.one;layer.offsetMin=layer.offsetMax=Vector2.zero;
            var canvas=layer.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=40;
            group=layer.gameObject.AddComponent<CanvasGroup>();group.ignoreParentGroups=true;group.blocksRaycasts=false;group.interactable=false;
            wheel=Rect(layer,"Item wheel",Vector2.one*(Radius*2),Vector2.zero);
            var bg=wheel.gameObject.AddComponent<RiftGraphic>();bg.Form=RiftGraphic.Shape.Disc;bg.color=new Color(.025f,.03f,.06f,.96f);bg.raycastTarget=false;
            var edge=Rect(wheel,"Ink",Vector2.one*(Radius*2),Vector2.zero).gameObject.AddComponent<RiftGraphic>();
            edge.Form=RiftGraphic.Shape.Ring;edge.Thickness=6;edge.color=ComicTheme.Ink;edge.raycastTarget=false;
            centerText=Text(wheel,"Selection","",17,new Vector2(Inner*2-14,Inner*2-14),Vector2.zero);
            centerText.textWrappingMode=TextWrappingModes.Normal;
            wheel.gameObject.SetActive(false);
            hud.Input.ResetTouch+=Cancel;
        }
        RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        TMP_Text Text(Transform parent,string name,string value,int size,Vector2 bounds,Vector2 position)
        {
            var t=Rect(parent,name,bounds,position).gameObject.AddComponent<TextMeshProUGUI>();
            t.font=quickLabel.font;t.text=value;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
            t.color=Color.white;ComicTheme.Text(t);t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=12;
            t.textWrappingMode=TextWrappingModes.NoWrap;return t;
        }
        bool Allowed => hud!=null && hud.Input!=null && hud.Input.Allowed && Application.isFocused;
        void Update()
        {
            if(hud==null)return;
            if(items==null)items=hud.Input.GetComponent<PlayerItems>();
            if(items==null)return;
            if(items.Bag!=bag)
            {
                string selected=bag!=null&&QuickSlot<bag.Slots.Count?bag.Slots[QuickSlot].item.id:null;
                Cancel();bag=items.Bag;QuickSlot=0;
                if(bag!=null)
                {
                    int index=bag.Slots.FindIndex(s=>s.item!=null&&s.item.id==selected);
                    if(index<0)index=bag.Slots.FindIndex(s=>s.item!=null&&!s.item.IsPassive);
                    if(index>=0)QuickSlot=index;
                }
                Rebuild();
            }
            RefreshQuick();
            if(!Allowed || (pressed&&pc==CampusInput.Mobile)){Cancel();return;}
            if(!CampusInput.Mobile)
            {
                var key=Keyboard.current;
                if(key!=null&&key.bKey.wasPressedThisFrame&&!hud.Input.AnySkillAiming)
                {Begin(Mouse.current!=null?Mouse.current.position.ReadValue():Vector2.zero);pc=true;Open();}
                if(pressed&&pc)
                {
                    if(key==null||Mouse.current==null){Cancel();return;}
                    Drag(Mouse.current.position.ReadValue());
                    if(key.escapeKey.wasPressedThisFrame){Cancel();return;}
                    if(!key.bKey.isPressed)End(pointer,false);
                }
            }
            if(pressed&&!IsOpen&&Time.unscaledTime-pressedAt>=HoldSeconds)Open();
            if(IsOpen){Position();Select(pointer);RefreshSlices();}
        }
        void Rebuild()
        {
            foreach(var s in slices)Destroy(s.root.gameObject);slices.Clear();
            int count=bag!=null?bag.Slots.Count:0;
            for(int i=0;i<count;i++)
            {
                var root=Rect(wheel,"Item "+(i+1),Vector2.one*(Radius*2-10),Vector2.zero);
                var sector=root.gameObject.AddComponent<ItemWheelSector>();sector.Configure(i,count,Inner,Radius-5);sector.raycastTarget=false;
                float angle=(90-i*360f/count)*Mathf.Deg2Rad;
                var iconRoot=Rect(root,"Icon",Vector2.one*Mathf.Min(68,240f/count),new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*137);
                var icon=iconRoot.gameObject.AddComponent<Image>();icon.raycastTarget=false;icon.preserveAspect=true;
                var arc=Rect(iconRoot,"Cooldown",Vector2.one*78,Vector2.zero).gameObject.AddComponent<MobileArc>();arc.raycastTarget=false;
                var quantity=Text(iconRoot,"Quantity","",19,new Vector2(74,38),new Vector2(0,-43));
                slices.Add(new Slice{root=root,sector=sector,icon=icon,arc=arc,count=quantity});
            }
            centerText.transform.SetAsLastSibling();
        }
        public void Begin(Vector2 screen)
        {
            if(!Allowed||bag==null||bag.Slots.Count==0)return;
            Cancel();pressed=true;pc=false;pressedAt=Time.unscaledTime;pointer=screen;
        }
        public void Drag(Vector2 screen){if(pressed){pointer=screen;if(IsOpen)Select(screen);}}
        void Open()
        {
            if(!pressed||bag==null||bag.Slots.Count==0)return;
            Highlight=-1;wheel.gameObject.SetActive(true);Position();RefreshSlices();
            if(pc)
            {
                oldLock=Cursor.lockState;oldVisible=Cursor.visible;cursorOwned=true;
                hud.Input.ItemWheelOpen=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
                pointer=RectTransformUtility.WorldToScreenPoint(null,wheel.position);
                Mouse.current?.WarpCursorPosition(pointer);
            }
        }
        void Position()
        {
            var safe=Screen.safeArea;
            var canvas=hud.GetComponentInParent<Canvas>();float unit=canvas.scaleFactor;
            // Expanded wheel stays above the joystick and left of the protected center.
            float joystickTop=safe.yMin;
            if(CampusInput.Mobile)
                foreach(var zone in hud.Zones)if(zone.role==TouchRole.Move)joystickTop=hud.ControlScreenBounds(zone).yMax+18*unit;
            float left=safe.xMin+10*unit,right=Mathf.Min(safe.xMax,Screen.width*.32f)-10*unit;
            float bottom=joystickTop,top=safe.yMax-12*unit;
            if(CampusInput.Mobile)
            {
                var vitals=hud.transform.Find("Vitals");
                if(vitals!=null)
                {
                    var corners=new Vector3[4];
                    foreach(var graphic in vitals.GetComponentsInChildren<Graphic>(true))
                    {
                        graphic.rectTransform.GetWorldCorners(corners);
                        foreach(var corner in corners)top=Mathf.Min(top,RectTransformUtility.WorldToScreenPoint(null,corner).y-12*unit);
                    }
                }
            }
            float requested=SettingsManager.Instance!=null?SettingsManager.Instance.Current.MobileControlScale:1;
            float scale=Mathf.Min(requested,(right-left)/(Radius*2*unit),(top-bottom)/(Radius*2*unit));
            scale=Mathf.Max(.1f,scale);float radius=Radius*scale*unit;
            var origin=RectTransformUtility.WorldToScreenPoint(null,button.position);
            var center=CampusInput.Mobile?origin+new Vector2(-120,390)*unit*scale:new Vector2(Screen.width*.23f,Screen.height*.53f);
            center.x=Mathf.Clamp(center.x,left+radius,right-radius);center.y=Mathf.Clamp(center.y,bottom+radius,top-radius);
            wheel.localScale=Vector3.one*scale;
            var parent=(RectTransform)wheel.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,center,null,out var local);wheel.anchoredPosition=local;
            group.alpha=SettingsManager.Instance!=null?Mathf.Max(.9f,SettingsManager.Instance.Current.MobileOpacity):1;
        }
        void Select(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(wheel,screen,null,out var point);
            int next=-1;
            if(point.magnitude>=Inner&&point.magnitude<=Radius&&slices.Count>0)
            {
                float angle=Mathf.Repeat(90-Mathf.Atan2(point.y,point.x)*Mathf.Rad2Deg+180f/slices.Count,360);
                next=Mathf.FloorToInt(angle/(360f/slices.Count))%slices.Count;
            }
            if(next!=Highlight){Highlight=next;if(next>=0)MobileFeedback.Confirm();}
        }
        public void End(Vector2 screen,bool canceled)
        {
            if(!pressed)return;
            if(!Allowed||canceled){Cancel();return;}
            if(!IsOpen&&Time.unscaledTime-pressedAt>=HoldSeconds)Open();
            if(IsOpen)
            {
                Select(screen);
                if(Highlight>=0){QuickSlot=Highlight;items.Use(QuickSlot);}
            }
            else if(button.GetComponent<MobileTouchZone>().IsRaycastLocationValid(screen,null))items.Use(QuickSlot);
            Cancel();
        }
        public void Cancel()
        {
            pressed=false;Highlight=-1;if(wheel!=null)wheel.gameObject.SetActive(false);
            if(cursorOwned)
            {
                cursorOwned=false;if(hud!=null&&hud.Input!=null)hud.Input.ItemWheelOpen=false;
                Cursor.lockState=oldLock;Cursor.visible=oldVisible;UIStateManager.Instance?.RefreshCursor();
            }
        }
        void RefreshQuick()
        {
            bool exists=bag!=null&&bag.Slots.Count>0;
            QuickSlot=exists?Mathf.Clamp(QuickSlot,0,bag.Slots.Count-1):0;
            quickIcon.enabled=exists;quickCount.text=exists?DevMode.Quantity(bag.Slots[QuickSlot].remaining):"0";
            quickLabel.text=LevelHUD.Vietnamese?"ĐỒ":"ITEMS";
            if(!exists){quickArc.SetFill(0);return;}
            var item=bag.Slots[QuickSlot].item;
            quickIcon.sprite=ItemIcons.Get(item);quickIcon.color=items.Availability(QuickSlot)==ItemUseResult.Used?Color.white:new Color(.45f,.45f,.45f,.7f);
            quickArc.color=item.tint;quickArc.SetFill(item.IsPassive?0:items.CooldownFraction);
        }
        void RefreshSlices()
        {
            for(int i=0;i<slices.Count;i++)
            {
                var s=slices[i];var data=bag.Slots[i];bool ready=items.Availability(i)==ItemUseResult.Used;
                s.icon.sprite=ItemIcons.Get(data.item);s.icon.color=ready?Color.white:new Color(.42f,.42f,.42f,.65f);
                s.count.text=DevMode.Quantity(data.remaining);s.count.color=ready?Color.white:new Color(.6f,.6f,.6f);
                s.arc.color=data.item.tint;s.arc.SetFill(data.item.IsPassive?0:items.CooldownFraction);
                s.sector.color=i==Highlight?new Color(.75f,.5f,.12f,.94f):ready?new Color(.13f,.16f,.24f,.96f):new Color(.08f,.09f,.13f,.95f);
            }
            bool vi=LevelHUD.Vietnamese;
            centerText.text=Highlight<0?(vi?"KÉO CHỌN\nTÂM: HỦY":"DRAG TO PICK\nCENTER: CANCEL"):
                bag.Slots[Highlight].item.Name(vi)+"\n"+Reason(items.Availability(Highlight),vi);
        }
        static string Reason(ItemUseResult result,bool vi)
        {
            switch(result)
            {
                case ItemUseResult.Used:return vi?"THẢ ĐỂ DÙNG":"RELEASE TO USE";
                case ItemUseResult.Cooldown:return vi?"HỒI CHIÊU":"COOLDOWN";
                case ItemUseResult.Passive:return vi?"TỰ ĐỘNG":"AUTO";
                case ItemUseResult.NothingToDo:return vi?"ĐÃ ĐẦY":"ALREADY FULL";
                case ItemUseResult.EmptySlot:return vi?"ĐÃ HẾT":"EMPTY";
                default:return vi?"KHÔNG DÙNG ĐƯỢC":"UNAVAILABLE";
            }
        }
        void OnApplicationFocus(bool focused){if(!focused)Cancel();}
        void OnApplicationPause(bool paused){if(paused)Cancel();}
        void OnDisable(){Cancel();}
        void OnDestroy(){Cancel();if(hud!=null&&hud.Input!=null)hud.Input.ResetTouch-=Cancel;}
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ItemWheelSector : MaskableGraphic
    {
        int index,count;float inner,outer;
        public void Configure(int slot,int total,float min,float max){index=slot;count=total;inner=min;outer=max;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(count<=0)return;
            float width=360f/count,start=90-index*width-width*.5f+1,end=start+width-2;
            int segments=Mathf.Max(4,Mathf.CeilToInt(width/5));
            for(int n=0;n<segments;n++)
            {
                float a=Mathf.Lerp(start,end,n/(float)segments)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(n+1)/(float)segments)*Mathf.Deg2Rad;
                var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));int offset=vh.currentVertCount;
                vh.AddVert(u*outer,color,Vector2.zero);vh.AddVert(v*outer,color,Vector2.zero);vh.AddVert(v*inner,color,Vector2.zero);vh.AddVert(u*inner,color,Vector2.zero);
                vh.AddTriangle(offset,offset+1,offset+2);vh.AddTriangle(offset,offset+2,offset+3);
            }
        }
    }
}
