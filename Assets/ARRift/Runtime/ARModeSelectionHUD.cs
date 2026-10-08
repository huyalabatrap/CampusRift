using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.UI;
using CampusRift.Progression;
namespace CampusRift.AR
{
    public sealed class ARModeSelectionHUD : MonoBehaviour
    {
        ARBattlefield field;ARModeSession session;RectTransform canvas,modePage,stagePage,scorePage;
        ARGameMode selected;int stage=1;bool daily;bool opened=true;int page;
        TMP_Text chosen,footer,scoreText;TMP_Text[] modeTitles,modeDescriptions,modeStars,stageTexts;Button[] modeButtons,stageButtons;Button start;Image[][] modeBadges,stageBadges;
        static string L(string vi,string en)=>LevelHUD.Vietnamese?vi:en;
        public bool Blocking=>opened||session!=null&&!session.SelectionConfirmed;
        void Start()
        {
            field=GetComponent<ARBattlefield>();session=field.ModeSession;selected=ARModeCatalog.Training;
            canvas=ARUI.Canvas(transform,"AR mode selection");canvas.GetComponentInParent<Canvas>(true).sortingOrder=100;
            var backdrop=ARUI.Rect(canvas.parent,"Mode backdrop",0,0,0,0);backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.sizeDelta=Vector2.zero;backdrop.SetAsFirstSibling();var shade=backdrop.gameObject.AddComponent<Image>();shade.color=new Color(.025f,.035f,.08f,.98f);shade.raycastTarget=true;
            ARUI.Text(canvas,L("AR RIFT · CHỌN CHẾ ĐỘ","AR RIFT · SELECT MODE"),0,451,1400,84,42).color=ComicTheme.Gold;
            ARUI.Round(canvas,"Exit modes","X",865,450,70,ARSessionBootstrap.Exit);
            modePage=ARUI.Rect(canvas,"Mode cards",0,0,1920,1080);stagePage=ARUI.Rect(canvas,"Ten stages",0,0,1920,1080);scorePage=ARUI.Rect(canvas,"Local scores",0,0,1920,1080);
            var modes=ARModeCatalog.All;modeTitles=new TMP_Text[modes.Length];modeDescriptions=new TMP_Text[modes.Length];modeStars=new TMP_Text[modes.Length];modeButtons=new Button[modes.Length];modeBadges=new Image[modes.Length][];
            for(int i=0;i<modes.Length;i++)
            {
                var m=modes[i];float x=(i%3-1)*555,y=i<3?220:-95;var card=ARUI.Panel(modePage,m.id,x,y,515,285);card.GetComponent<Image>().raycastTarget=true;
                modeTitles[i]=ARUI.Text(card,"",0,86,475,60,31);modeTitles[i].color=m.playable?ComicTheme.Gold:ComicTheme.Muted;
                modeDescriptions[i]=ARUI.Text(card,"",-15,9,450,108,22);modeStars[i]=ARUI.Text(card,"",-55,-70,340,52,22);modeBadges[i]=Badges(card,-55,-109);
                modeButtons[i]=ARUI.Round(card,"Select "+m.id,m.playable?">":"...",195,-84,70,()=>{selected=m;stage=0;daily=false;});modeButtons[i].interactable=m.playable;
            }
            var dailyCard=ARUI.Panel(modePage,"Daily challenge",555,-95,515,285);ARUI.Text(dailyCard,L("THỬ THÁCH NGÀY","DAILY CHALLENGE"),0,86,475,60,30).color=ComicTheme.Gold;
            ARUI.Text(dailyCard,L("Mode luân phiên · cùng seed UTC\nThủ Trận / Truy Rift / Đấu Long","Rotating mode · one UTC seed\nDefense / Rift Hunt / Dragon Duel"),-15,9,450,94,23);
            ARUI.Text(dailyCard,L("3–6 phút · trần AR riêng","3–6 minutes · separate AR cap"),-55,-87,340,66,22);ARUI.Round(dailyCard,"Select daily",">",195,-84,70,()=>{selected=ARModeCatalog.DailyMode(System.DateTime.UtcNow);stage=0;daily=true;});
            stageTexts=new TMP_Text[10];stageButtons=new Button[10];stageBadges=new Image[10][];
            for(int i=0;i<10;i++)
            {
                int n=i+1;var card=ARUI.Panel(stagePage,"AR stage "+n,(i%5-2)*333,i<5?185:-120,306,267);
                stageTexts[i]=ARUI.Text(card,"",0,49,278,144,23);stageBadges[i]=Badges(card,0,-32);stageButtons[i]=ARUI.Round(card,"Select stage "+n,">",0,-91,70,()=>{selected=ARModeCatalog.StageMode(n);stage=n;daily=false;});
            }
            scoreText=ARUI.Text(scorePage,"",0,70,1430,620,25);
            chosen=ARUI.Text(canvas,"",0,-296,1580,52,27);
            ARUI.Round(canvas,"Modes tab",L("CHẾ\nĐỘ","MODES"),-735,-420,100,()=>page=0);
            ARUI.Round(canvas,"Stages tab",L("10\nẢI","10\nSTAGES"),-540,-420,100,()=>page=1);
            ARUI.Round(canvas,"Scores tab",L("ĐIỂM","SCORES"),-345,-420,100,()=>page=2);
            ARUI.Round(canvas,"Active combat",L("CHỦ\nĐỘNG","ACTIVE"),135,-420,100,()=>GetComponent<ARCombatHUD>().ToggleActive());
            ARUI.Round(canvas,"Knowledge quiz",L("TRI\nTHỨC","QUIZ"),-105,-420,100,()=>session.KnowledgeEnabled=!session.KnowledgeEnabled);
            ARUI.Round(canvas,"Seal cosmetic",L("VỆT\nẤN","SEALS"),345,-420,100,CycleSeal);
            ARUI.Round(canvas,"Return modes",L("QUAY\nLẠI","BACK"),540,-420,100,()=>{if(session.SelectionConfirmed)opened=false;else page=0;});
            start=ARUI.Round(canvas,"Start selected",L("VÀO\nTRẬN","START"),735,-420,125,Confirm);
            footer=ARUI.Text(canvas,"",-70,-502,1650,52,20);
        }
        public void Open()
        {
            field=GetComponent<ARBattlefield>();session=field.ModeSession;
            if(field.Root!=null)field.placement.Reposition();
            session.Reopen();opened=true;page=0;
        }
        public void ShowStages(){opened=true;page=1;}
        void Confirm()
        {session.Choose(selected,stage,daily);if(session.SelectionConfirmed)opened=false;}
        void CycleSeal()
        {
            var p=ProfileService.Ensure();var s=ARProgression.Read(p.Data);var available=s.cosmetics;
            if(available==null||available.Count==0)return;int current=available.IndexOf(s.selectedSeal);ARProgression.SelectSeal(p,current+1<available.Count?available[current+1]:"");
        }
        void Update()
        {
            if(canvas==null)return;var consent=FindAnyObjectByType<ARSessionBootstrap>();bool show=Blocking&&(consent==null||consent.Accepted);canvas.GetComponentInParent<Canvas>(true).gameObject.SetActive(show);if(!show)return;
            var p=ProfileService.Ensure().Data;var modes=ARModeCatalog.All;
            for(int i=0;i<modes.Length;i++){var m=modes[i];modeTitles[i].text=m.Title(LevelHUD.Vietnamese);modeDescriptions[i].text=LevelHUD.Vietnamese?m.descriptionVN:m.descriptionEN;modeStars[i].text=(LevelHUD.Vietnamese?m.durationVN:m.durationEN)+(m.playable?"":L(" · ĐANG PHÁT TRIỂN"," · IN DEVELOPMENT"));modeStars[i].fontSize=m.playable?22:18;ShowBadges(modeBadges[i],ARProgression.ModeStars(p,m.id));modeButtons[i].targetGraphic.color=ComicTheme.Navy;}
            for(int i=0;i<10;i++){int n=i+1;bool unlocked=ARProgression.StageOpen(p,n);stageTexts[i].text=L("ẢI ","STAGE ")+n+"\n"+ARModeCatalog.Stages[i].Title(LevelHUD.Vietnamese)+(unlocked?"":L("\nQua Ải trước để mở","\nClear the previous stage"));ShowBadges(stageBadges[i],ARProgression.Stars(p,n));stageButtons[i].interactable=unlocked;stageButtons[i].targetGraphic.color=ComicTheme.Navy;}
            modePage.gameObject.SetActive(page==0);stagePage.gameObject.SetActive(page==1);scorePage.gameObject.SetActive(page==2);
            chosen.text=(session.KnowledgeEnabled?L("QUIZ BẬT · ","QUIZ ON · "):L("QUIZ TẮT · ","QUIZ OFF · "))+(session.ActiveCombat?L("CHỦ ĐỘNG (SÀN) · ","ACTIVE (FLOOR) · "):L("AN TOÀN · ","SAFE · "))+(daily?L("THỬ THÁCH NGÀY · ","DAILY CHALLENGE · ")+System.DateTime.UtcNow.ToString("yyyy-MM-dd"):selected.Title(LevelHUD.Vietnamese)+(stage>0?L(" · ẢI "," · STAGE ")+stage:""));
            start.interactable=selected.playable&&(stage==0||ARProgression.StageOpen(p,stage));
            var progress=ARProgression.Read(p);string seal=progress.selectedSeal==ARProgression.GoldSeal?L("Vàng","Gold"):progress.selectedSeal==ARProgression.AzureSeal?L("Lam","Azure"):L("Tắt","Off");
            footer.text=L("AR còn ","AR remaining ")+ARProgression.Available(p,System.DateTime.UtcNow)+"/60 LT · "+ARProgression.TotalStars(p)+L("/30 sao · Vệt ấn ","/30 stars · Seal ")+seal+L(" (mở tại 3 / 15 sao)"," (unlock at 3 / 15 stars)")+(DevMode.Active?L(" · DEV: không lưu"," · DEV: no save"):"");
            if(page==2){var scores=progress.scores?.OrderByDescending(s=>s.score).Take(8).ToArray();scoreText.text=L("BẢNG ĐIỂM CÁ NHÂN · TRÊN MÁY NÀY","PERSONAL SCORES · ON THIS DEVICE")+"\n\n";if(scores==null||scores.Length==0)scoreText.text+=L("Chưa có trận hoàn tất.","No completed runs yet.");else foreach(var s in scores){var m=modes.FirstOrDefault(a=>a.id==s.modeId);scoreText.text+=(m!=null?m.Title(LevelHUD.Vietnamese):s.modeId)+" · "+s.score+L(" điểm · "," pts · ")+StarText(s.stars)+" · "+s.utcDay+"\n";}scoreText.text+="\n"+L("Truy Rift: điểm đóng Rift + thời gian còn lại. Luyện Ấn: Perfect 1000 / Great 600; lưu điểm cao trên máy.","Rift Hunt: closure points + time bonus. Seal Practice: Perfect 1000 / Great 600; local best scores.");}
        }
        static Image[] Badges(Transform parent,float x,float y){var images=new Image[3];for(int i=0;i<3;i++){images[i]=ARUI.Rect(parent,"AR star "+i,x+(i-1)*30,y,25,25).gameObject.AddComponent<Image>();images[i].raycastTarget=false;images[i].preserveAspect=true;}return images;}
        static void ShowBadges(Image[] images,int stars){for(int i=0;i<3;i++)images[i].sprite=ContentImages.Star(i<stars);}
        public static string StarText(int n)=>Mathf.Clamp(n,0,3)+(LevelHUD.Vietnamese?"/3 sao":"/3 stars");
        void OnDestroy(){if(canvas!=null)Destroy(canvas.GetComponentInParent<Canvas>(true).gameObject);}
    }
}

