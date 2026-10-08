#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.Controls;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    public sealed class P13Capture:MonoBehaviour
    {
        [Serializable]sealed class ShotRecord{public string image,state,shelter,dragonPose;public int meteors,patches,doorNode;public float lowPass;public Vector3 cameraPosition,cameraEuler;}
        [Serializable]sealed class Report{public List<ShotRecord> shots=new List<ShotRecord>();}
        SkillSet1TestWorld world;FireBreathCycle cycle;
        public bool WindowOnly;
        List<ShotRecord> shots=new List<ShotRecord>();
        IEnumerator Frames(int n=6){for(int i=0;i<n;i++)yield return null;}
        void Place(Vector3 point,float yaw,float pitch)
        {world.PlacePlayer(point);world.camera.transform.position=point-Quaternion.Euler(0,yaw,0)*Vector3.forward*3.4f+Vector3.up*1.9f;world.camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);world.camera.fieldOfView=60;}
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("task/p13/screens/"+name+".png",frame.EncodeToPNG());Destroy(frame);
            var visuals=cycle.GetComponent<FireBreathVisuals>();var hud=cycle.GetComponent<FireWarningHUD>();var dragon=FindAnyObjectByType<SkyBeastController>();
            shots.Add(new ShotRecord{image=name,state=cycle.State.ToString(),shelter=ShelterDetector.AtFeet(world.player.transform.position).ToString(),dragonPose=dragon!=null?dragon.AnimationState:"",meteors=visuals.LiveMeteors,patches=cycle.Ground.ActiveCount,doorNode=hud.DoorNode,lowPass=visuals.LowPassHz,cameraPosition=world.camera.transform.position,cameraEuler=world.camera.transform.eulerAngles});
            File.WriteAllText("task/p13/screens/"+name+"-text-audit.json",JsonUtility.ToJson(ComicTextAudit.Scan("P13 "+name),true));
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("task/p13/screens");world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();UIValidation.SetResolution(1920,1080);world.player.enabled=false;
            yield return new WaitForSecondsRealtime(1.2f);
            foreach(var v in world.victims)v.gameObject.SetActive(false);
            cycle=FireBreathCycle.Ensure();cycle.AutoAdvance=false;cycle.Ground.enabled=false;
            var outdoor=new Vector3(10,.13f,-5);Place(outdoor,25,5);world.Lighting(false);SettingsManager.Instance.Sky.SetPreset(SkyPreset.Default);SettingsManager.Instance.Sky.SetBrightness(1);
            world.player.GetComponent<Monsters.PlayerMonsterHealth>().SetProgressionMaxHealth(10000);
            if(WindowOnly)
            {
                Place(new Vector3(27.48f,.13f,33),0,-30);world.camera.transform.position=new Vector3(27.48f,2,34.8f);
                cycle.StartDev(8);cycle.Advance(6);yield return new WaitForSeconds(2.2f);yield return Shot("breath-indoor-window");
                var previous=JsonUtility.FromJson<Report>(File.ReadAllText("task/p13/capture.json"));previous.shots.RemoveAll(s=>s.image=="breath-indoor-window");previous.shots.Add(shots[0]);
                File.WriteAllText("task/p13/capture.json",JsonUtility.ToJson(previous,true));cycle.StopCycle();world.player.enabled=true;world.End();
                File.WriteAllText("task/p13/window-capture-DONE.txt","1 real indoor north window camera screenshot / no geometry change");Destroy(gameObject);yield break;
            }
            cycle.StartDev(8);cycle.Advance(2.8f);yield return Frames(12);yield return Shot("warning");
            cycle.Advance(3.2f);yield return new WaitForSeconds(1.5f);cycle.Advance(1.5f);yield return Shot("breath-courtyard");
            world.Mode(true);UIValidation.SetResolution(2340,1080);yield return Frames(10);yield return Shot("hud-mobile");world.Mode(false);UIValidation.SetResolution(1920,1080);
            // A ground-floor room with a real window onto the courtyard; rain stays outside its wall.
            Place(new Vector3(35,.13f,24),180,2);world.camera.transform.position=new Vector3(35,1.8f,22.7f);yield return new WaitForSeconds(1);yield return Shot("breath-indoor-window");
            cycle.Advance(2.5f);cycle.Ground.Clear();Place(outdoor,25,16);
            foreach(var p in new[]{outdoor+new Vector3(2,0,4),outdoor+new Vector3(-3,0,6),outdoor+new Vector3(5,0,7),outdoor+new Vector3(-5,0,1)})cycle.Ground.SpawnAt(p,cycle.Profile);
            yield return new WaitForSeconds(1.1f);yield return Shot("afterfire-courtyard");
            cycle.StopCycle();Levels.LevelSession.Select(9);var director=Levels.LevelDirector.Ensure();director.Begin(Levels.LevelCatalog.Instance.Get(9));director.enabled=false;cycle.AutoAdvance=false;yield return Frames(4);var dragon=FindAnyObjectByType<SkyBeastController>();
            Place(new Vector3(10,.13f,-5),0,-68);SettingsManager.Instance.Sky.SetPreset(SkyPreset.Inferno);SettingsManager.Instance.Sky.SetBrightness(1);
            if(dragon!=null)dragon.SeekFlightForValidation(0);cycle.Advance(6);yield return new WaitForSeconds(1.25f);
            if(dragon!=null){dragon.SeekFlightForValidation(0);Vector3 view=dragon.Mouth.position-world.camera.transform.position;world.camera.transform.rotation=Quaternion.LookRotation(view);}
            cycle.Advance(1.25f);yield return Frames(4);yield return Shot("level9-dragon-breath");
            director.End();world.player.enabled=true;world.End();UIValidation.SetResolution(1920,1080);
            File.WriteAllText("task/p13/capture.json",JsonUtility.ToJson(new Report{shots=shots},true));File.WriteAllText("task/p13/capture-DONE.txt","6 real Unity camera screenshots / review fixture");Destroy(gameObject);
        }
    }
}
#endif
