using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARSpaceHUD : MonoBehaviour
    {
        ARBattlefield field;ARMonsterDirector director;ARSpaceModes space;
        RectTransform canvas,objective,swordPanel,compass;TMP_Text title,detail,swordText,direction;
        ARHandGraphic pointing;ARArcGraphic swordArc;Image[] backs=new Image[3];ARHandGraphic[] hands=new ARHandGraphic[3];
        string bossState="";float objectiveUntil;
        static string L(string vi,string en)=>LevelHUD.Vietnamese?vi:en;
        void Start()
        {
            field=GetComponent<ARBattlefield>();director=GetComponent<ARMonsterDirector>();space=GetComponent<ARSpaceModes>();
            canvas=ARUI.Canvas(transform,"AR 360 space HUD");canvas.GetComponentInParent<Canvas>(true).sortingOrder=11;
            objective=ARUI.Panel(canvas,"Space objective",0,363,620,92);title=ARUI.Text(objective,"",0,24,594,28,20);title.color=ComicTheme.Gold;
            detail=ARUI.Text(objective,"",0,-14,594,48,17);
            compass=ARUI.Panel(canvas,"Offscreen room compass",0,-427,490,46);direction=ARUI.Text(compass,"",0,0,470,40,18);direction.color=ComicTheme.Gold;
            swordPanel=ARUI.Rect(canvas,"Heaven Sword hold",740,-390,96,122);
            var button=ARUI.Round(swordPanel,"Point upward hold","",0,15,66,()=>{});button.interactable=false;
            pointing=ARUI.Rect(button.transform,"Pointing up",0,0,44,56).gameObject.AddComponent<ARHandGraphic>();pointing.Gesture=2;pointing.raycastTarget=false;
            swordArc=ARUI.Rect(button.transform,"Hold progress",0,0,74,74).gameObject.AddComponent<ARArcGraphic>();swordArc.color=ComicTheme.Gold;swordArc.raycastTarget=false;
            swordText=ARUI.Text(swordPanel,"",0,-40,96,32,15);swordText.color=ComicTheme.Gold;
            for(int i=0;i<3;i++){var r=ARUI.Rect(objective,"Required seal "+i,(i-1)*62,-24,32,32);backs[i]=r.gameObject.AddComponent<Image>();backs[i].sprite=ComicTheme.Sprite("round-mask");backs[i].raycastTarget=false;hands[i]=ARUI.Rect(r,"Assigned hand",0,0,26,30).gameObject.AddComponent<ARHandGraphic>();hands[i].raycastTarget=false;}
            foreach(var text in new[]{title,detail,swordText,direction})text.margin=Vector4.zero;
            ARHUDRegion.Add(objective,170);ARHUDRegion.Add(compass,170);ARHUDRegion.Add(swordPanel,170);
        }
        void Update()
        {
            if(canvas==null)return;var hud=GetComponent<ARBattleHUD>();var selection=GetComponent<ARModeSelectionHUD>();
            bool show=!(GetComponent<ARKnowledgeSeal>()?.Active??false)&&!field.ModeSession.Has(ARModeFeature.Rhythm)&&field.Root!=null&&field.Shrine!=null&&!field.placement.Adjusting&&!director.Finished&&!(hud?.ModalOpen??false);
            canvas.GetComponentInParent<Canvas>(true).gameObject.SetActive(show);if(!show)return;
            bool hunt=field.Mode.winRule==ARWinRule.CloseRifts,boss=space.Boss.Alive;
            string state=boss?space.Boss.Armor+":"+space.Boss.WeakPointOpen+":"+(space.Boss.Health>0)+":"+space.SwordReady:"";
            if(state!=bossState){bossState=state;objectiveUntil=Time.unscaledTime+3;}
            objective.gameObject.SetActive(hunt||boss&&Time.unscaledTime<objectiveUntil);swordPanel.gameObject.SetActive(boss);foreach(var b in backs)b.gameObject.SetActive(false);
            if(hunt)
            {
                title.text=L("TRUY RIFT · ","RIFT HUNT · ")+Mathf.CeilToInt(space.Remaining)+"s · "+space.Points+L(" điểm"," pts");
                var target=space.HuntTarget;detail.rectTransform.anchoredPosition=new Vector2(0,0);detail.rectTransform.sizeDelta=new Vector2(594,24);detail.fontSize=16;
                detail.text=target==null?L("Quét tường / sàn · xoay máy tìm Rift","Scan walls / floors · turn to find a Rift"):L("ĐÓNG TRONG ","CLOSE IN ")+Mathf.CeilToInt(target.expires-field.Clock)+L("s · không cần Linh Ấn đầy","s · no seal energy needed");
                if(target!=null){var seq=GestureSequenceMatcher.Seals[target.sequence];for(int i=0;i<seq.Length;i++){backs[i].gameObject.SetActive(true);bool lit=i<space.huntMatcher.Count;backs[i].color=lit?ComicTheme.Gold:ComicTheme.Navy;int gesture=System.Array.IndexOf(GestureSkillMapper.Labels,seq[i]);if(hands[i].Gesture!=gesture){hands[i].Gesture=gesture;hands[i].SetVerticesDirty();}hands[i].color=lit?ComicTheme.Ink:ComicTheme.Paper;}}
            }
            else if(boss)
            {
                title.text=field.Mode.Title(LevelHUD.Vietnamese)+L(" · RỒNG TRÊN ĐẦU"," · DRAGON OVERHEAD");detail.rectTransform.anchoredPosition=new Vector2(0,-14);detail.rectTransform.sizeDelta=new Vector2(594,48);detail.fontSize=17;
                detail.text=space.Boss.Armor>0?(space.Boss.WeakPointOpen?L("Ngắm điểm sáng · MỞ > V / XUỐNG > LÊN\nGiáp còn ","Aim at glow · PALM > V / DOWN > UP\nArmor left ")+space.Boss.Armor:L("Rồng đang bọc giáp · điểm yếu mở sau ","Dragon armored · weak point opens in ")+Mathf.CeilToInt(space.Boss.ArmorOpensIn)+"s"):space.Boss.Health>0?L("GIÁP VỠ · đánh điểm yếu · ","ARMOR BROKEN · strike weak point · ")+space.Boss.Health:space.SwordReady?L("KIẾM Ý ĐẦY · THIÊN KIẾM SẴN SÀNG","SWORD INTENT FULL · HEAVEN SWORD READY"):L("Dọn hết quái mặt đất để đầy Kiếm Ý","Clear every ground enemy to fill Sword Intent");
            }
            swordArc.Amount=space.SwordReady?Mathf.Max(.05f,space.SwordProgress):0;
            swordText.text=space.SwordReady?(space.SwordProgress>0?Mathf.RoundToInt(space.SwordProgress*100)+"%":L("GIỮ 1,5s","HOLD 1.5s")):L("KIẾM Ý","SWORD");
            if(boss&&space.SwordReady&&Time.unscaledTime<objectiveUntil)detail.text=L("Giơ ngón trỏ giữ 1,5s · ngước máy >35°","Hold pointing up 1.5s · tilt phone >35°");
            Vector3? targetPosition=null;string label="";float best=float.PositiveInfinity;
            foreach(var p in space.Rifts.Portals)
            {
                if(p.visual==null||!p.visual.gameObject.activeInHierarchy)continue;var v=field.placement.view.WorldToViewportPoint(p.Position);bool outside=v.z<=0||v.x<.08f||v.x>.92f||v.y<.1f||v.y>.9f;
                float distance=Vector3.Distance(p.Position,field.placement.view.transform.position);if(outside&&distance<best){targetPosition=p.Position;label=p.wall?L("RIFT TƯỜNG","WALL RIFT"):L("RIFT PHỤ","SECONDARY RIFT");best=distance;}
            }
            if(boss){var v=field.placement.view.WorldToViewportPoint(space.Boss.WeakPoint.position);if(v.z<=0||v.x<0||v.x>1||v.y<0||v.y>1){targetPosition=space.Boss.WeakPoint.position;label=L("RỒNG · NGƯỚC MÁY","DRAGON · LOOK UP");}}
            compass.gameObject.SetActive(targetPosition.HasValue);
            if(targetPosition.HasValue)
            {
                var delta=targetPosition.Value-field.placement.view.transform.position;var forward=Vector3.ProjectOnPlane(field.placement.view.transform.forward,Vector3.up);if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
                float angle=Vector3.SignedAngle(forward,Vector3.ProjectOnPlane(delta,Vector3.up),Vector3.up);
                string hint=Mathf.Abs(angle)>135?L("SAU LƯNG","BEHIND"):angle< -20?L("< TRÁI","< LEFT"):angle>20?L("PHẢI >","RIGHT >"):L("PHÍA TRƯỚC","AHEAD");
                direction.text=label+" · "+hint+" · "+Mathf.RoundToInt(Mathf.Abs(angle))+"°";
            }
        }
        void OnDestroy(){if(canvas!=null)Destroy(canvas.GetComponentInParent<Canvas>(true).gameObject);}
    }
}
