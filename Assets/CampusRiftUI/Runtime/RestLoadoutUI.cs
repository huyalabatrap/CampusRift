using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Controls;
using CampusRift.Levels;
using CampusRift.Skills;

namespace CampusRift.UI
{
    public sealed class RestLoadoutUI : MonoBehaviour
    {
        public static RestLoadoutUI Instance { get; private set; }
        public bool Visible => panel != null && panel.gameObject.activeSelf;
        public bool ButtonVisible => launch != null && launch.gameObject.activeSelf;
        UiKit kit; RectTransform frame, panel, launch; SkillLoadout loadout; int slot;
        bool mobile; string language; LevelDirector Director => LevelDirector.Instance;
        static string L(string vn,string en) => LevelHUD.Vietnamese ? vn : en;
        public static void Attach()
        {
            if(Instance!=null)return;
            new GameObject("Between-wave skill panel",typeof(RectTransform)).AddComponent<RestLoadoutUI>();
        }
        void Awake()
        {
            Instance=this;kit=UiKit.Create();if(kit==null){enabled=false;return;}
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            gameObject.AddComponent<GraphicRaycaster>();frame=kit.Fit(transform,"Safe loadout frame");
            BuildLaunch();
        }
        void BuildLaunch()
        {
            if(launch!=null)Destroy(launch.gameObject);
            mobile=CampusInput.Mobile;language=LevelHUD.Vietnamese?"vi":"en";
            launch=kit.Rect(frame,"Change skills",840,180,240,148);
            if(mobile){Round(launch,"CHANGE",L("ĐỔI KỸ NĂNG","CHANGE SKILLS"),76,0,88,Open);}
            else kit.Button(launch,L("ĐỔI KỸ NĂNG","CHANGE SKILLS"),0,0,240,76,Open,true,true,22);
            launch.gameObject.SetActive(false);
        }
        void Update()
        {
            if(kit==null)return;
            bool available=Director!=null&&Director.CanChangeSkills;
            if(Visible&&!available)Close();
            if(mobile!=CampusInput.Mobile || language!=(LevelHUD.Vietnamese?"vi":"en")){BuildLaunch();if(Visible)Draw();}
            launch.gameObject.SetActive(available&&!Visible&&UIStateManager.Instance.State==UIState.Gameplay);
        }
        public void Open()
        {
            if(Director==null||!Director.CanChangeSkills||UIStateManager.Instance.State!=UIState.Gameplay)return;
            loadout=Director.PlayerTransform.GetComponent<SkillLoadout>();if(loadout==null)return;
            if(!Director.SetRestLoadoutOpen(true))return;
            loadout.CancelAll();slot=0;UIStateManager.Instance.OpenModal(Close);
            if(panel==null)panel=kit.Stretch(frame,"Rest loadout modal");
            panel.gameObject.SetActive(true);launch.gameObject.SetActive(false);Draw();
        }
        public bool SelectSkill(string id)
        {
            if(!Visible||loadout==null||!loadout.EquipDuringRest(slot,id))return false;
            Draw();return true;
        }
        public void SelectSlot(int index){slot=Mathf.Clamp(index,0,3);if(Visible)Draw();}
        public void Close()
        {
            bool wasOpen=Visible;if(panel!=null)panel.gameObject.SetActive(false);
            Director?.SetRestLoadoutOpen(false);
            if(wasOpen&&UIStateManager.Instance!=null&&UIStateManager.Instance.State==UIState.Modal)UIStateManager.Instance.CloseModal();
        }
        void Draw()
        {
            kit.Clear(panel);
            kit.Image(panel,"Modal shade",0,0,1920,1080,new Color(0,0,0,.72f),null,true);
            var card=kit.Panel(panel,"Rest loadout comic",340,155,1240,780);
            kit.Text(card,L("ĐỔI KỸ NĂNG","CHANGE SKILLS"),34,20,1000,60,36,ComicTheme.Gold,TextAlignmentOptions.TopLeft,false);
            kit.Text(card,L("Đồng hồ nghỉ đang dừng · Chọn ô, rồi chọn kỹ năng","Rest timer paused · Select a slot, then a skill"),34,85,1060,46,22,null,TextAlignmentOptions.TopLeft,false);
            for(int i=0;i<4;i++)
            {
                int captured=i;var skill=loadout.Get(i);var def=skill!=null?skill.Definition:null;
                float x=40+i*294;
                if(mobile)
                {
                    Round(card,"Slot "+i,L("Ô ","SLOT ")+(i+1),x+75,148,112,()=>SelectSlot(captured),def,i==slot);
                    var caption=kit.Text(card,def!=null?def.LocalizedName(LevelHUD.Vietnamese):L("TRỐNG","EMPTY"),x,280,268,68,21,null,TextAlignmentOptions.Center,true);caption.enableAutoSizing=false;caption.fontSize=21;
                }
                else
                {
                    var b=kit.Button(card,(i==slot?"● ":"")+new[]{"Q","E","R","F"}[i]+"  "+(skill!=null?skill.ShortName:L("TRỐNG","EMPTY")),x,150,270,124,()=>SelectSlot(captured),true,i==slot,20);
                    if(def!=null)kit.Image(b.transform,"Skill icon",100,72,40,40,Color.white,HubUI.SkillIcon(def));
                }
            }
            var skills=SkillCatalog.Instance.skills.Where(s=>s!=null&&HubUI.SkillUnlocked(s,Progression.ProfileService.Instance.Cultivation)&&loadout.Find(s.id)!=null).ToArray();
            float top=mobile?355:304;float height=mobile?232:304;
            var list=kit.Scroll(card,"Available skills",32,top,1164,height,Mathf.Max(height,Mathf.Ceil(skills.Length/2f)*116));
            if(mobile&&list.rect.height>height)
            {
                var scroll=list.GetComponentInParent<ScrollRect>();
                Round(card,"Scroll next","",926,594,68,()=>{scroll.verticalNormalizedPosition=scroll.verticalNormalizedPosition<.05f?1:Mathf.Max(0,scroll.verticalNormalizedPosition-height/Mathf.Max(height,list.rect.height-height));},null,false,"v");
                var cueText=kit.Text(card,L("VUỐT ĐỂ XEM THÊM ↓","SWIPE FOR MORE ↓"),390,604,520,38,18,null,TextAlignmentOptions.MidlineRight,false);
                cueText.enableAutoSizing=false;cueText.fontSize=18;
                var rail=kit.Image(card,"Scroll rail",1203,top,7,height,ComicTheme.Ink);
                var thumb=kit.Image(rail.transform,"Scroll thumb",0,0,7,height*height/list.rect.height,ComicTheme.Gold);
                scroll.onValueChanged.AddListener(value=>{thumb.rectTransform.anchoredPosition=new Vector2(0,-(height-thumb.rectTransform.sizeDelta.y)*(1-value.y));cueText.text=value.y<.02f?L("VUỐT LÊN ĐỂ XEM LẠI ↑","SWIPE UP TO RETURN ↑"):L("VUỐT ĐỂ XEM THÊM ↓","SWIPE FOR MORE ↓");});
            }
            for(int i=0;i<skills.Length;i++)
            {
                var def=skills[i];float x=(i%2)*580,y=(i/2)*116;bool chosen=loadout.IndexOf(def.id)>=0;
                if(mobile)
                {
                    Round(list,def.id,"",x+12,y+4,88,()=>SelectSkill(def.id),def,chosen);
                    var name=kit.Text(list,def.LocalizedName(LevelHUD.Vietnamese),x+116,y+4,430,58,22,chosen?ComicTheme.Gold:(Color?)null,TextAlignmentOptions.MidlineLeft,true);name.enableAutoSizing=false;name.fontSize=22;
                    kit.Text(list,chosen?L("ĐÃ TRANG BỊ","EQUIPPED"):HubUI.ElementName(def.element,LevelHUD.Vietnamese),x+116,y+60,420,34,17,UiKit.Muted,TextAlignmentOptions.TopLeft,false);
                }
                else
                {
                    var b=kit.Button(list,def.LocalizedName(LevelHUD.Vietnamese),x+90,y+4,465,96,()=>SelectSkill(def.id),true,chosen,22);
                    kit.Image(list,"Skill icon",x+14,y+12,72,72,Color.white,HubUI.SkillIcon(def));
                }
            }
            kit.Text(card,L("Kỹ năng mới vào sẵn sàng. Kỹ năng bị gỡ giữ hồi chiêu.","New skills enter ready. Removed skills retain their cooldown."),34,mobile?670:630,mobile?800:980,50,20,null,TextAlignmentOptions.TopLeft,false);
            if(mobile)Round(card,"Close","",1090,658,88,Close,null,true,"OK");
            else kit.Button(card,L("TIẾP TỤC","CONTINUE"),916,688,290,72,Close,true,true,24);
            if(mobile)kit.Text(card,L("TIẾP TỤC","CONTINUE"),866,690,200,40,22,ComicTheme.Gold,TextAlignmentOptions.MidlineRight,false);
        }
        void Round(Transform parent,string name,string label,float x,float y,float size,Action click,SkillDefinition def=null,bool selected=false,string glyph="+")
        {
            var r=kit.Rect(parent,name,x,y,size,size);var disc=r.gameObject.AddComponent<RiftGraphic>();disc.Form=RiftGraphic.Shape.Disc;disc.color=selected?ComicTheme.Gold:ComicTheme.Navy;disc.raycastTarget=true;
            var ring=kit.Rect(r,"Ink circle",0,0,size,size).gameObject.AddComponent<RiftGraphic>();ring.Form=RiftGraphic.Shape.Ring;ring.Thickness=5;ring.color=ComicTheme.Ink;ring.raycastTarget=false;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=disc;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(()=>click());
            r.gameObject.AddComponent<RestRoundHit>();
            if(def!=null)kit.Image(r,"Icon",size*.2f,size*.2f,size*.6f,size*.6f,Color.white,HubUI.SkillIcon(def));
            else kit.Text(r,glyph,6,8,size-12,size-16,size*.4f,selected?ComicTheme.Ink:ComicTheme.Paper,TextAlignmentOptions.Center,false);
            if(!string.IsNullOrEmpty(label))kit.Text(parent,label,x-60,y+size+2,size+120,40,20,ComicTheme.Gold,TextAlignmentOptions.Center,false);
        }
        void OnDestroy(){Close();if(Instance==this)Instance=null;}
    }
    public sealed class RestRoundHit : MonoBehaviour,ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 point,Camera camera)
        {
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,point,camera,out var local))return false;
            return (local-rect.rect.center).sqrMagnitude<=Mathf.Pow(Mathf.Min(rect.rect.width,rect.rect.height)*.5f,2);
        }
    }
}
