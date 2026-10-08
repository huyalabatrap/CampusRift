#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P11_BENCH)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.UI;
namespace CampusRift.Combat
{
    public sealed class ReactionPerformance : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public string capturedAt,environment,gpu,cpu,quality,pipeline,error="";
            public int width,height,enemies=12,objectsBefore,objectsAfter,exhausted,peakParticles,samples,renderedFrames;
            public float idleFPS,activeFPS,lossPercent,meanIdleMs,meanActiveMs,p95Ms;
            public bool postProcessing,bloom,hdr,comicInk,passed;public long maxVfxFrameGCBytes;
        }
        readonly Report report=new Report();readonly List<float> idle=new List<float>(512),active=new List<float>(8192);ReactionTestWorld fixture;
        string Output=>Application.isEditor?"Artifacts/Reactions/fix3/Performance-Editor":"Artifacts/Reactions/fix3/Performance-Player";
        IEnumerator Start()
        {
            fixture=new ReactionTestWorld{hidePassive=false};var run=Run();
            while(true){bool more=false;object next=null;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){report.error=e.ToString();break;}if(!more)break;yield return next;}
            try{fixture.End();}catch(Exception e){report.error+=" Restore "+e;}
            Directory.CreateDirectory("Artifacts/Reactions/fix3");File.WriteAllText(Output+".json",JsonUtility.ToJson(report,true));File.WriteAllText(Output+"-DONE.txt",report.passed&&report.error.Length==0?"PASS":"FAIL");Destroy(gameObject);
        }
        IEnumerator Run()
        {
            fixture.Begin();var player=fixture.world.player;var pool=player.GetComponent<SkillVfxPool>();Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            fixture.world.Mode(false);var settings=SettingsManager.Instance.Current.Copy();settings.ResolutionWidth=1920;settings.ResolutionHeight=1080;settings.Fullscreen=false;settings.VSync=false;SettingsManager.Instance.Apply(settings,false);for(int i=0;i<60;i++)yield return null;
            fixture.Triple();yield return new WaitForSeconds(.18f);
            if(ReactionGpuBenchRender.Instance!=null)ReactionGpuBenchRender.Instance.Snapshot(Path.GetFullPath("Artifacts/Reactions/fix3/Player-crowd-render.png"));
            yield return new WaitForSeconds(3);fixture.Arrange();pool.Clear();DamageNumberPool.Instance.ReactionLabels.Clear();player.GetComponent<ReactionFeedback>().Hint.Clear();
            report.capturedAt=DateTime.UtcNow.ToString("o");report.environment=Application.isEditor?"Unity Editor":"Windows standalone Mono release; D3D12 offscreen URP StandardRequest/full camera stack to real 1920x1080 HDR target; GPU fence completed every frame; overlay HUD routed to camera for GPU target; PC quality/default gameplay camera/12 real enemies/passive swords/ComicInk/Bloom enabled; no screenshot or MCP calls in sampling; ten simultaneous IceLightning+FireExplosion+Convergence groups; display Present excluded";
            report.gpu=SystemInfo.graphicsDeviceName;report.cpu=SystemInfo.processorType;report.quality=QualitySettings.names[QualitySettings.GetQualityLevel()];report.width=Screen.width;report.height=Screen.height;
            var camera=fixture.world.camera;var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();report.postProcessing=data!=null&&data.renderPostProcessing;
            var stack=data!=null&&data.volumeStack!=null?data.volumeStack:UnityEngine.Rendering.VolumeManager.instance.stack;
            if(stack==null)throw new InvalidOperationException("No rendered volume stack; refuse non-rendered benchmark");
            var bloom=stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();report.bloom=bloom!=null&&bloom.IsActive();
            var asset=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            CapturePipeline(asset);
            report.comicInk&=ComicRendering.Enabled&&camera.gameObject.scene.name=="SampleScene";
            pool.ResetMetrics();report.objectsBefore=Objects(player);
            for(int i=0;i<240;i++){yield return null;idle.Add(Time.unscaledDeltaTime*1000);}
            for(int group=0;group<10;group++)
            {
                fixture.Triple();float until=Time.realtimeSinceStartup+1.1f;
                while(Time.realtimeSinceStartup<until){fixture.world.Look(90,14);yield return null;active.Add(Time.unscaledDeltaTime*1000);}
            }
            report.peakParticles=pool.PeakParticles;report.maxVfxFrameGCBytes=pool.MaxFrameGCBytes;report.exhausted=pool.ExhaustedCount;
            yield return new WaitForSeconds(3);report.objectsAfter=Objects(player);
            report.meanIdleMs=Mean(idle);report.meanActiveMs=Mean(active);idle.Sort();active.Sort();report.idleFPS=1000/idle[idle.Count/2];report.activeFPS=1000/active[active.Count/2];report.samples=active.Count;report.p95Ms=active[(int)(active.Count*.95f)];report.lossPercent=(1-report.activeFPS/report.idleFPS)*100;
            var gpuRender=ReactionGpuBenchRender.Instance;
            report.renderedFrames=gpuRender!=null?gpuRender.RenderedFrames:0;if(gpuRender!=null)report.error+=gpuRender.Error;
            report.passed=report.lossPercent<=10&&report.objectsBefore==report.objectsAfter&&report.exhausted==0&&pool.ActiveCount==0&&report.postProcessing&&report.bloom&&report.hdr&&report.comicInk&&(Application.isEditor||report.renderedFrames>report.samples);
        }
        static int Objects(Component player)=>player.GetComponentsInChildren<Transform>(true).Length+DamageNumberPool.Instance.GetComponentsInChildren<Transform>(true).Length;
        void CapturePipeline(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset asset)
        {if(asset!=null){report.pipeline=asset.name;report.hdr=asset.supportsHDR;foreach(var renderer in asset.rendererDataList)if(renderer!=null)foreach(var feature in renderer.rendererFeatures)if(feature is ComicInkFeature&&feature.isActive)report.comicInk=true;}}
        static float Mean(List<float> values){float total=0;foreach(float v in values)total+=v;return total/values.Count;}
    }
}
#endif
