using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARBattleHUD:MonoBehaviour
    {
        ARBattlefield field;ARMonsterDirector director;ARSkillCaster caster;ARSessionBootstrap consent;ARModeSelectionHUD selection;bool welcomeShown;TMP_Text resultStats;
        RectTransform canvas,content,statusPanel,rail,preview,placementPanel,modes,menu,help,result,exitConfirm,toastPanel,comboPanel,castPanel,scan;
        TMP_Text status,message,toast,combo,castText,resultTitle,modeLabel,pauseLabel;
        Image hp,spirit,handDot,centerReticle;float handUntil;Button placeButton,tableButton,floorButton;CanvasGroup menuGroup;Button dismiss;
        public bool MenuOpen {get;private set;}public bool HelpOpen {get;private set;}
        public bool ModalOpen=>MenuOpen||HelpOpen||exitOpen||(selection?.Blocking??false)||(GetComponent<ARTechHUD>()?.Blocking??false)||(GetComponent<ARCombatHUD>()?.SafetyOpen??false)||(director!=null&&director.Finished);
        bool exitOpen;float menuAmount,toastUntil,comboUntil,castUntil;int[] hints=new int[3];bool[] previousStatuses=new bool[3];float[] shownUntil=new float[5],flash=new float[5],shake=new float[5];
        readonly ARArcGraphic[] cooldown=new ARArcGraphic[5],charge=new ARArcGraphic[5];readonly RectTransform[] icons=new RectTransform[5],namePanels=new RectTransform[5];
        readonly TMP_Text[] names=new TMP_Text[5];readonly Image[] circles=new Image[5];ARLandmarkGraphic landmarks;AudioSource pressSource;AudioClip pressClip;
        readonly List<RectTransform> coverage=new List<RectTransform>();
        static string L(string vi,string en)=>LevelHUD.Vietnamese?vi:en;
        static readonly string[] vi={"THIÊN THỦ","HẮC ĐỘNG","NGỰ LÔI","VẠN KIẾM","HÀN BĂNG"},en={"GIANT HAND","BLACK HOLE","LIGHTNING","SWORD RAIN","ICE SEAL"};
        void Start()
        {
            field=GetComponent<ARBattlefield>();director=GetComponent<ARMonsterDirector>();caster=GetComponent<ARSkillCaster>();consent=FindAnyObjectByType<ARSessionBootstrap>();
            gameObject.AddComponent<ARDeveloperConsole>();
            gameObject.AddComponent<ARHUDBudget>();
            gameObject.AddComponent<ARTechSettings>();gameObject.AddComponent<ARDepthCollision>();gameObject.AddComponent<ARVoiceCommands>();gameObject.AddComponent<ARClipRecorder>();gameObject.AddComponent<ARTechHUD>();
            gameObject.AddComponent<ARCombatHUD>();selection=GetComponent<ARModeSelectionHUD>()??gameObject.AddComponent<ARModeSelectionHUD>();gameObject.AddComponent<ARSealTrail>();
            gameObject.AddComponent<ARHandInteractions>();gameObject.AddComponent<ARKnowledgeSeal>();gameObject.AddComponent<ARSealPractice>();gameObject.AddComponent<ARHandsStudyHUD>();
            gameObject.AddComponent<ARSpaceModes>();gameObject.AddComponent<ARSpaceHUD>();gameObject.AddComponent<ARRoomProbe>();
            canvas=ARUI.Canvas(transform,"AR Battle HUD");content=ARUI.Rect(canvas,"AR HUD content",0,0,1920,1080);
            var menuButton=ARUI.Round(content,"AR menu","",-860,450,88,()=>SetMenu(!MenuOpen));coverage.Add((RectTransform)menuButton.transform);
            for(int i=0;i<3;i++){var bar=ARUI.Rect(menuButton.transform,"Menu line",0,15-i*15,36,4).gameObject.AddComponent<Image>();bar.color=ComicTheme.Paper;bar.raycastTarget=false;}
            statusPanel=ARUI.Panel(content,"Shrine status",0,464,520,56);coverage.Add(statusPanel);status=ARUI.Text(statusPanel,"",0,2,488,40,20);
            hp=Bar(statusPanel,"HP",0,-15,472,7,ComicTheme.Gold);spirit=Bar(content,"Spirit",0,423,472,5,ComicTheme.Purple);coverage.Add((RectTransform)spirit.transform.parent);
            rail=ARUI.Rect(content,"Gesture rail",0,0,1920,1080);
            for(int i=0;i<5;i++)
            {
                var icon=ARUI.Round(rail,"Gesture icon "+i,"",860,270-i*112,84,()=>{});icon.transition=Selectable.Transition.None;icon.interactable=false;icons[i]=(RectTransform)icon.transform;circles[i]=(Image)icon.targetGraphic;circles[i].raycastTarget=false;
                var pic=ARUI.Rect(icons[i],"Skill icon",0,0,60,60).gameObject.AddComponent<Image>();pic.sprite=HubUI.SkillIcon(caster.map.Find(GestureSkillMapper.Labels[i]));pic.preserveAspect=true;pic.raycastTarget=false;
                var badge=ARUI.Rect(icons[i],"Gesture badge",31,-29,32,32);var bg=badge.gameObject.AddComponent<Image>();bg.sprite=ComicTheme.Sprite("round-mask");bg.color=ComicTheme.Ink;bg.raycastTarget=false;var hand=ARUI.Rect(badge,"Hand",0,0,26,26).gameObject.AddComponent<ARHandGraphic>();hand.Gesture=i;hand.raycastTarget=false;
                cooldown[i]=ARUI.Rect(icons[i],"Cooldown",0,0,84,84).gameObject.AddComponent<ARArcGraphic>();cooldown[i].color=ComicTheme.Purple;cooldown[i].raycastTarget=false;
                charge[i]=ARUI.Rect(icons[i],"Evidence",0,0,96,96).gameObject.AddComponent<ARArcGraphic>();charge[i].color=ComicTheme.Gold;charge[i].raycastTarget=false;coverage.Add(charge[i].rectTransform);
                namePanels[i]=ARUI.Panel(rail,"Skill name "+i,650,270-i*112,290,54);coverage.Add(namePanels[i]);names[i]=ARUI.Text(namePanels[i],"",0,0,270,48,21);namePanels[i].gameObject.SetActive(false);
            }
            var q=ARUI.Round(rail,"Gesture help","?",860,-390,56,()=>SetHelp(!HelpOpen));coverage.Add((RectTransform)q.transform);
            centerReticle=ARUI.Rect(content,"Center aim",0,0,22,22).gameObject.AddComponent<Image>();centerReticle.sprite=ComicTheme.Sprite("round-mask");centerReticle.raycastTarget=false;
            handDot=ARUI.Rect(rail,"Hand detected",796,329,28,28).gameObject.AddComponent<Image>();handDot.sprite=ComicTheme.Sprite("round-mask");handDot.raycastTarget=false;coverage.Add(handDot.rectTransform);
            placementPanel=ARUI.Panel(content,"Placement guidance",0,447,1050,66);message=ARUI.Text(placementPanel,"",0,0,1015,58,24);
            modes=ARUI.Panel(content,"Placement mode",0,353,288,70);tableButton=ARUI.Round(modes,"Table mode",L("BÀN","TABLE"),-70,0,64,()=>field.placement.SetFloor(false));floorButton=ARUI.Round(modes,"Floor mode",L("SÀN","FLOOR"),70,0,64,()=>field.placement.SetFloor(true));
            placeButton=ARUI.Round(content,"Place battlefield",L("ĐẶT\nTRẬN","PLACE"),0,-425,142,()=>{if(field.placement.Adjusting)field.placement.StartBattlefield();else field.placement.Confirm();});
            scan=ARUI.Rect(content,"Animated phone scan",0,30,100,150);var phone=ARUI.Panel(scan,"Phone",0,0,60,96);ARUI.Text(phone,"~",0,0,52,76,42);
            preview=ARUI.Panel(content,"Hand camera preview",-650,-465,220,104);var video=ARUI.Rect(preview,"In-memory camera",0,0,208,92).gameObject.AddComponent<ARCameraPreview>();video.sampler=GetComponent<FrameSampler>();video.raycastTarget=false;landmarks=ARUI.Rect(video.transform,"21 landmarks",0,0,208,92).gameObject.AddComponent<ARLandmarkGraphic>();landmarks.color=ComicTheme.Green;landmarks.raycastTarget=false;preview.gameObject.SetActive(false);coverage.Add(preview);
            toastPanel=ARUI.Panel(content,"Feedback toast",0,-340,680,62);toast=ARUI.Text(toastPanel,"",0,0,650,54,23);coverage.Add(toastPanel);
            comboPanel=ARUI.Panel(content,"Combo toast",0,-260,720,62);combo=ARUI.Text(comboPanel,"",0,0,690,54,23);coverage.Add(comboPanel);
            castPanel=ARUI.Panel(content,"Cast name",0,-164,660,76);castText=ARUI.Text(castPanel,"",0,0,630,68,32);coverage.Add(castPanel);
            // Transparent raycast backdrop: outside taps close popovers without placing a Rift.
            dismiss=ARUI.Rect(content,"Dismiss popover",0,0,1920,1080).gameObject.AddComponent<Button>();var transparent=dismiss.gameObject.AddComponent<Image>();transparent.color=Color.clear;transparent.raycastTarget=true;dismiss.targetGraphic=transparent;dismiss.onClick.AddListener(()=>{SetMenu(false);SetHelp(false);exitOpen=false;});
            menu=ARUI.Panel(content,"Menu popover",-678,-15,416,790);menu.GetComponent<Image>().raycastTarget=true;menuGroup=menu.gameObject.AddComponent<CanvasGroup>();
            MenuItem(L("Chọn chế độ","Choose game mode"),"M",350,()=>{SetMenu(false);SetHelp(false);selection.Open();});
            pauseLabel=MenuItem("Pause","II",280,()=>field.UserPaused=!field.UserPaused);
            MenuItem(L("Đổi vị trí","Reposition"),"+",210,()=>{SetMenu(false);field.UserPaused=false;field.placement.Reposition();});
            modeLabel=MenuItem("Mode","<>" ,140,()=>{field.UserPaused=false;field.placement.SetFloor(!field.placement.settings.Floor);SetMenu(false);});
            MenuItem(L("Xem tay","Hand view"),"O",70,()=>{preview.gameObject.SetActive(!preview.gameObject.activeSelf);SetMenu(false);});
            MenuItem(L("Công nghệ AR","AR technology"),"T",0,()=>GetComponent<ARTechHUD>().Open(true));
            MenuItem(L("Quay / dừng clip","Record / stop clip"),"R",-70,()=>{SetMenu(false);GetComponent<ARTechHUD>().Record();});
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            var check=GetComponent<ARGestureCheck>()??gameObject.AddComponent<ARGestureCheck>();
            MenuItem("[ARDiag]","D",-140,()=>{SetMenu(false);FindAnyObjectByType<ARDeviceDiagnostics>()?.Toggle();});
            MenuItem("Kiểm Ấn","?",-210,()=>{SetMenu(false);SetHelp(false);check.Open();});
#endif
            MenuItem(L("Chủ động / an toàn","Active / safety"),"!",-280,()=>{SetMenu(false);GetComponent<ARCombatHUD>().ToggleActive();});
            MenuItem(L("Về sảnh","Exit to hub"),"X",-350,()=>{SetMenu(false);exitOpen=true;});menu.gameObject.SetActive(false);
            help=ARUI.Panel(content,"Gesture guide",0,5,1080,820);help.GetComponent<Image>().raycastTarget=true;ARUI.Text(help,L("5 CHIÊU + KIM CHUNG / 3 COMBO","5 SKILLS + GOLD BELL / 3 COMBOS"),0,270,780,62,30);
            for(int i=0;i<5;i++){var h=ARUI.Rect(help,"Guide hand "+i,-315,170-i*68,48,54).gameObject.AddComponent<ARHandGraphic>();h.Gesture=i;h.raycastTarget=false;ARUI.Text(help,LevelHUD.Vietnamese?vi[i]:en[i],20,170-i*68,600,58,24);}
            ARUI.Text(help,L("HÀN BĂNG → NGỰ LÔI: BĂNG LÔI LIỆT\nTHIÊN THỦ → VẠN KIẾM: PHÁ GIÁP\nHẮC ĐỘNG → VẠN KIẾM: TỤ SÁT","ICE → LIGHTNING: ICE LIGHTNING\nHAND → SWORDS: ARMOR SHATTER\nBLACK HOLE → SWORDS: CONVERGENCE"),0,-195,790,136,22);
            ARUI.Text(help,L("NGÓN CÁI LÊN: KIM CHUNG · đỡ 1 đòn · hồi 6s\nSàn + Chủ động: né ngang / cúi 25cm. Đeo tai nghe.\nLinh Ấn đầy: V → LÊN → MỞ / XUỐNG → NẮM → LÊN / MỞ → NẮM","THUMB UP: GOLD BELL · blocks once · 6s cooldown\nFloor + Active: move sideways / duck 25cm. Wear headphones.\nFull seals: V → UP → PALM / DOWN → FIST → UP / PALM → FIST"),0,-333,1010,118,20);
            ARUI.Round(help,"Close help","X",365,270,58,()=>SetHelp(false));help.gameObject.SetActive(false);
            exitConfirm=ARUI.Panel(content,"Exit confirmation",0,0,600,280);exitConfirm.GetComponent<Image>().raycastTarget=true;ARUI.Text(exitConfirm,L("Thoát trận?","Leave the battle?"),0,70,550,68,32);ARUI.Round(exitConfirm,"Cancel exit",L("Ở LẠI","STAY"),-140,-45,100,()=>exitOpen=false);ARUI.Round(exitConfirm,"Confirm exit",L("THOÁT","EXIT"),140,-45,100,ARSessionBootstrap.Exit);
            result=ARUI.Panel(content,"Battle result",0,20,1000,440);result.GetComponent<Image>().raycastTarget=true;resultTitle=ARUI.Text(result,"",0,145,950,90,38);ARUI.Round(result,"Replay",L("CHƠI\nLẠI","REPLAY"),-345,-130,116,()=>{field.UserPaused=false;director.StartBattle();});ARUI.Round(result,"Result reposition",L("ĐỔI\nVỊ TRÍ","MOVE"),-115,-130,116,()=>{field.UserPaused=false;field.placement.Reposition();});ARUI.Round(result,"Result exit",L("VỀ\nSẢNH","EXIT"),345,-130,116,()=>exitOpen=true);
            resultStats=ARUI.Text(result,"",0,36,930,100,25);ARUI.Round(result,"Result modes",L("CHẾ\nĐỘ","MODES"),115,-130,116,()=>{field.UserPaused=false;selection.Open();});
            caster.source.Result+=Received;caster.source.Invalidated+=ClearHand;caster.CastAttempted+=Cast;field.Built+=NewBattle;
            pressSource=gameObject.AddComponent<AudioSource>();pressSource.playOnAwake=false;pressSource.spatialBlend=0;pressSource.volume=.1f;pressClip=AudioClip.Create("AR press",1920,1,48000,false);var samples=new float[1920];for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*2*Mathf.PI*780/48000f)*(1-i/(float)samples.Length);pressClip.SetData(samples,0);
            welcomeShown=PlayerPrefs.GetInt("CampusRift.AR.HelpSeen",0)!=0;
            foreach(var r in new[]{statusPanel,(RectTransform)spirit.transform.parent,(RectTransform)menuButton.transform,handDot.rectTransform,centerReticle.rectTransform,(RectTransform)q.transform})ARHUDRegion.Add(r,180);
            foreach(var r in charge)ARHUDRegion.Add(r.rectTransform,180);
            foreach(var r in namePanels)ARHUDRegion.Add(r,50);
            ARHUDRegion.Add(preview,10);ARHUDRegion.Add(toastPanel,130);ARHUDRegion.Add(comboPanel,140);ARHUDRegion.Add(castPanel,150);
        }
        TMP_Text MenuItem(string text,string symbol,float y,UnityEngine.Events.UnityAction action){var row=ARUI.Rect(menu,text,0,y,384,64);var image=row.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,.01f);var button=row.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);ARUI.Round(row,text+" icon",symbol,-142,0,52,()=>action());var label=ARUI.Text(row,text,32,0,260,58,23);label.alignment=TextAlignmentOptions.Left;return label;}
        static Image Bar(Transform parent,string name,float x,float y,float w,float h,Color color){var r=ARUI.Rect(parent,name,x,y,w,h);var bg=r.gameObject.AddComponent<Image>();bg.color=new Color(.1f,.1f,.1f,.8f);bg.raycastTarget=false;var fill=ARUI.Rect(r,name+" fill",0,0,w,h).gameObject.AddComponent<Image>();fill.sprite=ComicTheme.Sprite("round-mask");fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.color=color;fill.raycastTarget=false;return fill;}
        public void SetMenu(bool value){MenuOpen=value;if(value){menu.gameObject.SetActive(true);SetHelp(false);}UpdatePause();}
        public void SetHelp(bool value){HelpOpen=value;if(value){MenuOpen=false;PlayerPrefs.SetInt("CampusRift.AR.HelpSeen",1);}help.gameObject.SetActive(value);UpdatePause();}
        void UpdatePause(){if(field==null)return;field.MenuPaused=(GetComponent<ARTechHUD>()?.Blocking??false)||MenuOpen||HelpOpen||exitOpen||selection!=null&&selection.Blocking||(GetComponent<ARCombatHUD>()?.SafetyOpen??false);field.placement.InputBlocked=field.MenuPaused||consent!=null&&!consent.Accepted;}
        void NewBattle(){for(int i=0;i<3;i++){hints[i]=0;previousStatuses[i]=false;}preview.gameObject.SetActive(false);}
        void ClearHand(){handUntil=0;}
        void Received(GestureFrame f){landmarks.Set(f);if(f.handPresent||f.landmarks!=null&&f.landmarks.Length==63)handUntil=Time.unscaledTime+.25f;else handUntil=0;}
        void Cast(string label,bool ok)
        {
            int index=System.Array.IndexOf(GestureSkillMapper.Labels,label);if(index<0||index>=icons.Length)return;shownUntil[index]=Time.unscaledTime+1;
            toast.text=caster.Feedback;charge[index].Amount=1;toastUntil=Time.unscaledTime+1.2f;
            if(ok){flash[index]=.6f;castUntil=Time.unscaledTime+.6f;castText.text=LevelHUD.Vietnamese?vi[index]:en[index];if(UIAudioManager.Instance!=null)UIAudioManager.Instance.Click();else{pressSource.volume=SettingsManager.Instance!=null?SettingsManager.Instance.Current.UIVolume*.12f:.1f;pressSource.PlayOneShot(pressClip);}}
            else shake[index]=.35f;
        }
        public void ShowCombo(int i){if(i<0||i>=3||hints[i]>=3)return;hints[i]++;comboUntil=Time.unscaledTime+2;string[] textsVi={"→ NGỰ LÔI = BĂNG LÔI LIỆT","→ VẠN KIẾM = PHÁ GIÁP","→ VẠN KIẾM = TỤ SÁT"},textsEn={"→ LIGHTNING = ICE LIGHTNING","→ SWORD RAIN = ARMOR SHATTER","→ SWORD RAIN = CONVERGENCE"};combo.text=LevelHUD.Vietnamese?textsVi[i]:textsEn[i];}
        void Update()
        {
            if(content==null)return;bool accepted=consent==null||consent.Accepted;bool selecting=selection!=null&&selection.Blocking;bool external=(GetComponent<ARTechHUD>()?.Blocking??false)||(GetComponent<ARCombatHUD>()?.SafetyOpen??false);content.gameObject.SetActive(accepted&&!selecting&&!external);UpdatePause();if(!accepted||selecting||external)return;if(!welcomeShown){welcomeShown=true;SetHelp(true);}
            bool fighting=field.Root!=null&&!field.placement.Adjusting&&field.Shrine!=null;
            field.placement.English=!LevelHUD.Vietnamese;placementPanel.gameObject.SetActive(!fighting);modes.gameObject.SetActive(field.Root==null);placeButton.gameObject.SetActive(!fighting);placeButton.interactable=field.placement.ReticleValid||field.placement.Adjusting;placeButton.GetComponentInChildren<TMP_Text>().text=field.placement.Adjusting?L("BẮT\nĐẦU","START"):L("ĐẶT\nTRẬN","PLACE");message.text=field.placement.Message;
            scan.gameObject.SetActive(field.Root==null&&!field.placement.ReticleValid);scan.anchoredPosition=new Vector2(Mathf.Sin(Time.unscaledTime*2)*50,20);scan.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*2)*12);
            tableButton.targetGraphic.color=!field.placement.settings.Floor?ComicTheme.Gold:ComicTheme.Navy;floorButton.targetGraphic.color=field.placement.settings.Floor?ComicTheme.Gold:ComicTheme.Navy;tableButton.GetComponentInChildren<TMP_Text>().color=!field.placement.settings.Floor?ComicTheme.Ink:ComicTheme.Paper;floorButton.GetComponentInChildren<TMP_Text>().color=field.placement.settings.Floor?ComicTheme.Ink:ComicTheme.Paper;
            statusPanel.gameObject.SetActive(fighting);spirit.transform.parent.gameObject.SetActive(fighting);rail.gameObject.SetActive(fighting&&!field.ModeSession.Has(ARModeFeature.Rhythm)&&!(GetComponent<ARKnowledgeSeal>()?.Active??false));
            if(fighting){hp.fillAmount=field.Shrine.CurrentHealth/field.Shrine.maxHealth;status.text=field.ModeSession.Has(ARModeFeature.Rhythm)?field.Mode.Title(LevelHUD.Vietnamese):L("LINH TRẬN","SHRINE")+L("     ĐỢT ","     WAVE ")+director.Wave+"/"+director.WaveCount+" · "+field.Mode.Title(LevelHUD.Vietnamese);var power=caster.Caster!=null?caster.Caster.GetComponent<CampusRift.Combat.SpiritPower>():null;spirit.fillAmount=power!=null?power.Current/field.placement.settings.spirit:0;}
            pauseLabel.text=field.UserPaused?L("Tiếp tục","Resume"):L("Tạm dừng","Pause");modeLabel.text=field.placement.settings.Floor?L("Sàn  /  chuyển Bàn","Floor  /  switch Table"):L("Bàn  /  chuyển Sàn","Table  /  switch Floor");
            menuAmount=Mathf.MoveTowards(menuAmount,MenuOpen?1:0,Time.unscaledDeltaTime/.15f);menuGroup.alpha=menuAmount;menu.localScale=Vector3.one*Mathf.Lerp(.9f,1,menuAmount);menuGroup.interactable=MenuOpen;menuGroup.blocksRaycasts=MenuOpen;if(!MenuOpen&&menuAmount<=0)menu.gameObject.SetActive(false);
            help.gameObject.SetActive(HelpOpen);exitConfirm.gameObject.SetActive(exitOpen);dismiss.gameObject.SetActive(MenuOpen||HelpOpen||exitOpen);result.gameObject.SetActive(director.Finished&&!field.CheckLoad);resultTitle.text=director.Won?L("CHIẾN THẮNG!","VICTORY!"):field.Shrine!=null&&field.Shrine.IsDead&&field.Mode.loseOnShrine?L("LINH TRẬN ĐÃ VỠ","SHRINE DEFEATED"):L("CHƯA HOÀN THÀNH","NOT COMPLETED");
            var summary=field.ModeSession.Result;resultStats.text=summary==null?"":ARModeSelectionHUD.StarText(summary.stars)+" · "+summary.score+L(" điểm · "," pts · ")+summary.reward+" LT"+(summary.saved?"":L(" (không lưu)"," (not saved)"))+"\n"+field.Mode.StarHint(LevelHUD.Vietnamese);
            centerReticle.gameObject.SetActive(fighting);centerReticle.color=caster.AimValid?ComicTheme.Green:ComicTheme.Muted;bool hand=Time.unscaledTime<handUntil&&caster.source.SamplingActive&&!caster.source.Recovering;handDot.color=!hand?ComicTheme.Muted:caster.gestures.Geometry.inFrame?ComicTheme.Green:ComicTheme.Gold;
            if(fighting){var types=new[]{CampusRift.Combat.StatusType.Freeze,CampusRift.Combat.StatusType.Stun,CampusRift.Combat.StatusType.Pulled};for(int i=0;i<3;i++){bool active=false;foreach(var enemy in director.Actors)if(enemy!=null&&enemy.Alive&&enemy.Status.Has(types[i])){active=true;break;}if(active&&!previousStatuses[i])ShowCombo(i);previousStatuses[i]=active;}}
            int focused=0;for(int i=1;i<5;i++)if(Mathf.Max(shownUntil[i],caster.gestures.Evidence[i]>.15f?Time.unscaledTime+1:0)>Mathf.Max(shownUntil[focused],caster.gestures.Evidence[focused]>.15f?Time.unscaledTime+1:0))focused=i;
            for(int i=0;i<5;i++){string label=GestureSkillMapper.Labels[i];var skill=caster.Runtime(label);cooldown[i].Amount=skill!=null?skill.CooldownRemaining/Mathf.Max(.1f,skill.CooldownDuration):0;float evidence=caster.gestures.Evidence[i];charge[i].Amount=Mathf.MoveTowards(charge[i].Amount,Mathf.Clamp01(evidence),Time.unscaledDeltaTime*6);if(evidence>.15f)shownUntil[i]=Time.unscaledTime+1;flash[i]=Mathf.Max(0,flash[i]-Time.unscaledDeltaTime);shake[i]=Mathf.Max(0,shake[i]-Time.unscaledDeltaTime);circles[i].color=flash[i]>0?ComicTheme.Gold:new Color(.12f,.06f,.23f,.94f);icons[i].anchoredPosition=new Vector2(860+(shake[i]>0?Mathf.Sin(Time.unscaledTime*65)*10:0),270-i*112);namePanels[i].gameObject.SetActive(fighting&&i==focused&&Time.unscaledTime<shownUntil[i]);names[i].text=LevelHUD.Vietnamese?vi[i]:en[i];namePanels[i].anchoredPosition=new Vector2(Mathf.Lerp(650,620,Mathf.Clamp01((shownUntil[i]-Time.unscaledTime)*5)),270-i*112);}
            if(fighting&&field.Paused&&!field.MenuPaused){toast.text=field.UserPaused?L("ĐÃ TẠM DỪNG","PAUSED"):field.AnchorLost?L("Nhìn lại vào chiến trường","Look back at the battlefield"):L("Di chuyển chậm lại / thêm ánh sáng","Move slowly / improve lighting");toastUntil=Time.unscaledTime+.2f;}
            var battleEvents=GetComponent<ARBattleEvents>();if(fighting&&battleEvents!=null&&!string.IsNullOrEmpty(battleEvents.Message)){toast.text=battleEvents.Message+(battleEvents.RescueActive?" · "+Mathf.RoundToInt(battleEvents.RescueProgress*100)+"%":"");toastUntil=Time.unscaledTime+.2f;}
            if(!string.IsNullOrEmpty(caster.source.Error)){toast.text=caster.source.Error;toastUntil=Time.unscaledTime+.2f;}
            toastPanel.gameObject.SetActive(Time.unscaledTime<toastUntil);comboPanel.gameObject.SetActive(fighting&&Time.unscaledTime<comboUntil);castPanel.gameObject.SetActive(fighting&&Time.unscaledTime<castUntil&&!(GetComponent<ARCombatHUD>()?.UltimateVisible??false));
            if(ModalOpen)foreach(var rect in new[]{statusPanel,(RectTransform)spirit.transform.parent,rail,preview,placementPanel,modes,(RectTransform)placeButton.transform,scan,toastPanel,comboPanel,castPanel,centerReticle.rectTransform})rect.gameObject.SetActive(false);
        }
        // Sum the visible top-level occupied rectangles, avoiding nested icon/bar double counting.
        public object Coverage()
        {Canvas.ForceUpdateCanvases();float total=0;var rows=new List<object>();var corners=new Vector3[4];foreach(var r in coverage){if(r==null||!r.gameObject.activeInHierarchy)continue;r.GetWorldCorners(corners);float a=Mathf.Abs((corners[2].x-corners[0].x)*(corners[2].y-corners[0].y));total+=a;rows.Add(new {r.name,area=a});}return new {width=Screen.width,height=Screen.height,total,percent=total/(Screen.width*Screen.height)*100,rectangles=rows};}
        void OnDestroy(){if(caster!=null){if(caster.source!=null){caster.source.Result-=Received;caster.source.Invalidated-=ClearHand;}caster.CastAttempted-=Cast;}if(field!=null)field.Built-=NewBattle;if(pressClip!=null)Destroy(pressClip);}
    }
}



