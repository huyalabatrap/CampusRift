#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Levels;
using CampusRift.Progression;
using CampusRift.UI;
using CampusRift.SkyBeast;
namespace CampusRift.Validation
{
    public sealed class P17MobilePerf:MonoBehaviour
    {
        [Serializable] public sealed class Sample {public int level,frames,peakEnemies,particles,width,height;public double seconds,fps,p95ms;public long drawCalls,triangles;public string fireState;}
        [Serializable] public sealed class Report {public string method="One 5-second Unity Editor sample per level 7/10, Mobile Low, 1920x1080, real spawn and AI, fixed outdoor camera, immortal QA player. L10 held Breath for this diagnostic. Uncapped/vsync off. Not Android device performance, not worst-case/persistent heat.";public string cpu,gpu;public List<Sample> samples=new List<Sample>();}
        IEnumerator Start()
        {
            var report=new Report{cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName};var original=SettingsManager.Instance.Current.Copy();int oldTarget=Application.targetFrameRate;int oldVSync=QualitySettings.vSyncCount;
            ProfileService.Instance.UseTransient(new ProfileData());ProfileService.Instance.Cultivation.SetState(Realm.DoKiep,1,0);TutorialDirector.Suppress=true;UIStateManager.Instance.EnterScene(true);
            var settings=original.Copy();settings.ControlMode=Controls.ControlMode.Mobile;settings.Quality=0;settings.VSync=false;settings.LocalTelemetryEnabled=false;settings.TelemetryConsentAsked=true;SettingsManager.Instance.Apply(settings,false);UIValidation.SetResolution(1920,1080);Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            var player=FindAnyObjectByType<CampusExplorer>();player.enabled=false;player.GetComponent<Monsters.PlayerMonsterHealth>().Revive(1,0);player.GetComponent<Monsters.PlayerMonsterHealth>().respawnOnDefeat=false;
            var director=LevelDirector.Ensure();var health=player.GetComponent<Monsters.PlayerMonsterHealth>();var qa=player.gameObject.AddComponent<V2QaPlayer>();qa.God=true;
            foreach(int level in new[]{7,10})
            {
                director.Begin(LevelCatalog.Instance.Get(level));float until=Time.realtimeSinceStartup+20;while(director.AliveCount<6&&Time.realtimeSinceStartup<until)yield return null;
                if(level==10){var fire=FireBreathCycle.Ensure();fire.AutoAdvance=false;fire.Advance(fire.Remaining+.01f);}
                player.followCamera.transform.position=new Vector3(12,4,-12);player.followCamera.transform.LookAt(new Vector3(0,3,4));yield return new WaitForSecondsRealtime(1);
                var sample=new Sample{level=level,width=Screen.width,height=Screen.height};var ms=new List<float>();double begin=Time.realtimeSinceStartupAsDouble;
                while(Time.realtimeSinceStartupAsDouble-begin<5)
                {
                    yield return null;sample.frames++;ms.Add(Time.unscaledDeltaTime*1000);sample.peakEnemies=Mathf.Max(sample.peakEnemies,director.AliveCount);sample.drawCalls+=UnityEditor.UnityStats.drawCalls;sample.triangles+=UnityEditor.UnityStats.triangles;
                }
                sample.seconds=Time.realtimeSinceStartupAsDouble-begin;sample.fps=sample.frames/sample.seconds;ms.Sort();sample.p95ms=ms[Mathf.Min(ms.Count-1,(int)(ms.Count*.95))];sample.drawCalls/=sample.frames;sample.triangles/=sample.frames;
                foreach(var p in FindObjectsByType<ParticleSystem>())sample.particles+=p.particleCount;sample.fireState=FireBreathCycle.Instance!=null?FireBreathCycle.Instance.State.ToString():"Disabled";report.samples.Add(sample);
                File.WriteAllText("task/p17/mobile-editor.json",JsonUtility.ToJson(report,true));director.End();yield return null;
            }
            qa.God=false;Destroy(qa);player.enabled=true;ProfileService.Instance.EndTransient();SettingsManager.Instance.Apply(original,false);Application.targetFrameRate=oldTarget;QualitySettings.vSyncCount=oldVSync;File.WriteAllText("task/p17/mobile-editor-DONE.txt","2 short Editor Mobile samples complete");Destroy(gameObject);
        }
    }
}
#endif
