#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P12_BENCH)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.SkyBeast;
using CampusRift.Combat;
using CampusRift.UI;
using CampusRift.Progression;
namespace CampusRift.Validation
{
    public sealed class P12StandaloneBench : MonoBehaviour
    {
        [Serializable] public sealed class Report {public string capturedAt,environment,gpu,cpu,error="";public int enemies=25,dragons=1,samples,renderedFrames,runtimeErrors,minLiving=25,visibleSkins;public string[] counterNames;public float medianFPS,meanMs,p95Ms;public long triangles,drawCalls,batches,setPass;public bool passed,dragonVisible;}
        readonly Report report=new Report();readonly List<float> times=new List<float>();
        void RuntimeLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.runtimeErrors++;report.error=message;}}
        IEnumerator Start(){if(Application.isEditor)yield break;Application.logMessageReceived+=RuntimeLog;Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            for(int i=0;i<30;i++)yield return null;UIStateManager.Instance.EnterScene(true);var settings=SettingsManager.Instance.Current.Copy();settings.ControlMode=Controls.ControlMode.PC;settings.VSync=false;settings.SkyBrightness=1;settings.Quality=1;settings.ResolutionWidth=1920;settings.ResolutionHeight=1080;settings.Fullscreen=false;SettingsManager.Instance.Apply(settings,false);
            ProfileService.Instance.UseTransient(new ProfileData{cultivation=new CultivationData{realm=6,tier=5}});
            var player=FindAnyObjectByType<CampusExplorer>();player.spawnPosition=new Vector3(-5,.13f,2);player.ReturnToSpawn();player.SetValidationCameraOrbit(90,-35);var old=FindAnyObjectByType<Monsters.MonsterBrain>();if(old!=null)old.gameObject.SetActive(false);
            player.GetComponent<PlayerStats>().SetModifier(StatSource.Buff,"p12-bench-hp",StatType.MaxHealth,900000,0);player.GetComponent<Monsters.PlayerMonsterHealth>().Heal(1e6f);
            SettingsManager.Instance.Sky.SetPreset(SkyPreset.Dusk);var roster=LevelCatalog.Instance.Get(10).spawnTable.roster;var scale=LevelCatalog.Instance.Get(6).Scaling;
            for(int i=0;i<25;i++){float angle=(-45+i*90f/24)*Mathf.Deg2Rad;var e=EnemyPool.Ensure().Spawn(roster[i%7].archetype,player.transform.position+new Vector3(Mathf.Cos(angle)*(8+i%3*2),0,Mathf.Sin(angle)*(8+i%3*2)),scale);e.Vitality.SetMaxHealth(1000000,true);}
            var dragon=SkyBeastPresence.Spawn("023");dragon.definition=Instantiate(dragon.definition);dragon.definition.center=new Vector3(110,0,2);dragon.definition.orbitRadius=12;dragon.definition.altitude=140;
            var render=gameObject.AddComponent<ReactionGpuBenchRender>();render.Initialize(player.followCamera);
            for(int i=0;i<180;i++){player.SetValidationCameraOrbit(90,-35);yield return null;}
            render.Snapshot(Path.GetFullPath("Artifacts/P12/performance/PC-warmup-render.png"));
            var handles=new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(handles);var counters=new List<ProfilerRecorder>();var names=new List<string>();foreach(var h in handles){var desc=Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(h);if(desc.Category==ProfilerCategory.Render&&desc.Name.EndsWith("Draw Calls Count")){names.Add(desc.Name);counters.Add(ProfilerRecorder.StartNew(desc.Category,desc.Name,1));}}report.counterNames=names.ToArray();
            var tri=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);var batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);var passes=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count",1);
            float until=Time.realtimeSinceStartup+18;while(Time.realtimeSinceStartup<until){player.SetValidationCameraOrbit(90,-35);yield return null;times.Add(Time.unscaledDeltaTime*1000);report.triangles=Math.Max(report.triangles,tri.LastValue);long totalDraws=0;foreach(var counter in counters)totalDraws+=counter.LastValue;report.drawCalls=Math.Max(report.drawCalls,totalDraws);report.minLiving=Math.Min(report.minLiving,EnemyDirector.Instance.Active.Count);report.batches=Math.Max(report.batches,batches.LastValue);report.setPass=Math.Max(report.setPass,passes.LastValue);}
            foreach(var counter in counters)counter.Dispose();tri.Dispose();draws.Dispose();batches.Dispose();passes.Dispose();times.Sort();report.samples=times.Count;report.medianFPS=1000/times[times.Count/2];float sum=0;foreach(float t in times)sum+=t;report.meanMs=sum/times.Count;report.p95Ms=times[(int)(times.Count*.95f)];report.renderedFrames=render.RenderedFrames;if(render.Error.Length>0)report.error=render.Error;report.gpu=SystemInfo.graphicsDeviceName;report.cpu=SystemInfo.processorType;report.capturedAt=DateTime.UtcNow.ToString("o");
            report.environment="Windows standalone Mono release / D3D12 / full URP HDR1920x1080 and actual comic HUD converted to camera canvas / GPU fence per frame / PC quality1, VSync off / 25 live mixed-model enemies, level6 AI/stat scaling (no level7 self-fuse), full seven-model roster / real dragon, fixture orbit110,2 radius12 altitude140 to keep above the campus roof / camera distance3.4 FOV60 pitch-35 / no screenshot or MCP during sampling / display Present excluded / player and enemy HP buff only to prevent fixture deaths";
            var planes=GeometryUtility.CalculateFrustumPlanes(player.followCamera);foreach(var r in FindObjectsByType<SkinnedMeshRenderer>())if(r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds))report.visibleSkins++;
            foreach(var r in dragon.GetComponentsInChildren<SkinnedMeshRenderer>())if(r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds)&&CombatLine.Clear(player.followCamera.transform.position,r.bounds.center,player.transform,true))report.dragonVisible=true;
            report.passed=report.dragonVisible&&report.runtimeErrors==0&&report.triangles>100000&&report.drawCalls>0&&report.visibleSkins>=8&&report.minLiving>=25&&report.samples>120&&report.renderedFrames>report.samples&&report.error.Length==0&&report.medianFPS>=60;Directory.CreateDirectory("Artifacts/P12/performance");File.WriteAllText("Artifacts/P12/performance/PC.json",JsonUtility.ToJson(report,true));render.Snapshot(Path.GetFullPath("Artifacts/P12/performance/PC-render.png"));File.WriteAllText("Artifacts/P12/performance/DONE.txt",report.passed?"PASS":"FAIL");yield return new WaitForSecondsRealtime(.5f);Application.Quit();
        }
    }
}
#endif
