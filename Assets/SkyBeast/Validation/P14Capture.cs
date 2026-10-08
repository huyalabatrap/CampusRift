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
    public sealed class P14Capture:MonoBehaviour
    {
        [Serializable]sealed class ShotRecord{public string image,state,source;public int phase,beasts,strikes;public float furyWarning;public Vector3 camera,angle;}
        [Serializable]sealed class Report{public List<ShotRecord> shots=new List<ShotRecord>();public int fpsFrames;public double fpsSeconds,fps;public string method="One 3s Unity Editor sample at level10 phase3, real scheduler + six meteor strikes + Breath, 1920x1080, Android target/mobile RP; no native/device benchmark. AI/waves held, HP10000.";}
        public bool MeasureFps;
        public bool FuryOnly;
        Report report=new Report();SkillSet1TestWorld world;FireBreathCycle cycle;SkyBeastScheduler scheduler;SkyStrikePool pool;
        Vector3 point=new Vector3(10,.13f,-5);const string Root="task/p14/screens/";
        IEnumerator Frames(int count=8){for(int i=0;i<count;i++)yield return null;}
        void Place(float yaw=25,float pitch=8)
        {world.PlacePlayer(point);world.camera.transform.position=point-Quaternion.Euler(0,yaw,0)*Vector3.forward*3.4f+Vector3.up*1.9f;world.camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);world.camera.fieldOfView=60;}
        void Begin(int level)
        {
            LevelDirector.Ensure().Begin(LevelCatalog.Instance.Get(level));LevelDirector.Instance.enabled=false;
            scheduler=SkyBeastScheduler.Instance;cycle=FireBreathCycle.Instance;cycle.AutoAdvance=false;cycle.Ground.enabled=false;
            world.player.GetComponent<Monsters.PlayerMonsterHealth>().SetProgressionMaxHealth(10000);world.player.enabled=false;Place();
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+name+".png",frame.EncodeToPNG());Destroy(frame);
            report.shots.Add(new ShotRecord{image=name,state=cycle.State.ToString(),phase=scheduler.Phase,beasts=scheduler.Beasts.Count,source=scheduler.BreathSource!=null?scheduler.BreathSource.definition.id:"",strikes=pool!=null?pool.ActiveCount:0,furyWarning=scheduler.Fury!=null?scheduler.Fury.Remaining:0,camera=world.camera.transform.position,angle=world.camera.transform.eulerAngles});
            File.WriteAllText(Root+name+"-text-audit.json",JsonUtility.ToJson(ComicTextAudit.Scan("P14 "+name),true));
            File.WriteAllText("task/p14/capture.json",JsonUtility.ToJson(report,true));
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Root);world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();UIValidation.SetResolution(1920,1080);yield return new WaitForSecondsRealtime(1);
            foreach(var v in world.victims)v.gameObject.SetActive(false);
            if(FuryOnly)
            {
                report=JsonUtility.FromJson<Report>(File.ReadAllText("task/p14/capture.json"));report.shots.RemoveAll(s=>s.image=="long-no"||s.image=="long-no-warning");
                Begin(10);scheduler.ApplySkySwordHit();scheduler.ApplySkySwordHit();yield return Frames();pool=scheduler.GetComponent<SkyStrikePool>();pool.enabled=false;FindAnyObjectByType<MeteorShower>().enabled=false;
                scheduler.Fury.Request();scheduler.Fury.enabled=false;scheduler.Fury.Advance(1);yield return Frames();yield return Shot("long-no-warning");scheduler.Fury.Advance(4);yield return new WaitForSecondsRealtime(1.2f);yield return Shot("long-no");
                LevelDirector.Instance.End();world.player.enabled=true;world.End();File.WriteAllText("task/p14/capture.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/p14/capture-DONE.txt","Updated Fury sky / two frames");Destroy(gameObject);yield break;
            }
            Begin(9);yield return Frames();yield return Shot("level9-segments");
            cycle.Advance(10);cycle.Ground.Clear();pool=scheduler.GetComponent<SkyStrikePool>();pool.enabled=false;var feather=FindAnyObjectByType<FeatherBarrage>();feather.enabled=false;
            feather.TryDrop();pool.Advance(1.12f);yield return Frames(3);yield return Shot("feathers-telegraph");pool.Advance(.1f);yield return Frames(2);yield return Shot("feathers-impact");pool.Clear();cycle.Ground.Clear();
            Begin(10);pool=null;world.Mode(true);UIValidation.SetResolution(2340,1080);yield return Frames(12);yield return Shot("level10-two-bars-mobile");
            world.Mode(false);UIValidation.SetResolution(1920,1080);scheduler.ApplySkySwordHit();scheduler.ApplySkySwordHit();yield return Frames();pool=scheduler.GetComponent<SkyStrikePool>();pool.enabled=false;
            FindAnyObjectByType<MeteorShower>().enabled=false;
            // Hold the real warning flight at a readable point on the P12 orbit, then let its climb/pose run.
            var dragon=scheduler.Beasts[0];dragon.SeekFlightForValidation(0);Place();yield return new WaitForSecondsRealtime(3);
            var bounds=new Bounds(dragon.transform.position,Vector3.zero);bool firstBounds=true;foreach(var skin in dragon.GetComponentsInChildren<SkinnedMeshRenderer>())if(skin.enabled){if(firstBounds){bounds=skin.bounds;firstBounds=false;}else bounds.Encapsulate(skin.bounds);}
            world.camera.transform.LookAt(bounds.center);yield return Frames(3);yield return Shot("warning-dragon-climb");Place();
            FindAnyObjectByType<MeteorShower>().TryDrop();pool.Advance(1.43f);yield return Frames(3);yield return Shot("meteors-telegraph");pool.Advance(.09f);yield return Frames(2);yield return Shot("meteors-impact");pool.Clear();
            // One brief requested FPS sample. Keep the production six-point sequence running beside fire.
            cycle.Advance(cycle.Profile.warningSeconds);
            if(MeasureFps){pool.enabled=true;FindAnyObjectByType<MeteorShower>().TryDrop();yield return new WaitForSecondsRealtime(.25f);double start=Time.realtimeSinceStartupAsDouble;int frames=0;while(Time.realtimeSinceStartupAsDouble-start<3){yield return null;frames++;}report.fpsFrames=frames;report.fpsSeconds=Time.realtimeSinceStartupAsDouble-start;report.fps=frames/report.fpsSeconds;pool.enabled=false;}
            else if(File.Exists("task/p14/capture-first.json")){var first=JsonUtility.FromJson<Report>(File.ReadAllText("task/p14/capture-first.json"));report.fpsFrames=first.fpsFrames;report.fpsSeconds=first.fpsSeconds;report.fps=first.fps;report.method=first.method+" Measured on first visuals before HUD/size/fury sky polish; not remeasured.";}
            cycle.Advance(4);cycle.Ground.Clear();scheduler.Fury.Request();scheduler.Fury.enabled=false;scheduler.Fury.Advance(1);yield return Frames();yield return Shot("long-no-warning");scheduler.Fury.Advance(4);yield return new WaitForSecondsRealtime(1.2f);yield return Shot("long-no");
            LevelDirector.Instance.End();world.player.enabled=true;world.End();UIValidation.SetResolution(1920,1080);File.WriteAllText("task/p14/capture.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/p14/capture-DONE.txt","Captured "+report.shots.Count+" frames; FPS "+report.fps.ToString("F2"));Destroy(gameObject);
        }
    }
}
#endif
