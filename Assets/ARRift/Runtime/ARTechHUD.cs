using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARTechHUD : MonoBehaviour
    {
        public bool Blocking=>panel!=null&&(panel.gameObject.activeSelf||warning.gameObject.activeSelf);
        ARTechSettings settings;ARDepthCollision depth;ARVoiceCommands voice;ARClipRecorder clip;ARSkillCaster caster;
        RectTransform panel,warning,dual,recording,backdrop;TMP_Text handsState,voiceState,recordState,dualState;ScrollRect scroll;readonly TMP_Text[] toggles=new TMP_Text[6];readonly ARHandGraphic[] trackedGlyphs=new ARHandGraphic[2];float nextPaint;
        static string L(string vi,string en)=>LevelHUD.Vietnamese?vi:en;
        void Start()
        {
            settings=GetComponent<ARTechSettings>();depth=GetComponent<ARDepthCollision>();voice=GetComponent<ARVoiceCommands>();clip=GetComponent<ARClipRecorder>();caster=GetComponent<ARSkillCaster>();
            var canvas=ARUI.Canvas(transform,"AR Tech2 HUD");canvas.GetComponentInParent<Canvas>().sortingOrder=25;
            recording=ARUI.Rect(canvas,"Recording indicator",-716,450,180,60);
            var dot=ARUI.Rect(recording,"Recording red dot",-60,0,14,14).gameObject.AddComponent<Image>();dot.sprite=ComicTheme.Sprite("round-mask");dot.color=ComicTheme.Red;dot.raycastTarget=false;
            var record=ARUI.Round(recording,"Stop recording",L("DỪNG","STOP"),8,0,60,Record);recordState=record.GetComponentInChildren<TMP_Text>();recording.gameObject.SetActive(false);
            dual=ARUI.Panel(canvas,"Two hand identity status",-650,-175,340,104);
            ARUI.Text(dual,L("HAI TAY · THIÊN THỦ ĐÔI","TWO HANDS · TWIN HANDS"),0,34,320,24,16).color=ComicTheme.Gold;
            for(int i=0;i<2;i++){var glyph=ARUI.Rect(dual,"Tracked hand "+i,-54+i*108,4,30,34).gameObject.AddComponent<ARHandGraphic>();glyph.Gesture=0;glyph.raycastTarget=false;trackedGlyphs[i]=glyph;}
            dualState=ARUI.Text(dual,"",0,-31,320,28,13);dualState.margin=Vector4.zero;
            backdrop=ARUI.Rect(canvas,"Technology backdrop",0,0,1920,1080);var shade=backdrop.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.78f);shade.raycastTarget=true;
            panel=ARUI.Panel(canvas,"Technology switches",0,0,1040,880);panel.GetComponent<Image>().raycastTarget=true;
            ARUI.Text(panel,L("CÔNG NGHỆ AR · TÙY CHỌN","AR TECHNOLOGY · OPTIONS"),-35,370,830,60,30);ARUI.Round(panel,"Close technology","X",450,370,64,()=>Open(false));
            var viewport=ARUI.Rect(panel,"Technology scroll viewport",0,-22,944,664);var surface=viewport.gameObject.AddComponent<Image>();surface.color=new Color(0,0,0,.01f);surface.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var rows=ARUI.Rect(viewport,"Technology scroll content",0,0,920,930);rows.anchorMin=rows.anchorMax=new Vector2(.5f,1);rows.pivot=new Vector2(.5f,1);rows.anchoredPosition=Vector2.zero;
            scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=rows;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            string[] vi={"Hai bàn tay","Chế độ giá đỡ","Va chạm đồ thật · Depth","Hô tên chiêu · ngoại tuyến","Phản chiếu phòng · Probe","Cho phép quay clip"};
            string[] en={"Two hands","Phone stand mode","Real objects · Depth collision","Voice spell · offline","Room reflections · Probe","Enable clip recording"};
            for(int i=0;i<6;i++){int index=i;float y=-46-i*86;var text=ARUI.Text(rows,LevelHUD.Vietnamese?vi[i]:en[i],-60,y,720,58,26);text.alignment=TextAlignmentOptions.Left;var button=ARUI.Round(rows,"Toggle technology "+i,"",382,y,64,()=>Toggle(index));toggles[i]=ARUI.Text(button.transform,"",0,0,60,54,20);}
            handsState=ARUI.Text(rows,"",0,-645,850,182,22);voiceState=ARUI.Text(rows,"",0,-824,850,144,21);
            foreach(RectTransform child in rows){var position=child.anchoredPosition;child.anchorMin=child.anchorMax=new Vector2(.5f,1);child.anchoredPosition=position;}
            ARUI.Text(panel,L("Vuốt để xem thêm tùy chọn và chú thích","Swipe for more options and details"),0,-398,920,30,18);
            warning=ARUI.Panel(canvas,"Clip privacy warning",0,0,1050,500);warning.GetComponent<Image>().raycastTarget=true;
            ARUI.Text(warning,L("CLIP CÓ HÌNH PHÒNG THẬT","CLIP INCLUDES YOUR REAL ROOM"),0,170,940,66,32);
            ARUI.Text(warning,L("Chỉ quay khi bạn đồng ý. Clip không có âm thanh.\nLưu vào thư viện máy; app không tự gửi đi đâu.\nTối đa 90 giây · bấm dừng bất cứ lúc nào.\nAndroid sẽ hỏi quyền ghi màn hình riêng.","Record only with your consent. Video has no audio.\nSaved to the device gallery; the app does not upload it.\nUp to 90 seconds · stop at any time.\nAndroid will request separate screen capture consent."),0,28,950,200,26);
            ARUI.Round(warning,"Cancel recording",L("HỦY","CANCEL"),-220,-155,112,()=>{warning.gameObject.SetActive(false);SyncPause();});
            ARUI.Round(warning,"Confirm recording",L("QUAY\nCLIP","RECORD"),220,-155,112,()=>{warning.gameObject.SetActive(false);SyncPause();clip.StartConfirmed();});
            panel.gameObject.SetActive(false);warning.gameObject.SetActive(false);backdrop.gameObject.SetActive(false);
            ARHUDRegion.Add(dual,40);ARHUDRegion.Add(recording,190);
        }
        void SyncPause(){GetComponent<ARBattleHUD>().SetMenu(false);}
        public void Open(bool value){panel.gameObject.SetActive(value);warning.gameObject.SetActive(false);backdrop.gameObject.SetActive(value);if(value)scroll.verticalNormalizedPosition=1;SyncPause();}
        public void Record(){if(clip.Recording){clip.Stop();return;}if(!clip.Pending&&settings.RecordingAllowed){panel.gameObject.SetActive(false);warning.gameObject.SetActive(true);backdrop.gameObject.SetActive(true);SyncPause();}else Open(true);}
        void Toggle(int i){switch(i){case 0:settings.TwoHands=!settings.TwoHands;break;case 1:settings.StandMode=!settings.StandMode;break;case 2:settings.DepthCollision=!settings.DepthCollision;break;case 3:voice.Toggle();break;case 4:settings.RoomProbe=!settings.RoomProbe;break;case 5:settings.Recording=!settings.Recording;break;}}
        void Update()
        {
            if(panel==null||Time.unscaledTime<nextPaint)return;nextPaint=Time.unscaledTime+.1f;bool[] state={settings.TwoHands,settings.StandMode,settings.DepthCollision,settings.Voice,settings.RoomProbe,settings.Recording};
            backdrop.gameObject.SetActive(Blocking);recording.gameObject.SetActive(clip.Recording&&!Blocking);var battleHud=GetComponent<ARBattleHUD>();bool showDual=settings.TwoHandsAllowed&&!battleHud.ModalOpen&&caster.field.Root!=null&&!caster.field.placement.Adjusting&&caster.field.Mode.winRule!=ARWinRule.CompleteRhythm&&!(GetComponent<ARKnowledgeSeal>()?.Active??false);
            dual.gameObject.SetActive(showDual);dualState.text=L("Tay A #","Hand A #")+caster.twoHands.Identity[0]+" · "+L("Tay B #","Hand B #")+caster.twoHands.Identity[1]+"\n"+L("Ngắm tâm · hạ hai tay để nhả","Center aim · lower both hands to release");
            for(int i=0;i<2;i++){int glyph=System.Array.IndexOf(GestureSkillMapper.Labels,caster.twoHands.Pose[i]);trackedGlyphs[i].Gesture=Mathf.Max(0,glyph);trackedGlyphs[i].color=glyph>=0?ComicTheme.Gold:ComicTheme.Muted;trackedGlyphs[i].SetVerticesDirty();}
            for(int i=0;i<6;i++){toggles[i].text=state[i]?L("BẬT","ON"):L("TẮT","OFF");toggles[i].color=state[i]?ComicTheme.Gold:ComicTheme.Paper;}
            handsState.text=L("2 tay: ","Two hands: ")+(settings.TwoHandsAllowed?L("sẵn sàng","ready"):L("cần máy đủ mạnh hoặc giá đỡ · nhiệt/FPS luôn ưu tiên","needs a capable device or stand · thermal/FPS gate always applies"))+"\n"+L("MỞ + MỞ: Thiên Thủ đôi    |    NẮM + V: tuyệt kỹ khi Ấn đầy","PALM + PALM: Twin Hands    |    FIST + V: ultimate with full seals");
            voiceState.text=L("Depth: ","Depth: ")+(depth.Supported?L("hỗ trợ","supported"):L("thiết bị chưa hỗ trợ","device unavailable"))+L(" · tối đa 10 ray/frame.\nGiọng nói mặc định tắt · quyền micro riêng · không lưu/gửi âm thanh."," · up to 10 rays/frame.\nVoice is off by default · separate mic permission · no audio storage/upload.");
            recordState.text=L("DỪNG","STOP");
        }
    }
}
