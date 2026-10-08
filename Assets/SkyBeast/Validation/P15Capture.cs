using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.UI;
using CampusRift.Levels;
using CampusRift.Progression;
using CampusRift.Enemies;
namespace CampusRift.SkyBeast
{
#if UNITY_EDITOR
    public sealed class P15Capture:MonoBehaviour
    {
        [Serializable]sealed class ShotRecord{public string image,state;public float intent,progress;public int swords,width,height;public Vector3 camera,angle;}
        [Serializable]sealed class Sample{public string quality,pipeline,target;public int frames,swords,width,height;public double seconds,fps;public Vector3 camera,angle;}
        [Serializable]sealed class Report{public string method="Unity framebuffer captures, actual level8/9 combat. AI and wave holds/DEV damage, transient profile/HP10000. One3s sample per quality at held peak fleet. Not native/device performance or balance evidence.";public List<ShotRecord> shots=new List<ShotRecord>();public List<Sample> samples=new List<Sample>();}
        readonly Report report=new Report();SkillSet1TestWorld world;LevelDirector director;HeavenSwordUltimate u;FireBreathCycle fire;HeavenSwordCinematic cine;
        readonly Vector3 outdoor=new Vector3(-5,.13f,2);Vector3 indoor;int oldCap,oldVSync;const string Root="task/p15/screens/";
        void Write()=>File.WriteAllText("task/p15/capture.json",JsonUtility.ToJson(report,true));
        void Place(Vector3 p){world.PlacePlayer(p);world.camera.transform.position=p+new Vector3(-2.8f,1.9f,-1.8f);world.camera.transform.rotation=Quaternion.Euler(14,58,0);world.camera.fieldOfView=60;}
        IEnumerator Shot(string name)
        {
            yield return null;yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+name+".png",frame.EncodeToPNG());Destroy(frame);
            report.shots.Add(new ShotRecord{image=name,state=u.State.ToString(),intent=u.Intent!=null?u.Intent.Fraction:0,progress=cine!=null?cine.Progress:0,swords=cine!=null?cine.Swords.SwordCount:0,width=Screen.width,height=Screen.height,camera=world.camera.transform.position,angle=world.camera.transform.eulerAngles});
            File.WriteAllText(Root+name+"-text-audit.json",JsonUtility.ToJson(ComicTextAudit.Scan("P15 "+name),true));Write();
        }
        IEnumerator Clear(bool captureProgress=false)
        {
            bool shot=false;float deadline=Time.realtimeSinceStartup+25;
            while(!director.AwaitingSkySword&&Time.realtimeSinceStartup<deadline)
            {
                foreach(var e in new List<EnemyInstance>(director.Alive))
                {
                    if(e.Brain!=null)e.Brain.enabled=false;e.GetComponent<EnemyAbilityRunner>()?.Cancel();e.Motor?.Stop();
                    if(e.Alive)HeavenSwordPlayTest.Kill(e);
                }
                if(captureProgress&&!shot&&u.Intent.Fraction>.25f){shot=true;director.enabled=false;yield return Shot("intent-charging");director.enabled=true;}
                yield return null;
            }
            director.enabled=false;yield return new WaitForSecondsRealtime(1.1f);
        }
        void Begin(int level,Realm realm)
        {
            UIStateManager.Instance.EnterScene(true);ProfileService.Instance.Cultivation.SetState(realm,1,0);LevelSession.Select(level);director.Begin(LevelCatalog.Instance.Get(level));director.enabled=true;
            u=HeavenSwordUltimate.Instance;fire=FireBreathCycle.Instance;fire.AutoAdvance=false;fire.Ground.Clear();fire.GetComponent<FireBreathVisuals>().enabled=false;
            world.player.enabled=false;var hp=world.player.GetComponent<Monsters.PlayerMonsterHealth>();hp.SetProgressionMaxHealth(10000);hp.Revive(1,0);Place(outdoor);
        }
        IEnumerator Measure(string quality)
        {
            var sample=new Sample{quality=quality,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(),swords=cine.Swords.SwordCount,width=Screen.width,height=Screen.height,camera=world.camera.transform.position,angle=world.camera.transform.eulerAngles};
            double start=Time.realtimeSinceStartupAsDouble;while(Time.realtimeSinceStartupAsDouble-start<3){yield return null;sample.frames++;}sample.seconds=Time.realtimeSinceStartupAsDouble-start;sample.fps=sample.frames/sample.seconds;report.samples.Add(sample);Write();
        }
        IEnumerator WaitUntil(Func<bool> condition,float seconds=12){float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Root);oldCap=Application.targetFrameRate;oldVSync=QualitySettings.vSyncCount;world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();UIValidation.SetResolution(1920,1080);yield return new WaitForSecondsRealtime(.8f);
            foreach(var v in world.victims)v.gameObject.SetActive(false);foreach(var n in ShelterGraphReference.Graph.Nodes)if(n.WorldPosition.y<1&&ShelterDetector.AtFeet(n.WorldPosition)==Shelter.Indoor){indoor=n.WorldPosition;break;}
            director=LevelDirector.Ensure();director.introSeconds=.01f;director.spawnInterval=.005f;director.portalLead=0;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            Begin(8,Realm.HoaThan);world.Mode(false);FireVisualQuality.Override=FireEffectQuality.PcHigh;yield return Clear(true);yield return Shot("intent-full-pc-ready");Place(indoor);yield return Shot("pc-need-outdoor");
            world.Mode(true);yield return new WaitForSecondsRealtime(.25f);yield return Shot("mobile-need-outdoor");Place(outdoor);yield return new WaitForSecondsRealtime(.2f);yield return Shot("mobile-ready");world.Mode(false);yield return new WaitForSecondsRealtime(.2f);
            u.TryChannel();yield return new WaitForSecondsRealtime(.9f);yield return Shot("channel-array");yield return WaitUntil(()=>HeavenSwordCinematic.Active!=null);cine=HeavenSwordCinematic.Active;cine.HoldForCapture=true;
            cine.SeekForCapture(.18f);yield return Shot("cinematic-01-rise");cine.SeekForCapture(.40f);yield return Shot("cinematic-02-gather");yield return Measure("PC High peak2400");
            cine.SeekForCapture(.58f);yield return Shot("cinematic-03-giant-sword");cine.SeekForCapture(.70f);yield return Shot("cinematic-04-descent");cine.SeekForCapture(.736f);yield return Shot("cinematic-05-impact");cine.SeekForCapture(.93f);yield return new WaitForSecondsRealtime(1.2f);yield return Shot("cinematic-06-fall-dawn");cine.HoldForCapture=false;yield return WaitUntil(()=>!cine.Playing,3);yield return new WaitForSecondsRealtime(.2f);yield return Shot("victory-dawn");yield return new WaitForSecondsRealtime(2.5f);yield return Shot("level8-results");director.End();
            Begin(9,Realm.LuyenHu);world.Mode(true);var settings=SettingsManager.Instance.Current.Copy();settings.Quality=0;SettingsManager.Instance.Apply(settings,false);FireVisualQuality.Override=FireEffectQuality.Mobile;yield return Clear();u.TryChannel();yield return WaitUntil(()=>HeavenSwordCinematic.Active!=null);cine=HeavenSwordCinematic.Active;cine.SeekForCapture(.40f);yield return Measure("Mobile peak960");cine.SeekForCapture(.58f);yield return Shot("cinematic-mobile-sword");cine.Skip();director.End();
            world.player.enabled=true;world.End();QualitySettings.vSyncCount=oldVSync;Application.targetFrameRate=oldCap;FireVisualQuality.Override=FireEffectQuality.Automatic;Write();File.WriteAllText("task/p15/capture-DONE.txt","DONE "+report.shots.Count+" images / "+report.samples.Count+" samples");Destroy(gameObject);
        }
    }
#endif
}

