using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Progression;
using CampusRift.Levels;
namespace CampusRift.UI
{
    public sealed partial class HubUI
    {
        public enum EndgamePage { Tower, Nightmare, Achievements, Records, Costumes }
        public EndgamePage EndgameCurrent {get;private set;}
        GameObject costumeStage;RenderTexture costumeTexture;
        public PlayerCostume PreviewCostume {get;private set;}
        public void OpenEndgame(EndgamePage page){EndgameCurrent=page;Select(Tab.Endgame);}
        static string RuleName(TowerRule rule,bool vn)=>rule==TowerRule.FireMoon?(vn?"Hỏa Nguyệt · quái Hỏa +30% máu/sát thương":"Fire Moon · fire enemies +30% health/damage"):rule==TowerRule.Lightning?(vn?"Lôi Kiếp · sét báo trước 1,2 giây":"Lightning · strikes warn for 1.2 seconds"):(vn?"Tĩnh Lặng · không có Tầm Yêu":"Silence · enemy finder disabled");
        void BuildEndgame()
        {
            var p=ProfileService.Instance;var e=EndgameService.State(p.Data);
            kit.Panel(content,"Endgame",0,0,1800,750);
            var endgameTitle=kit.Text(content,L("BEYOND THE RIFT","SAU KHE NỨT"),30,12,650,65,34,UiKit.Gold,TextAlignmentOptions.MidlineLeft,false);endgameTitle.margin=new Vector4(9,8,14,8);
            kit.Text(content,L("Cosmetic rewards · shared limit ","Thưởng thẩm mỹ · trần chung ")+EndgameService.Available(p.Data,DateTime.UtcNow)+"/100 "+L("stones left today (UTC)","Linh Thạch còn lại hôm nay (UTC)"),700,22,1060,44,23,UiKit.Muted,TextAlignmentOptions.MidlineRight,false);
            string[] vn={"THÁP","ÁC MỘNG","THÀNH TỰU","KỶ LỤC","TRANG PHỤC"},en={"TOWER","NIGHTMARE","ACHIEVEMENTS","RECORDS","COSTUMES"};
            for(int i=0;i<5;i++){var page=(EndgamePage)i;CompactAction(content,Vn?vn[i]:en[i],(i+1).ToString(),30+i*350,86,328,()=>OpenEndgame(page),true,EndgameCurrent==page);}
            switch(EndgameCurrent)
            {
                case EndgamePage.Nightmare:DrawNightmare(p);break;
                case EndgamePage.Achievements:DrawAchievements(p);break;
                case EndgamePage.Records:DrawRecords(p);break;
                case EndgamePage.Costumes:DrawCostumes(p);break;
                default:
                    kit.Text(content,L("TRIAL TOWER","THÁP THÍ LUYỆN"),50,194,1000,60,42,UiKit.Gold);
                    kit.Text(content,L("Highest cleared floor: ","Tầng cao nhất đã qua: ")+e.highestFloor,50,275,1000,52,32);
                    kit.Text(content,RuleName(EndgameFactory.Weekly(DateTime.UtcNow),Vn),50,350,1280,56,28);
                    kit.Text(content,L("Weekly rule rotates on Monday UTC. Each floor: +8% health/damage, capped at ×5. Every 5 floors: elites; every 10: Shaban. Up to 32 enemies, platform concurrency caps retained.","Luật đổi mỗi thứ Hai UTC. Mỗi tầng: +8% máu/sát thương, tối đa ×5. Mỗi 5 tầng có tinh anh, mỗi 10 có Shaban. Tối đa 32 quái/tầng, giữ giới hạn quái đồng thời."),50,420,1250,114,26);
                    kit.Text(content,L("10 stones/floor; Tower + Nightmare share 100/day. Study remains the main source of power.","10 Linh Thạch/tầng; Tháp + Ác Mộng chung 100/ngày. Học vẫn là nguồn sức mạnh chính."),50,562,1250,80,24,UiKit.Muted);
                    bool open=EndgameService.TowerOpen(p.Data);
                    ActionButton(content,L("ENTER FLOOR 1","VÀO TẦNG 1"),"GO",1390,340,330,()=>Prepare(EndgameFactory.Tower(1,DateTime.UtcNow)),open);
                    if(!open)kit.Text(content,L("Clear normal level 10 to unlock.","Qua màn 10 thường để mở."),1380,515,340,110,25,UiKit.Danger);
                    break;
            }
        }
        void Prepare(LevelDefinition d){LoadoutUI.Level=d;UIStateManager.Instance.OpenLoadout();}
        Button CompactAction(Transform parent,string label,string glyph,float x,float y,float width,Action click,bool enabled=true,bool selected=false)
        {
            if(!Controls.CampusInput.Mobile)return kit.Button(parent,label,x,y,width,78,click,enabled,selected,22);
            const float size=78;var r=kit.Rect(parent,"Round "+label,x+width-size-6,y,size,size);
            var disc=r.gameObject.AddComponent<RiftGraphic>();disc.Form=RiftGraphic.Shape.Disc;disc.color=selected?ComicTheme.Gold:ComicTheme.Navy;
            var ring=kit.Rect(r,"Ink",0,0,size,size).gameObject.AddComponent<RiftGraphic>();ring.Form=RiftGraphic.Shape.Ring;ring.Thickness=4;ring.color=ComicTheme.Ink;ring.raycastTarget=false;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=disc;b.interactable=enabled;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(()=>{UIAudioManager.Instance?.Click();click();});r.gameObject.AddComponent<RestRoundHit>();
            kit.Text(r,glyph,8,8,62,62,23,selected?ComicTheme.Ink:ComicTheme.Paper,TextAlignmentOptions.Center,false);
            kit.Text(parent,label,x+8,y+12,width-size-24,54,20,UiKit.Gold,TextAlignmentOptions.MidlineRight,false);return b;
        }
        Button ActionButton(Transform parent,string label,string glyph,float x,float y,float width,Action click,bool enabled=true)
        {
            if(!Controls.CampusInput.Mobile)return kit.Button(parent,label,x,y,width,78,click,enabled,true,23);
            float size=112;var r=kit.Rect(parent,"Round "+label,x+(width-size)/2,y,size,size);
            var disc=r.gameObject.AddComponent<RiftGraphic>();disc.Form=RiftGraphic.Shape.Disc;disc.color=enabled?ComicTheme.Gold:ComicTheme.PanelColor;
            var ring=kit.Rect(r,"Ink",0,0,size,size).gameObject.AddComponent<RiftGraphic>();ring.Form=RiftGraphic.Shape.Ring;ring.Thickness=5;ring.color=ComicTheme.Ink;ring.raycastTarget=false;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=disc;b.interactable=enabled;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(()=>{UIAudioManager.Instance?.Click();click();});r.gameObject.AddComponent<RestRoundHit>();
            kit.Text(r,glyph,12,20,88,68,30,ComicTheme.Ink,TextAlignmentOptions.Center,false);
            kit.Text(parent,label,x,y+116,width,42,20,UiKit.Gold,TextAlignmentOptions.Center,false);return b;
        }
        void DrawNightmare(ProfileService p)
        {
            kit.Text(content,L("+50% enemy stats · two affixes per elite · fire cycle −20% · 20 stones/clear","Quái +50% chỉ số · tinh anh 2 phụ tố · chu kỳ Thiên Hỏa −20% · 20 Linh Thạch/lượt"),35,185,1730,44,24);
            var list=kit.Scroll(content,"Nightmare list",30,240,1740,465,950);
            for(int i=1;i<=10;i++)
            {
                int index=i;float y=(i-1)*94;bool open=EndgameService.NightmareOpen(p.Data,i);var n=EndgameService.State(p.Data).Nightmare(i);
                kit.Panel(list,"Nightmare "+i,4,y+4,1710,88);
                kit.Text(list,L("LEVEL ","MÀN ")+i+" · "+LevelCatalog.Instance.Get(i).LocalizedName(Vn),30,y+18,740,48,26);
                kit.Text(list,open?(n?.cleared??false?L("Badge earned","Đã có huy hiệu"):L("Ready","Sẵn sàng")):L("Requires three normal stars","Cần ba sao màn thường"),810,y+24,500,46,23,open?UiKit.Gold:UiKit.Muted);
                // Round mobile entry fits the row; labels are separate from the circle.
                CompactAction(list,L("PREPARE","CHUẨN BỊ"),"GO",1380,y+8,300,()=>Prepare(EndgameFactory.Nightmare(index)),open,true);
            }
        }
        void DrawAchievements(ProfileService p)
        {
            string[] groups={"study","combat","explore"};string[] names={L("STUDY","HỌC TẬP"),L("COMBAT","CHIẾN ĐẤU"),L("EXPLORATION","KHÁM PHÁ")};
            for(int col=0;col<3;col++)
            {
                kit.Text(content,names[col],45+col*585,185,550,40,26,UiKit.Gold);
                var entries=EndgameService.Achievements.Where(a=>a.group==groups[col]).ToArray();var list=kit.Scroll(content,names[col],35+col*585,235,560,480,entries.Length*208);
                for(int i=0;i<entries.Length;i++)
                {
                    var a=entries[i];bool open=EndgameService.TitleOpen(p.Data,a.id);float y=i*208;
                    kit.Panel(list,a.id,5,y+5,540,194,open);
                    kit.Text(list,Vn?a.nameVN:a.nameEN,26,y+20,484,48,29,open?ComicTheme.Navy:UiKit.Gold);
                    kit.Text(list,Vn?a.conditionVN:a.conditionEN,26,y+69,484,62,22,open?ComicTheme.Navy:UiKit.Muted);
                    CompactAction(list,open?L("EQUIP TITLE","MANG DANH HIỆU"):L("LOCKED","CHƯA MỞ"),"OK",24,y+116,480,()=>{EndgameService.EquipTitle(p,a.id);OpenEndgame(EndgamePage.Achievements);},open,open);
                }
            }
        }
        void DrawRecords(ProfileService p)
        {
            kit.Text(content,L("LOCAL RECORDS · Highest tower floor ","KỶ LỤC TRÊN MÁY · Tầng Tháp cao nhất ")+EndgameService.State(p.Data).highestFloor,40,190,1680,44,28,UiKit.Gold);
            kit.Text(content,L("LEVEL","MÀN"),72,245,180,40,25);
            kit.Text(content,L("NORMAL","THƯỜNG"),460,245,500,40,25);
            kit.Text(content,L("NIGHTMARE / STARS","ÁC MỘNG / SAO"),1060,245,600,40,25);
            var list=kit.Scroll(content,"Records",40,302,1710,400,660);
            for(int i=1;i<=10;i++)
            {
                float y=(i-1)*64;var normal=p.Data.Level(i,false);var n=EndgameService.State(p.Data).Nightmare(i);
                kit.Text(list,i.ToString(),32,y,170,52,25);
                kit.Text(list,normal!=null&&normal.bestTime>0?LevelResultUI.FormatTime(normal.bestTime):"—",420,y,500,52,25);
                kit.Text(list,n!=null&&n.bestTime>0?LevelResultUI.FormatTime(n.bestTime)+" / "+Convert.ToString(n.stars,2).Count(c=>c=='1')+L(" stars"," sao"):"—",1020,y,600,52,25);
            }
        }
        void DrawCostumes(ProfileService p)
        {
            kit.Text(content,L("APPEARANCE ONLY · No stat bonuses","CHỈ ĐỔI NGOẠI HÌNH · Không cộng chỉ số"),40,185,1720,48,27,UiKit.Gold);
            var list=kit.Scroll(content,"Costumes",35,244,990,460,PlayerCostume.Catalog.Length*186);
            for(int i=0;i<PlayerCostume.Catalog.Length;i++)
            {
                var c=PlayerCostume.Catalog[i];float y=i*186;bool open=PlayerCostume.Open(p.Data,c);var a=EndgameService.Achievements.FirstOrDefault(x=>x.id==c.achievement);
                kit.Panel(list,c.id,5,y+4,964,174);
                kit.Text(list,Vn?c.vn:c.en,28,y+18,590,46,30,UiKit.Gold);
                kit.Text(list,a==null?L("Available","Có sẵn"):L("Unlock: ","Mở: ")+(Vn?a.nameVN:a.nameEN),28,y+74,590,74,24,UiKit.Muted);
                ActionButton(list,EndgameService.State(p.Data).costume==c.id?L("EQUIPPED","ĐANG MẶC"):L("EQUIP","MẶC"),"OK",630,y+16,300,()=>{PlayerCostume.Equip(p,c.id);OpenEndgame(EndgamePage.Costumes);},open);
            }
            kit.Panel(content,"Preview",1060,244,690,460);
            CreatePreview();var rect=kit.Rect(content,"Live costume preview",1070,252,670,446);var raw=rect.gameObject.AddComponent<RawImage>();raw.texture=costumeTexture;raw.raycastTarget=false;
        }
        void CreatePreview()
        {
            var prefab=Resources.Load<GameObject>("P22/PlayerPreview");if(prefab==null)return;
            costumeStage=new GameObject("Costume preview stage");costumeStage.transform.position=new Vector3(0,-1000,0);
            var hero=Instantiate(prefab,costumeStage.transform);hero.transform.localPosition=Vector3.zero;hero.transform.localRotation=Quaternion.identity;
            foreach(var t in hero.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            PreviewCostume=hero.AddComponent<PlayerCostume>();PreviewCostume.Apply(EndgameService.State(ProfileService.Instance.Data).costume);
            var renderers=hero.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var go=new GameObject("Preview camera");go.transform.SetParent(costumeStage.transform,false);var cam=go.AddComponent<Camera>();cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.04f,.07f,.13f);cam.orthographic=true;cam.orthographicSize=bounds.size.y*.59f;cam.transform.position=bounds.center+new Vector3(0,0,4);cam.transform.LookAt(bounds.center);cam.nearClipPlane=.05f;cam.farClipPlane=10;
            costumeTexture=new RenderTexture(672,448,24);cam.targetTexture=costumeTexture;
            var light=new GameObject("Preview light");light.transform.SetParent(costumeStage.transform,false);var lamp=light.AddComponent<Light>();lamp.type=LightType.Directional;lamp.cullingMask=1<<31;lamp.intensity=1.5f;light.transform.rotation=Quaternion.Euler(30,-35,0);
        }
        void ClearEndgamePreview(){if(costumeStage!=null)Destroy(costumeStage);costumeStage=null;PreviewCostume=null;if(costumeTexture!=null){costumeTexture.Release();Destroy(costumeTexture);}costumeTexture=null;}
    }
}
