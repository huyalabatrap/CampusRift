using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.UI;
namespace CampusRift.AR
{
    // Separate safe-area canvas keeps incoming-camera warnings independent of ground aiming.
    public sealed class ARCombatHUD:MonoBehaviour
    {
        ARBattlefield field;ARSkillCaster caster;ARPlayerCombat combat;
        RectTransform canvas,meter,sealRows,warning,shieldBadge,safety,ultimatePanel;
        Canvas safetyCanvas;TMP_Text sealText,warningText,dodgeText,shieldText,ultimateText,activeText;
        Image sealFill;ARArcGraphic warningRing,shieldArc;ARImpactBorder impact;
        readonly Image[,] sealBacks=new Image[3,3];readonly ARHandGraphic[,] sealHands=new ARHandGraphic[3,3];
        bool asking;float ultimateUntil,introUntil;Transform battleRoot;
        readonly RectTransform[] sequenceRows=new RectTransform[3];
        static string L(string vi,string en)=>LevelHUD.Vietnamese?vi:en;
        void Start()
        {
            field=GetComponent<ARBattlefield>();caster=GetComponent<ARSkillCaster>();combat=GetComponent<ARPlayerCombat>();
            canvas=ARUI.Canvas(transform,"AR combat HUD");canvas.GetComponentInParent<Canvas>(true).sortingOrder=10;
            meter=ARUI.Rect(canvas,"Linh An",-650,310,330,44);sealText=ARUI.Text(meter,"",0,8,330,28,17);sealText.color=ComicTheme.Gold;
            var track=ARUI.Rect(meter,"Seal track",0,-14,330,6).gameObject.AddComponent<Image>();track.color=ComicTheme.Ink;track.raycastTarget=false;
            sealFill=ARUI.Rect(track.transform,"Seal energy",0,0,330,6).gameObject.AddComponent<Image>();sealFill.sprite=ComicTheme.Sprite("round-mask");sealFill.type=Image.Type.Filled;sealFill.fillMethod=Image.FillMethod.Horizontal;sealFill.color=ComicTheme.Gold;sealFill.raycastTarget=false;
            sealRows=ARUI.Rect(canvas,"Seal sequences",-650,240,330,82);
            string[] titles={L("VẠN KIẾM QUY TÔNG","SWORD CONVERGENCE"),L("BĂNG THIÊN LÔI NGỤC","ICE LIGHTNING PRISON"),L("THIÊN THỦ HẤP TINH","STAR ABSORPTION")};
            for(int row=0;row<3;row++)
            {
                var rowRoot=ARUI.Rect(sealRows,"Sequence row "+row,0,0,330,82);sequenceRows[row]=rowRoot;
                float y=0;var title=ARUI.Text(rowRoot,titles[row],0,y+28,330,24,16);title.margin=Vector4.zero;
                for(int step=0;step<GestureSequenceMatcher.Seals[row].Length;step++)
                {
                    float x=(step-1)*62+(row==2?31:0);var r=ARUI.Rect(rowRoot,"Seal "+row+" "+step,x,y-8,42,42);var bg=r.gameObject.AddComponent<Image>();bg.sprite=ComicTheme.Sprite("round-mask");bg.raycastTarget=false;sealBacks[row,step]=bg;
                    var hand=ARUI.Rect(r,"Intent hand",0,0,38,42).gameObject.AddComponent<ARHandGraphic>();hand.Gesture=System.Array.IndexOf(GestureSkillMapper.Labels,GestureSequenceMatcher.Seals[row][step]);hand.raycastTarget=false;sealHands[row,step]=hand;
                    if(step<GestureSequenceMatcher.Seals[row].Length-1){var arrow=ARUI.Text(rowRoot,">",x+31,y-8,16,28,18);arrow.margin=Vector4.zero;}
                }
            }
            warning=ARUI.Rect(canvas,"Incoming camera warning",0,0,220,220);warningRing=warning.gameObject.AddComponent<ARArcGraphic>();warningRing.color=ComicTheme.Red;warningRing.raycastTarget=false;warningText=ARUI.Text(warning,"",0,145,620,40,23);warningText.color=ComicTheme.Red;
            dodgeText=ARUI.Text(canvas,"",0,210,500,50,32);dodgeText.color=ComicTheme.Gold;
            var border=ARUI.Rect(canvas.parent,"Cracked impact",0,0,0,0);border.anchorMin=Vector2.zero;border.anchorMax=Vector2.one;border.sizeDelta=Vector2.zero;impact=border.gameObject.AddComponent<ARImpactBorder>();impact.raycastTarget=false;
            shieldBadge=ARUI.Rect(canvas,"Golden Bell status",860,-288,72,72);var button=ARUI.Round(shieldBadge,"Shield gesture","",0,0,72,()=>{});button.interactable=false;var thumb=ARUI.Rect(button.transform,"Thumb up",0,0,40,49).gameObject.AddComponent<ARHandGraphic>();thumb.Gesture=5;thumb.raycastTarget=false;
            shieldArc=ARUI.Rect(shieldBadge,"Shield cooldown",0,0,78,78).gameObject.AddComponent<ARArcGraphic>();shieldArc.color=ComicTheme.Gold;shieldArc.raycastTarget=false;shieldText=ARUI.Text(shieldBadge,"",-190,0,270,60,21);
            activeText=ARUI.Text(canvas,"",-650,175,400,42,17);
            ultimatePanel=ARUI.Panel(canvas,"Ultimate announcement",0,-164,600,60);ultimateText=ARUI.Text(ultimatePanel,"",0,0,570,52,27);ultimateText.color=ComicTheme.Gold;
            var safeRoot=ARUI.Canvas(transform,"Active combat safety");safetyCanvas=safeRoot.GetComponentInParent<Canvas>(true);safetyCanvas.sortingOrder=120;
            var dim=ARUI.Rect(safeRoot.parent,"Safety backdrop",0,0,0,0);dim.anchorMin=Vector2.zero;dim.anchorMax=Vector2.one;dim.sizeDelta=Vector2.zero;dim.SetAsFirstSibling();var shade=dim.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.9f);shade.raycastTarget=true;
            safety=ARUI.Panel(safeRoot,"Active safety acknowledgement",0,0,1150,640);safety.GetComponent<Image>().raycastTarget=true;
            ARUI.Text(safety,L("CHỦ ĐỘNG · CHỈ Ở SÀN","ACTIVE COMBAT · FLOOR ONLY"),0,245,1080,70,35).color=ComicTheme.Gold;
            ARUI.Text(safety,L("Dọn khoảng trống quanh bạn. Đứng vững, tránh cầu thang, xe cộ và người khác.\nQuái sẽ bắn về điện thoại: dịch ngang hoặc cúi ít nhất 25 cm. Không cần chạy.\nChế độ Bàn luôn an toàn, không có đòn cần né. Tắt Chủ động bất cứ lúc nào trong menu.\nĐeo tai nghe để nghe hướng đạn. Nhìn xung quanh trước khi di chuyển.","Clear space around you. Stay steady; avoid stairs, traffic and other people.\nEnemies shoot at your phone: move sideways or duck at least 25 cm. No running needed.\nTable mode stays safe. Turn Active off at any time in the menu.\nWear headphones for directional cues. Look around before moving."),0,40,1040,320,27);
            ARUI.Round(safety,"Keep safe",L("AN\nTOÀN","SAFE"),-225,-223,120,()=>CloseSafety(false));ARUI.Round(safety,"Acknowledge active",L("BẬT\nCHỦ ĐỘNG","ENABLE"),225,-223,120,()=>CloseSafety(true));safetyCanvas.gameObject.SetActive(false);
            sealText.margin=Vector4.zero;caster.UltimateFired+=Ultimate;caster.CastAttempted+=Cast;field.GetComponent<ARMonsterDirector>().BattleStarted+=NewBattle;
            foreach(var r in new[]{meter,shieldArc.rectTransform,shieldText.rectTransform})ARHUDRegion.Add(r,180);
            foreach(var r in new[]{warning,warningText.rectTransform,dodgeText.rectTransform})ARHUDRegion.Add(r,220);
            ARHUDRegion.Add(ultimatePanel,160);ARHUDRegion.Add(sealRows,120);ARHUDRegion.Add(activeText.rectTransform,20);
        }
        void NewBattle(){introUntil=Time.unscaledTime+5;ultimateUntil=0;}
        public void ToggleActive()
        {
            if(field.ModeSession.ActiveCombat){field.ModeSession.ActiveCombat=false;return;}
            asking=true;safetyCanvas.gameObject.SetActive(true);field.MenuPaused=true;field.placement.InputBlocked=true;
        }
        public bool SafetyOpen=>asking;
        public bool UltimateVisible=>Time.unscaledTime<ultimateUntil;
        void CloseSafety(bool enable){field.ModeSession.ActiveCombat=enable;asking=false;safetyCanvas.gameObject.SetActive(false);}
        void Ultimate(ARUltimate kind){string[] vi={"VẠN KIẾM QUY TÔNG!","BĂNG THIÊN LÔI NGỤC!","THIÊN THỦ HẤP TINH!"},en={"SWORD CONVERGENCE!","ICE LIGHTNING PRISON!","STAR ABSORPTION!"};ultimateText.text=LevelHUD.Vietnamese?vi[(int)kind]:en[(int)kind];ultimateUntil=Time.unscaledTime+1.8f;}
        void Cast(string label,bool ok){if(label!="Thumb_Up")return;shieldText.text=caster.Feedback;}
        void Update()
        {
            if(canvas==null)return;var hud=GetComponent<ARBattleHUD>();bool fighting=field.Root!=null&&field.Shrine!=null&&!field.placement.Adjusting;bool visible=fighting&&!field.ModeSession.Has(ARModeFeature.Rhythm)&&!(GetComponent<ARKnowledgeSeal>()?.Active??false)&&!asking&&!(hud?.ModalOpen??false);canvas.GetComponentInParent<Canvas>(true).gameObject.SetActive(visible);
            if(battleRoot!=field.Root){battleRoot=field.Root;introUntil=Time.unscaledTime+5;}
            sealText.text=caster.Seal>=100?L("KẾT ẤN SẴN SÀNG","SEALS READY"):L("LINH ẤN · ","SEAL ENERGY · ")+Mathf.RoundToInt(caster.Seal)+" / 100";sealFill.fillAmount=caster.Seal/100;
            bool hunt=field.Mode.winRule==ARWinRule.CloseRifts;meter.gameObject.SetActive(!hunt);sealRows.gameObject.SetActive(!hunt&&field.ModeSession.Has(ARModeFeature.Sequences)&&caster.sequences.Count>0);
            int matching=-1;for(int row=0;row<3;row++)if((caster.sequences.CandidateMask&(1<<row))!=0){matching=row;break;}
            for(int row=0;row<3;row++)sequenceRows[row].gameObject.SetActive(row==matching);
            for(int row=0;row<3;row++)for(int step=0;step<GestureSequenceMatcher.Seals[row].Length;step++){bool lit=caster.Seal>=100&&(caster.sequences.CandidateMask&(1<<row))!=0&&step<caster.sequences.Count;sealBacks[row,step].color=lit?ComicTheme.Gold:caster.Seal>=100?ComicTheme.Purple:ComicTheme.Navy;sealHands[row,step].color=lit?ComicTheme.Ink:ComicTheme.Paper;}
            bool incoming=combat.WarningRemaining>0&&combat.Enabled&&!field.Paused;warning.gameObject.SetActive(incoming);warningRing.Amount=1;warningRing.color=new Color(1,.16f,.24f,.65f+.25f*Mathf.Sin(Time.unscaledTime*12));warningText.text=L("ĐẠN TỚI · NÉ / KIM CHUNG","INCOMING · DODGE / GOLD BELL")+" · "+combat.WarningRemaining.ToString("0.0")+"s";
            dodgeText.text=Time.unscaledTime<combat.DodgeUntil?L("NÉ!","DODGE!"):Time.unscaledTime<combat.NoticeUntil?combat.Notice:"";dodgeText.gameObject.SetActive(!string.IsNullOrEmpty(dodgeText.text));
            impact.color=new Color(1,.12f,.18f,Mathf.Clamp01((combat.ImpactUntil-Time.unscaledTime)/.5f)*.9f);impact.gameObject.SetActive(Time.unscaledTime<combat.ImpactUntil&&visible);
            shieldArc.Amount=combat.CooldownRemaining/ARPlayerCombat.ShieldCooldown;shieldText.text=combat.ShieldActive?L("KIM CHUNG · ĐANG ĐỠ","GOLD BELL · GUARDING"):combat.CooldownRemaining>0?L("KIM CHUNG · ","GOLD BELL · ")+combat.CooldownRemaining.ToString("0.0")+"s":L("NGÓN CÁI LÊN · ĐỠ","THUMB UP · GUARD");
            activeText.text=combat.Enabled?L("CHỦ ĐỘNG · NÉ NGANG / CÚI 25cm","ACTIVE · SIDESTEP / DUCK 25cm"):field.ModeSession.ActiveCombat?L("CHỦ ĐỘNG CHỜ SÀN","ACTIVE WAITS FOR FLOOR"):L("AN TOÀN · ĐEO TAI NGHE","SAFE · WEAR HEADPHONES");activeText.gameObject.SetActive(Time.unscaledTime<introUntil);ultimatePanel.gameObject.SetActive(Time.unscaledTime<ultimateUntil);
        }
        void OnDestroy(){if(field!=null)field.GetComponent<ARMonsterDirector>().BattleStarted-=NewBattle;if(caster!=null){caster.UltimateFired-=Ultimate;caster.CastAttempted-=Cast;}if(canvas!=null)Destroy(canvas.GetComponentInParent<Canvas>(true).gameObject);if(safetyCanvas!=null)Destroy(safetyCanvas.gameObject);}
    }
}
