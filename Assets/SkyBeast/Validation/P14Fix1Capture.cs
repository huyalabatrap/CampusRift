#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.UI;
using CampusRift.Levels;
namespace CampusRift.SkyBeast
{
    // Brief visual smoke + one Rest/Fury pair. Actual campus/player, never saved to the scene.
    public sealed class P14Fix1Capture:MonoBehaviour
    {
        [Serializable]sealed class Shot{public string image,state;public int phase,strikes,particles;public bool fury;public Vector3 camera,angle,mouth;}
        [Serializable]sealed class Sample{public string name,state,quality,pipeline;public int phase,frames,width,height,budget,strikeSlots,minStrikes,maxStrikes;public bool fury,comicInk;public double seconds,fps;public Vector3 camera,angle;}
        [Serializable]sealed class Report{public string target,gpu,method="One 3s sample per state, same fixed gameplay camera, Editor Android/MobileRP 1920x1080; VSync/cap off. Real level10 phase3 scheduler; Fury timer/AI/waves held, HP10000, six live gameplay strikes. Not native/device FPS.";public List<Shot> shots=new List<Shot>();public List<Sample> samples=new List<Sample>();}
        public bool MeasureFps=true;
        SkillSet1TestWorld world;FireBreathCycle cycle;SkyBeastScheduler scheduler;SkyStrikePool pool;FireBreathVisuals visuals;
        readonly Report report=new Report();readonly Vector3 point=new Vector3(10,.13f,-5);const string Root="task/p14/screens/fix1/";
        int oldCap,oldVsync;
        IEnumerator Wait(float seconds){yield return new WaitForSecondsRealtime(seconds);}
        void Place(float yaw=-35,float pitch=8){world.PlacePlayer(point);world.camera.transform.position=point-Quaternion.Euler(0,yaw,0)*Vector3.forward*3.4f+Vector3.up*1.9f;world.camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);world.camera.fieldOfView=60;}
        void Begin(int level)
        {
            LevelDirector.Ensure().Begin(LevelCatalog.Instance.Get(level));LevelDirector.Instance.enabled=false;
            scheduler=SkyBeastScheduler.Instance;cycle=FireBreathCycle.Instance;cycle.AutoAdvance=false;cycle.Ground.enabled=false;visuals=cycle.GetComponent<FireBreathVisuals>();
            world.player.GetComponent<Monsters.PlayerMonsterHealth>().SetProgressionMaxHealth(10000);world.player.enabled=false;Place();
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+name+".png",frame.EncodeToPNG());Destroy(frame);
            report.shots.Add(new Shot{image=name,state=cycle.State.ToString(),phase=scheduler.Phase,strikes=pool.ActiveCount,particles=visuals.ParticleCount,fury=cycle.IsFury,camera=world.camera.transform.position,angle=world.camera.transform.eulerAngles,mouth=scheduler.Beasts[0].Mouth.position});
            File.WriteAllText(Root+name+"-text-audit.json",JsonUtility.ToJson(ComicTextAudit.Scan("P14 fix1 "+name),true));Write();
        }
        void Write()=>File.WriteAllText("task/p14/fix1-capture.json",JsonUtility.ToJson(report,true));
        IEnumerator Measure(string name)
        {
            var sample=new Sample{name=name,state=cycle.State.ToString(),phase=scheduler.Phase,fury=cycle.IsFury,quality=FireVisualQuality.Current.ToString(),pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,comicInk=ComicRendering.Enabled,width=Screen.width,height=Screen.height,budget=visuals.Budget,strikeSlots=pool.CreatedCount,camera=world.camera.transform.position,angle=world.camera.transform.eulerAngles,minStrikes=6};
            double begin=Time.realtimeSinceStartupAsDouble;
            while(Time.realtimeSinceStartupAsDouble-begin<3){yield return null;sample.frames++;sample.minStrikes=Mathf.Min(sample.minStrikes,pool.ActiveCount);sample.maxStrikes=Mathf.Max(sample.maxStrikes,pool.ActiveCount);}
            sample.seconds=Time.realtimeSinceStartupAsDouble-begin;sample.fps=sample.frames/sample.seconds;report.samples.Add(sample);Write();
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Root);oldCap=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();UIValidation.SetResolution(1920,1080);yield return Wait(1);
            foreach(var v in world.victims)v.gameObject.SetActive(false);Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;FireVisualQuality.Override=FireEffectQuality.Mobile;
            report.target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();report.gpu=SystemInfo.graphicsDeviceName;UnityEngine.Random.InitState(140103);
            Begin(9);yield return null;pool=scheduler.GetComponent<SkyStrikePool>();pool.enabled=false;var feather=FindAnyObjectByType<FeatherBarrage>();feather.enabled=false;
            cycle.Advance(20);cycle.Ground.Clear();world.Lighting(false);Place();
            // Place three real pooled feathers in view; avoid fixture teleport velocity predicting off-screen.
            for(int i=0;i<3;i++)pool.Launch(point+new Vector3(-3-i*1.2f,0,2+i),SkyStrikeKind.Feather,scheduler.Beasts[0],cycle.Profile);
            pool.Advance(1.17f);yield return null;yield return Capture("feathers-falling-light");world.Lighting(true);yield return Wait(.15f);yield return Capture("feathers-falling-dark");pool.Clear();
            Begin(10);scheduler.ApplySkySwordHit();scheduler.ApplySkySwordHit();yield return null;pool=scheduler.GetComponent<SkyStrikePool>();pool.enabled=false;
            var meteor=FindAnyObjectByType<MeteorShower>();meteor.enabled=false;scheduler.Fury.enabled=false;
            cycle.Advance(cycle.Profile.warningSeconds+cycle.Profile.breathSeconds+cycle.Profile.afterfireSeconds);cycle.Ground.Clear();SettingsManager.Instance.Sky.SetPreset(LevelCatalog.Instance.Get(10).sky);SettingsManager.Instance.Sky.SetBrightness(1);Place();
            // Allow earlier cosmetic fire to dissipate before the clean Rest sample.
            yield return Wait(8);Place();if(MeasureFps)yield return Measure("Rest");
            world.Lighting(false);meteor.TryDrop();pool.Advance(1.5f);yield return Wait(.38f);yield return Capture("meteors-impact-light");world.Lighting(true);yield return Wait(.15f);yield return Capture("meteors-impact-dark");pool.Clear();
            // Restore level10 sky, then use the real once-per-run Fury entry and socket pose.
            SettingsManager.Instance.Sky.SetPreset(LevelCatalog.Instance.Get(10).sky);SettingsManager.Instance.Sky.SetBrightness(1);Place();
            scheduler.Beasts[0].SeekFlightForValidation(0);scheduler.Fury.Request();scheduler.Fury.Advance(5);yield return Wait(1.2f);
            meteor.TryDrop();pool.enabled=true;yield return Wait(.1f);if(MeasureFps)yield return Measure("Phase3-Meteor6-LongNo");
            yield return Capture("long-no-courtyard");
            var mouth=scheduler.Beasts[0].Mouth;world.camera.transform.LookAt(Vector3.Lerp(mouth.position,point+Vector3.up, .35f));yield return Wait(.15f);yield return Capture("long-no-mouth");
            LevelDirector.Instance.End();world.player.enabled=true;world.End();QualitySettings.vSyncCount=oldVsync;Application.targetFrameRate=oldCap;FireVisualQuality.Override=FireEffectQuality.Automatic;
            Write();File.WriteAllText("task/p14/fix1-capture-DONE.txt","DONE: "+report.shots.Count+" images; "+report.samples.Count+" samples");Destroy(gameObject);
        }
    }
}
#endif
