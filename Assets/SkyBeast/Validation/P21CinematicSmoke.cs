#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.UI;
using CampusRift.Skills;
using CampusRift.Levels;
using CampusRift.Progression;
using CampusRift.Enemies;
namespace CampusRift.SkyBeast
{
    public sealed class P21CinematicSmoke:MonoBehaviour
    {
        [Serializable]public sealed class Check {public string name;public bool pass;}
        [Serializable]public sealed class Shot {public string name;public Vector3 camera,angle;public int width,height;public float progress;}
        [Serializable]public sealed class Report {public int passed,failed;public string method="Single compact Editor smoke + Unity framebuffer captures. Transient profile, accelerated real waves, held AI, DEV phase skip for finale; not balance/device evidence.";public List<Check> checks=new List<Check>();public List<Shot> shots=new List<Shot>();public int frames,swords;public double seconds,fps;public string pipeline,target;}
        const string Root="task/p21/";readonly Report report=new Report();SkillSet1TestWorld world;LevelDirector d;HeavenSwordUltimate u;HeavenSwordCinematic cine;FireBreathCycle fire;P21StoryCinematic story;
        int oldCap,oldSync;readonly Vector3 outdoor=new Vector3(-5,.13f,2);
        public bool MeasureFps=true;
        public bool CaptureScreens=true;
        bool identityHold;Canvas identityCanvas;readonly List<Canvas> identityHidden=new List<Canvas>();
        void LateUpdate(){if(identityHold)foreach(var c in FindObjectsByType<Canvas>())if(c!=identityCanvas&&c.enabled){if(!identityHidden.Contains(c))identityHidden.Add(c);c.enabled=false;}}
        void CheckResult(bool pass,string name){report.checks.Add(new Check{name=name,pass=pass});if(pass)report.passed++;else report.failed++;Write();}
        void Write(){File.WriteAllText(Root+"Cinematic.json",JsonUtility.ToJson(report,true));}
        IEnumerator Wait(Func<bool> predicate,float seconds=12){float end=Time.realtimeSinceStartup+seconds;while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;}
        IEnumerator ShotFrame(string name)
        {
            yield return new WaitForSecondsRealtime(.08f);yield return new WaitForEndOfFrame();if(!CaptureScreens)yield break;var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+"screens/"+name+".png",frame.EncodeToPNG());Destroy(frame);
            report.shots.Add(new Shot{name=name,camera=world.camera.transform.position,angle=world.camera.transform.eulerAngles,width=Screen.width,height=Screen.height,progress=story!=null?story.Progress:cine!=null?cine.Progress:0});
            File.WriteAllText(Root+"screens/"+name+"-text-audit.json",JsonUtility.ToJson(ComicTextAudit.Scan(name),true));Write();
        }
        void Begin(int level)
        {
            UIStateManager.Instance.EnterScene(true);ProfileService.Instance.Cultivation.SetState(level==10?Realm.DoKiep:Realm.HoaThan,1,0);LevelSession.Select(level);d.Begin(LevelCatalog.Instance.Get(level));d.enabled=true;u=HeavenSwordUltimate.Instance;
            fire=FireBreathCycle.Ensure();fire.AutoAdvance=false;if(level>=8){fire.GetComponent<FireBreathVisuals>().enabled=false;fire.Ground.Clear();}
            world.PlacePlayer(outdoor);world.player.enabled=false;world.player.GetComponent<Monsters.PlayerMonsterHealth>().SetProgressionMaxHealth(10000);world.player.GetComponent<Monsters.PlayerMonsterHealth>().Revive(1,0);
            SettingsManager.Instance.Sky.SetBrightness(.9f);world.camera.transform.position=outdoor+new Vector3(-3,2,-3);world.camera.transform.rotation=Quaternion.Euler(12,45,0);
        }
        IEnumerator ClearWave()
        {
            float deadline=Time.realtimeSinceStartup+20;
            while(!d.AwaitingSkySword&&Time.realtimeSinceStartup<deadline){foreach(var e in new List<EnemyInstance>(d.Alive)){e.Brain?.StopAllCoroutines();if(e.Brain!=null)e.Brain.enabled=false;e.GetComponent<EnemyAbilityRunner>()?.Cancel();e.Motor?.Stop();if(e.Alive)HeavenSwordPlayTest.Kill(e);}yield return null;}
            d.enabled=false;CheckResult(d.AwaitingSkySword&&u.Intent.Full,"Real wave feeds full intent L"+d.Level.index);
        }
        IEnumerator Identity(string name,SkyBeastController beast)
        {
            beast.HoldCinematic(new Vector3(0,90,15),Quaternion.Euler(0,150,0));yield return new WaitForSecondsRealtime(.2f);
            var bounds=beast.GetComponentInChildren<SkinnedMeshRenderer>().bounds;world.camera.transform.position=bounds.center+new Vector3(60,-35,-105);world.camera.transform.LookAt(bounds.center);world.camera.fieldOfView=58;
            identityHold=true;identityCanvas=ComboUIFactory.Canvas("P21 identity",transform,80);ComboUIFactory.Text("Identity caption",identityCanvas.transform,new Vector2(1600,70),new Vector2(0,-470),36).text=name=="giao"?"XÍCH HỎA GIAO":name=="chu-tuoc"?"TÀ HÓA CHU TƯỚC":"CỬU U HỎA LONG VƯƠNG";
            var accent=beast.GetComponent<SkyBeastIdentity>();CheckResult(accent!=null&&accent.Accent!=null&&accent.Accent.sharedMaterial!=null&&accent.AccentTriangles<400,"Identity rigid skin / material / budget "+name);
            if(name=="long-vuong")CheckResult(accent.HornCount==9,"Long Vuong exactly nine crown horns");yield return ShotFrame("identity-"+name);
            identityHold=false;Destroy(identityCanvas.gameObject);identityCanvas=null;foreach(var c in identityHidden)if(c!=null)c.enabled=true;identityHidden.Clear();
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Root+"screens");world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();yield return null;foreach(var v in world.victims)v.gameObject.SetActive(false);
            TutorialDirector.Suppress=true;d=LevelDirector.Ensure();d.introSeconds=.01f;d.spawnInterval=.005f;d.portalLead=0;oldCap=Application.targetFrameRate;oldSync=QualitySettings.vSyncCount;
            // Reveal waits while paused, first run seven seconds; skip restores every temporary lock.
            Begin(7);d.enabled=false;UIStateManager.Instance.Pause();StartCoroutine(SkyBeastEnding.Play());yield return new WaitForSecondsRealtime(.15f);
            CheckResult(P21StoryCinematic.Active==null,"Reveal does not start while paused");UIStateManager.Instance.Resume();yield return Wait(()=>P21StoryCinematic.Active!=null);story=P21StoryCinematic.Active;
            CheckResult(story!=null&&story.Duration==7&&story.RevealDragon.definition.id=="020","First reveal seven seconds / supplied Long Vuong020");
            if(story!=null){foreach(float p in new[]{.10f,.35f,.65f,.90f}){story.SeekForCapture(p);yield return ShotFrame("reveal-"+Mathf.RoundToInt(p*100));}
                story.HoldForCapture=false;UIStateManager.Instance.Pause();float pausedProgress=story.Progress;yield return new WaitForSecondsRealtime(.15f);CheckResult(story.Progress==pausedProgress,"Active reveal freezes while paused");UIStateManager.Instance.Resume();story.Skip();yield return Wait(()=>!story.Playing);CheckResult(!world.player.GetComponent<Controls.CampusInput>().UltimateLocked&&fire.CinematicPaused==false,"Reveal skip restores input / fire gate");}
            story=null;StartCoroutine(SkyBeastEnding.Play());yield return Wait(()=>P21StoryCinematic.Active!=null);story=P21StoryCinematic.Active;CheckResult(story.Duration==3.5f&&ProfileService.Instance.Data.longVuongRevealSeen,"Repeat reveal shortened and flag persists");
            float start=Time.realtimeSinceStartup;yield return Wait(()=>!story.Playing,5);CheckResult(Time.realtimeSinceStartup-start>=3.3f&&!story.Playing,"Unskipped repeat completes on unscaled clock");story=null;
            StartCoroutine(SkyBeastEnding.Play());yield return Wait(()=>P21StoryCinematic.Active!=null);P21StoryCinematic.CancelActive();yield return null;CheckResult(P21StoryCinematic.Active==null&&!world.player.GetComponent<Controls.CampusInput>().UltimateLocked&&!fire.CinematicPaused,"Cancel active story restores locks without exception");d.End();
            var music=Audio.LevelMusicDirector.Instance;music.enabled=false;music.PlayBoss(5,false);string boss5=music.CurrentTrack;music.PlayBoss(7,false);string boss7=music.CurrentTrack;music.PlayBoss(7,true);CheckResult(boss5!=boss7&&music.CurrentTrack!=boss7,"Distinct Shaban5 / Shaban7 / phase2 music");music.enabled=true;
            // Each level's actual channel starts the authored Timeline; seek only holds capture frames.
            for(int level=8;level<=10;level++)
            {
                Begin(level);world.Mode(level==10);yield return ClearWave();var scheduler=SkyBeastScheduler.Instance;
                if(level==10){scheduler.ApplySkySwordHit();scheduler.ApplySkySwordHit();scheduler.Fury.Request();scheduler.Fury.Advance(5);fire.Advance(12);CheckResult(scheduler.Fury.Completed,"DEV finale respects full five-second Fury warning and twelve-second breath");}
                var beast=scheduler.SwordTarget.GetComponent<SkyBeastController>();yield return Identity(level==8?"giao":level==9?"chu-tuoc":"long-vuong",beast);
                string expected=level==8?"P21/Music/giao":level==9?"P21/Music/bird":"P21/Music/king";
                CheckResult(Audio.LevelMusicDirector.Instance.CurrentTrack==expected&&Audio.LevelMusicDirector.Instance.CurrentVoice.outputAudioMixerGroup!=null,"Distinct beast music / Settings mixer L"+level);
                world.camera.transform.position=outdoor+new Vector3(-3,2,-3);world.camera.transform.rotation=Quaternion.Euler(12,45,0);world.PlacePlayer(outdoor);
                CheckResult(u.TryChannel(),"Actual ultimate channel starts L"+level);yield return Wait(()=>HeavenSwordCinematic.Active!=null);cine=HeavenSwordCinematic.Active;
                CheckResult(cine!=null&&cine.Timeline.playableAsset.name.Contains(level.ToString()),"Authored Timeline bound L"+level);if(cine==null)continue;
                CheckResult(cine.Duration==(level==8?6:level==9?3:4.5f),"P15 duration and longer finale L"+level);
                cine.SeekForCapture(.40f);cine.HoldForCapture=false;UIStateManager.Instance.Pause();float p=cine.Progress,remaining=fire.Remaining;yield return new WaitForSecondsRealtime(.15f);CheckResult(cine.Progress==p&&fire.Remaining==remaining,"Timeline and fire freeze while paused L"+level);UIStateManager.Instance.Resume();
                cine.SeekForCapture(.30f);yield return ShotFrame("sword"+level+"-gather");cine.SeekForCapture(.58f);yield return ShotFrame("sword"+level+"-blade");cine.SeekForCapture(.736f);yield return ShotFrame("sword"+level+"-impact");cine.SeekForCapture(.88f);yield return ShotFrame("sword"+level+"-fall");
                if(level==8&&MeasureFps){cine.SeekForCapture(.40f);Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;double at=Time.realtimeSinceStartupAsDouble;int frames=0;while(Time.realtimeSinceStartupAsDouble-at<3){frames++;yield return null;}report.frames=frames;report.seconds=Time.realtimeSinceStartupAsDouble-at;report.fps=frames/report.seconds;report.swords=cine.Swords.SwordCount;report.pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name;report.target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();Application.targetFrameRate=oldCap;QualitySettings.vSyncCount=oldSync;Write();}
                bool hit=cine.HitApplied;cine.SeekForCapture(1);cine.HoldForCapture=false;yield return Wait(()=>!cine.Playing);CheckResult(hit&&Time.timeScale==1&&(level==10?P21StoryCinematic.Active!=null:!world.player.GetComponent<Controls.CampusInput>().UltimateLocked),"Hit once / unscaled / correct story input handoff L"+level);cine=null;
                if(level==10)
                {
                    yield return Wait(()=>P21StoryCinematic.Active!=null);story=P21StoryCinematic.Active;CheckResult(story!=null&&story.Ending&&story.Duration==20&&ProfileService.Instance.Data.riftBreakerUnlocked,"Final hit enters dawn20s / unlock Rift Breaker");
                    if(story!=null){story.SeekForCapture(.12f);yield return ShotFrame("ending-rift-closes");yield return new WaitForSecondsRealtime(3.2f);story.SeekForCapture(.90f);yield return ShotFrame("ending-hero-dawn");story.Skip();yield return null;CheckResult(story.CreditsVisible,"Dawn skip still reaches credits");yield return ShotFrame("credits-title");story.SetCreditPage(1);yield return ShotFrame("credits-sources");
                        var asset=Resources.Load<TextAsset>("P21/Credits");CheckResult(asset!=null&&asset.text.Contains("Morgan Strauss")&&asset.text.Contains("Freesound")&&asset.text.Contains("Poly Haven")&&asset.text.Contains("Kenney"),"Credits include CC-BY models / Freesound / Poly Haven / Kenney");story.Skip();yield return Wait(()=>!story.Playing);story=null;yield return new WaitForSecondsRealtime(2.5f);CheckResult(UIStateManager.Instance.State==UIState.Victory&&d.WinsRaised==1&&!world.player.GetComponent<Controls.CampusInput>().UltimateLocked,"Credits skip returns to single Results");}
                }
                d.End();
            }
            UIStateManager.Instance.EnterScene(true);float musicDb=0;var mixer=SettingsManager.Instance.Mixer;mixer.GetFloat("MusicVolume",out musicDb);SkyBeastAudioDuck.Ensure().Duck(.4f);yield return new WaitForSecondsRealtime(.25f);float low=0;mixer.GetFloat("MusicVolume",out low);CheckResult(low<musicDb-8,"Roar ducks shared Music mixer without competing volume path");yield return new WaitForSecondsRealtime(1.2f);float restored=0;mixer.GetFloat("MusicVolume",out restored);CheckResult(Mathf.Abs(restored-musicDb)<.1f,"Music mixer recovers after roar");
            world.End();Write();File.WriteAllText(Root+"Cinematic-DONE.txt",report.passed+" PASS / "+report.failed+" FAIL");Destroy(gameObject);
        }
        void OnDestroy(){TutorialDirector.Suppress=false;P21StoryCinematic.CancelActive();if(d!=null)d.End();Application.targetFrameRate=oldCap;QualitySettings.vSyncCount=oldSync;}
    }
}
#endif
