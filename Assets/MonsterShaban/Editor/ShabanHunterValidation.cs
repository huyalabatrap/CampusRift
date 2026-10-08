using System;
using System.Collections.Generic;
using System.IO;
using CampusRift.Monsters;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ShabanHunterValidation
{
    [Serializable] public sealed class Report { public string phase; public List<string> passed=new List<string>(); public List<string> failed=new List<string>(); }
    static Report report;
    static void Check(bool condition,string label){(condition?report.passed:report.failed).Add(label);}
    static string Save()
    {
        Directory.CreateDirectory("Assets/MonsterShaban/Validation/Hunter");
        File.WriteAllText("Assets/MonsterShaban/Validation/Hunter/"+report.phase+".json",JsonUtility.ToJson(report,true));
        return report.phase+": "+report.passed.Count+" pass / "+report.failed.Count+" fail "+string.Join("; ",report.failed);
    }
    public static string Memory()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Validate in Play Mode.");
        report=new Report{phase="01-Memory"};
        var brain=Object.FindAnyObjectByType<MonsterBrain>();brain.enabled=false;brain.GetComponent<MonsterPerception>().enabled=false;
        var memory=brain.GetComponent<MonsterMemory>();memory.Forget();float now=Time.time;
        for(int i=0;i<12;i++)memory.ObserveVisual(new Vector3(i,0,0),Vector3.right*6,now-2+i*.15f);
        Check(memory.RecentObservations.Count==brain.config.ObservationCapacity,"Bounded observation history retains recent evidence");
        Check(memory.LastSeenPosition.x==11,"Latest visual position survives history eviction");
        float timestamp=memory.LatestTime;Vector3 point=memory.LatestPosition;
        memory.RememberInference(new Vector3(200,30,100),MonsterEvidenceType.Prediction,.7f);
        Check(memory.LatestTime==timestamp && memory.LatestPosition==point,"Prediction cannot refresh real evidence or move its position");
        var record=new MonsterMemoryRecord{Timestamp=now-3,Confidence=1};
        Check(record.ConfidenceAt(now,16)<1 && record.ConfidenceAt(now+5,16)<record.ConfidenceAt(now,16),"Confidence decays exponentially");
        Check(record.ConfidenceAt(now+20,16)==0,"Old evidence expires");
        memory.RememberSound(new Vector3(0,0,5),.3f,now-.4f);memory.RememberSound(new Vector3(1,0,5),.7f,now-.2f);
        Check(memory.SoundMovementDirection.x>.9f,"Sequential sound evidence estimates a direction");
        memory.RememberSound(Vector3.one*500,.8f,now-3);
        Check(memory.LastHeardPosition.x==1,"Older sounds cannot replace newer evidence");
        memory.Forget();Check(!memory.HasEvidence && memory.RecentObservations.Count==0,"Forget clears encounter observations");
        return Save();
    }
    public static string Motion()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Validate in Play Mode.");
        report=new Report{phase="02-Motion"};
        var config=Object.FindAnyObjectByType<MonsterBrain>().config;var trail=new PlayerTrailHistory();
        for(int i=0;i<20;i++)trail.Observe(Vector3.right*i*.75f,i*.125f,config);
        Check(Mathf.Abs(trail.PlayerSpeed-6)<.1f,"Position samples estimate speed without Rigidbody");
        Check(trail.Samples.Count==config.TrailCapacity,"Trail is bounded");
        Check(trail.Coherence>.98f && trail.Trend==MovementTrend.Straight,"Straight trajectory has high coherence");
        trail.Observe(new Vector3(14.25f,0,.75f),2.5f,config);
        Check(trail.TurnRate>90 && trail.Acceleration.magnitude>1,"Sudden turn produces turn rate and acceleration");
        trail.Observe(new Vector3(40,0,40),4,config);
        Check(trail.SegmentSamples==1 && trail.PlayerSpeed==0,"Reacquisition cannot infer unseen travel velocity");
        trail.Observe(Vector3.one*500,4.125f,config);
        Check(trail.SegmentSamples==1 && trail.PlayerSpeed==0,"Respawn/teleport discontinuity is rejected");
        trail.Clear();for(int i=0;i<8;i++)trail.Observe(new Vector3(i, i*.25f,0),i*.125f,config);
        Check(trail.Trend==MovementTrend.ChangingFloor,"Observed vertical trajectory marks floor transition");
        return Save();
    }
    public static string Interception()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Validate in Play Mode.");
        report=new Report{phase="06-Interception"};float p,m;
        Check(InterceptionPlanner.ArrivesFirst(30,10,8,6.2f,.45f,out p,out m),"A real shortcut can beat a faster player to the exit");
        Check(!InterceptionPlanner.ArrivesFirst(15,10,15,6.2f,.45f,out p,out m),"Chasing behind the faster player is not a valid intercept");
        Check(!InterceptionPlanner.ArrivesFirst(15,10,8,6.2f,.45f,out p,out m),"Safety margin rejects a near tie");
        var brain=Object.FindAnyObjectByType<MonsterBrain>();var planner=brain.GetComponent<InterceptionPlanner>();
        Check(planner!=null,"Existing monster owns the planner");
        float distance;Check(planner.RouteDistance(new Vector3(8,.1f,-4),new Vector3(12,.1f,-4),out distance) && distance>=3.5f,"Player and monster ETA use an actual NavMesh route");
        var list=new List<DestinationCandidate>();
        DestinationPredictor.Build(brain.GetComponent<MonsterNavigation>().roomGraph,new Vector3(8,.1f,-4),Vector3.forward*10,.8f,brain.config,brain.GetComponent<MonsterPerception>(),list);
        bool forward=true;foreach(var c in list)if(c.Position.z<=-4)forward=false;
        Check(list.Count>0 && list.Count<=brain.config.DestinationCandidateCount && forward,"Destination hypotheses are bounded and lie ahead of the observed heading");
        return Save();
    }
    public static string Graph()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Validate in Play Mode.");
        report=new Report{phase="05-Graph"};
        var graph=ScriptableObject.CreateInstance<RoomGraph>();
        graph.Nodes=new[]{new RoomNode{WorldPosition=Vector3.zero,Neighbors=new[]{new RoomConnection{Neighbor=1,Cost=3,Distance=2},new RoomConnection{Neighbor=2,Cost=2,Distance=2}}},
            new RoomNode{WorldPosition=Vector3.right,Neighbors=new[]{new RoomConnection{Neighbor=3,Cost=3,Distance=2}}},
            new RoomNode{WorldPosition=Vector3.forward,Neighbors=new[]{new RoomConnection{Neighbor=3,Cost=2,Distance=2}}},new RoomNode{WorldPosition=Vector3.one}};
        var route=new List<int>();float length;
        Check(RoomPathfinder.FindNodes(graph,0,3,route,out length) && route.Count==3 && route[1]==2,"A star prefers lower-cost connection sequence");
        Check(Mathf.Abs(length-4)<.01f,"ETA length uses metres rather than weighted navigation cost");
        Check(RoomPathfinder.FindNodes(graph,0,3,route,out length,0,2) && route[1]==1,"A blocked connection can use an alternate route");
        Object.Destroy(graph);
        var campus=Object.FindAnyObjectByType<MonsterNavigation>().roomGraph;int stairs=0,doors=0,invalid=0;
        foreach(var node in campus.Nodes)foreach(var edge in node.Neighbors)
        {if(edge.ConnectionType==RoomConnectionType.Stair)stairs++;if(edge.ConnectionType==RoomConnectionType.Door)doors++;if(edge.Neighbor<0 || edge.Neighbor>=campus.Nodes.Length || edge.Cost<edge.Distance-.01f)invalid++;}
        Check(stairs>0 && doors>0,"Campus graph has stair and door connections");
        Check(invalid==0,"Campus connections have valid indices and admissible costs");
        return Save();
    }
    public static string Search()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Validate in Play Mode.");
        report=new Report{phase="04-ProbabilitySearch"};
        var brain=Object.FindAnyObjectByType<MonsterBrain>();brain.enabled=false;brain.GetComponent<MonsterPerception>().enabled=false;
        var memory=brain.GetComponent<MonsterMemory>();memory.Forget();
        var search=brain.GetComponent<MonsterSearch>();var original=search.landmarks;
        var origin=brain.transform.position;
        search.landmarks=new[]{new SearchLandmark{RoomID="East room",Position=origin+Vector3.right*4,Kind="Door"},
            new SearchLandmark{RoomID="West room",Position=origin+Vector3.left*4,Kind="Door"},
            new SearchLandmark{RoomID="Stair upper",Position=origin+new Vector3(6,3,0),Kind="Stair"}};
        search.Begin(origin,new Vector3(6,1,0));
        SearchCandidate east=null,west=null,stairs=null;
        foreach(var c in search.Candidates){if(c.Room=="East room")east=c;if(c.Room=="West room")west=c;if(c.Room=="Stair upper")stairs=c;}
        Check(search.Target==origin,"Search first visits last known evidence");
        Check(east!=null && west!=null && east.Probability>west.Probability,"Heading weights the forward room above the room behind");
        Check(stairs!=null && stairs.ExpansionLevel>=3,"Search includes connected upper-floor hypotheses");
        float prior=east!=null?east.Probability:0;search.RegisterNegativeEvidence(east);
        Check(east!=null && east.Visited && east.Probability<=prior*brain.config.VisitedPenalty+.001f,"An inspected empty room receives negative evidence");
        Check(!memory.HasEvidence,"An empty search result cannot create player evidence");
        search.Begin(origin,Vector3.right*6);float repeated=0;foreach(var c in search.Candidates)if(c.Room=="East room")repeated=c.Probability;
        Check(repeated<prior,"Rebuilding a search retains recent visited penalties");
        Check(search.Candidates.Count<=brain.config.MaxSearchCandidates,"Candidate count stays bounded");
        search.landmarks=original;return Save();
    }
    static float MassWithin(MonsterBelief belief,Vector3 point,float radius,float vertical=1.6f)=>belief.MassNear(point,radius,vertical);

    public static string Belief()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Validate in Play Mode.");
        report=new Report{phase="07-Belief"};
        var brain=Object.FindAnyObjectByType<MonsterBrain>();brain.enabled=false;
        var perception=brain.GetComponent<MonsterPerception>();var belief=brain.GetComponent<MonsterBelief>();
        var nav=brain.GetComponent<MonsterNavigation>();var player=Object.FindAnyObjectByType<CampusRift.CampusExplorer>();
        belief.enabled=false;perception.enabled=false;nav.Stop();
        Check(belief.EnsureGraph() && belief.Graph.Count>=nav.roomGraph.Nodes.Length,"Belief runs on the shared campus topology");
        Vector3 origin=brain.transform.position;
        // Player far away and hidden: nothing below may depend on where it really is.
        var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=origin+new Vector3(-45,0,40);controller.enabled=true;
        belief.ObserveSighting(origin+brain.transform.forward*6,brain.transform.right*7,Time.time);
        var mass=new float[belief.Graph.Count];belief.AccumulateNodeMass(mass);
        float top=0;foreach(var m in mass)top=Mathf.Max(top,m);
        Check(belief.Active && top>0.99f,"A sighting collapses the belief onto the observed place");
        Vector3 start=origin+brain.transform.forward*6;
        for(int i=0;i<20;i++)belief.Step(0.2f);
        belief.AccumulateNodeMass(mass);int spread=0;foreach(var m in mass)if(m>0.002f)spread++;
        Vector3 mean=Vector3.zero;foreach(var p in belief.Particles)mean+=(belief.PositionOf(p)-start)*p.Weight;
        Check(spread>=5,"Unobserved hypotheses spread over reachable places ("+spread+" nodes)");
        Check(Vector3.Dot(Vector3.ProjectOnPlane(mean,Vector3.up),brain.transform.right)>0,"Hypotheses keep moving along the observed heading");
        Check(MassWithin(belief,player.transform.position,5f,3f)<0.02f,"Belief ignores the hidden player's true position");
        // Negative evidence: hypotheses in plain view of the monster are ruled out.
        belief.ObserveSighting(start,Vector3.zero,Time.time);
        perception.enabled=true;
        for(int i=0;i<3;i++)belief.Step(0.05f);
        Check(MassWithin(belief,start,2.5f)<0.25f,"Places in plain view without the player are ruled out");
        perception.enabled=false;
        // Hearing re-anchors the belief.
        Vector3 sound=start+brain.transform.forward*14;
        UnityEngine.AI.NavMeshHit hit;if(UnityEngine.AI.NavMesh.SamplePosition(sound,out hit,3,UnityEngine.AI.NavMesh.AllAreas))sound=hit.position;
        float before=MassWithin(belief,sound,8f);
        belief.ObserveSound(sound,.8f,Time.time,Vector3.zero);
        Check(MassWithin(belief,sound,8f)>Mathf.Max(0.25f,before),"A heard footstep pulls the belief to the sound");
        // Contradicting everything reseeds a consistent belief instead of breaking it.
        var particles=belief.Particles;for(int i=0;i<particles.Length;i++)particles[i].Weight=0;
        int reseeds=belief.Reseeds;belief.Step(0.2f);
        float sum=0;bool finite=true;foreach(var p in belief.Particles){sum+=p.Weight;finite&=!float.IsNaN(p.Weight)&&!float.IsInfinity(p.Weight);}
        Check(belief.Reseeds>reseeds && finite && Mathf.Abs(sum-1)<0.01f,"A fully contradicted belief reseeds from the last evidence");
        var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<50;i++)belief.Step(0.2f);watch.Stop();
        Check(watch.Elapsed.TotalMilliseconds/50<2.5,"Belief update cost "+(watch.Elapsed.TotalMilliseconds/50).ToString("F2")+" ms per tick");
        belief.Deactivate();belief.enabled=true;perception.enabled=true;brain.enabled=true;
        return Save();
    }

    public static string Lifts()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Validate in Play Mode.");
        report=new Report{phase="08-Lifts"};
        var brain=Object.FindAnyObjectByType<MonsterBrain>();brain.enabled=false;
        var belief=brain.GetComponent<MonsterBelief>();belief.enabled=false;
        var perception=brain.GetComponent<MonsterPerception>();perception.enabled=false;
        belief.EnsureGraph();var graph=belief.Graph;
        int landings=0,floors=0;foreach(var l in graph.Lifts){floors+=l.Landing.Length;foreach(int n in l.Landing)if(n>=0)landings++;}
        Check(graph.Lifts.Length==Object.FindObjectsByType<CampusRift.CampusElevator>().Length && landings>=floors*0.95f,"Lift lobbies are part of the topology ("+landings+"/"+floors+")");
        int e=-1;for(int i=0;i<graph.Lifts.Length;i++)if(graph.Lifts[i].Elevator.building=="E")e=i;
        var lift=graph.Lifts[e];float now=Time.time;
        belief.ObserveSighting(lift.Lobby[0],Vector3.zero,now);
        belief.ObserveLiftBoarding(e,0,now,.92f);
        Check(belief.RiderMass(e)>0.85f,"Seeing the player step into a car makes the belief ride it");
        belief.ObserveLiftPassing(e,2,1,now+1);
        bool behind=false;foreach(var p in belief.Particles)if(p.Mode==BeliefMode.InLift && p.Lift==e && p.LiftTo<=2)behind=true;
        Check(!behind,"A car seen moving up past F3 cannot have dropped its rider below");
        belief.ObserveLiftStop(e,4,now+2,now-1);
        bool elsewhere=false;foreach(var p in belief.Particles)if(p.Mode==BeliefMode.InLift && p.Lift==e && p.LiftTo!=4)elsewhere=true;
        Check(!elsewhere,"An observed stop tells where the rider gets off");
        for(int i=1;i<=10;i++)belief.Step(0.2f,now+2+i*0.2f);
        Check(MassWithin(belief,lift.Lobby[4],12f,1.6f)>0.6f,"Right after the stop the belief is at the F5 landing");
        for(int i=11;i<=25;i++)belief.Step(0.2f,now+2+i*0.2f);
        Check(MassWithin(belief,lift.Lobby[4],30f,1.6f)+MassWithin(belief,lift.Lobby[4]+Vector3.up*3.6f,30f,1.6f)+MassWithin(belief,lift.Lobby[4]-Vector3.up*3.6f,30f,1.6f)>0.7f,
            "Seconds later it spreads from there (F5 and the adjacent floors), not across the campus");
        belief.ObserveLiftBoarding(e,4,Time.time,.95f);belief.ObserveLiftEmpty(e,4,Time.time);
        Check(belief.RiderMass(e)<0.1f,"An empty open car rules out riders");
        Check(ElevatorShaftGuard.GuardCount>0,"Shaft guards exist where a shaft has walkable polygons ("+ElevatorShaftGuard.GuardCount+")");
        var agent=brain.GetComponent<UnityEngine.AI.NavMeshAgent>();var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=UnityEngine.AI.NavMesh.AllAreas};
        int closed=0,checkedShafts=0;var path=new UnityEngine.AI.NavMeshPath();
        foreach(var info in graph.Lifts)
        {
            var el=info.Elevator;if(el.IsMoving || el.DoorAmount>0.3f || info.Landing[0]<0)continue;
            Vector3 inside=el.cabinSpaces[0].center;inside.y=el.floors[0].height;
            checkedShafts++;
            bool complete=UnityEngine.AI.NavMesh.CalculatePath(info.Lobby[0],inside,filter,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete
                && Vector3.Distance(path.corners[path.corners.Length-1],inside)<0.5f;
            if(!complete)closed++;
        }
        Check(checkedShafts>0 && closed==checkedShafts,"Closed landing doors keep the monster out of the shaft ("+closed+"/"+checkedShafts+")");
        // A monster standing in a doorway keeps the doors from closing on it.
        var body=brain.GetComponent<CapsuleCollider>();var elevator=lift.Elevator;
        Vector3 saved=brain.transform.position;agent.enabled=false;
        var doorway=elevator.floors[elevator.CurrentFloor].doorways[0];
        brain.transform.position=new Vector3(doorway.center.x,elevator.floors[elevator.CurrentFloor].height,doorway.center.z);Physics.SyncTransforms();
        bool blocked=elevator.IsDoorwayBlocked();
        brain.transform.position=saved;Physics.SyncTransforms();agent.enabled=true;
        Check(blocked,"A monster in the doorway holds the lift doors open");
        // Upper-floor car (no NavMesh inside): from the doorway the monster reaches the far wall.
        var player=Object.FindAnyObjectByType<CampusRift.CampusExplorer>();var playerBody=player.GetComponent<CharacterController>();
        Vector3 playerSaved=player.transform.position;
        elevator.RequestFloor(4);for(int i=0;i<800 && !(elevator.CurrentFloor==4 && !elevator.IsMoving && elevator.DoorAmount>=1f);i++)elevator.Tick(0.05f);
        Vector3 back=elevator.cabinSpaces[0].center+Vector3.up*(elevator.floors[4].height-elevator.floors[0].height)-elevator.outward*0.6f;
        back.y=elevator.floors[4].height+0.05f;
        playerBody.enabled=false;player.transform.position=back;playerBody.enabled=true;
        var upper=elevator.floors[4].doorways[0];agent.enabled=false;
        Quaternion savedRotation=brain.transform.rotation;
        brain.transform.SetPositionAndRotation(new Vector3(upper.center.x,elevator.floors[4].height,upper.center.z)+elevator.outward*0.45f,Quaternion.LookRotation(-elevator.outward));Physics.SyncTransforms();
        var combat=brain.GetComponent<MonsterCombat>();perception.enabled=true;perception.Scan();
        float reach=combat.Reach,gap=Vector3.Distance(brain.transform.position,player.transform.position);
        Check(perception.CanSeePlayer && gap>brain.config.AttackDistance && gap<=reach,"From the doorway it reaches a player pressed to the back of an upper-floor car ("+gap.ToString("F2")+" m, reach "+reach.ToString("F1")+")");
        playerBody.enabled=false;player.transform.position=playerSaved;playerBody.enabled=true;
        brain.transform.SetPositionAndRotation(saved,savedRotation);Physics.SyncTransforms();agent.enabled=true;perception.enabled=false;
        elevator.RequestFloor(0);for(int i=0;i<800 && !(elevator.CurrentFloor==0 && !elevator.IsMoving);i++)elevator.Tick(0.05f);
        belief.Deactivate();belief.enabled=true;perception.enabled=true;brain.enabled=true;
        return Save();
    }

    public static string NavigationRobustness()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Validate in Play Mode.");
        report=new Report{phase="09-Navigation"};
        var brain=Object.FindAnyObjectByType<MonsterBrain>();brain.enabled=false;
        var nav=brain.GetComponent<MonsterNavigation>();nav.Stop();
        Vector3 origin=brain.transform.position;
        var throttle=typeof(MonsterNavigation).GetField("nextPathTime",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var strategy=typeof(MonsterNavigation).GetField("nextStrategyTime",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        System.Action unthrottle=()=>{throttle.SetValue(nav,0f);strategy.SetValue(nav,0f);};
        // A point hanging in the air above open ground, well away from any walkable surface.
        Vector3 ledge=origin+brain.transform.forward*4f+Vector3.up*4.5f;UnityEngine.AI.NavMeshHit probe;
        for(int i=0;i<16 && UnityEngine.AI.NavMesh.SamplePosition(ledge,out probe,2.2f,UnityEngine.AI.NavMesh.AllAreas);i++)
            ledge=origin+Quaternion.Euler(0,i*45,0)*Vector3.forward*(3+i)+Vector3.up*4.5f;
        unthrottle();bool direct=nav.MoveTo(ledge);
        Check(!direct,"A point off the walkable surface is not reported reachable");
        unthrottle();Check(nav.MoveNear(ledge,6f) && (nav.Agent.hasPath || nav.Agent.pathPending),"The monster heads for the closest walkable spot instead of freezing");
        nav.Stop();
        // X block, top-floor lift lobby: far beyond a single NavMesh query, so it uses the room graph.
        var belief=brain.GetComponent<MonsterBelief>();belief.EnsureGraph();var target=origin;
        CampusNavGraph.Lift x=null,c=null;
        foreach(var l in belief.Graph.Lifts){if(l.Elevator.building=="X"){target=l.Lobby[l.Lobby.Length-1];x=l;}if(l.Elevator.building=="C")c=l;}
        var shared=nav.config;nav.config=Object.Instantiate(shared);nav.config.RideLifts=false; // the walking route itself
        unthrottle();nav.MoveTo(target);int route=nav.Route.Count;var first=nav.Status;
        unthrottle();throttle.SetValue(nav,0f);nav.MoveTo(target+Vector3.right*2);
        Check(route>0 && first==NavigationStatus.GraphRoute && nav.Route.Count==route && nav.Status==NavigationStatus.GraphRoute,
            "A long multi-floor route survives small goal changes (route "+route+", "+first+" -> "+nav.Status+")");
        nav.Stop();
        // Lift or stairs: decided from the walking legs, the wait implied by the car it can see, and the ride.
        // (The ride itself is exercised end to end by ShabanTraversalPlayTest.)
        nav.config.RideLifts=true;
        const System.Reflection.BindingFlags hidden=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var liftCheck=typeof(MonsterNavigation).GetField("nextLiftCheck",hidden);
        var awareness=brain.GetComponent<MonsterElevatorAwareness>();
        var tick=typeof(MonsterElevatorAwareness).GetMethod("Update",hidden);var nextTick=typeof(MonsterElevatorAwareness).GetField("nextTick",hidden);
        var agent=nav.Agent;Quaternion savedRotation=brain.transform.rotation;
        System.Action<CampusNavGraph.Lift,int,int> standAndRead=(lift,floor,carFloor)=>
        {
            var e=lift.Elevator;e.RequestFloor(carFloor);
            for(int i=0;i<6000 && (e.IsMoving || e.CurrentFloor!=carFloor || e.State!=CampusRift.CampusElevator.LiftState.Closed);i++)e.Tick(0.05f);
            nav.Stop();agent.enabled=false;brain.transform.SetPositionAndRotation(lift.Lobby[floor],Quaternion.LookRotation(-e.outward));agent.enabled=true;Physics.SyncTransforms();
            nextTick.SetValue(awareness,0f);tick.Invoke(awareness,null); // reads the floor display from the lobby
            liftCheck.SetValue(nav,0f);unthrottle();nav.SetSpeed(brain.config.ChaseSpeed);
        };
        int xTop=x.Lobby.Length-1;
        standAndRead(x,0,0);
        Check(awareness.Knowledge.Length>0 && Array.Exists(awareness.Knowledge,k=>k.Building=="X" && k.Floor==0 && Time.time-k.ReadAt<1f),"The floor display is readable from the lobby, right under it");
        nav.MoveTo(x.Lobby[xTop]);
        Check(nav.UsingLift && nav.LiftPlan.Contains("X F1->F"+(xTop+1)),"Car waiting at F1: the 12-floor climb takes the lift ("+nav.LastLiftDecision+")");
        unthrottle();nav.MoveTo(x.Lobby[0]+(x.Lobby[0]-x.Elevator.floors[0].entrances[0]).normalized*1.5f);
        Check(!nav.UsingLift,"A goal back on the same floor cancels the lift plan");
        standAndRead(x,0,xTop);
        nav.MoveTo(x.Lobby[10]);
        Check(!nav.UsingLift && nav.LastLiftDecision.EndsWith("stairs"),"Car seen parked at the top: the stairs win ("+nav.LastLiftDecision+")");
        standAndRead(c,0,0);
        nav.MoveTo(c.Lobby[1]);
        Check(!nav.UsingLift && nav.LastLiftDecision.EndsWith("stairs"),"One floor up: the stairs win even with the car at hand ("+nav.LastLiftDecision+")");
        standAndRead(x,0,0);
        nav.Stop();agent.enabled=false;brain.transform.SetPositionAndRotation(origin,savedRotation);agent.enabled=true;Physics.SyncTransforms();
        nav.config=shared;brain.enabled=true;
        return Save();
    }

    public static string Pursuit()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Validate in Play Mode.");
        report=new Report{phase="03-Pursuit"};
        var brain=Object.FindAnyObjectByType<MonsterBrain>();brain.enabled=false;
        var prediction=brain.GetComponent<MonsterPrediction>();var config=brain.config;
        Check(prediction.PredictionHorizon(20,0)>prediction.PredictionHorizon(2,0),"Distant targets receive a longer prediction horizon");
        Check(prediction.PredictionHorizon(20,400)<prediction.PredictionHorizon(20,0),"Rapid turns shorten prediction");
        Check(prediction.PredictionHorizon(1000,0)<=config.MaxPredictionTime,"Prediction horizon is bounded");
        prediction.ShouldDelayTurn(Vector3.right*6,Time.time);
        bool first=prediction.ShouldDelayTurn(Vector3.forward*6,Time.time);
        bool held=prediction.ShouldDelayTurn(Vector3.forward*6,Time.time+config.ReactionDelay*.5f);
        bool released=prediction.ShouldDelayTurn(Vector3.forward*6,Time.time+config.ReactionDelay+.01f);
        Check(first && held && !released,"A 90 degree turn holds the previous prediction until reaction delay expires");
        var trail=new PlayerTrailHistory();for(int i=0;i<8;i++)trail.Observe(Vector3.right*i,i*.125f,config);
        float straight=prediction.Confidence(trail);trail.Observe(new Vector3(7,0,1),1,config);
        Check(straight>prediction.Confidence(trail),"Unstable direction lowers prediction confidence");
        return Save();
    }
}
