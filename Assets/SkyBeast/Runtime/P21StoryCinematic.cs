using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using CampusRift.UI;
using CampusRift.Progression;
namespace CampusRift.SkyBeast
{
    [DefaultExecutionOrder(32001)]
    public sealed class P21StoryCinematic:MonoBehaviour
    {
        public static P21StoryCinematic Active {get;private set;}
        public static bool LastSkipped {get;private set;}
        public bool Playing {get;private set;}
        public bool Skipped {get;private set;}
        public bool Ending {get;private set;}
        public bool CreditsVisible {get;private set;}
        public float Duration {get;private set;}
        public float Progress=>Mathf.Clamp01(age/Duration);
        public bool HoldForCapture;
        public SkyBeastController RevealDragon=>dragon;
        float age,lastTick;bool exit;Camera cam;Vector3 camPosition,playerPosition;Quaternion camRotation,playerRotation;float fov,brightness;SkyPreset preset;AudioSource ambient;
        Canvas overlay;TMP_Text subtitle,title,pageLabel;Button skip;GameObject creditPanel;int creditPage;string[] pages;
        CampusExplorer explorer;bool explorerEnabled,inputLocked;Controls.CampusInput input;
        SkyLightingController sky;SkyBeastController dragon;P21RiftVisual rift;Vector3 center,riftPoint;bool roared;
        FireBreathCycle fire;SkyBeastScheduler scheduler;bool firePaused,schedulerPaused;
        readonly List<Canvas> hidden=new List<Canvas>();
        readonly List<Renderer> avatarHidden=new List<Renderer>();
        public static IEnumerator Play(bool ending)
        {
            CancelActive();while(Time.timeScale<=0)yield return null;
            var c=new GameObject(ending?"P21 campus dawn / credits":"P21 Long Vuong reveal").AddComponent<P21StoryCinematic>();
            c.Begin(ending);if(!c.Playing){Destroy(c.gameObject);yield break;}
            try {while(c!=null&&c.Playing)yield return null;}finally{if(c!=null){c.Restore();Destroy(c.gameObject);}}
        }
        public static void CancelActive(){var c=Active;if(c!=null){c.Restore();Destroy(c.gameObject);}}
        void Begin(bool ending)
        {
            cam=Camera.main;if(cam==null)return;Active=this;Playing=true;Ending=ending;LastSkipped=false;
            Duration=ending?20:ProfileService.Instance?.Data.longVuongRevealSeen==true?3.5f:7;
            lastTick=Time.realtimeSinceStartup;
            camPosition=cam.transform.position;camRotation=cam.transform.rotation;fov=cam.fieldOfView;
            explorer=FindAnyObjectByType<CampusExplorer>();explorerEnabled=explorer!=null&&explorer.enabled;
            if(explorer!=null){playerPosition=explorer.transform.position;playerRotation=explorer.transform.rotation;explorer.CancelDash();explorer.enabled=false;input=explorer.GetComponent<Controls.CampusInput>();}
            if(input!=null){inputLocked=input.UltimateLocked;input.UltimateLocked=true;input.ResetAll();}
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            sky=SettingsManager.Instance?.Sky;if(sky!=null){brightness=sky.Brightness;preset=sky.Preset;sky.SetPreset(SkyPreset.Inferno);sky.SetBrightness(Mathf.Max(brightness,.7f));}
            center=new Vector3(0,0,-5);var graph=ShelterGraphReference.Graph;
            if(graph!=null&&graph.Nodes.Length>0){var bounds=new Bounds(graph.Nodes[0].WorldPosition,Vector3.zero);foreach(var n in graph.Nodes)bounds.Encapsulate(n.WorldPosition);center=new Vector3(bounds.center.x,0,bounds.center.z);}
            fire=FireBreathCycle.Instance;if(fire!=null){firePaused=fire.CinematicPaused;fire.CinematicPaused=true;}
            scheduler=SkyBeastScheduler.Instance;if(scheduler!=null){schedulerPaused=scheduler.CinematicPaused;scheduler.CinematicPaused=true;}
            overlay=ComboUIFactory.Canvas("Ending skip",transform,90);overlay.gameObject.AddComponent<GraphicRaycaster>();
            Bar("Top",new Vector2(1920,85),new Vector2(0,497));Bar("Bottom",new Vector2(1920,160),new Vector2(0,-460));
            title=ComboUIFactory.Text("Story title",overlay.transform,new Vector2(1600,65),new Vector2(0,497),34);title.color=ComicTheme.Gold;
            title.text=ending?(LevelHUD.Vietnamese?"CAMPUS RIFT · BÌNH MINH":"CAMPUS RIFT · DAWN"):(LevelHUD.Vietnamese?"CỬU U HỎA LONG VƯƠNG":"THE NINE HORN FIRE DRAGON KING");
            subtitle=ComboUIFactory.Text("Story subtitle",overlay.transform,new Vector2(1330,110),new Vector2(-200,-460),28);subtitle.textWrappingMode=TextWrappingModes.Normal;
            var sr=ComboUIFactory.Rect("Skip",overlay.transform,new Vector2(300,65),new Vector2(760,-475));ComicTheme.Frame(sr.gameObject,"button-red",true);skip=sr.gameObject.AddComponent<Button>();skip.onClick.AddListener(Skip);
            var skipLabel=ComboUIFactory.Text("Skip label",sr,new Vector2(280,55),Vector2.zero,24);skipLabel.margin=new Vector4(5,4,7,12);skipLabel.text=Controls.CampusInput.Mobile?(LevelHUD.Vietnamese?"BỎ QUA":"SKIP"):(LevelHUD.Vietnamese?"BỎ QUA [SPACE]":"SKIP [SPACE]");
            riftPoint=center+new Vector3(0,105,45);rift=new GameObject("P21 torn sky rift").AddComponent<P21RiftVisual>();rift.transform.SetParent(transform,false);rift.Build(riftPoint,camPosition);
            if(!ending){dragon=SkyBeastPresence.Spawn("020");dragon.transform.SetParent(transform);dragon.HoldCinematic(riftPoint+new Vector3(0,-20,-18),Quaternion.Euler(0,180,0));}
            else
            {
                var profile=ProfileService.Instance;if(profile!=null){profile.Data.riftBreakerUnlocked=true;profile.MarkDirty();profile.Flush();}
                if(explorer!=null){var cc=explorer.GetComponent<CharacterController>();bool on=cc!=null&&cc.enabled;if(on)cc.enabled=false;explorer.transform.SetPositionAndRotation(new Vector3(-5,.13f,2),Quaternion.Euler(0,45,0));if(on)cc.enabled=true;foreach(var r in explorer.characterAnimator.GetComponentsInChildren<Renderer>())if(!r.enabled){avatarHidden.Add(r);r.enabled=true;}}
                Audio.LevelMusicDirector.Instance?.PlayStory(true);
                var sound=new GameObject("P21 dawn ambient birds");sound.transform.SetParent(transform,false);ambient=sound.AddComponent<AudioSource>();ambient.playOnAwake=false;ambient.spatialBlend=0;ambient.loop=true;ambient.volume=.10f;ambient.clip=Resources.Load<AudioClip>("P21/dawn-birds");var mixer=SettingsManager.Instance?.Mixer;if(mixer!=null){var groups=mixer.FindMatchingGroups("SFX");if(groups.Length>0)ambient.outputAudioMixerGroup=groups[0];}if(ambient.clip!=null)ambient.Play();
                BuildCredits();
            }
            HideHud();Render();
        }
        void Bar(string name,Vector2 size,Vector2 pos){var r=ComboUIFactory.Rect(name,overlay.transform,size,pos);var im=r.gameObject.AddComponent<Image>();im.color=new Color(.015f,.01f,.025f,.96f);im.raycastTarget=false;}
        void HideHud(){foreach(var c in FindObjectsByType<Canvas>())if(c!=overlay&&c.GetComponent<ImportantCaptions>()==null&&c.enabled){if(!hidden.Contains(c))hidden.Add(c);c.enabled=false;}}
        void LateUpdate()
        {
            if(!Playing)return;
            float now=Time.realtimeSinceStartup,elapsed=Mathf.Max(0,now-lastTick);lastTick=now;
            HideHud();if(Time.timeScale<=0)return;
            if(Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)Skip();
            if(exit){Restore();return;}if(HoldForCapture)return;
            // A cinematic created during a long frame must not charge time before Begin.
            // Updating lastTick while paused also prevents catch-up on resume.
            age+=elapsed;Render();
            if(!Ending&&age>=Duration){MarkRevealSeen();Restore();}
            else if(Ending&&age>=Duration&&!CreditsVisible)ShowCredits();
        }
        void Render()
        {
            subtitle.enabled=SettingsManager.Instance?.Current.Subtitles??true;
            float p=Progress;var from=center+new Vector3(75,35,-150);var to=center+new Vector3(48,32,-110);
            Vector3 look=Vector3.Lerp(center+Vector3.up*22,riftPoint,Ending?.12f:.43f);
            if(Ending){if(p<.65f){from=center+new Vector3(60,35,-100);to=center+new Vector3(45,28,-90);look=Vector3.Lerp(center+Vector3.up*18,riftPoint,.38f*(1-Mathf.SmoothStep(0,1,p/.48f)));}else{float dolly=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,p));from=to=new Vector3(-5,.13f,2)+Vector3.Lerp(new Vector3(6.36f,3.87f,6.36f),new Vector3(5,3.4f,5),dolly);look=new Vector3(-5,1.5f,2);}sky?.SetDawnProgress(Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.65f,p)));rift.Show(1-Mathf.SmoothStep(0,1,p/.42f),cam.transform.position);}
            else
            {
                rift.Show(Mathf.SmoothStep(0,1,p/.40f),cam.transform.position);
                if(dragon!=null){dragon.gameObject.SetActive(p>=.25f);dragon.transform.position=riftPoint+new Vector3(0,-22,Mathf.Lerp(25,-20,Mathf.SmoothStep(0,1,p/.65f)));if(p>=.40f&&!roared){roared=true;dragon.Roar();Audio.LevelMusicDirector.Instance?.PlayStory(false);}}
            }
            cam.transform.position=Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,p));cam.transform.rotation=Quaternion.LookRotation(look-cam.transform.position);cam.fieldOfView=Ending?(p<.65f?60:48):Mathf.Lerp(65,42,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.7f,p)));
            if(!Ending&&dragon!=null&&p>.3f){float close=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.7f,p));var focus=dragon.transform.position+Vector3.up*7;cam.transform.position=Vector3.Lerp(cam.transform.position,focus+new Vector3(54,10,-78),close);cam.transform.rotation=Quaternion.LookRotation(Vector3.Lerp(look,focus,close)-cam.transform.position);}
            subtitle.text=Ending?(LevelHUD.Vietnamese?"Khe Nứt khép lại. Một ngày mới trên học viện.":"The Rift closes. A new day begins on campus."):
                p<.25f?(LevelHUD.Vietnamese?"Shaban đã tan biến… nhưng Khe Nứt vẫn còn.":"Shaban has fallen… but the Rift remains."):
                p<.55f?(LevelHUD.Vietnamese?"Chủ nhân Cửu U đã thức tỉnh.":"The ruler of the Nine Hells awakens."):
                (LevelHUD.Vietnamese?"Học để mạnh hơn. Thiên Kiếm sẽ chấm dứt Thiên Hỏa.":"Grow through study. The Heaven Sword can end the fire storm.");
        }
        void BuildCredits()
        {
            var asset=Resources.Load<TextAsset>("P21/Credits");pages=(asset!=null?asset.text:"Campus Rift").Replace("\r\n","\n").Split(new[]{"\n---PAGE---\n"},System.StringSplitOptions.None);
            var panel=ComboUIFactory.Rect("Credits card",overlay.transform,new Vector2(1580,850),Vector2.zero);creditPanel=panel.gameObject;ComicTheme.Frame(panel.gameObject,"panel",false);
            var text=ComboUIFactory.Text("Credits content",panel,new Vector2(1470,650),new Vector2(0,20),22);text.enableAutoSizing=false;text.textWrappingMode=TextWrappingModes.Normal;text.alignment=TextAlignmentOptions.TopLeft;text.color=ComicTheme.Paper;text.fontStyle=FontStyles.Normal;text.fontSharedMaterial=ComicTheme.Font.material;
            pageLabel=ComboUIFactory.Text("Credits page",panel,new Vector2(600,55),new Vector2(0,-350),24);pageLabel.color=ComicTheme.Gold;
            CreditButton(panel,"Previous",-570,()=>SetCreditPage(creditPage-1),LevelHUD.Vietnamese?"TRƯỚC":"PREVIOUS");
            CreditButton(panel,"Next",570,()=>SetCreditPage(creditPage+1),LevelHUD.Vietnamese?"TIẾP":"NEXT");
            creditPanel.SetActive(false);SetCreditPage(0);
        }
        void CreditButton(Transform parent,string name,float x,UnityEngine.Events.UnityAction action,string label){var r=ComboUIFactory.Rect(name,parent,new Vector2(220,60),new Vector2(x,-350));ComicTheme.Frame(r.gameObject,"button-red",true);r.gameObject.AddComponent<Button>().onClick.AddListener(action);ComboUIFactory.Text(name+" label",r,new Vector2(200,50),Vector2.zero,24).text=label;}
        public void SetCreditPage(int page){if(pages==null)return;creditPage=(page%pages.Length+pages.Length)%pages.Length;creditPanel.GetComponentInChildren<TMP_Text>(true).text=pages[creditPage];pageLabel.text=(creditPage+1)+" / "+pages.Length;}
        void ShowCredits(){CreditsVisible=true;creditPanel.SetActive(true);title.text=LevelHUD.Vietnamese?"DANH SÁCH THỰC HIỆN":"CREDITS";subtitle.text=LevelHUD.Vietnamese?"Danh hiệu PHÁ RIFT đã mở · Vào Sảnh → Hậu Kết để chơi Tháp / Ác Mộng":"RIFT BREAKER unlocked · Visit Hub → Endgame for Trial Tower / Nightmare";}
        void MarkRevealSeen(){var p=ProfileService.Instance;if(p!=null){p.Data.longVuongRevealSeen=true;p.MarkDirty();p.Flush();}}
        public void Skip(){if(!Playing||Time.timeScale<=0)return;Skipped=true;if(Ending&&!CreditsVisible){age=Duration;Render();ShowCredits();return;}if(!Ending)MarkRevealSeen();exit=true;}
