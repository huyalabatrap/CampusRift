using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARHandsStudyHUD : MonoBehaviour
    {
        ARBattlefield field;ARMonsterDirector director;ARHandInteractions hands;ARKnowledgeSeal quiz;ARSealPractice rhythm;
        float introUntil;Transform battleRoot;TMP_Text instructions;
        RectTransform canvas,handPanel,quizPanel,answerPanel,rhythmPanel,lane;TMP_Text handText,question,quizHint,answers,rhythmTitle,rhythmStats,grade,tutor;
        readonly RectTransform[] noteRects=new RectTransform[12];readonly ARHandGraphic[] glyphs=new ARHandGraphic[12];
        static readonly string[] vi={"THIÊN THỦ · MỞ TAY","HẮC ĐỘNG · NẮM TAY","NGỰ LÔI · TRỎ LÊN","VẠN KIẾM · CHỮ V","HÀN BĂNG · CÁI XUỐNG","KIM CHUNG · CÁI LÊN"};
        static readonly string[] en={"GIANT HAND · OPEN PALM","BLACK HOLE · CLOSED FIST","LIGHTNING · POINT UP","SWORD RAIN · V SIGN","ICE SEAL · THUMB DOWN","GOLD BELL · THUMB UP"};
        static string L(string v,string e)=>LevelHUD.Vietnamese?v:e;
        void Start()
        {
            field=GetComponent<ARBattlefield>();director=GetComponent<ARMonsterDirector>();hands=GetComponent<ARHandInteractions>();quiz=GetComponent<ARKnowledgeSeal>();rhythm=GetComponent<ARSealPractice>();
            director.BattleStarted+=NewBattle;canvas=ARUI.Canvas(transform,"AR hands study HUD");canvas.GetComponentInParent<Canvas>(true).sortingOrder=15;
            handPanel=ARUI.Panel(canvas,"Hand physics help",-650,-330,340,130);handText=ARUI.Text(handPanel,"",0,0,314,116,17);
            quizPanel=ARUI.Panel(canvas,"Knowledge seal question",0,350,1180,132);question=ARUI.Text(quizPanel,"",0,22,1120,68,23);quizHint=ARUI.Text(quizPanel,"",0,-39,1120,40,17);question.fontStyle=FontStyles.Normal;
            answerPanel=ARUI.Panel(canvas,"Readable rune answers",0,-440,1300,128);answers=ARUI.Text(answerPanel,"",0,0,1260,116,18);answers.fontStyle=FontStyles.Normal;answers.alignment=TextAlignmentOptions.Left;answers.enableAutoSizing=true;answers.fontSizeMin=12;answers.fontSizeMax=18;
            rhythmPanel=ARUI.Panel(canvas,"Seal practice board",0,180,1360,132);
            rhythmTitle=ARUI.Text(rhythmPanel,L("LUYỆN ẤN · NHỊP CỬ CHỈ","SEAL PRACTICE · GESTURE RHYTHM"),-450,49,410,27,19);rhythmTitle.color=ComicTheme.Gold;
            rhythmStats=ARUI.Text(rhythmPanel,"",215,49,830,27,16);
            lane=ARUI.Panel(rhythmPanel,"Moving seals lane",0,0,1300,72);
            var beat=ARUI.Rect(lane,"Beat target ring",-470,0,62,62).gameObject.AddComponent<ARArcGraphic>();beat.Amount=1;beat.color=ComicTheme.Gold;beat.raycastTarget=false;
            ARUI.Text(lane,L("KẾT ẤN","CAST"),-470,0,110,24,13).color=ComicTheme.Gold;
            for(int i=0;i<noteRects.Length;i++)
            {noteRects[i]=ARUI.Rect(lane,"Approaching seal "+i,0,0,54,54);var bg=noteRects[i].gameObject.AddComponent<Image>();bg.sprite=ComicTheme.Sprite("round-mask");bg.color=ComicTheme.Purple;bg.raycastTarget=false;glyphs[i]=ARUI.Rect(noteRects[i],"Gesture glyph",0,0,36,44).gameObject.AddComponent<ARHandGraphic>();glyphs[i].raycastTarget=false;}
            grade=ARUI.Text(rhythmPanel,"",460,-49,320,26,19);grade.color=ComicTheme.Gold;
            tutor=ARUI.Text(rhythmPanel,"",-25,-49,530,26,16);
            instructions=ARUI.Text(canvas,L("Ô ấn đi từ phải sang trái · kết đúng lúc chạm vòng vàng\nPerfect ≤90ms · Great ≤200ms · Nhịp tăng dần · Hạ tay để nhả ấn","Seals move right to left · match as they touch the gold ring\nPerfect ≤90ms · Great ≤200ms · Increasing pace · Lower your hand to release"),0,57,1380,64,18);
            foreach(var text in new[]{handText,question,quizHint,answers,rhythmTitle,rhythmStats,grade,tutor,instructions})text.margin=Vector4.zero;
            foreach(var rect in new[]{rhythmPanel,lane,answerPanel})foreach(var image in rect.GetComponentsInChildren<Image>(true)){var color=image.color;color.a*=.42f;image.color=color;}
            ARHUDRegion.Add(handPanel,20);ARHUDRegion.Add(quizPanel,220);ARHUDRegion.Add(answerPanel,220);ARHUDRegion.Add(rhythmPanel,220);ARHUDRegion.Add(instructions.rectTransform,30);
        }
        void NewBattle(){introUntil=Time.unscaledTime+5;}
        void Update()
        {
            if(canvas==null)return;var hud=GetComponent<ARBattleHUD>();
            bool show=field.Root!=null&&field.Shrine!=null&&!field.placement.Adjusting&&!director.Finished&&!(hud?.ModalOpen??false);
            canvas.GetComponentInParent<Canvas>(true).gameObject.SetActive(show);if(!show)return;
            if(battleRoot!=field.Root){battleRoot=field.Root;introUntil=Time.unscaledTime+5;}
            bool practice=field.ModeSession.Has(ARModeFeature.Rhythm);instructions.gameObject.SetActive(practice&&rhythm.Elapsed<5);
            handPanel.gameObject.SetActive(!practice&&!quiz.Active&&field.ModeSession.Has(ARModeFeature.HandPhysics)&&(Time.unscaledTime<introUntil||Time.unscaledTime<hands.NoticeUntil));quizPanel.gameObject.SetActive(quiz.Active);answerPanel.gameObject.SetActive(quiz.Active&&quiz.AnswersClipped);rhythmPanel.gameObject.SetActive(practice);
            handText.text=Time.unscaledTime<hands.NoticeUntil?hands.Notice:L("VÉO: nhấc quái Choáng / Băng · vung rồi thả để ném\nQUẸT NGANG: kiếm khí · hồi ","PINCH: lift stunned / frozen foes · swing and release to throw\nSWIPE SIDEWAYS: sword wave · cooldown ")+hands.SwipeCooldown.ToString("0.0")+"s";
            if(quiz.Active)
            {question.text=L("ẤN TRI THỨC · ","KNOWLEDGE SEAL · ")+Mathf.CeilToInt(quiz.Remaining)+"s\n"+quiz.Session.Questions[0].Data.prompt;quizHint.text=string.IsNullOrEmpty(quiz.Notice)?L("Đưa TÂM MÀN HÌNH vào A–D rồi NẮM TAY · đúng: buff + LT · sai: sổ ôn","Aim the SCREEN CENTER at A–D, then close your FIST · correct: buff + LT · incorrect: notebook"):quiz.Notice;quizHint.enableAutoSizing=true;quizHint.fontSizeMin=14;quizHint.fontSizeMax=21;
                answers.text="";var options=quiz.Session.Questions[0].Options;for(int i=0;i<options.Count;i++)answers.text+=(i==quiz.Target?"> ":"   ")+((char)('A'+i))+". "+options[i].text+(i<options.Count-1?"\n":"");}
            if(!practice)return;
            rhythmStats.text=L("ĐIỂM ","SCORE ")+rhythm.Points+" · Perfect "+rhythm.Perfect+" · Great "+rhythm.Great+" · Miss "+rhythm.Miss+" · "+Mathf.CeilToInt((float)(ARSealPractice.Duration-rhythm.Elapsed))+"s";
            int slot=0;ARSealPractice.Note next=null;
            foreach(var note in rhythm.Notes)
            {
                if(note.judged)continue;if(next==null)next=note;double ahead=note.at-rhythm.Elapsed;
                if(ahead<-.2||ahead>4.6||slot>=noteRects.Length)continue;
                var rect=noteRects[slot];rect.gameObject.SetActive(true);rect.anchoredPosition=new Vector2(-470+(float)ahead*230,0);
                int g=System.Array.IndexOf(GestureSkillMapper.Labels,note.label);glyphs[slot].Gesture=g;glyphs[slot].SetVerticesDirty();slot++;
            }
            for(int i=slot;i<noteRects.Length;i++)noteRects[i].gameObject.SetActive(false);
            grade.text=Time.unscaledTime<rhythm.JudgmentUntil?rhythm.Judgment:field.Paused?L("TẠM DỪNG","PAUSED"):L("THEO NHỊP","FOLLOW THE BEAT");
            int index=next!=null?System.Array.IndexOf(GestureSkillMapper.Labels,next.label):-1;tutor.text=index>=0?(LevelHUD.Vietnamese?vi[index]:en[index]):L("HOÀN THÀNH","COMPLETE");
        }
        void OnDestroy(){if(director!=null)director.BattleStarted-=NewBattle;if(canvas!=null)Destroy(canvas.GetComponentInParent<Canvas>(true).gameObject);}
    }
}
