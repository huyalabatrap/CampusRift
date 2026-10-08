#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using CampusRift.Skills;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    // Short Editor-only diagnostic fixture. Never saved in SampleScene or shipped in a build.
    public sealed class FirePerformanceCapture : MonoBehaviour
    {
        [Serializable] public sealed class Counter { public string name,unit; public double mean,max; }
        [Serializable] public sealed class Sample
        {
            public string name,state,quality,pipeline; public bool comicInk;public int qualityIndex,budget,width,height,visibleEnemies;public int frames,meteors,particles,patches,drawCalls,batches,setPass,lights; public long triangles;
            public double seconds,fps,meanMs,p95Ms,gpuMs,cpuMs; public bool gpuTimingAvailable;
            public Vector3 cameraPosition,cameraEuler; public List<Counter> counters=new List<Counter>();
        }
        [Serializable] public sealed class Report
        {
            public string label,gpu,cpu,target,method="Unity Editor framebuffer 1920x1080, same fixed camera and deterministic seed; VSync/target cap off, AI held, actual URP + comic HUD; one 5s sample per state after warmup. Not native/device FPS.";
            public List<Sample> samples=new List<Sample>(); public List<string> availableCounters=new List<string>();
        }
        public string Label="before";
        public bool Diagnostics=true,OtherEffects=true,HoldForDebugger=true,DragonOnly,EnemiesOnly,QuickFix1;
        public string Stage="starting";
        SkillSet1TestWorld world; FireBreathCycle cycle; FireBreathVisuals visuals; SkyBeastController dragon;
        Report report; readonly List<ProfilerRecorder> recorders=new List<ProfilerRecorder>(); readonly List<ProfilerRecorderDescription> descriptions=new List<ProfilerRecorderDescription>();
        readonly FrameTiming[] timings=new FrameTiming[1]; int oldTarget; bool oldProfiler;
        static readonly Vector3 Outdoor=new Vector3(10,.13f,-5);
        void Write()=>File.WriteAllText("task/perf/"+Label+".json",JsonUtility.ToJson(report,true));
        void Configure(bool mobile)
        {
            world.Mode(mobile);var s=SettingsManager.Instance.Current.Copy();s.Quality=mobile?0:1;s.ComicEffects=true;s.VSync=false;SettingsManager.Instance.Apply(s,false);
            QualitySettings.SetQualityLevel(s.Quality,true);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            FireVisualQuality.Override=mobile?FireEffectQuality.Automatic:FireEffectQuality.PcHigh;
        }
        void Place(float yaw=25,float pitch=8)
        {
            world.PlacePlayer(Outdoor);world.camera.transform.position=Outdoor-Quaternion.Euler(0,yaw,0)*Vector3.forward*3.4f+Vector3.up*1.9f;
            world.camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);world.camera.fieldOfView=60;
        }
        IEnumerator Wait(float seconds){yield return new WaitForSecondsRealtime(seconds);}
        IEnumerator Measure(string name)
        {
            Stage=name;yield return Wait(1.5f);
            var sample=new Sample{name=name,state=cycle.State.ToString(),quality=FireVisualQuality.Current.ToString(),cameraPosition=world.camera.transform.position,cameraEuler=world.camera.transform.eulerAngles,
                qualityIndex=QualitySettings.GetQualityLevel(),pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,comicInk=ComicRendering.Enabled,budget=visuals.Budget,width=Screen.width,height=Screen.height};
            var sums=new double[recorders.Count];var max=new double[recorders.Count];var frameMs=new List<double>(1200);
            double begin=Time.realtimeSinceStartupAsDouble,gpu=0,cpu=0;int timed=0;
            while(Time.realtimeSinceStartupAsDouble-begin<5)
            {
                FrameTimingManager.CaptureFrameTimings();yield return null;sample.frames++;frameMs.Add(Time.unscaledDeltaTime*1000);
                if(name=="Warning")cycle.Advance(Time.unscaledDeltaTime);
                for(int i=0;i<recorders.Count;i++){double v=recorders[i].LastValue;sums[i]+=v;if(v>max[i])max[i]=v;}
                sample.drawCalls+=UnityEditor.UnityStats.drawCalls;sample.setPass+=UnityEditor.UnityStats.setPassCalls;sample.triangles+=UnityEditor.UnityStats.triangles;
                if(FrameTimingManager.GetLatestTimings(1,timings)>0&&timings[0].gpuFrameTime>0){gpu+=timings[0].gpuFrameTime;cpu+=timings[0].cpuFrameTime;timed++;}
            }
            sample.seconds=Time.realtimeSinceStartupAsDouble-begin;sample.fps=sample.frames/sample.seconds;sample.meanMs=1000/sample.fps;
            frameMs.Sort();sample.p95Ms=frameMs[(int)(frameMs.Count*.95)];sample.gpuTimingAvailable=timed>0;sample.gpuMs=timed>0?gpu/timed:0;sample.cpuMs=timed>0?cpu/timed:0;
            sample.drawCalls/=sample.frames;sample.batches/=sample.frames;sample.setPass/=sample.frames;sample.triangles/=sample.frames;
            sample.meteors=visuals.LiveMeteors;sample.particles=visuals.ParticleCount;sample.patches=cycle.Ground.ActiveCount;sample.lights=FindObjectsByType<Light>(FindObjectsSortMode.None).Length;
            var planes=GeometryUtility.CalculateFrustumPlanes(world.camera);
            foreach(var victim in world.victims)if(victim.gameObject.activeInHierarchy)
                foreach(var skin in victim.GetComponentsInChildren<SkinnedMeshRenderer>())if(skin.enabled&&skin.isVisible&&GeometryUtility.TestPlanesAABB(planes,skin.bounds)){sample.visibleEnemies++;break;}
            for(int i=0;i<recorders.Count;i++)sample.counters.Add(new Counter{name=descriptions[i].Name,unit=descriptions[i].UnitType.ToString(),mean=sums[i]/sample.frames,max=max[i]});
            report.samples.Add(sample);Write();
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("task/perf/screens/"+Label+"-"+name+".png",frame.EncodeToPNG());Destroy(frame);
        }
        void BeginState(FireBreathCycle.Phase state)
        {
            foreach(var p in visuals.GetComponentsInChildren<ParticleSystem>(true))p.Clear();
            UnityEngine.Random.InitState(130513);cycle.StartDev(8);
            if(state==FireBreathCycle.Phase.Breath)cycle.Advance(6);
            if(state==FireBreathCycle.Phase.Afterfire)cycle.Advance(10);
            if(state==FireBreathCycle.Phase.Rest){cycle.Advance(20);cycle.Ground.Clear();}
        }
        void RenderGroup(string group,bool enabled)
        {
            foreach(var r in visuals.GetComponentsInChildren<Renderer>(true))
            {
                bool match=group=="lines"?r is LineRenderer:group=="particles"?r is ParticleSystemRenderer:r is SpriteRenderer;
                if(match)r.enabled=enabled;
            }
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("task/perf/screens");oldTarget=Application.targetFrameRate;oldProfiler=UnityEngine.Profiling.Profiler.enabled;
            UnityEngine.Profiling.Profiler.enabled=false;
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();UIValidation.SetResolution(1920,1080);Application.targetFrameRate=-1;world.player.enabled=false;
            yield return Wait(1.2f);Configure(false);foreach(var v in world.victims)v.gameObject.SetActive(false);
            cycle=FireBreathCycle.Ensure();cycle.AutoAdvance=false;visuals=cycle.GetComponent<FireBreathVisuals>();Place();world.Lighting(false);SettingsManager.Instance.Sky.SetPreset(SkyPreset.Default);SettingsManager.Instance.Sky.SetBrightness(1);
            world.player.GetComponent<Monsters.PlayerMonsterHealth>().SetProgressionMaxHealth(100000);
            report=new Report{label=Label,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,target=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString()};
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            foreach(var h in handles)
            {
                var d=ProfilerRecorderHandle.GetDescription(h);report.availableCounters.Add(d.Category.Name+" / "+d.Name+" / "+d.UnitType);
                if(d.Name=="Main Thread"||d.Name=="Render Thread"||d.Name=="GC Allocated In Frame"||d.Name=="GC.Collect"||d.Name=="Update.ScriptRunBehaviourUpdate"||d.Name=="PreLateUpdate.ScriptRunBehaviourLateUpdate"||d.Name=="ParticleSystem.Update"||d.Name=="Physics.Simulate")
                {var r=ProfilerRecorder.StartNew(d.Category,d.Name,1);if(r.Valid){recorders.Add(r);descriptions.Add(d);}else r.Dispose();}
            }
            if(EnemiesOnly)
            {
                Configure(true);BeginState(FireBreathCycle.Phase.Rest);yield return Measure("Mobile-empty");
                var forward=Vector3.ProjectOnPlane(world.camera.transform.forward,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,forward);
                for(int i=0;i<world.victims.Length;i++){var victim=world.victims[i];victim.gameObject.SetActive(true);var motor=victim.GetComponent<Enemies.MinionMotor>();motor.Place(Outdoor+forward*(8+(i%2)*2)+right*((i-3)*1.3f));motor.Stop();}
                yield return Measure("Mobile-seven-visible-TieuYeu-two-sided");
                foreach(var r in recorders)r.Dispose();recorders.Clear();Write();Stage="done";File.WriteAllText("task/perf/"+Label+"-DONE.txt","DONE: Mobile empty / 7 visible P12 TieuYeu with two-sided shader, 5s each");yield break;
            }
            if(!DragonOnly)foreach(var state in QuickFix1?new[]{FireBreathCycle.Phase.Rest,FireBreathCycle.Phase.Breath}:new[]{FireBreathCycle.Phase.Rest,FireBreathCycle.Phase.Warning,FireBreathCycle.Phase.Breath,FireBreathCycle.Phase.Afterfire})
            {BeginState(state);yield return Measure(state.ToString());if(state==FireBreathCycle.Phase.Breath)yield return Shot("courtyard-breath");}
            if(Diagnostics&&!DragonOnly)
            {
                BeginState(FireBreathCycle.Phase.Breath);RenderGroup("lines",false);yield return Measure("Breath-no-lines");RenderGroup("lines",true);
                RenderGroup("particles",false);yield return Measure("Breath-no-particles");RenderGroup("particles",true);
                visuals.enabled=false;yield return Measure("Breath-no-visual-Update");visuals.enabled=true;
            }
            cycle.StopCycle();foreach(var p in visuals.GetComponentsInChildren<ParticleSystem>(true))p.Clear();yield return Wait(4);
            if(OtherEffects&&!DragonOnly)
            {
                BeginState(FireBreathCycle.Phase.Rest);ComicRendering.PreviewEnabled=false;yield return Measure("Rest-ComicInk-off");ComicRendering.PreviewEnabled=null;
                foreach(var v in world.victims)v.gameObject.SetActive(true);world.Arrange();Place();yield return Measure("Rest-seven-P12-enemies");
                foreach(var v in world.victims)v.gameObject.SetActive(false);
            }
            Levels.LevelSession.Select(9);var director=Levels.LevelDirector.Ensure();director.Begin(Levels.LevelCatalog.Instance.Get(9));director.enabled=false;cycle.AutoAdvance=false;
            yield return Wait(.3f);dragon=FindAnyObjectByType<SkyBeastController>();dragon.SeekFlightForValidation(0);dragon.HoldCinematic(dragon.transform.position,Quaternion.Euler(0,270,0));cycle.Advance(6);dragon.RequestBreath(120);
            yield return Wait(1.5f);var animator=dragon.GetComponentInChildren<Animator>();animator.speed=0;Place(0,-60);world.camera.transform.rotation=Quaternion.LookRotation(dragon.Mouth.position+Vector3.down*17-world.camera.transform.position);
            cycle.StopCycle();foreach(var p in visuals.GetComponentsInChildren<ParticleSystem>(true))p.Clear();yield return Wait(4);
            if(QuickFix1)
            {
                BeginState(FireBreathCycle.Phase.Breath);yield return Wait(2);yield return Shot("dragon9-breath");
                Write();Stage="done";File.WriteAllText("task/perf/"+Label+"-DONE.txt","DONE: PC High Rest/Breath once, courtyard and dragon9 screenshots");
                if(!HoldForDebugger)Finish();yield break;
            }
            BeginState(FireBreathCycle.Phase.Rest);yield return Measure("Dragon9-Rest");BeginState(FireBreathCycle.Phase.Breath);yield return Measure("Dragon9-Breath");yield return Shot("dragon9-breath");
            if(OtherEffects)
            {
                var lod=dragon.GetComponentInChildren<LODGroup>();if(lod!=null){lod.ForceLOD(1);yield return Measure("Dragon9-Breath-LOD1");lod.ForceLOD(-1);}
            }
            if(DragonOnly){Write();Stage="done";File.WriteAllText("task/perf/"+Label+"-DONE.txt","DONE: corrected active dragon, fixed Spell_Loop pose, actual mouth cone; "+report.samples.Count+" samples");yield break;}
            cycle.StopCycle();SkyBeastPresence.StopAll();yield return Wait(.2f);Place();Configure(true);yield return Wait(.5f);
            BeginState(FireBreathCycle.Phase.Rest);yield return Wait(4);yield return Measure("Mobile-Rest");BeginState(FireBreathCycle.Phase.Breath);yield return Measure("Mobile-Breath");
            if(!Label.StartsWith("before")){Configure(false);FireVisualQuality.Override=FireEffectQuality.PcLow;BeginState(FireBreathCycle.Phase.Rest);yield return Wait(4);yield return Measure("PC-Low-Rest");BeginState(FireBreathCycle.Phase.Breath);yield return Measure("PC-Low-Breath");}
            Configure(false);BeginState(FireBreathCycle.Phase.Breath);yield return Wait(2);
            foreach(var r in recorders)r.Dispose();recorders.Clear();Write();Stage="done";File.WriteAllText("task/perf/"+Label+"-DONE.txt","DONE: "+report.samples.Count+" short Editor samples; held PC Breath for Frame Debugger");
            if(!HoldForDebugger)Finish();
        }
        public void Finish()
        {
            cycle.StopCycle();world.player.enabled=true;world.End();Application.targetFrameRate=oldTarget;UnityEngine.Profiling.Profiler.enabled=oldProfiler;Destroy(gameObject);
        }
        void OnDestroy(){foreach(var r in recorders)r.Dispose();}
    }
}
#endif