#if UNITY_EDITOR
        public void SeekForCapture(float fraction){HoldForCapture=true;age=Mathf.Clamp01(fraction)*Duration;Render();if(Ending&&fraction>=1)ShowCredits();}
#endif
        void Restore()
        {
            if(!Playing)return;Playing=false;LastSkipped=Skipped;if(Active==this)Active=null;
            if(ambient!=null)ambient.Stop();
            foreach(var c in hidden)if(c!=null)c.enabled=true;hidden.Clear();if(overlay!=null)overlay.gameObject.SetActive(false);
            if(explorer!=null){explorer.enabled=explorerEnabled;if(Ending){var cc=explorer.GetComponent<CharacterController>();bool on=cc!=null&&cc.enabled;if(on)cc.enabled=false;explorer.transform.SetPositionAndRotation(playerPosition,playerRotation);if(on)cc.enabled=true;foreach(var r in avatarHidden)if(r!=null)r.enabled=false;avatarHidden.Clear();}}
            if(input!=null){input.UltimateLocked=inputLocked;input.ResetAll();}
            if(cam!=null){cam.transform.SetPositionAndRotation(camPosition,camRotation);cam.fieldOfView=fov;}
            if(fire!=null)fire.CinematicPaused=firePaused;if(scheduler!=null)scheduler.CinematicPaused=schedulerPaused;
            if(sky!=null){if(Ending)sky.SetDawnProgress(1);else sky.SetPreset(Skipped||age>=Duration?SkyPreset.Inferno:preset);sky.SetBrightness(brightness);}
            UIStateManager.Instance?.RefreshCursor();
        }
        void OnDestroy(){Restore();}
    }
}
