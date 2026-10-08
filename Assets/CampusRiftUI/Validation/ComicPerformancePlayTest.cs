#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Controls;
using CampusRift.Levels;
using CampusRift.Progression;
using UnityEngine;

namespace CampusRift.UI
{
    public sealed class ComicPerformancePlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Trial {public bool ink;public int samples;public float medianMs,meanMs,p95Ms,fps;public double gpuMs;}
        [Serializable] public sealed class Report { public string environment="Unity Editor Windows Game view, 1920x1080, PC quality, VSync off, same paused wave with two enemies and gameplay HUD";public string gpu,cpu;public int enemies;public List<Trial> trials=new List<Trial>();public float offFps,onFps,fpsLossPercent;public bool withinTenPercent,toggleWorks,lowQualityOff,mobileOff; }
        readonly Report report=new Report();
        public static void Begin(){new GameObject("Comic performance review").AddComponent<ComicPerformancePlayTest>();}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;
            var original=SettingsManager.Instance.Current.Copy();var pc=original.Copy();pc.Quality=1;pc.VSync=false;pc.ControlMode=ControlMode.PC;pc.ComicEffects=true;pc.SkyBrightness=1;
            SettingsManager.Instance.Apply(pc,false);ProfileService.Instance.UseTransient(new ProfileData());UIValidation.SetResolution(1920,1080);
            GameSceneManager.Instance.StartLevel(1);
            while(GameSceneManager.Instance.IsLoading||LevelDirector.Instance==null||LevelDirector.Instance.Level==null)yield return null;
            for(int i=0;i<5;i++)yield return null;SettingsManager.Instance.Apply(pc,false);
            UIStateManager.Instance.EnterScene(true);Time.timeScale=1;
            float deadline=Time.realtimeSinceStartup+20;while(LevelDirector.Instance.AliveCount<2&&Time.realtimeSinceStartup<deadline)yield return null;
            Time.timeScale=0;report.enemies=LevelDirector.Instance.AliveCount;report.gpu=SystemInfo.graphicsDeviceName;report.cpu=SystemInfo.processorType;
            report.toggleWorks=ComicRendering.Enabled;ComicRendering.PreviewEnabled=false;report.toggleWorks&=!ComicRendering.Enabled;ComicRendering.PreviewEnabled=null;
            QualitySettings.SetQualityLevel(0,true);report.lowQualityOff=!ComicRendering.Enabled;QualitySettings.SetQualityLevel(1,true);
            var mobile=pc.Copy();mobile.ControlMode=ControlMode.Mobile;SettingsManager.Instance.Apply(mobile,false);report.mobileOff=!ComicRendering.Enabled;SettingsManager.Instance.Apply(pc,false);
            foreach(bool ink in new[]{false,true,false,true})
            {
                ComicRendering.PreviewEnabled=ink;
                for(int i=0;i<45;i++)yield return null;
                if(report.trials.Count<2){ScreenCapture.CaptureScreenshot("task/ui-comic/screens/round3/ink-"+(ink?"on":"off")+".png");for(int i=0;i<30;i++)yield return null;}
                var times=new List<float>();var gpu=new List<double>();var timing=new FrameTiming[1];
                for(int i=0;i<240;i++)
                {
                    FrameTimingManager.CaptureFrameTimings();yield return null;
                    times.Add(Time.unscaledDeltaTime*1000);
                    if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0)gpu.Add(timing[0].gpuFrameTime);
                }
                times.Sort();float median=times[times.Count/2];
                report.trials.Add(new Trial {ink=ink,samples=times.Count,meanMs=times.Average(),medianMs=median,p95Ms=times[(int)(times.Count*.95f)],fps=1000/median,gpuMs=gpu.Count>0?gpu.Average():0});
                Directory.CreateDirectory("task/ui-comic/tests/round3");File.WriteAllText("task/ui-comic/tests/round3/Performance.json",JsonUtility.ToJson(report,true));
            }
            report.offFps=report.trials.Where(t=>!t.ink).Average(t=>t.fps);report.onFps=report.trials.Where(t=>t.ink).Average(t=>t.fps);
            report.fpsLossPercent=(1-report.onFps/report.offFps)*100;report.withinTenPercent=report.fpsLossPercent<=10;
            File.WriteAllText("task/ui-comic/tests/round3/Performance.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/ui-comic/tests/round3/Performance-DONE.txt",report.fpsLossPercent.ToString("0.00")+"% median FPS loss");
            ComicRendering.PreviewEnabled=null;LevelSession.Clear();ProfileService.Instance.EndTransient();SettingsManager.Instance.Apply(original,false);Time.timeScale=1;
            Destroy(gameObject);
        }
    }
}
#endif
