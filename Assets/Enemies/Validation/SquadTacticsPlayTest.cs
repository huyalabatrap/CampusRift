#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.SkyBeast;
using CampusRift.Combat;

namespace CampusRift.Validation
{
    // One paired run on the unmodified campus NavMesh. Old Director/ranged/hearing movement is
    // retained behind Squad.Enabled=false; this is an executable baseline, not a simulated line.
    public sealed class SquadTacticsPlayTest:P12PlayTest
    {
        [Serializable] public sealed class Metric
        {
            public string scenario,mode;
            public Vector3 start,finish;
            public float sharedPairs,meanRouteOverlap,angularCoverage,plannedCoverage,escapeClearFraction,peakCoverage;
            public int closePhases,landingGuards;
            public int samples,interceptions,pathBudget,actors,routeChoices,blockedDoors,availableDoors;
            public bool completeEscape;
            public List<string> roles=new List<string>();
            public List<string> formationSamples=new List<string>();
        }
        [Serializable] sealed class Results
        {
            public string method="One before/after run, identical real prefabs/start positions/player route, tier3 (Warning tier4), 8 seconds per case. Shared pair = >=4m of 2m route cells; overlap = pair Jaccard; actual coverage=360-largest angular gap. Trajectories and current NavMesh paths are sampled, no native/device/balance claim.";
            public List<Metric> scenarios=new List<Metric>();
        }
        readonly Results results=new Results();
        readonly List<EnemyInstance> actors=new List<EnemyInstance>();
        readonly List<GameObject> debugObjects=new List<GameObject>();
        readonly List<List<Vector3>> trails=new List<List<Vector3>>();
        NavMeshPath path;
        readonly Vector3[] buffer=new Vector3[128];
        readonly System.Reflection.FieldInfo velocity=typeof(CampusExplorer).GetField("planarVelocity",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        EnemyDirector director;FireBreathCycle fire;
        bool oldOrtho;float oldSize;bool oldPlayerEnabled;string title;
        Material lineMaterial;
        public bool PicturesOnly;
        void SaveResults(){if(PicturesOnly)return;Directory.CreateDirectory("Artifacts/AI");File.WriteAllText("Artifacts/AI/Squad-fix1.json",JsonUtility.ToJson(results,true));}
        Vector3 OnMesh(Vector3 p){return NavMesh.SamplePosition(p,out var hit,3,NavMesh.AllAreas)?hit.position:p;}
        protected override IEnumerator Run()
        {
            Begin();path=new NavMeshPath();director=EnemyDirector.Ensure();oldPlayerEnabled=world.player.enabled;world.player.enabled=false;
            oldOrtho=world.camera.orthographic;oldSize=world.camera.orthographicSize;
            var hp=world.player.GetComponent<PlayerMonsterHealth>();hp.SetProgressionMaxHealth(90000);hp.Revive(1,0);hp.respawnOnDefeat=false;
            lineMaterial=Resources.Load<Material>("EnemyVfx/P12Telegraph");
            Directory.CreateDirectory("task/ai/screens/fix1");
            Vector3[] starts={new Vector3(-5,0,2),new Vector3(-27,0,16),new Vector3(-26.2f,4,17.6f),new Vector3(-5,0,2)};
            Vector3[] finishes={new Vector3(12,0,2),new Vector3(-26,0,25),new Vector3(-26.2f,8,17.6f),new Vector3(3,0,2)};
            string[] names={"yard","corridor-two-entries","stairs","outdoor-warning"};
            for(int scenario=0;scenario<4;scenario++)
            {
                if(PicturesOnly&&scenario!=2)continue;
                for(int mode=0;mode<2;mode++)yield return Case(names[scenario],OnMesh(starts[scenario]),OnMesh(finishes[scenario]),mode==1,scenario==3,scenario==2);
                if(PicturesOnly)continue;
                var before=results.scenarios[results.scenarios.Count-2];var after=results.scenarios[results.scenarios.Count-1];
                Check(after.actors==8&&after.samples>5,names[scenario]+" actual 8-actor chase sampled");
                Check(after.pathBudget<=3,names[scenario]+" explicit path budget <=3/frame");
                Check(after.escapeClearFraction==1,names[scenario]+" reachable escape in 100% samples");
                float[] v1Overlap={.05122279f,.10825543f,.27627239f,.05413043f};
                Check(after.meanRouteOverlap<=v1Overlap[scenario],names[scenario]+" Jaccard no worse than shipped v1");
                if(scenario==0||scenario==3)Check(after.angularCoverage>=240,names[scenario]+" actual average angular coverage >=240 degrees");
                if(scenario==1)Check(after.angularCoverage>before.angularCoverage+30,"Corridor actual average angular coverage grows by >30 degrees");
                if(scenario!=2)Check(after.interceptions>=2,names[scenario]+" at least two actual forward interceptions");
                else Check(after.landingGuards>0,"Stairs has a guard ahead at the upper landing");
                if(scenario==3)Check(after.blockedDoors<=EnemyTactics.BlockLimit(after.availableDoors)&&after.availableDoors>after.blockedDoors,"Warning retains P16 free entrance / blocker cap");
            }
            if(PicturesOnly)yield break;
            Check(results.scenarios.Exists(x=>x.mode=="after"&&x.interceptions>0),"At least one actual interception arrives ahead of the moving player");
            director.Squad.Enabled=true;SaveResults();File.WriteAllText("Artifacts/AI/Squad-fix1-DONE.txt",report.passed.Count+" PASS / "+report.failed.Count+" FAIL");
        }
        IEnumerator Case(string name,Vector3 start,Vector3 finish,bool after,bool warning,bool stairs)
        {
            ClearDebug();EnemyPool.Ensure().ReleaseAll();director.ResetCounters();director.Squad.Enabled=after;
            fire=FireBreathCycle.Instance;if(fire!=null)fire.StopCycle();
            world.PlacePlayer(start);Vector3 heading=Vector3.ProjectOnPlane(finish-start,Vector3.up).normalized;
            if(heading.sqrMagnitude<.1f)heading=Vector3.forward;
            Vector3 side=Vector3.Cross(Vector3.up,heading);
            velocity.SetValue(world.player,heading*2.4f);world.player.transform.forward=heading;
            actors.Clear();trails.Clear();
            var goblin=UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");
            for(int i=0;i<8;i++)
            {
                Vector3 spawn=stairs?new Vector3(-26.2f,0,17.6f)+side*(i%4-1.5f)*1.2f-heading*(i/4)*2:
                    start-heading*(9+i/4*2)+side*(i%4-1.5f)*1.2f;
                var e=EnemyPool.Instance.Spawn(goblin,OnMesh(spawn),new EnemyScaling{health=100,damage=1,speed=1,aiTier=warning?4:3,level=3},false);
                if(e!=null){actors.Add(e);trails.Add(new List<Vector3>());}
            }
            float ready=Time.time+4;while(Time.time<ready&&actors.Exists(e=>e.Brain.State==MinionState.Spawn))yield return null;
            if(warning){fire=FireBreathCycle.Ensure();fire.StartDev(8);fire.AutoAdvance=false;fire.GetComponent<FireBreathVisuals>().enabled=false;director.Tactics.PlanWarning();}
            // Real connected player route, including vertical stair links; do not teleport across walls.
            bool playerRoute=NavMesh.CalculatePath(start,finish,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
            Check(playerRoute,name+" player route exists ("+(after?"after":"before")+")");
            Vector3[] route=path.corners;
            float length=0;for(int i=1;i<route.Length;i++)length+=Vector3.Distance(route[i-1],route[i]);
            var metric=new Metric{scenario=name,mode=after?"after":"before",start=start,finish=finish,actors=actors.Count};
            float begin=Time.time,nextSample=begin+.3f;int clear=0;var interceptions=new HashSet<EnemyInstance>();
            while(Time.time-begin<8)
            {
                float t=Time.time-begin;Vector3 p=Along(route,Mathf.Min(length,t*2.4f));
                var cc=world.player.GetComponent<CharacterController>();cc.enabled=false;world.player.transform.position=p;cc.enabled=true;
                world.player.transform.forward=heading;velocity.SetValue(world.player,t*2.4f<length?heading*2.4f:Vector3.zero);
                if(Time.time>=nextSample)
                {
                    nextSample=Time.time+.35f;metric.samples++;
                    MeasurePaths(metric);float coverage=Coverage(false);metric.angularCoverage+=coverage;metric.peakCoverage=Mathf.Max(metric.peakCoverage,coverage);metric.plannedCoverage+=Coverage(true);
                    if(after)
                    {
                        string row=t.ToString("F2")+"s cov="+coverage.ToString("F0")+" "+director.Squad.Phase;
                        foreach(var order in director.Squad.Members)if(order.role==SquadRole.Interceptor)
                            row+=" | "+order.enemy.name+" at="+order.enemy.transform.position+" goal="+order.target+" v="+order.enemy.Motor.Velocity.magnitude.ToString("F1")+" speed="+order.enemy.Motor.Agent.speed.ToString("F1")+" remaining="+order.enemy.Motor.RemainingDistance.ToString("F1");
                        metric.formationSamples.Add(row);
                    }
                    bool escape=after?director.Squad.HasEscape:AnyEscape(p);
                    if(escape)clear++;metric.completeEscape|=escape;
                    for(int i=0;i<actors.Count;i++)
                    {
                        trails[i].Add(actors[i].transform.position);
                        if(after&&director.Squad.TryInspect(actors[i],out var a)&&a.role==SquadRole.Interceptor&&
                            Vector3.Distance(actors[i].transform.position,a.target)<2.2f&&Vector3.Dot(Vector3.ProjectOnPlane(a.target-p,Vector3.up),heading)>2)interceptions.Add(actors[i]);
                    }
                }
                yield return null;
            }
            metric.sharedPairs/=Mathf.Max(1,metric.samples);metric.meanRouteOverlap/=Mathf.Max(1,metric.samples);
            metric.angularCoverage/=Mathf.Max(1,metric.samples);metric.plannedCoverage/=Mathf.Max(1,metric.samples);
            metric.escapeClearFraction=(float)clear/Mathf.Max(1,metric.samples);metric.interceptions=interceptions.Count;
            metric.pathBudget=director.Squad.MaxPathsPerFrame;
            metric.closePhases=director.Squad.CloseCount;
            if(stairs&&after)foreach(var e in actors)
                if(director.Squad.TryInspect(e,out var guard)&&guard.guarding&&e.transform.position.y>=finish.y-1.5f&&Vector3.Distance(e.transform.position,finish)<12)metric.landingGuards++;
            foreach(var a in director.Squad.Members)if(a.ready){metric.roles.Add(a.enemy.name+": "+a.role+" -> "+a.target);if(a.via)metric.routeChoices++;}
            if(warning){metric.availableDoors=director.Tactics.Entrances.Count;metric.blockedDoors=director.Tactics.BlockedCount;}
            results.scenarios.Add(metric);SaveResults();
            title=name+" / "+metric.mode+" | colored: actual NavMesh routes | green: reserved escape";
            DrawRoutes(after,world.player.transform.position.y+1.8f);
            world.camera.orthographic=true;world.camera.orthographicSize=22;
            Vector3 cameraPoint=(start+finish)*.5f;cameraPoint.y=world.player.transform.position.y+3;
            world.camera.transform.position=cameraPoint;world.camera.transform.rotation=Quaternion.Euler(90,0,0);
            // Camera is below the next ceiling for a runtime cutaway; no scene/renderers are edited.
            yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("task/ai/screens/fix1/"+name+"-"+metric.mode+".png",frame.EncodeToPNG());Destroy(frame);
            if(after&&name=="yard")
            {
                ClearDebug();world.camera.orthographic=false;world.camera.fieldOfView=100;
                world.camera.transform.position=world.player.transform.position-heading*1.8f+Vector3.Cross(Vector3.up,heading)*.7f+Vector3.up*2.5f;
                world.camera.transform.LookAt(world.player.transform.position+heading*6+Vector3.up*1.3f);
                yield return new WaitForEndOfFrame();frame=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes("task/ai/screens/fix1/player-view.png",frame.EncodeToPNG());Destroy(frame);
            }
            ClearDebug();if(fire!=null)fire.StopCycle();EnemyPool.Instance.ReleaseAll();yield return null;
        }
        static Vector3 Along(Vector3[] route,float distance)
        {if(route.Length==0)return Vector3.zero;for(int i=1;i<route.Length;i++){float len=Vector3.Distance(route[i-1],route[i]);if(distance<=len)return Vector3.Lerp(route[i-1],route[i],distance/Mathf.Max(.001f,len));distance-=len;}return route[route.Length-1];}
        bool AnyEscape(Vector3 point)
        {for(int i=0;i<12;i++){Vector3 want=OnMesh(point+Quaternion.Euler(0,i*30,0)*Vector3.forward*7);if(NavMesh.CalculatePath(point,want,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)return true;}return false;}
        float Coverage(bool planned)
        {
            var angles=new List<float>();foreach(var e in actors)
            {
                Vector3 p=e.transform.position;if(planned&&director.Squad.TryGet(e,out var a))p=a.target;
                Vector3 d=p-world.player.transform.position;if(new Vector2(d.x,d.z).sqrMagnitude<.1f)continue;
                angles.Add(Mathf.Repeat(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,360));
            }
            angles.Sort();float gap=360;if(angles.Count>1){gap=0;for(int i=0;i<angles.Count;i++)gap=Mathf.Max(gap,Mathf.Repeat(angles[(i+1)%angles.Count]-angles[i],360));}return 360-gap;
        }
        void MeasurePaths(Metric metric)
        {
            var cells=new List<HashSet<Vector3Int>>();
            foreach(var e in actors)
            {
                var set=new HashSet<Vector3Int>();if(e.Motor.OnMesh&&e.Motor.Agent.hasPath)
                {
                    int n=e.Motor.Agent.path.GetCornersNonAlloc(buffer);
                    for(int i=1;i<n;i++){int steps=Mathf.CeilToInt(Vector3.Distance(buffer[i-1],buffer[i])/1);for(int j=0;j<=steps;j++){var p=Vector3.Lerp(buffer[i-1],buffer[i],(float)j/Mathf.Max(1,steps));set.Add(new Vector3Int(Mathf.FloorToInt(p.x/2),Mathf.FloorToInt(p.y/2),Mathf.FloorToInt(p.z/2)));}}
                }
                cells.Add(set);
            }
            int pairs=0,shared=0;float overlap=0;
            for(int i=0;i<cells.Count;i++)for(int j=i+1;j<cells.Count;j++)
            {
                if(cells[i].Count==0||cells[j].Count==0)continue;
                int common=0;foreach(var c in cells[i])if(cells[j].Contains(c))common++;
                pairs++;if(common>=2)shared++;overlap+=(float)common/Mathf.Max(1,cells[i].Count+cells[j].Count-common);
            }
            metric.sharedPairs+=(float)shared/Mathf.Max(1,pairs);metric.meanRouteOverlap+=overlap/Mathf.Max(1,pairs);
        }
        void DrawRoutes(bool after,float plane)
        {
            for(int i=0;i<actors.Count;i++)
            {
                var e=actors[i];Color color=Color.HSVToRGB((float)i/actors.Count,.85f,1);
                if(e.Motor.OnMesh&&e.Motor.Agent.hasPath)Line("path "+i,e.Motor.Agent.path.corners,color,plane,.15f);
                Line("trail "+i,trails[i].ToArray(),new Color(color.r,color.g,color.b,.5f),plane,.055f);
                Vector3 p=e.transform.position;Line("actor "+i,new[]{p+Vector3.left*.4f,p+Vector3.right*.4f},color,plane,.6f);
            }
            if(after&&director.Squad.HasEscape){NavMesh.CalculatePath(world.player.transform.position,director.Squad.EscapePoint,NavMesh.AllAreas,path);Line("escape",path.corners,Color.green,plane,.27f);}
        }
        void Line(string name,Vector3[] points,Color color,float plane,float width)
        {
            if(points.Length<2)return;var go=new GameObject("Squad fixture "+name);debugObjects.Add(go);
            var l=go.AddComponent<LineRenderer>();l.sharedMaterial=lineMaterial;l.widthMultiplier=width;l.startColor=l.endColor=color;
            l.positionCount=points.Length;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;
            for(int i=0;i<points.Length;i++){var p=points[i];p.y=plane;l.SetPosition(i,p);}
        }
        void ClearDebug(){foreach(var go in debugObjects)if(go!=null)Destroy(go);debugObjects.Clear();}
        void OnGUI(){if(!string.IsNullOrEmpty(title)){GUI.Box(new Rect(15,15,900,36),title);}}
        protected override void Cleanup()
        {
            ClearDebug();if(director!=null)director.Squad.Enabled=true;if(fire!=null)fire.StopCycle();
            velocity.SetValue(world.player,Vector3.zero);world.player.enabled=oldPlayerEnabled;world.camera.orthographic=oldOrtho;world.camera.orthographicSize=oldSize;base.Cleanup();
        }
    }
}
#endif
