#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P10_BENCH)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using CampusRift.Skills;
using CampusRift.Levels;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.SkyBeast;
using CampusRift.Combat;
using UnityEngine.AI;
namespace CampusRift.UI
{
    public sealed class P23Performance:MonoBehaviour
    {
        [Serializable] public sealed class Sample {public string scenario;public int frames,enemies,visibleEnemies,particles,swords;public Vector3 camera,focus;public double seconds,fps;public float p95ms;}
        [Serializable] public sealed class Report {public string context,cpu,gpu,os,unity,quality,method="One 1-second warmup + 5-second unscaled frame sample per heavy scenario; no balance bot or repeated benchmark. Cinematic held at gather frame.";public int width,height;public List<Sample> samples=new List<Sample>();public string error;public int stressSpawned,stressArchetypes,stressElites;}
        PlayerMonsterHealth qaHealth;
        bool KeepAlive(DamageInfo hit){qaHealth.Revive(1,0);return true;}
        readonly Report report=new Report();SkillSet1TestWorld world;LevelDirector d;string root;int cap,sync;Vector3 focus;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot()
        {if(Application.isEditor||Array.IndexOf(Environment.GetCommandLineArgs(),"-p23-perf")<0)return;var g=new GameObject("P23 explicit development performance");DontDestroyOnLoad(g);g.AddComponent<P23Performance>();}
        IEnumerator Measure(string name)
        {
            yield return new WaitForSecondsRealtime(1);var times=new List<float>();double begin=Time.realtimeSinceStartupAsDouble;int frames=0;
            while(Time.realtimeSinceStartupAsDouble-begin<5){frames++;times.Add(Time.unscaledDeltaTime*1000);yield return null;}
            times.Sort();var f=FireBreathCycle.Instance?.GetComponent<FireBreathVisuals>();var sample=new Sample{scenario=name,frames=frames,seconds=Time.realtimeSinceStartupAsDouble-begin,enemies=EnemyPool.Instance.ActiveCount,visibleEnemies=VisibleEnemies(),camera=world.camera.transform.position,focus=focus,particles=f?.ParticleCount??0,swords=HeavenSwordCinematic.Active?.Swords.SwordCount??0,p95ms=times.Count>0?times[Mathf.Clamp(Mathf.CeilToInt(times.Count*.95f)-1,0,times.Count-1)]:0};sample.fps=frames/sample.seconds;report.samples.Add(sample);Write();
            yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(root,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        int VisibleEnemies()
        {
            var planes=GeometryUtility.CalculateFrustumPlanes(world.camera);int n=0;
            foreach(var e in EnemyDirector.Ensure().Active)
            {
                if(e==null||!e.Alive)continue;var renderer=e.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds));
                if(renderer!=null&&CombatLine.Clear(world.camera.transform.position,renderer.bounds.center,world.player.transform))n++;
            }
            return n;
        }
        Vector3 FindOpenCourt(Vector3 preferred)
        {
            for(int step=0;step<40;step++)
            {
                var candidate=preferred+new Vector3((step%5-2)*5,0,-(step/5)*5);
                if(!NavMesh.SamplePosition(candidate,out var hit,.5f,NavMesh.AllAreas)||ShelterDetector.AtFeet(hit.position)!=Shelter.Outdoor)continue;
                return hit.position;
            }
            throw new Exception("No clear outdoor stress court on existing NavMesh");
        }
        void FrameCourt(Vector3 target)
        {
            for(int i=0;i<16;i++)
            {
                var position=target+Quaternion.Euler(0,i*22.5f,0)*new Vector3(0,7,-12);
                if(!CombatLine.Clear(position,target+Vector3.up,world.player.transform))continue;
                world.camera.transform.position=position;world.camera.transform.LookAt(target+Vector3.up);world.camera.fieldOfView=58;return;
            }
            throw new Exception("No unoccluded stress camera");
        }
        void Write(){File.WriteAllText(Path.Combine(root,"performance.json"),JsonUtility.ToJson(report,true));}
        IEnumerator Start()
        {
            root=Application.isEditor?"task/p23/perf-editor":Path.GetFullPath(Path.Combine(Application.dataPath,"../P23-Perf"));Directory.CreateDirectory(root);cap=Application.targetFrameRate;sync=QualitySettings.vSyncCount;
            if(SceneManager.GetActiveScene().name!="SampleScene"){SceneManager.LoadScene("SampleScene");yield return null;yield return new WaitForSecondsRealtime(3);}
            var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool more=false;object next=null;try{more=e.MoveNext();if(more)next=e.Current;}catch(Exception ex){report.error=ex.ToString();break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
            if(qaHealth!=null)qaHealth.BeforeDefeat-=KeepAlive;HeavenSwordCinematic.Active?.Cancel();if(d!=null)d.End();if(world!=null&&world.player!=null)world.End();Application.targetFrameRate=cap;QualitySettings.vSyncCount=sync;Write();File.WriteAllText(Path.Combine(root,"DONE.txt"),report.error??"Completed "+report.samples.Count+" heavy samples");if(!Application.isEditor)Application.Quit();Destroy(gameObject);
        }
        IEnumerator Run()
        {
            Application.runInBackground=true;TutorialDirector.Suppress=true;SkillSet1TestWorld.RuntimeEnemyTemplate=LevelCatalog.Instance.Get(1).waves[0].entries[0].archetype;
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();yield return null;foreach(var v in world.victims)v.gameObject.SetActive(false);world.player.enabled=false;world.player.followCamera=null;world.player.GetComponent<PlayerMonsterHealth>().SetProgressionMaxHealth(100000);world.player.GetComponent<PlayerMonsterHealth>().Revive(1,600);qaHealth=world.player.GetComponent<PlayerMonsterHealth>();qaHealth.BeforeDefeat+=KeepAlive;
            var s=SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.TextSize=0;s.AccessibleColors=AccessiblePalette.Default;s.Subtitles=false;s.ResolutionWidth=1920;s.ResolutionHeight=1080;s.Fullscreen=false;s.VSync=false;SettingsManager.Instance.Apply(s,false);Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            report.context=Application.isEditor?"Editor":"Windows development player";report.cpu=SystemInfo.processorType;report.gpu=SystemInfo.graphicsDeviceName;report.os=SystemInfo.operatingSystem;report.unity=Application.unityVersion;report.quality=QualitySettings.names[QualitySettings.GetQualityLevel()];report.width=Screen.width;report.height=Screen.height;
            d=LevelDirector.Ensure();d.introSeconds=.01f;d.spawnInterval=.005f;d.portalLead=0;
            var level=LevelCatalog.Instance.Get(10);d.Begin(level);d.enabled=false;
            focus=FindOpenCourt(world.origin+Vector3.right*10);world.PlacePlayer(world.origin);
            var roster=level.spawnTable.roster.Select(r=>r.archetype).Where(a=>a!=null).Distinct().ToArray();
            var types=new HashSet<EnemyArchetype>();
            for(int i=0;i<30;i++)
            {
                var point=focus+new Vector3((i%6-2.5f)*1.1f,0,(i/6-2)*1.2f);
                var enemy=EnemyPool.Ensure().Spawn(roster[i%roster.Length],point,level.Scaling,false);
                if(enemy==null)throw new Exception("Stress spawn failed at "+point);
                types.Add(enemy.archetype);report.stressSpawned++;
                if(i<2){enemy.Elite.Configure(10,2304+i,i==0?EliteAffixKind.FireHeart:EliteAffixKind.Guardian);report.stressElites++;}
            }
            report.stressArchetypes=types.Count;FrameCourt(focus);
            yield return null;if(VisibleEnemies()<20)throw new Exception("Crowd view is occluded: "+VisibleEnemies()+" visible");
            report.method="Each scenario: one 1s warmup + 5s unscaled sample, no balance bot. Crowd is a controlled 30-monster stress fixture (above normal cap14), full current roster and 2 elites on existing outdoor NavMesh; AI active; player kept alive by a fixture-only BeforeDefeat revival because Begin resets progression health. Fire uses real VFX, no manual damage ticks. Cinematic held at gather.";
            yield return Measure("level10-crowd");d.End();
            d.Begin(LevelCatalog.Instance.Get(8));d.enabled=false;world.PlacePlayer(focus);FrameCourt(focus);
            var fire=FireBreathCycle.Ensure();fire.AutoAdvance=false;fire.RestartWarning();fire.Advance(fire.PhaseDuration+.01f);yield return Measure("fire-breath");fire.StopCycle();
            d.End();d.Begin(LevelCatalog.Instance.Get(8));d.enabled=true;float deadline=Time.realtimeSinceStartup+30;while(!d.AwaitingSkySword&&Time.realtimeSinceStartup<deadline){foreach(var e in new List<EnemyInstance>(d.Alive))if(e.Alive)e.Vitality.ApplyDamage(Combat.DamageInfo.Create(100000,Combat.Element.None,Combat.DamageSource.Skill,e.transform.position,Vector3.up,world.player.gameObject));yield return null;}d.enabled=false;
            var ultimate=HeavenSwordUltimate.Instance;if(!ultimate.TryChannel())throw new Exception("Heavy cinematic fixture could not channel");deadline=Time.realtimeSinceStartup+10;while(HeavenSwordCinematic.Active==null&&Time.realtimeSinceStartup<deadline)yield return null;var cine=HeavenSwordCinematic.Active;if(cine==null)throw new Exception("No active Heaven Sword cinematic");
            cine.HoldForCapture=true;typeof(HeavenSwordCinematic).GetField("age",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(cine,cine.Duration*.4f);yield return Measure("heaven-sword");cine.Cancel();
        }
    }
}
#endif
