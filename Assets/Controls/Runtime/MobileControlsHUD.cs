using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.UI;
using CampusRift.Skills;

namespace CampusRift.Controls
{
    public enum MobileSkillState { Ready, Aiming, Casting, Cooldown, Locked, NoCharge, Unavailable }
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class MobileControlsHUD : MonoBehaviour
    {
        public CampusInput Input {get;private set;}
        public ContextInteraction Context {get;private set;}
        public ItemWheelUI Items {get;private set;}
        public RectTransform SafeRoot {get;private set;}
        public float PixelScale => canvas.scaleFactor*controlScale;
        public float RightControlScale => rightScale;
        public float PixelScaleFor(TouchRole role)=>canvas.scaleFactor*ScaleFor(role);
        public static bool IsRightControl(TouchRole role)=>role!=TouchRole.Look&&role!=TouchRole.Move&&role!=TouchRole.Pause&&role!=TouchRole.Interact&&role!=TouchRole.LockOn&&role!=TouchRole.Item;
        float ScaleFor(TouchRole role)=>IsRightControl(role)?rightScale:leftScale;
        public MobileSkillState HandState {get;private set;}
        public MobileSkillState WallState {get;private set;}
        public MobileSkillState PhantomState {get;private set;}
        public readonly List<MobileTouchZone> Zones=new List<MobileTouchZone>();
        sealed class Face {public RectTransform rect,glyph;public RiftGraphic edge;public TMP_Text label,state,seconds;public MobileArc arc;public float pulse;public bool held;
            public SkillRuntime runtime;public bool bound;public SkillState last;public string status;}
        SkillLoadout loadout;
        readonly Dictionary<MobileTouchZone,Vector2> basePositions=new Dictionary<MobileTouchZone,Vector2>();
        readonly Dictionary<TouchRole,Face> faces=new Dictionary<TouchRole,Face>();
        readonly List<GameObject> pcOnly=new List<GameObject>();
        Canvas canvas;CanvasGroup group;RectTransform layer,cancelZone,breakthrough;Vector2 breakthroughPC;
        TMP_Text hint;VoidWallSkill wall;GiantHandSkill hand;PhantomDecoySkill phantom;ElevatorInteraction lift;GameplayHUD hud;
        CampusExplorer explorer;
        GameObject liftPanel;TMP_Text liftTitle;Button[] floors;bool mode,sprintToggle;float nextUI,controlScale=1,rightScale=1,leftScale=1;
        Rect lastSafe;int lastWidth,lastHeight;int previousImpact;
        float lastCanvasScale;Vector2 lastRootSize;Matrix4x4 lastRootTransform;
        static readonly Color Violet=new Color(.64f,.37f,1),Cyan=new Color(.25f,.85f,1),Gold=new Color(1,.73f,.3f),Muted=new Color(.4f,.46f,.57f);
        void Start()
        {
            hud=GetComponent<GameplayHUD>();Input=FindAnyObjectByType<CampusInput>();if(Input==null){enabled=false;return;}
            explorer=Input.GetComponent<CampusExplorer>();
            Context=Input.GetComponent<ContextInteraction>();wall=Input.GetComponent<VoidWallSkill>();hand=Input.GetComponent<GiantHandSkill>();phantom=Input.GetComponent<PhantomDecoySkill>();lift=Input.GetComponent<ElevatorInteraction>();
            loadout=Input.Loadout;if(loadout!=null)loadout.LoadoutChanged+=RefreshSkillFaces;
            canvas=GetComponentInParent<Canvas>();Build();
            pcOnly.Add(hud.Skills.gameObject);
            var w=GetComponent<VoidWallHUD>();var h=GetComponent<GiantHandHUD>();
            if(w!=null)pcOnly.Add(w.instruction.gameObject);if(h!=null)pcOnly.Add(h.instruction.gameObject);
            var p=GetComponent<PhantomDecoyHUD>();if(p!=null && p.instruction!=null)pcOnly.Add(p.instruction.gameObject);
            foreach(var t in GetComponentsInChildren<TMP_Text>(true))if(!string.IsNullOrEmpty(t.text)&&t.text.Contains("WASD"))pcOnly.Add(t.gameObject);
            breakthrough=(RectTransform)hud.Breakthrough.transform;breakthroughPC=breakthrough.anchoredPosition;
            Input.ResetTouch+=ReleaseAll;
            if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed+=Apply;
            if(UIStateManager.Instance!=null)UIStateManager.Instance.Changed+=StateChanged;
            Apply(SettingsManager.Instance!=null?SettingsManager.Instance.Current:SettingsManager.Defaults());
        }
        static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 anchor,Vector2 position)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        static RectTransform Stretch(Transform parent,string name)
        {var r=Rect(parent,name,Vector2.zero,Vector2.zero,Vector2.zero);r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        TMP_Text Text(Transform parent,string name,string value,int size,Vector2 dimensions,Vector2 pos)
        {
            dimensions.y=Mathf.Max(dimensions.y,size*1.4f+10);var r=Rect(parent,name,dimensions,new Vector2(.5f,.5f),pos);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=hud.Skills.GetSlot(1).KeyLabel.font;t.text=value;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=Color.white;t.raycastTarget=false;ComicTheme.Text(t,size>=26);t.margin=new Vector4(3,0,3,0);t.textWrappingMode=TextWrappingModes.NoWrap;t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=Mathf.Max(14,size*.7f);return t;
        }
        RiftGraphic Graphic(RectTransform r,RiftGraphic.Shape shape,Color color,bool raycast=false)
        {var g=r.gameObject.AddComponent<RiftGraphic>();g.Form=shape;g.color=color;g.raycastTarget=raycast;if(shape==RiftGraphic.Shape.Panel)ComicTheme.Frame(r.gameObject,r.name=="Close"?"button-red":"panel");return g;}
        MobileTouchZone Zone(RectTransform r,TouchRole role)
        {var zone=r.gameObject.AddComponent<MobileTouchZone>();zone.role=role;zone.hud=this;Zones.Add(zone);return zone;}
        Face Button(TouchRole role,Vector2 anchor,Vector2 position,float size,string name,Color accent)
        {
            var r=Rect(SafeRoot,role.ToString(),Vector2.one*size,anchor,position);
            Graphic(r,RiftGraphic.Shape.Disc,new Color(.035f,.045f,.085f,.87f),true);Zone(r,role);
            var ink=Graphic(Rect(r,"Ink circle",Vector2.one*size,new Vector2(.5f,.5f),Vector2.zero),RiftGraphic.Shape.Ring,ComicTheme.Ink);ink.Thickness=6;
            var edge=Graphic(Rect(r,"Rift edge",Vector2.one*(size-10),new Vector2(.5f,.5f),Vector2.zero),RiftGraphic.Shape.Ring,accent);edge.Thickness=3;
            var arc=Rect(r,"Cooldown arc",Vector2.one*(size-16),new Vector2(.5f,.5f),Vector2.zero).gameObject.AddComponent<MobileArc>();arc.color=accent;arc.raycastTarget=false;
            // Captions stay inside their circle; cooldown/state is shown by the ring and tint.
            var label=Text(r,"Action",name,18,new Vector2(size-12,30),new Vector2(0,-size*.29f));
            var state=Text(r,"State","",14,new Vector2(size-12,24),Vector2.zero);state.gameObject.SetActive(false);
            var icon=Rect(r,"Glyph",Vector2.one*(size*.36f),new Vector2(.5f,.5f),new Vector2(0,size*.1f));
            var seconds=Text(r,"Cooldown seconds","",32,new Vector2(size*.75f,50),Vector2.zero);seconds.transform.SetAsLastSibling();
            if(MobileTouchZone.IsSlot(role)){}
            else if(role==TouchRole.Hand){var g=icon.gameObject.AddComponent<GiantHandGlyph>();g.raycastTarget=false;g.color=Gold;}
            else if(role==TouchRole.Wall){var g=icon.gameObject.AddComponent<VoidWallGlyph>();g.raycastTarget=false;g.color=Violet;}
            else if(role==TouchRole.Phantom){var g=icon.gameObject.AddComponent<PhantomGlyph>();g.raycastTarget=false;g.color=Violet;}
            else if(role==TouchRole.Item){}
            else {var g=icon.gameObject.AddComponent<MobileGlyph>();g.role=role;g.color=role==TouchRole.Pause?Color.white:accent;g.raycastTarget=false;}
            var face=new Face{rect=r,glyph=icon,edge=edge,label=label,state=state,seconds=seconds,arc=arc};faces.Add(role,face);return face;
        }
        void Build()
        {
            layer=Stretch(transform,"Mobile Controls");var childCanvas=layer.gameObject.AddComponent<Canvas>();childCanvas.overrideSorting=true;childCanvas.sortingOrder=20;
            layer.gameObject.AddComponent<GraphicRaycaster>();group=layer.gameObject.AddComponent<CanvasGroup>();group.ignoreParentGroups=true;
            SafeRoot=Stretch(layer,"Safe Area");
            var look=Stretch(SafeRoot,"Camera swipe region");look.anchorMin=new Vector2(.38f,.06f);look.anchorMax=new Vector2(1,.88f);
            var target=look.gameObject.AddComponent<Image>();target.color=Color.clear;Zone(look,TouchRole.Look);
            var stick=Rect(SafeRoot,"Movement joystick",Vector2.one*230,Vector2.zero,new Vector2(190,210));
            Graphic(stick,RiftGraphic.Shape.Disc,new Color(.035f,.045f,.085f,.55f),true);
            Graphic(Rect(stick,"Orbit",Vector2.one*214,new Vector2(.5f,.5f),Vector2.zero),RiftGraphic.Shape.Ring,Cyan);
            var knob=Rect(stick,"Thumb",Vector2.one*84,new Vector2(.5f,.5f),Vector2.zero);Graphic(knob,RiftGraphic.Shape.Disc,new Color(.35f,.8f,1,.8f));
            Zone(stick,TouchRole.Move).knob=knob;Text(stick,"Move hint","MOVE",20,new Vector2(230,30),new Vector2(0,-143));
            // Left thumb: movement, utility actions and one quick-item button.
            Button(TouchRole.Interact,Vector2.zero,new Vector2(440,450),96,"USE",Cyan);
            Button(TouchRole.LockOn,Vector2.zero,new Vector2(140,490),96,"LOCK",Cyan);
            var item=Button(TouchRole.Item,Vector2.zero,new Vector2(440,210),112,"ITEMS",Gold);
            Items=gameObject.AddComponent<ItemWheelUI>();Items.Initialize(this,item.rect,item.glyph,item.label,item.arc);
            // Right thumb: an arc around the largest Attack circle, plus edge height for special actions.
            Button(TouchRole.Attack,Vector2.right,new Vector2(-150,190),210,"ATTACK",Gold);
            Button(TouchRole.Dash,Vector2.right,new Vector2(-350,125),96,"DASH",Cyan);
            Button(TouchRole.Jump,Vector2.right,new Vector2(-110,420),112,"JUMP",Cyan);
            Button(TouchRole.Sprint,Vector2.right,new Vector2(-300,425),104,"BOOST",Cyan);
            Button(TouchRole.Skill1,Vector2.right,new Vector2(-510,265),128,"",Violet);
            Button(TouchRole.Skill2,Vector2.right,new Vector2(-475,475),128,"",Violet);
            Button(TouchRole.Skill3,Vector2.right,new Vector2(-310,665),128,"",Violet);
            Button(TouchRole.Skill4,Vector2.right,new Vector2(-130,670),140,"",Gold);
            Button(TouchRole.Ultimate,Vector2.right,new Vector2(-490,845),128,"SWORD",Gold);
            RefreshSkillFaces();
            Button(TouchRole.Pause,new Vector2(0,1),new Vector2(100,-350),88,"",Muted);
            cancelZone=Button(TouchRole.Cancel,Vector2.right,new Vector2(-130,875),88,"CANCEL",new Color(1,.38f,.43f)).rect;
            hint=Text(SafeRoot,"Gesture hint","",24,new Vector2(780,54),Vector2.zero);hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,0);hint.rectTransform.anchoredPosition=new Vector2(0,54);
            hint.fontSizeMin=12;
            BuildLift();foreach(var zone in Zones)basePositions[zone]=((RectTransform)zone.transform).anchoredPosition;
        }
        void BuildLift()
        {
            var panel=Rect(SafeRoot,"Lift floor selection",new Vector2(660,750),new Vector2(.5f,.5f),Vector2.zero);liftPanel=panel.gameObject;
            Graphic(panel,RiftGraphic.Shape.Panel,new Color(.025f,.035f,.065f,.98f),true);
            liftTitle=Text(panel,"Title","SELECT FLOOR",32,new Vector2(620,90),new Vector2(0,320));floors=new Button[12];
            for(int i=0;i<floors.Length;i++)
            {
                int floor=i;var r=Rect(panel,"Floor "+i,new Vector2(170,105),new Vector2(.5f,.5f),new Vector2((i%3-1)*195,200-(i/3)*130));
                var g=Graphic(r,RiftGraphic.Shape.Panel,new Color(.14f,.12f,.25f),true);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=g;
                Text(r,"Label",(i+1).ToString("00"),32,new Vector2(150,80),Vector2.zero);b.onClick.AddListener(()=>lift.SelectFloor(floor));floors[i]=b;
            }
            var close=Rect(panel,"Close",new Vector2(560,96),new Vector2(.5f,.5f),new Vector2(0,-300));var bg=Graphic(close,RiftGraphic.Shape.Panel,new Color(.19f,.12f,.3f),true);
            var button=close.gameObject.AddComponent<Button>();button.targetGraphic=bg;button.onClick.AddListener(()=>lift.ClosePanel());Text(close,"Label","CLOSE",26,new Vector2(500,70),Vector2.zero);
            liftPanel.SetActive(false);
        }
        void Apply(GameSettings settings)
        {
            mode=CampusInput.Mobile;controlScale=settings.MobileControlScale;group.alpha=settings.MobileOpacity;
            foreach(var go in pcOnly)if(go!=null)go.SetActive(!mode);
            breakthrough.anchoredPosition=mode?breakthroughPC+new Vector2(0,480):breakthroughPC;
            UpdateVisibility();Layout();
        }
        void StateChanged(UIState state){UpdateVisibility();}
        void UpdateVisibility()
        {
            bool gameplay=UIStateManager.Instance==null||UIStateManager.Instance.GameplayInputEnabled;
            bool modal=UIStateManager.Instance!=null&&UIStateManager.Instance.State==UIState.Modal&&lift!=null&&lift.PanelOpen;
            layer.gameObject.SetActive(mode&&(gameplay||modal));
            foreach(var zone in Zones)zone.gameObject.SetActive(gameplay);
            liftPanel.SetActive(mode&&modal);hint.gameObject.SetActive(gameplay);
            if(modal){liftTitle.text="LIFT "+lift.PanelElevator.building+" / SELECT FLOOR";for(int i=0;i<floors.Length;i++)floors[i].gameObject.SetActive(i<lift.PanelElevator.FloorCount);}
        }
        void Layout()
        {
            lastSafe=Screen.safeArea;lastWidth=Screen.width;lastHeight=Screen.height;
            ApplySafeArea(lastSafe);
            lastCanvasScale=canvas.scaleFactor;lastRootSize=SafeRoot.rect.size;lastRootTransform=SafeRoot.localToWorldMatrix;
        }
        public void ApplySafeArea(Rect safe)
        {
            SafeRoot.anchorMin=safe.min/new Vector2(Screen.width,Screen.height);SafeRoot.anchorMax=safe.max/new Vector2(Screen.width,Screen.height);
            SafeRoot.offsetMin=SafeRoot.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();
            float unit=Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null,SafeRoot.TransformPoint(Vector3.zero)),
                RectTransformUtility.WorldToScreenPoint(null,SafeRoot.TransformPoint(Vector3.right)));
            // Use screen percentages, not the authored HUD width: its centered rect
            // has side margins at wider aspect ratios. Reserve all captions and pulse.
            Vector2 clusterMin=new Vector2(float.PositiveInfinity,float.PositiveInfinity),clusterMax=-clusterMin;
            foreach(var zone in Zones)
            {
                if(!IsRightControl(zone.role))continue;
                var bounds=ControlLocalBounds(zone);
                clusterMin=Vector2.Min(clusterMin,basePositions[zone]+bounds.min*1.07f);
                clusterMax=Vector2.Max(clusterMax,basePositions[zone]+bounds.max*1.07f);
            }
            var right=UnityEngine.Rect.MinMaxRect(Mathf.Max(safe.xMin,Screen.width*.64f)+12,safe.yMin+12,
                safe.xMax-12,Mathf.Min(safe.yMax-12,Screen.height*.82f));
            var extent=clusterMax-clusterMin;
            rightScale=Mathf.Min(controlScale,right.width/(extent.x*unit),right.height/(extent.y*unit));
            Vector2 origin=new Vector2(right.xMax-clusterMax.x*rightScale*unit,right.yMin-clusterMin.y*rightScale*unit);
            var left=UnityEngine.Rect.MinMaxRect(safe.xMin+1,safe.yMin+1,Mathf.Min(safe.xMax,Screen.width*.30f)-1,safe.yMax-1);
            // Keep the left cluster coherent at larger user scales instead of clamping
            // circles individually into one another. Pause is separately tied to Vitals.
            float leftOrigin=RectTransformUtility.WorldToScreenPoint(null,SafeRoot.TransformPoint(SafeRoot.rect.min)).x;
            leftScale=Mathf.Min(controlScale,(left.xMax-Mathf.Max(left.xMin,leftOrigin))/(540*unit),left.height/(580*unit));
            foreach(var zone in Zones)
            {
                if(zone.role==TouchRole.Look)continue;
                float scale=ScaleFor(zone.role);zone.transform.localScale=Vector3.one*scale;
                var r=(RectTransform)zone.transform;var bounds=ControlLocalBounds(zone);
                Vector2 center;
                if(IsRightControl(zone.role))center=origin+basePositions[zone]*rightScale*unit;
                else
                {
                    var local=SafeRoot.rect.min+Vector2.Scale(r.anchorMin,SafeRoot.rect.size)+basePositions[zone]*scale;
                    center=RectTransformUtility.WorldToScreenPoint(null,SafeRoot.TransformPoint(local));
                    center=ClampCenter(center,bounds,scale*unit,left);
                }
                SetScreenCenter(r,center);
            }
            // Vitals does not scale with the touch controls. Place Pause below its
            // lowest graphic (including the Spirit row), instead of scaling a fixed gap.
            var vitals=hud.transform.Find("Vitals");
            if(vitals!=null)
            {
                float bottom=float.PositiveInfinity;var corners=new Vector3[4];
                foreach(var graphic in vitals.GetComponentsInChildren<Graphic>(true))
                {
                    graphic.rectTransform.GetWorldCorners(corners);
                    foreach(var corner in corners)bottom=Mathf.Min(bottom,RectTransformUtility.WorldToScreenPoint(null,corner).y);
                }
                if(!float.IsInfinity(bottom))
                {
                    var pause=faces[TouchRole.Pause].rect;
                    var bounds=ControlLocalBounds(pause.GetComponent<MobileTouchZone>());
                    var center=RectTransformUtility.WorldToScreenPoint(null,pause.position);
                    center.y=bottom-bounds.yMax*leftScale*unit*1.07f-16*unit;
                    SetScreenCenter(pause,ClampCenter(center,bounds,leftScale*unit,left));
                }
            }
            // The bottom hint stays in the exempt center strip, with room between
            // its rectangle and both clusters even for long translated text.
            hint.rectTransform.sizeDelta=new Vector2(Screen.width*.30f/unit,54);
            SetScreenCenter(hint.rectTransform,new Vector2(Screen.width*.48f,safe.yMin+Mathf.Max(30,42*unit)));
        }
        static Vector2 ClampCenter(Vector2 center,Rect bounds,float scale,Rect allowed)
        {
            var min=bounds.min*scale*1.07f;var max=bounds.max*scale*1.07f;
            return new Vector2(Mathf.Clamp(center.x,allowed.xMin-min.x,allowed.xMax-max.x),
                Mathf.Clamp(center.y,allowed.yMin-min.y,allowed.yMax-max.y));
        }
        void SetScreenCenter(RectTransform rect,Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(SafeRoot,screen,null,out var local);
            rect.anchoredPosition=local-(SafeRoot.rect.min+Vector2.Scale(rect.anchorMin,SafeRoot.rect.size));
        }
        void Update()
        {
            if(Input==null||!mode)return;
            if(Screen.width!=lastWidth||Screen.height!=lastHeight||Screen.safeArea!=lastSafe)Layout();
            foreach(var f in faces.Values)
            {
                f.pulse=Mathf.MoveTowards(f.pulse,0,Time.unscaledDeltaTime*2);
                float scale=ScaleFor(f.rect.GetComponent<MobileTouchZone>().role)*(f.held?.93f:1+f.pulse*.07f);
                if(Mathf.Abs(f.rect.localScale.x-scale)>.001f)f.rect.localScale=Vector3.one*scale;
            }
            if(Time.unscaledTime<nextUI)return;nextUI=Time.unscaledTime+.08f;
            bool aiming=Input.Aiming.HasValue;cancelZone.gameObject.SetActive(aiming);
            HandState=StateOf(CampusAction.Hand);WallState=StateOf(CampusAction.Wall);PhantomState=StateOf(CampusAction.Phantom);
            for(int i=0;i<SkillLoadout.SlotCount;i++)UpdateSlotFace(TouchRole.Skill1+i);
            if(hand!=null&&hand.ImpactCount!=previousImpact){previousImpact=hand.ImpactCount;MobileFeedback.Confirm();}
            SetText(faces[TouchRole.Interact].label,LevelHUD.Vietnamese?"DÙNG":"USE");faces[TouchRole.Interact].edge.color=Context.Available?Cyan:Muted;
            if(explorer!=null)
            {
                var sprintFace=faces[TouchRole.Sprint];
                SetText(sprintFace.state,"");
                sprintFace.arc.SetFill(explorer.EnergyFraction);
                sprintFace.edge.color=explorer.BoostExhausted?Muted:Cyan;
            }
            bool vi=LevelHUD.Vietnamese;
            SetText(faces[TouchRole.Attack].label,vi?"ĐÁNH":"ATTACK");
            SetText(faces[TouchRole.Dash].label,vi?"NÉ":"DASH");SetText(faces[TouchRole.Jump].label,vi?"NHẢY":"JUMP");
            SetText(faces[TouchRole.LockOn].label,vi?"KHÓA":"LOCK");SetText(faces[TouchRole.Sprint].label,vi?"TỐC":"BOOST");
            SetText(faces[TouchRole.Cancel].label,vi?"HỦY":"CANCEL");
            string text=Input.Aiming==CampusAction.Hand?HandState==MobileSkillState.Unavailable?hand.Target.reason:"NEAREST MONSTER LOCKED / RELEASE TO CAST / SLIDE TO CANCEL":
                Input.Aiming==CampusAction.Wall?$"DRAG DOWN: NEAR / UP: FAR / SIDE: TURN   {wall.PreviewDistance:0.0}m":
                aiming?"DRAG TO AIM / RELEASE TO CAST / SLIDE TO CANCEL":"HOLD A SKILL TO AIM • HAND AUTO LOCKS";
            SetText(hint,aiming?(LevelHUD.Vietnamese?"KÉO ĐỂ NGẮM • THẢ ĐỂ DÙNG • KÉO VÀO HỦY":text):" ");UpdateUltimateFace();
        }
        void UpdateUltimateFace()
        {
            if(!faces.TryGetValue(TouchRole.Ultimate,out var f))return;
            var u=SkyBeast.HeavenSwordUltimate.Instance;bool show=u!=null&&u.Visible;
            f.rect.gameObject.SetActive(show);if(!show)return;
            bool vi=LevelHUD.Vietnamese;SetText(f.label,vi?"T.KIẾM":"SWORD");SetText(f.state,UI.SwordIntentUI.StateLabel(u.State,vi));
            f.arc.SetFill(u.Channeling?u.ChannelProgress:u.Intent!=null?u.Intent.Fraction:0);
            bool ready=u.State==SkyBeast.HeavenSwordUltimate.ButtonState.Ready;f.edge.color=ready&&SettingsManager.Instance?.Current.ReduceSkillFlashes!=true?Color.Lerp(Gold,Color.white,(Mathf.Sin(Time.unscaledTime*5)+1)*.35f):u.State==SkyBeast.HeavenSwordUltimate.ButtonState.Blocked?ComicTheme.Red:Gold;
        }
        static void SetText(TMP_Text target,string value){if(target.text!=value)target.text=value;}
        public static MobileSkillState ToMobile(SkillState s)=>s==SkillState.NoSpirit?MobileSkillState.Unavailable:(MobileSkillState)(int)s;
        // State of the slot holding a skill identity; an unequipped skill reads as Locked.
        public MobileSkillState StateOf(CampusAction identity)
        {
            var r=Input.RuntimeFor(identity);return r==null?MobileSkillState.Locked:ToMobile(r.GetState());
        }
        // Touch role of the slot that currently holds a skill identity (tests and tutorials locate buttons with it).
        public TouchRole RoleFor(CampusAction identity)
        {
            int slot=loadout!=null?loadout.SlotOfAction(identity):-1;return slot<0?TouchRole.Cancel:TouchRole.Skill1+slot;
        }
        void RefreshSkillFaces()
        {
            for(int i=0;i<SkillLoadout.SlotCount;i++)
            {
                if(!faces.TryGetValue(TouchRole.Skill1+i,out var face))continue;
                var runtime=loadout!=null?loadout.Get(i):null;
                if(face.bound&&face.runtime==runtime)continue;
                face.runtime=runtime;face.bound=true;face.status=null;
                for(int c=face.glyph.childCount-1;c>=0;c--)Destroy(face.glyph.GetChild(c).gameObject);
                foreach(var g in face.glyph.GetComponents<Graphic>())Destroy(g);
                SetText(face.label,runtime!=null?CompactSkillName(runtime.ShortName):"—");
                if(runtime==null){face.edge.color=Muted;SetText(face.state,"");face.arc.SetFill(0);continue;}
                var glyphRect=Rect(face.glyph,"Skill glyph",Vector2.zero,Vector2.zero,Vector2.zero);glyphRect.anchorMax=Vector2.one;
                if(runtime.Definition!=null&&runtime.Definition.icon!=null)
                {var image=glyphRect.gameObject.AddComponent<Image>();image.sprite=runtime.Definition.icon;image.preserveAspect=true;image.raycastTarget=false;glyphRect.localScale=Vector3.one*1.5f;}
                else if(runtime.GlyphType!=null&&typeof(Graphic).IsAssignableFrom(runtime.GlyphType))
                {var g=(Graphic)glyphRect.gameObject.AddComponent(runtime.GlyphType);g.color=runtime.Accent;g.raycastTarget=false;}
                var mark=Rect(face.glyph,"Element shape",new Vector2(30,30),new Vector2(1,1),new Vector2(-12,-12));var markImage=mark.gameObject.AddComponent<Image>();markImage.sprite=Accessibility.SymbolSprite(runtime.Definition!=null?runtime.Definition.element:CampusRift.Combat.Element.None);markImage.raycastTarget=false;
            }
            nextUI=0;
        }
        void UpdateSlotFace(TouchRole role)
        {
            if(!faces.TryGetValue(role,out var f)||f.runtime==null)return;
            var r=f.runtime;SetText(f.label,CompactSkillName(r.ShortName));var state=r.GetState();var mobile=ToMobile(state);
            f.edge.color=mobile==MobileSkillState.Unavailable?new Color(1,.35f,.4f):mobile==MobileSkillState.Cooldown||mobile==MobileSkillState.Locked||mobile==MobileSkillState.NoCharge?Muted:r.Accent;
            f.arc.SetFill(r.CooldownRemaining/Mathf.Max(.01f,r.CooldownDuration));
            bool cooling=mobile==MobileSkillState.Cooldown;
            SetText(f.seconds,cooling?Mathf.CeilToInt(r.CooldownRemaining)+"s":"");f.glyph.gameObject.SetActive(!cooling);
            string status=r.StatusText;
            if(state==SkillState.NoSpirit)status=LevelHUD.Vietnamese?"THIẾU LINH LỰC":"NO SPIRIT";
            if(!string.IsNullOrEmpty(status))
            {if(f.status!=null&&status!=f.status)f.pulse=1;f.status=status;SetText(f.state,status);}
            else SetText(f.state,mobile==MobileSkillState.Cooldown?Mathf.CeilToInt(r.CooldownRemaining)+"s":mobile==MobileSkillState.NoCharge?"EMPTY":mobile.ToString().ToUpperInvariant());
            if(f.last==SkillState.Cooldown&&state==SkillState.Ready)f.pulse=1;f.last=state;
        }
        static string CompactSkillName(string name)
        {
            if(string.IsNullOrEmpty(name))return "—";
            if(name.Contains("HỎA LIÊN"))return "HỎA LIÊN";
            if(name.Contains("NGŨ LÔI"))return "NGŨ LÔI";
            if(name.Contains("HÀN BĂNG"))return "HÀN BĂNG";
            if(name.Contains("KIM CHUNG"))return "KIM CHUNG";
            if(name.Contains("HƯ KHÔNG BÍCH")||name=="VOID WALL")return LevelHUD.Vietnamese?"HƯ KHÔNG":"WALL";
            return name;
        }
        void LateUpdate()
        {
            // FitFrame can change an ancestor scale/position while the canvas scale
            // and local safe-root size stay identical. Run after it and include
            // the complete transform when deciding to reapply screen positions.
            if(mode&&canvas!=null&&SafeRoot!=null&&
                (!Mathf.Approximately(canvas.scaleFactor,lastCanvasScale)||SafeRoot.rect.size!=lastRootSize||SafeRoot.localToWorldMatrix!=lastRootTransform))Layout();
        }
        public void PressBoost()
        {
            bool toggle=SettingsManager.Instance!=null&&SettingsManager.Instance.Current.BoostToggle;
            if(toggle)sprintToggle=!sprintToggle;
            Input.TouchSprint=toggle?sprintToggle:true;
        }
        static Rect ControlLocalBounds(MobileTouchZone zone)
        {
            var r=(RectTransform)zone.transform;var min=r.rect.min;var max=r.rect.max;
            var corners=new Vector3[4];
            // Include empty state rows too: cooldown/locked/no-spirit text must fit later.
            foreach(var text in zone.GetComponentsInChildren<TMP_Text>(true))
            {
                text.rectTransform.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    Vector2 p=r.InverseTransformPoint(corner);
                    min=Vector2.Min(min,p-new Vector2(6,3));max=Vector2.Max(max,p+new Vector2(6,3));
                }
            }
            return UnityEngine.Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        public Rect ControlScreenBounds(MobileTouchZone zone)
        {
            var r=(RectTransform)zone.transform;var bounds=ControlLocalBounds(zone);
            float pulse=ScaleFor(zone.role)*1.07f/r.localScale.x;
            Vector2 min=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(bounds.min*pulse));
            Vector2 max=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(bounds.max*pulse));
            return UnityEngine.Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        public void ReleaseBoost(){Input.TouchSprint=sprintToggle;}
        public void PressVisual(TouchRole role,bool held){if(faces.TryGetValue(role,out var face)){face.held=held;if(held)face.pulse=1;}}
        public void FlashUnavailable(TouchRole role){if(faces.TryGetValue(role,out var face)){face.pulse=1;SetText(face.state,"UNAVAILABLE");}}
        public void ShowAim(){cancelZone.gameObject.SetActive(true);nextUI=0;}
        public bool InCancelZone(Vector2 screen)=>cancelZone.gameObject.activeInHierarchy&&cancelZone.GetComponent<MobileTouchZone>().IsRaycastLocationValid(screen,null);
        public void SetCancelVisual(bool cancel){faces[TouchRole.Cancel].edge.color=cancel?Color.white:new Color(1,.38f,.43f);}
        void ReleaseAll(){sprintToggle=false;foreach(var z in Zones)z.Release();}
        void OnDestroy()
        {if(loadout!=null)loadout.LoadoutChanged-=RefreshSkillFaces;if(Input!=null)Input.ResetTouch-=ReleaseAll;if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed-=Apply;if(UIStateManager.Instance!=null)UIStateManager.Instance.Changed-=StateChanged;}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MobileArc : MaskableGraphic
    {
        float fill;
        public void SetFill(float value){value=Mathf.Clamp01(value);if(Mathf.Abs(fill-value)<.002f)return;fill=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float radius=rectTransform.rect.width*.5f;
            int count=Mathf.CeilToInt(fill*60);
            for(int n=0;n<count;n++)
            {
                float a=Mathf.PI*.5f-n*Mathf.PI*2/60,b=Mathf.PI*.5f-Mathf.Min(n+1,fill*60)*Mathf.PI*2/60;
                Vector2 u=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));int start=vh.currentVertCount;
                vh.AddVert(u*radius,color,Vector2.zero);vh.AddVert(v*radius,color,Vector2.zero);vh.AddVert(v*(radius-8),color,Vector2.zero);vh.AddVert(u*(radius-8),color,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MobileGlyph : MaskableGraphic
    {
        public TouchRole role;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(role==TouchRole.Pause){Line(vh,new Vector2(.3f,.15f),new Vector2(.3f,.85f));Line(vh,new Vector2(.7f,.15f),new Vector2(.7f,.85f));}
            else if(role==TouchRole.Cancel){Line(vh,new Vector2(.2f,.2f),new Vector2(.8f,.8f));Line(vh,new Vector2(.2f,.8f),new Vector2(.8f,.2f));}
            else if(role==TouchRole.Interact){Line(vh,new Vector2(.2f,.15f),new Vector2(.2f,.85f));Line(vh,new Vector2(.2f,.85f),new Vector2(.8f,.85f));Line(vh,new Vector2(.8f,.85f),new Vector2(.8f,.15f));Line(vh,new Vector2(.35f,.5f),new Vector2(.7f,.5f));}
            else if(role==TouchRole.Ultimate){Line(vh,new Vector2(.5f,.28f),new Vector2(.5f,.94f));Line(vh,new Vector2(.5f,.94f),new Vector2(.35f,.72f));Line(vh,new Vector2(.5f,.94f),new Vector2(.65f,.72f));Line(vh,new Vector2(.35f,.72f),new Vector2(.35f,.36f));Line(vh,new Vector2(.65f,.72f),new Vector2(.65f,.36f));Line(vh,new Vector2(.24f,.32f),new Vector2(.76f,.32f));Line(vh,new Vector2(.5f,.12f),new Vector2(.5f,.3f));Line(vh,new Vector2(.39f,.1f),new Vector2(.61f,.1f));}
            else if(role==TouchRole.Attack){Line(vh,new Vector2(.22f,.2f),new Vector2(.78f,.8f));Line(vh,new Vector2(.36f,.62f),new Vector2(.62f,.36f));Line(vh,new Vector2(.14f,.14f),new Vector2(.3f,.3f));}
            else if(role==TouchRole.Dash){Line(vh,new Vector2(.12f,.22f),new Vector2(.38f,.5f));Line(vh,new Vector2(.38f,.5f),new Vector2(.12f,.78f));Line(vh,new Vector2(.5f,.22f),new Vector2(.76f,.5f));Line(vh,new Vector2(.76f,.5f),new Vector2(.5f,.78f));}
            else if(role==TouchRole.LockOn){foreach(var sx in new[]{.2f,.8f})foreach(var sy in new[]{.2f,.8f}){Line(vh,new Vector2(sx,sy),new Vector2(sx+(sx<.5f?.18f:-.18f),sy));Line(vh,new Vector2(sx,sy),new Vector2(sx,sy+(sy<.5f?.18f:-.18f)));}}
            else{Line(vh,new Vector2(.15f,.5f),new Vector2(.5f,.85f));Line(vh,new Vector2(.5f,.85f),new Vector2(.85f,.5f));Line(vh,new Vector2(.5f,.85f),new Vector2(.5f,.15f));if(role==TouchRole.Sprint){Line(vh,new Vector2(.1f,.18f),new Vector2(.28f,.36f));Line(vh,new Vector2(.72f,.18f),new Vector2(.9f,.36f));}}
        }
        void Line(VertexHelper vh,Vector2 a,Vector2 b)
        {
            var rect=rectTransform.rect;a=rect.min+Vector2.Scale(a,rect.size);b=rect.min+Vector2.Scale(b,rect.size);Vector2 d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*rect.width*.045f;int start=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
