#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P10_BENCH)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.UI;
namespace CampusRift.Skills
{
    public sealed class SkillSet1Performance:MonoBehaviour
    {
        [Serializable] public sealed class Trial {public string skill;public int samples,peakParticles;public long maxVfxFrameGCBytes;public float idleStartFps,idleDestinationFps,baselineFps,castFps,lossPercent,meanBaselineMs,meanCastMs,p95Ms;public bool withinTenPercent;}
        [Serializable] public sealed class Report {public string capturedAt,environment,gpu,cpu,quality,pipeline;public int width,height;public string error="";public int renderedFrames;public bool postProcessing,bloom,comicInk,hdr;public float bloomIntensity;public List<Trial> trials=new List<Trial>();}
        readonly Report report=new Report();SkillSet1TestWorld world;SkillVfxPool pool;SpiritPower spirit;
        public string onlySkill="";
        string Output=>Application.isEditor?"Artifacts/Skills/fix3/Performance-Editor":"Artifacts/Skills/fix3/Performance-Player";
        readonly List<float> baseline=new List<float>(2048),destination=new List<float>(512),active=new List<float>(2048);
        void Update(){if(world!=null&&world.player!=null&&UIStateManager.Instance.State!=UIState.Gameplay)UIStateManager.Instance.EnterScene(true);}
        IEnumerator Start()
        {
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();pool=world.player.GetComponent<SkillVfxPool>();spirit=world.player.GetComponent<SpiritPower>();
            for(int i=0;i<10;i++)yield return null;
            report.capturedAt=DateTime.UtcNow.ToString("o");report.environment="Unity 6000.6.2f1 Windows Editor / StandaloneWindows64 / PC Quality / VSync off / normal gameplay camera / seven real enemies / comic ink and HUD enabled; frame medians, three casts each; no screenshots or MCP calls during sampling. Flash baseline samples both origin/yaw90 and dash destination/yaw235 without casting, weighted by active-frame counts in each camera view; both unweighted idle FPS are retained.";
            report.gpu=SystemInfo.graphicsDeviceName;report.cpu=SystemInfo.processorType;report.width=Screen.width;report.height=Screen.height;
            CaptureGraphics();
            if(!Application.isEditor)report.environment=report.environment.Replace("Windows Editor","Windows standalone player (Mono release benchmark)");
            foreach(var skill in world.player.GetComponents<Set1SkillRuntime>())
            {
                if(onlySkill.Length>0&&skill.Id!=onlySkill)continue;
                if(onlySkill.Length==0&&skill.Id!="van-kiem-quyet"&&skill.Id!="than-kiem-ngu-loi"&&skill.Id!="phat-no-hoa-lien"&&skill.Id!="tich-lich-nhat-thiem")continue;
                world.Arrange(skill is LightningFlashRuntime);if(skill is ChainLightningRuntime)world.ChainLayout();world.player.GetComponent<SkillLoadout>().Equip(2,skill.Id);pool.Clear();
                for(int i=0;i<90;i++)yield return null;
                baseline.Clear();destination.Clear();active.Clear();pool.ResetMetrics();int destinationFrames=0;
                for(int i=0;i<240;i++){yield return null;baseline.Add(Time.unscaledDeltaTime*1000);}
                float idleStart=MedianFps(baseline),idleEnd=idleStart;
                if(skill is LightningFlashRuntime){world.PlacePlayer(world.origin+Vector3.right*10);for(int i=0;i<90;i++){world.Look(235,14);yield return null;}for(int i=0;i<240;i++){world.Look(235,14);yield return null;destination.Add(Time.unscaledDeltaTime*1000);}idleEnd=MedianFps(destination);}
                for(int cast=0;cast<3;cast++)
                {
                    world.Arrange(skill is LightningFlashRuntime);if(skill is ChainLightningRuntime)world.ChainLayout();skill.ResetCooldownForValidation();spirit.Refill();yield return null;
                    if(!skill.CastAt(world.origin+Vector3.right*8))throw new InvalidOperationException("Performance cast failed: "+skill.Id);
                    float end=Time.realtimeSinceStartup+10;while(skill.IsCasting&&Time.realtimeSinceStartup<end){bool atEnd=skill is LightningFlashRuntime&&world.player.LastDashDistance>8;world.Look(atEnd?235:90,14);yield return null;active.Add(Time.unscaledDeltaTime*1000);if(atEnd)destinationFrames++;}
                    yield return new WaitForSeconds(3.2f);
                }
                if(destination.Count>0){float[] origin=baseline.ToArray();baseline.Clear();for(int i=0;i<active.Count-destinationFrames;i++)baseline.Add(origin[i%origin.Length]);for(int i=0;i<destinationFrames;i++)baseline.Add(destination[i%destination.Count]);}
                float meanBase=Mean(baseline),meanCast=Mean(active);baseline.Sort();active.Sort();float baseFps=1000/baseline[baseline.Count/2],castFps=1000/active[active.Count/2];float loss=(1-castFps/baseFps)*100;
                report.trials.Add(new Trial{skill=skill.Id,samples=active.Count,peakParticles=pool.PeakParticles,maxVfxFrameGCBytes=pool.MaxFrameGCBytes,idleStartFps=idleStart,idleDestinationFps=idleEnd,baselineFps=baseFps,castFps=castFps,lossPercent=loss,withinTenPercent=loss<=10&&pool.MaxFrameGCBytes==0,meanBaselineMs=meanBase,meanCastMs=meanCast,p95Ms=active[(int)(active.Count*.95f)]});
                Directory.CreateDirectory("Artifacts/Skills/fix3");File.WriteAllText(Output+".json",JsonUtility.ToJson(report,true));
            }
            var render=ReactionGpuBenchRender.Instance;report.renderedFrames=render!=null?render.RenderedFrames:0;report.error=render!=null?render.Error:"";
            if(!Application.isEditor)report.environment+=" D3D12 offscreen URP StandardRequest renders full HDR camera stack; GPU fence each frame; overlay HUD routed to camera; display Present excluded.";
            File.WriteAllText(Output+".json",JsonUtility.ToJson(report,true));
            world.End();bool pass=report.postProcessing&&report.bloom&&report.comicInk&&report.hdr&&report.error.Length==0&&(Application.isEditor||report.renderedFrames>0);foreach(var t in report.trials)pass&=t.withinTenPercent;File.WriteAllText(Output+"-DONE.txt",pass?"PASS":"FAIL");Destroy(gameObject);
        }
        static float Mean(List<float> values){float total=0;for(int i=0;i<values.Count;i++)total+=values[i];return total/values.Count;}
        static float MedianFps(List<float> values){float[] copy=values.ToArray();Array.Sort(copy);return 1000/copy[copy.Length/2];}
        void CaptureGraphics()
        {
            report.quality=QualitySettings.names[QualitySettings.GetQualityLevel()];
            var data=world.camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();report.postProcessing=data!=null&&data.renderPostProcessing;
            var stack=data!=null&&data.volumeStack!=null?data.volumeStack:UnityEngine.Rendering.VolumeManager.instance.stack;var bloom=stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();report.bloom=bloom!=null&&bloom.IsActive();report.bloomIntensity=bloom!=null?bloom.intensity.value:0;
            var asset=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if(asset!=null){report.pipeline=asset.name;report.hdr=asset.supportsHDR;foreach(var renderer in asset.rendererDataList)if(renderer!=null)foreach(var feature in renderer.rendererFeatures)if(feature is ComicInkFeature&&feature.isActive)report.comicInk=true;}
            report.comicInk&=ComicRendering.Enabled&&world.camera.gameObject.scene.name=="SampleScene";
        }
    }
#if UNITY_EDITOR
    public sealed class SkillSet1VisualBatch:MonoBehaviour
    {
        IEnumerator Start()
        {
            string[] ids={"van-kiem-quyet","than-kiem-ngu-loi","phat-no-hoa-lien","tich-lich-nhat-thiem"};
            foreach(string id in ids)
            {UIStateManager.Instance.EnterScene(true);Time.timeScale=1;var go=new GameObject("P10 final capture "+id);var capture=go.AddComponent<SkillVisualCapture>();capture.skillId=id;while(capture!=null)yield return null;}
            Directory.CreateDirectory("Artifacts/Skills/fix3");File.WriteAllText("Artifacts/Skills/fix3/SkillSet1-Visual-DONE.txt","DONE");Destroy(gameObject);
        }
        void Update(){if(UIStateManager.Instance.State!=UIState.Gameplay)UIStateManager.Instance.EnterScene(true);}
    }
#endif
}
#endif
