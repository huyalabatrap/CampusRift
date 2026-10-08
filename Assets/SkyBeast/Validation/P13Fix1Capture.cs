#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    public sealed class P13Fix1Capture:MonoBehaviour
    {
        const string Root="task/p13/screens/fix1/";
        [Serializable]sealed class ShotRecord{public string image,state,shelter,dragonPose;public int meteors,impacts,scorches,plumes,particles,patches,budget,doorNode;public bool doorMarker;public float lowPass,storm,panelHeight;public Vector3 cameraPosition,cameraEuler;}
        [Serializable]sealed class MarkerRecord{public bool visible;public int node,auditIssues;public Vector3 localMin,localMax;}
        [Serializable]sealed class Report{public List<ShotRecord> shots=new List<ShotRecord>();public float restFps,breathFps;public int restFrames,breathFrames;public float restSeconds,breathSeconds;public string fpsMethod="One 2.5s Editor sample per state, identical 1920x1080 view, PC quality; fixture holds cycle/AI. Diagnostic only; no native/Android benchmark.";}
        Report report=new Report();SkillSet1TestWorld world;FireBreathCycle cycle;
        public bool MeasureFps;
        public bool DragonOnly;
        public bool MobileOnly;
        public bool WarningOnly;
        IEnumerator Frames(int n=6){for(int i=0;i<n;i++)yield return null;}
        void Place(Vector3 point,float yaw,float pitch)
        {world.PlacePlayer(point);world.camera.transform.position=point-Quaternion.Euler(0,yaw,0)*Vector3.forward*3.4f+Vector3.up*1.9f;world.camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);world.camera.fieldOfView=60;}
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+name+".png",frame.EncodeToPNG());Destroy(frame);
            var v=cycle.GetComponent<FireBreathVisuals>();var hud=cycle.GetComponent<FireWarningHUD>();var dragon=FindAnyObjectByType<SkyBeastController>();
            float height=0;foreach(var rect in hud.GetComponentsInChildren<RectTransform>())if(rect.name=="Shelter / Fire warning")height=rect.sizeDelta.y;
            report.shots.Add(new ShotRecord{image=name,state=cycle.State.ToString(),shelter=ShelterDetector.AtFeet(world.player.transform.position).ToString(),dragonPose=dragon!=null?dragon.AnimationState:"",meteors=v.LiveMeteors,impacts=v.ImpactCount,scorches=v.ActiveScorches,plumes=v.PlumeCount,particles=v.ParticleCount,patches=cycle.Ground.ActiveCount,budget=v.Budget,doorNode=hud.DoorNode,doorMarker=hud.DoorMarkerVisible,lowPass=v.LowPassHz,storm=SettingsManager.Instance.Sky.FireStormIntensity,panelHeight=height,cameraPosition=world.camera.transform.position,cameraEuler=world.camera.transform.eulerAngles});
            File.WriteAllText(Root+name+"-text-audit.json",JsonUtility.ToJson(ComicTextAudit.Scan("P13 fix1 "+name),true));
        }
        IEnumerator Fps(bool breathing)
        {
            double begin=Time.realtimeSinceStartupAsDouble;int frames=0;
            while(Time.realtimeSinceStartupAsDouble-begin<2.5){yield return null;frames++;}
            float seconds=(float)(Time.realtimeSinceStartupAsDouble-begin);
            if(breathing){report.breathFps=frames/seconds;report.breathFrames=frames;report.breathSeconds=seconds;}
            else{report.restFps=frames/seconds;report.restFrames=frames;report.restSeconds=seconds;}
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Root);world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();UIValidation.SetResolution(1920,1080);world.player.enabled=false;
            yield return new WaitForSecondsRealtime(1.2f);foreach(var v in world.victims)v.gameObject.SetActive(false);
            cycle=FireBreathCycle.Ensure();cycle.AutoAdvance=false;cycle.Ground.enabled=false;
            Vector3 outdoor=new Vector3(10,.13f,-5);Place(outdoor,25,8);world.Lighting(false);SettingsManager.Instance.Sky.SetPreset(SkyPreset.Default);SettingsManager.Instance.Sky.SetBrightness(1);
            world.player.GetComponent<Monsters.PlayerMonsterHealth>().SetProgressionMaxHealth(10000);
            if(WarningOnly)
            {
                report=JsonUtility.FromJson<Report>(File.ReadAllText("task/p13/fix1-capture.json"));report.shots.RemoveAll(s=>s.image=="warning"||s.image=="warning-start");
                cycle.StartDev(8);cycle.Advance(1);yield return Frames(12);yield return Shot("warning-start");
                cycle.Advance(3.6f);yield return Frames(12);yield return Shot("warning");
                world.Mode(true);UIValidation.SetResolution(2340,1080);yield return Frames(12);
                var hud=cycle.GetComponent<FireWarningHUD>();var canvasRect=(RectTransform)hud.transform.Find("Fire Warning Comic Canvas");RectTransform markerRect=null;
                foreach(var rect in hud.GetComponentsInChildren<RectTransform>())if(rect.name=="Entrance screen / edge marker"){markerRect=rect;break;}
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect,markerRect);
                File.WriteAllText("task/p13/fix1-mobile-warning.json",JsonUtility.ToJson(new MarkerRecord{visible=hud.DoorMarkerVisible,node=hud.DoorNode,localMin=bounds.min,localMax=bounds.max,auditIssues=ComicTextAudit.Scan("P13 fix1 mobile warning").issues.Count},true));
                cycle.StopCycle();world.player.enabled=true;world.End();UIValidation.SetResolution(1920,1080);
                File.WriteAllText("task/p13/fix1-capture.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/p13/fix1-capture-DONE.txt","Updated Warning screenshots / reserved mobile marker area");Destroy(gameObject);yield break;
            }
            if(MobileOnly)
            {
                report=JsonUtility.FromJson<Report>(File.ReadAllText("task/p13/fix1-capture.json"));report.shots.RemoveAll(s=>s.image=="hud-mobile");
                cycle.StartDev(8);cycle.Advance(6);yield return new WaitForSecondsRealtime(2.5f);
                world.Mode(true);UIValidation.SetResolution(2340,1080);yield return new WaitForSecondsRealtime(1.5f);yield return Shot("hud-mobile");
                cycle.StopCycle();world.player.enabled=true;world.End();UIValidation.SetResolution(1920,1080);
                File.WriteAllText("task/p13/fix1-capture.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/p13/fix1-capture-DONE.txt","Updated mobile screenshot / scorch pool transition cap");Destroy(gameObject);yield break;
            }
            if(DragonOnly&&File.Exists("task/p13/fix1-capture.json")){report=JsonUtility.FromJson<Report>(File.ReadAllText("task/p13/fix1-capture.json"));report.shots.RemoveAll(s=>s.image=="level9-dragon-breath");}
            if(!DragonOnly)
            {
            if(MeasureFps){cycle.StartDev(8);cycle.Advance(20);yield return new WaitForSecondsRealtime(4);yield return Fps(false);}
            else if(File.Exists("task/p13/fix1-first-capture.json"))
            {
                var first=JsonUtility.FromJson<Report>(File.ReadAllText("task/p13/fix1-first-capture.json"));
                report.restFps=first.restFps;report.breathFps=first.breathFps;report.restFrames=first.restFrames;report.breathFrames=first.breathFrames;report.restSeconds=first.restSeconds;report.breathSeconds=first.breathSeconds;
                report.fpsMethod=first.fpsMethod+" Measured on first VFX pass, before single-puff shader cleanup / reduced particle caps; not remeasured.";
            }
            cycle.StartDev(8);cycle.Advance(1);yield return Frames(12);yield return Shot("warning-start");
            cycle.Advance(3.6f);yield return Frames(12);yield return Shot("warning");
            cycle.Advance(1.4f);yield return new WaitForSecondsRealtime(3.2f);if(MeasureFps)yield return Fps(true);yield return Shot("breath-courtyard");
            Place(new Vector3(-5,.13f,2),70,2);yield return new WaitForSecondsRealtime(1.5f);yield return Shot("breath-courtyard-wide");Place(outdoor,25,8);
            world.Mode(true);UIValidation.SetResolution(2340,1080);yield return new WaitForSecondsRealtime(1.5f);yield return Shot("hud-mobile");world.Mode(false);UIValidation.SetResolution(1920,1080);
            // Reuse the already verified north ground-floor window, with the camera physically inside.
            Place(new Vector3(27.48f,.13f,33),0,-30);world.camera.transform.position=new Vector3(27.48f,2,34.8f);
            yield return new WaitForSecondsRealtime(2.5f);yield return Shot("breath-indoor-window");
            Place(outdoor,25,16);cycle.Advance(4);cycle.Ground.Clear();
            foreach(var point in new[]{outdoor+new Vector3(2,0,4),outdoor+new Vector3(-3,0,6),outdoor+new Vector3(5,0,7),outdoor+new Vector3(-5,0,1)})cycle.Ground.SpawnAt(point,cycle.Profile);
            yield return new WaitForSecondsRealtime(1.2f);yield return Shot("afterfire-courtyard");
            }
            cycle.StopCycle();Levels.LevelSession.Select(9);var director=Levels.LevelDirector.Ensure();director.Begin(Levels.LevelCatalog.Instance.Get(9));director.enabled=false;cycle.AutoAdvance=false;yield return Frames(4);
            var dragon=FindAnyObjectByType<SkyBeastController>();Place(outdoor,0,-60);SettingsManager.Instance.Sky.SetBrightness(1);
            if(dragon!=null)dragon.SeekFlightForValidation(0);cycle.Advance(6);yield return new WaitForSecondsRealtime(1.3f);
            if(dragon!=null){dragon.SeekFlightForValidation(0);world.camera.transform.rotation=Quaternion.LookRotation(dragon.Mouth.position+Vector3.down*17-world.camera.transform.position);}
            yield return Frames(5);yield return Shot("level9-dragon-breath");
            director.End();world.player.enabled=true;world.End();UIValidation.SetResolution(1920,1080);
            File.WriteAllText("task/p13/fix1-capture.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/p13/fix1-capture-DONE.txt",report.shots.Count+" Unity framebuffer screenshots / FPS measured once on first pass");Destroy(gameObject);
        }
    }
}
#endif
