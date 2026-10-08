#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.Simulation;
using CampusRift.UI;
using CampusRift.Enemies;
using CampusRift.Skills;
namespace CampusRift.AR
{
    public sealed class ARNavigationPlayTest:MonoBehaviour
    {
        [Serializable]public sealed class Report{public List<string> passed=new List<string>(),failed=new List<string>();public float meanFps,p50Ms,p95Ms,renderScale;public int frames,minActors;public bool swordRainAccepted;public string environment="Unity Editor / Android target / XR Simulation / Mobile VFX. Not a device benchmark.";}
        Report report=new Report();string profile;GameSettings settings;UnityEngine.Rendering.RenderPipelineAsset previous;
        void Save(){Directory.CreateDirectory("task/ar/m7");File.WriteAllText("task/ar/m7/navigation.json",JsonUtility.ToJson(report,true));}
        void Check(bool value,string label){(value?report.passed:report.failed).Add(label);Save();}
        IEnumerator Wait(Func<bool> condition,float seconds=45){float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;if(!condition())throw new Exception("AR navigation wait timeout");}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;settings=SettingsManager.Instance.Current.Copy();var test=settings.Copy();test.TelemetryConsentAsked=true;test.LocalTelemetryEnabled=false;test.TextSize=0;SettingsManager.Instance.Apply(test,false);TutorialDirector.Suppress=true;
            profile=JsonUtility.ToJson(CampusRift.Progression.ProfileService.Instance.Data);previous=QualitySettings.renderPipeline;
            var run=Run();while(true){bool next;object current=null;try{next=run.MoveNext();if(next)current=run.Current;}catch(Exception e){Check(false,e.ToString());break;}if(!next)break;yield return current;}
            SettingsManager.Instance.Apply(settings,false);Save();File.WriteAllText("task/ar/m7/DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
        IEnumerator Run()
        {
            UIStateManager.Instance.EnterScene(false);UIStateManager.Instance.OpenHub();yield return new WaitForSecondsRealtime(1);
            for(int visit=0;visit<2;visit++)
            {
                var entry=FindAnyObjectByType<ARHubEntry>();Check(entry!=null&&entry.Supported&&entry.button.gameObject.activeInHierarchy,"Hub AR entry visible "+visit);entry.button.onClick.Invoke();
                yield return Wait(()=>FindAnyObjectByType<ARSessionBootstrap>()!=null);
                var consent=FindAnyObjectByType<ARSessionBootstrap>();yield return null;
                Check(XRGeneralSettings.Instance.Manager.activeLoader==null,"No loader before safety/camera consent "+visit);consent.Continue();consent.Continue();
                yield return Wait(()=>FindAnyObjectByType<ARXRLoaderControl>()?.Ready==true);
                Check(XRGeneralSettings.Instance.Manager.activeLoader!=null,"Loader active only inside AR "+visit);
                yield return null;var selection=FindAnyObjectByType<ARModeSelectionHUD>();selection.GetComponentsInChildren<Button>(true).First(b=>b.name=="Select training").onClick.Invoke();selection.GetComponentsInChildren<Button>(true).First(b=>b.name=="Start selected").onClick.Invoke();FindAnyObjectByType<ARBattleHUD>().SetHelp(false);yield return null;
                if(visit==0)
                {
                    var placement=FindAnyObjectByType<RiftPlacementService>();placement.SetFloor(false);placement.enabled=false;
                    yield return Wait(()=>FindAnyObjectByType<SimulationCameraPoseProvider>()!=null);var camera=FindAnyObjectByType<SimulationCameraPoseProvider>();
                    foreach(float yaw in new[]{155f,170,180,195,210,180}){camera.transform.position=new Vector3(.75f,1.5f,.1f);camera.transform.rotation=Quaternion.Euler(40,yaw,0);yield return new WaitForSecondsRealtime(.55f);}
                    // Aim at a real scanned polygon's incenter; fixed coordinates can hit a partial edge.
                    float scanUntil=Time.realtimeSinceStartup+40;bool centered=false;
                    while(!centered&&Time.realtimeSinceStartup<scanUntil)
                    {
                        float best=.21f;Vector3 point=Vector3.zero;
                        foreach(var plane in placement.planes.trackables){if(plane.alignment!=UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp||plane.trackingState!=UnityEngine.XR.ARSubsystems.TrackingState.Tracking||plane.subsumedBy!=null)continue;var polygon=plane.boundary.ToArray();if(ARPlaneScoring.Area(polygon)<placement.settings.MinimumArea)continue;float radius;var q=ARPlaneScoring.Incenter(polygon,out radius);if(radius>best){best=radius;point=plane.transform.TransformPoint(new Vector3(q.x,0,q.y));centered=true;}}
                        if(centered){camera.transform.position=point+new Vector3(0,.72f,.86f);camera.transform.LookAt(point);}else yield return new WaitForSecondsRealtime(.5f);
                    }
                    yield return null;placement.enabled=true;yield return Wait(()=>placement.ReticleValid);placement.Confirm();yield return Wait(()=>placement.Root!=null);yield return Wait(()=>placement.Adjusting);placement.StartBattlefield();var field=placement.GetComponent<ARBattlefield>();yield return Wait(()=>placement.GetComponent<ARMonsterDirector>().Actors.Count>0);
                    field.Shrine.SetProgressionMaxHealth(10000);field.Shrine.Revive(1,0);var director=field.GetComponent<ARMonsterDirector>();var archetype=director.Actors[0].archetype;director.enabled=false;
                    var actors=new List<EnemyInstance>();for(int i=0;i<6;i++){var e=EnemyPool.Ensure().SpawnAR(archetype,field.Root,field.Root.TransformPoint(new Vector3((i%3-1)*1.1f,0,(i/3-.5f)*1.2f)),field);e.Vitality.SetMaxHealth(10000,true);e.GetComponent<ARMinionBrain>().enabled=false;actors.Add(e);}
                    camera.transform.position=field.Root.position+new Vector3(.4f,.8f,1);camera.transform.LookAt(field.Root.position);yield return new WaitForSecondsRealtime(1);
                    var caster=field.GetComponent<ARSkillCaster>();var sword=caster.Caster.GetComponent<SwordRainRuntime>();report.swordRainAccepted=sword.CastAt(field.Root.position);
                    var samples=new List<float>();float begin=Time.realtimeSinceStartup,end=begin+2.5f;report.minActors=6;
                    while(Time.realtimeSinceStartup<end){yield return null;samples.Add(Time.unscaledDeltaTime*1000);report.minActors=Mathf.Min(report.minActors,actors.Count(x=>x!=null&&x.Alive));}
                    report.frames=samples.Count;report.meanFps=samples.Count/(Time.realtimeSinceStartup-begin);samples.Sort();report.p50Ms=samples[samples.Count/2];report.p95Ms=samples[Mathf.Min(samples.Count-1,(int)(samples.Count*.95f))];report.renderScale=FindAnyObjectByType<ARXRLoaderControl>().SessionPipeline.renderScale;
                    Check(report.swordRainAccepted&&report.minActors==6,"Single Editor sample: six AR monsters + Sword Rain");
                    field.UserPaused=true;yield return null;float clock=field.Clock;yield return new WaitForSecondsRealtime(.3f);Check(field.Clock==clock&&Time.timeScale==1,"HUD pause remains local");field.UserPaused=false;
                    int priorFps=Application.targetFrameRate;Application.targetFrameRate=20;yield return new WaitForSecondsRealtime(6);Application.targetFrameRate=priorFps;
                    Check(field.GetComponent<ARAdaptiveQuality>().Reduced&&Mathf.Approximately(FindAnyObjectByType<ARXRLoaderControl>().SessionPipeline.renderScale,.85f),"Slow frames reduce session render scale to .85");
                }
                ARSceneNavigation.Exit();yield return Wait(()=>SceneManager.GetActiveScene().name=="MainMenu"&&HubUI.Instance!=null&&HubUI.Instance.Visible);
                Check(XRGeneralSettings.Instance.Manager.activeLoader==null&&!XRGeneralSettings.Instance.Manager.isInitializationComplete,"XR deinitialized after exit "+visit);
                Check(QualitySettings.renderPipeline==previous&&!SkillVfxPool.ForceMobileQuality,"Pipeline and VFX policy restored "+visit);
                Check(profile==JsonUtility.ToJson(CampusRift.Progression.ProfileService.Instance.Data),"Profile/loadout unchanged after visit "+visit);
            }
            ScreenCapture.CaptureScreenshot("task/ar/screens/m7-hub.png");
        }
    }
}
#endif
