using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRift.Monsters
{
    [Serializable]
    public sealed class SearchLandmark
    {
        public string RoomID, BuildingID;
        public int FloorID;
        public string Kind;
        public Vector3 Position;
    }
    public enum SearchMode { Evidence, Belief, LiftDisplay, Landmarks }

    // Searching after contact is lost. The first stop is the last real evidence (when it is
    // worth visiting); afterwards goals come from the monster's belief: the place with the most
    // probability mass per second of walking, anywhere on the campus it can reach on foot.
    // Without a belief (no graph), the original landmark candidates are used.
    public sealed class MonsterSearch : MonoBehaviour
    {
        public MonsterAIConfig config;
        public SearchLandmark[] landmarks = new SearchLandmark[0];
        public Vector3 Center { get; private set; }
        public Vector3 Target { get; private set; }
        public bool Finished { get; private set; } = true;
        public int ActiveExpansion { get; private set; }
        public IReadOnlyList<SearchCandidate> Candidates => candidates;
        public SearchMode Mode { get; private set; } = SearchMode.Landmarks;
        public string GoalReason { get; private set; } = "";
        public float GoalMass { get; private set; }
        public float GoalTravelSeconds { get; private set; }
        public float DesiredSpeed { get; private set; }
        public int GoalsReached { get; private set; }
        // Upper bound on search speed (roaming after a hunt uses the patrol pace).
        [NonSerialized] public float SpeedLimit = float.PositiveInfinity;
        readonly List<SearchCandidate> candidates = new List<SearchCandidate>(64);
        struct Visit { public Vector3 Position; public float Time; }
        readonly List<Visit> visits = new List<Visit>(64);
        readonly Queue<Vector2Int> frontier = new Queue<Vector2Int>(256);
        readonly HashSet<int> expanded = new HashSet<int>();
        MonsterNavigation navigation;
        MonsterMemory memory;
        MonsterPerception perception;
        MonsterBelief belief;
        MonsterElevatorAwareness lifts;
        SearchCandidate current;
        float targetSince, lookUntil, initialLookYaw, nextPlan, goalDeadline;
        bool looking, goalOutOfReach;
        // Belief planning state
        CampusNavGraph graph;
        CampusNavGraph.Scratch scratch;
        float[] mass, cost, blockedUntil, lookedAt;
        readonly List<int> local = new List<int>(64);
        readonly Dictionary<long,float> regional = new Dictionary<long,float>(512);
        const float RegionCell = 8f, RegionBand = 3f;
        int goalNode = -1, infoLift = -1;
        float goalMassAtChoice, lookStarted;

        void Awake()
        {
            navigation=GetComponent<MonsterNavigation>();memory=GetComponent<MonsterMemory>();perception=GetComponent<MonsterPerception>();
            belief=GetComponent<MonsterBelief>();lifts=GetComponent<MonsterElevatorAwareness>();
        }
        void Start(){if(belief==null)belief=GetComponent<MonsterBelief>();if(lifts==null)lifts=GetComponent<MonsterElevatorAwareness>();}
        public SearchLandmark NearestLandmark(Vector3 point)
        {
            SearchLandmark nearest=null;float best=64;
            foreach(var landmark in landmarks)
            {
                if(Mathf.Abs(point.y-landmark.Position.y)>1.6f)continue;
                float d=(point-landmark.Position).sqrMagnitude;if(d<best){best=d;nearest=landmark;}
            }
            return nearest;
        }
        public static float DirectionPrior(Vector3 evidence,Vector3 heading,Vector3 candidate)
        {
            var delta=Vector3.ProjectOnPlane(candidate-evidence,Vector3.up);
            if(heading.sqrMagnitude<.1f || delta.sqrMagnitude<.1f)return .55f;
            return Mathf.Lerp(.1f,.9f,(Vector3.Dot(heading.normalized,delta.normalized)+1)*.5f);
        }
        public void Begin(Vector3 evidence,Vector3 rememberedVelocity) => Begin(evidence, rememberedVelocity, true);
        public void Begin(Vector3 evidence,Vector3 rememberedVelocity,bool visitEvidenceFirst)
        {
            Center=evidence;candidates.Clear();current=null;looking=false;Finished=false;ActiveExpansion=0;
            goalNode=-1;nextPlan=0;GoalReason="";
            visits.RemoveAll(v=>Time.time-v.Time>config.NegativeEvidenceDuration);
            Add(evidence,1,"Last known position",0,"Direct sensory evidence");
            Vector3 heading=Vector3.ProjectOnPlane(rememberedVelocity,Vector3.up).normalized;
            foreach(var landmark in landmarks)
            {
                float distance=Vector3.Distance(landmark.Position,evidence);
                if(distance>config.SearchRadius*1.6f)continue;
                float directional=DirectionPrior(evidence,heading,landmark.Position);
                bool stair=landmark.Kind=="Stair" || landmark.RoomID.Contains("Stair");
                float vertical=landmark.Position.y-evidence.y;
                float floorBias=Mathf.Abs(vertical)<1.6f?1f:(stair?1f:.65f);
                if(stair && Mathf.Abs(rememberedVelocity.y)>.4f)floorBias=vertical*rememberedVelocity.y>0?1.35f:.5f;
                float score=(.2f+.65f*directional)*Mathf.Exp(-distance/(config.SearchRadius*1.3f))*floorBias;
                int level=Mathf.Abs(vertical)>1.6f?3:distance<5?1:2;
                Add(landmark.Position,score,landmark.RoomID,level,stair?"Stair / floor continuation":"Door aligned with observed trajectory");
            }
            ExpandGraph(evidence,heading,rememberedVelocity.y);
            if(candidates.Count<4)
                for(int i=0;i<4;i++)
                {
                    var direction=Quaternion.Euler(0,i*90,0)*(heading.sqrMagnitude>.1f?heading:Vector3.forward);
                    NavMeshHit hit;
                    if(NavMesh.SamplePosition(evidence+direction*4,out hit,1,NavMesh.AllAreas) && Mathf.Abs(hit.position.y-evidence.y)<1)
                        Add(hit.position,.4f*DirectionPrior(evidence,heading,hit.position),"Local sector "+i,1,"Reachable local sector");
                }
            if(visitEvidenceFirst)
            {
                SelectNext();Mode=SearchMode.Evidence;
                // A lead a few seconds old is still hot: keep running to where the player vanished.
                bool hot=memory!=null && Time.time-memory.LatestTime<4f && Vector3.Distance(transform.position,evidence)>4f;
                DesiredSpeed=hot?config.ChaseSpeed*0.9f:config.InvestigateSpeed;
                return;
            }
            candidates[0].Visited=true;
            if(BeliefReady)PlanWithBelief(true);else SelectNext();
        }
        void ExpandGraph(Vector3 evidence,Vector3 heading,float verticalVelocity)
        {
            var graph=navigation.roomGraph;if(graph==null || graph.Nodes.Length==0)return;
            int start=-1;float best=64;
            for(int i=0;i<graph.Nodes.Length;i++)
            {
                var delta=graph.Nodes[i].WorldPosition-evidence;
                if(Mathf.Abs(delta.y)>1.6f || delta.sqrMagnitude>=best)continue;
                start=i;best=delta.sqrMagnitude;
            }
            if(start<0)return;
            frontier.Clear();expanded.Clear();frontier.Enqueue(new Vector2Int(start,0));expanded.Add(start);
            while(frontier.Count>0 && expanded.Count<300)
            {
                var entry=frontier.Dequeue();var node=graph.Nodes[entry.x];
                if(entry.y>=4)continue;
                foreach(var edge in node.Neighbors)
                {
                    if(edge.Neighbor<0 || edge.Neighbor>=graph.Nodes.Length || !expanded.Add(edge.Neighbor))continue;
                    var next=graph.Nodes[edge.Neighbor];float distance=Vector3.Distance(next.WorldPosition,evidence);
                    if(distance>config.SearchRadius*2.2f)continue;
                    bool stairs=edge.ConnectionType==RoomConnectionType.Stair;
                    float prior=DirectionPrior(evidence,heading,next.WorldPosition);
                    if(stairs && Mathf.Abs(verticalVelocity)>.4f)prior*=verticalVelocity*(next.WorldPosition.y-evidence.y)>0?1.4f:.55f;
                    float score=(.15f+prior*.55f)*Mathf.Exp(-distance/(config.SearchRadius*1.5f));
                    Add(next.WorldPosition,score,next.RoomID,Mathf.Max(stairs?3:1,entry.y+1),stairs?"Connected staircase / floor":"Connected zone expansion");
                    frontier.Enqueue(new Vector2Int(edge.Neighbor,entry.y+1));
                }
            }
        }
        void Add(Vector3 position,float probability,string room,int level,string reason)
        {
            foreach(var candidate in candidates)if((candidate.Position-position).sqrMagnitude<2.25f)return;
            foreach(var visit in visits)
                if((visit.Position-position).sqrMagnitude<4 && (memory==null || memory.LatestTime<=visit.Time))probability*=config.VisitedPenalty;
            var added=new SearchCandidate{Position=position,Probability=Mathf.Clamp01(probability),Room=room,ExpansionLevel=level,Reason=reason};
            int count=0,worst=-1;float lowest=float.PositiveInfinity;
            for(int i=0;i<candidates.Count;i++)if(candidates[i].ExpansionLevel==level)
            {count++;if(candidates[i].Probability<lowest){lowest=candidates[i].Probability;worst=i;}}
            if(count>=Mathf.Max(4,config.MaxSearchCandidates/4))
            {if(probability>lowest)candidates[worst]=added;return;}
            if(candidates.Count<config.MaxSearchCandidates)candidates.Add(added);
        }
        void SelectNext()
        {
            Mode=SearchMode.Landmarks;
            current=null;float best=-1;
            for(;ActiveExpansion<=4 && current==null;ActiveExpansion++)
                foreach(var candidate in candidates)
                    if(!candidate.Visited && candidate.ExpansionLevel<=ActiveExpansion && candidate.Probability>best)
                    {best=candidate.Probability;current=candidate;}
            if(current==null){Finished=true;navigation.Stop();return;}
            ActiveExpansion=Mathf.Max(0,ActiveExpansion-1);Target=current.Position;targetSince=Time.time;looking=false;
            GoalReason=current.Reason;DesiredSpeed=config.SearchSpeed;
            goalDeadline=Time.time+Mathf.Max(8,Vector3.Distance(Center,Target)/navigation.ScaleSpeed(config.SearchSpeed)+5);
        }
        public void RegisterNegativeEvidence(SearchCandidate candidate)
        {
            if(candidate==null || candidate.Visited)return;
            candidate.Visited=true;candidate.Probability*=config.VisitedPenalty;
            while(visits.Count>=64)visits.RemoveAt(0);visits.Add(new Visit{Position=candidate.Position,Time=Time.time});
            foreach(var other in candidates)
                if(other!=candidate && (other.Position-candidate.Position).sqrMagnitude<9)other.Probability*=config.VisitedPenalty;
            if(memory!=null)memory.RememberInference(candidate.Position,MonsterEvidenceType.SearchResult,1-candidate.Probability);
        }

        bool BeliefReady => belief != null && belief.isActiveAndEnabled && belief.Active && belief.EnsureGraph();

        public void Tick()
        {
            if(Finished)return;
            if(looking)
            {
                navigation.Stop();
                // Hold only while the display is actually readable from here (a fresh reading).
                bool watching=Mode==SearchMode.LiftDisplay && lifts!=null && lifts.WorthWatching(infoLift,Time.time) && Time.time-lookStarted<25f &&
                    (Time.time-lookStarted<1.5f || lifts.Knowledge[infoLift].ReadAt>=lookStarted);
                if(watching)
                {
                    // Stand facing the floor display until the car stops.
                    lookUntil=Mathf.Max(lookUntil,Time.time+0.3f);
                    transform.rotation=Quaternion.Euler(0,initialLookYaw+Mathf.Sin(Time.time*1.3f)*12f,0);
                    return;
                }
                float progress=1-Mathf.Clamp01((lookUntil-Time.time)/config.SearchLookSeconds);
                transform.rotation=Quaternion.Euler(0,initialLookYaw+Mathf.Sin(progress*Mathf.PI*2)*55,0);
                if(Time.time<lookUntil)return;
                looking=false;GoalsReached++;
                if(Mode==SearchMode.Landmarks || Mode==SearchMode.Evidence)
                {
                    if(current!=null)
                    {
                        if(!perception.CanSeePlayer && (memory==null || memory.LastHeardTime<targetSince))RegisterNegativeEvidence(current);
                        else current.Visited=true;
                    }
                }
                if(goalNode>=0 && lookedAt!=null)lookedAt[goalNode]=Time.time;
                // Could not read the display from here: try other leads for a while.
                if(Mode==SearchMode.LiftDisplay && lifts!=null && infoLift>=0 && lifts.Knowledge[infoLift].ReadAt<lookStarted && goalNode>=0)
                    blockedUntil[goalNode]=Time.time+30f;
                // Looked from the end of a partial path: the goal itself is out of reach for now.
                if(goalOutOfReach && goalNode>=0 && blockedUntil!=null)blockedUntil[goalNode]=Time.time+25f;
                goalOutOfReach=false;
                if(BeliefReady)PlanWithBelief(true);else SelectNext();
                return;
            }
            if(Mode!=SearchMode.Evidence && BeliefReady)PlanWithBelief(false);
            if(Finished)return;
            navigation.SetSpeed(Mathf.Min(DesiredSpeed>0?DesiredSpeed:config.SearchSpeed,SpeedLimit));
            bool reachable=navigation.MoveTo(Target);
            if(Time.time-targetSince>.5f && navigation.Arrived)
            {
                // The end of a partial path is only the closest reachable vantage point.
                goalOutOfReach=!navigation.DestinationReachable && (transform.position-Target).sqrMagnitude>2.5f*2.5f;
                looking=true;lookStarted=Time.time;lookUntil=Time.time+config.SearchLookSeconds;navigation.Stop();
                initialLookYaw=Mode==SearchMode.LiftDisplay && infoLift>=0 && graph!=null
                    ? Quaternion.LookRotation(-graph.Lifts[infoLift].Elevator.outward).eulerAngles.y : LookYaw();
                return;
            }
            bool failed=navigation.LastMoveFailed || (!reachable && navigation.Status==NavigationStatus.Unreachable);
            if(failed || Time.time>goalDeadline)
            {
                if(current!=null && (Mode==SearchMode.Landmarks || Mode==SearchMode.Evidence))
                {current.Visited=true;current.Probability*=.05f;current.Reason+="; inaccessible this attempt";}
                if(goalNode>=0 && blockedUntil!=null)blockedUntil[goalNode]=Time.time+(failed?25f:15f);
                if(BeliefReady)PlanWithBelief(true);else SelectNext();
            }
        }

        // Face the unexplored side: the probability mass around the monster, else keep heading.
        float LookYaw()
        {
            float yaw=transform.eulerAngles.y;
            if(!BeliefReady)return yaw;
            Vector3 here=transform.position,sum=Vector3.zero;
            var particles=belief.Particles;
            for(int i=0;i<particles.Length;i++)
            {
                if(particles[i].Mode==BeliefMode.InLift)continue;
                Vector3 d=belief.PositionOf(particles[i])-here;
                if(Mathf.Abs(d.y)>1.6f)continue;
                d.y=0;float m=d.magnitude;
                if(m<1f || m>14f)continue;
                sum+=d/m*particles[i].Weight;
            }
            return sum.sqrMagnitude>1e-6f?Quaternion.LookRotation(sum).eulerAngles.y:yaw;
        }

        bool EnsurePlanner()
        {
            if(graph!=null && graph==belief.Graph)return true;
            graph=belief.Graph;if(graph==null)return false;
            scratch=graph.CreateScratch();
            mass=new float[graph.Count];cost=new float[graph.Count];blockedUntil=new float[graph.Count];lookedAt=new float[graph.Count];
            for(int i=0;i<graph.Count;i++){blockedUntil[i]=-1;lookedAt[i]=-10000;}
            return true;
        }

        static long RegionKey(int x,int y,int z)=>((long)(x+4096)*8192+(y+4096))*8192+(z+4096);

        void BuildRegions()
        {
            regional.Clear();
            for(int i=0;i<mass.Length;i++)
            {
                if(mass[i]<=0)continue;
                Vector3 p=graph.Position[i];
                long key=RegionKey(Mathf.FloorToInt(p.x/RegionCell),Mathf.FloorToInt(p.y/RegionBand),Mathf.FloorToInt(p.z/RegionCell));
                regional.TryGetValue(key,out float sum);regional[key]=sum+mass[i];
            }
        }

        // Mass in the ~24 m block of the same floor band around a node.
        float RegionalMass(int node)
        {
            Vector3 p=graph.Position[node];
            int cx=Mathf.FloorToInt(p.x/RegionCell),cy=Mathf.FloorToInt(p.y/RegionBand),cz=Mathf.FloorToInt(p.z/RegionCell);
            float sum=0;
            for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
                if(regional.TryGetValue(RegionKey(cx+dx,cy,cz+dz),out float m))sum+=m;
            return sum;
        }

        // What a goal is worth: a room counts its own hypotheses (the corridor outside being seen
        // empty says nothing about what is behind its closed door); other goals their surroundings.
        float GoalValue(int node)=>graph.RoomInterior[node]?mass[node]:LocalMass(node);

        float LocalMass(int node)
        {
            local.Clear();
            graph.QueryRadius(graph.Position[node],config.SearchGoalRadius,1.6f,local);
            float sum=0;foreach(int n in local)sum+=mass[n];
            return sum;
        }

        // Choose where to look next: probability mass around a node, discounted by the time it
        // takes to get there (stairs, or a lift when faster), with hysteresis so the
        // monster commits to a plan instead of dithering between similar places.
        void PlanWithBelief(bool force)
        {
            if(!EnsurePlanner())return;
            float now=Time.time;
            belief.AccumulateNodeMass(mass,0.8f);
            if(!force && now<nextPlan)
            {
                // Replan early when the current goal has been seen empty on the way.
                if(goalNode<0 || Mode!=SearchMode.Belief || GoalValue(goalNode)>=goalMassAtChoice*0.3f)return;
            }
            nextPlan=now+config.SearchReplanRate;
            float speed=navigation.ChaseSpeed*0.9f; // travel-time estimate at hunting pace
            // A lift ride counts when it is faster than the stairs (the monster takes it then).
            graph.TravelCosts(transform.position,cost,scratch,800f,config.RideLifts?speed:0f);
            BuildRegions();
            float fresh=now-belief.LastEvidenceTime;
            int best=-1,bestLift=-1;float bestScore=0,bestMass=0;SearchMode bestMode=SearchMode.Belief;string reason="";
            for(int i=0;i<graph.Count;i++)
            {
                if(mass[i]<0.0005f || float.IsInfinity(cost[i]) || now<blockedUntil[i])continue;
                float around=LocalMass(i);
                // Local mass says where to look; regional mass says which area to be in.
                float travel=cost[i]/Mathf.Max(0.5f,speed);
                float score=(around+0.5f*RegionalMass(i))/(1f+travel/config.SearchTravelHorizon);
                if(now-lookedAt[i]<20f)score*=0.2f;
                if(i==goalNode)score*=1.35f;
                if(score>bestScore){bestScore=score;best=i;bestMass=around;}
            }
            // Information goal: where did the lift go? Read the display on the nearest landing.
            if(lifts!=null)
                for(int l=0;l<graph.Lifts.Length;l++)
                {
                    var k=lifts.Knowledge.Length>l?lifts.Knowledge[l]:null;
                    if(k==null || !lifts.WorthWatching(l,now))continue;
                    float unsure=belief.UnconfirmedLiftMass(l,k.BoardedAt-1f,lifts.ConfirmedFloor(l));
                    if(unsure<0.25f)continue;
                    int landing=NearestLanding(l);
                    if(landing<0 || float.IsInfinity(cost[landing]) || now<blockedUntil[landing])continue;
                    float score=unsure*0.9f/(1f+cost[landing]/Mathf.Max(0.5f,speed)/config.SearchTravelHorizon);
                    if(now-lookedAt[landing]<20f)score*=0.2f;
                    if(landing==goalNode)score*=1.35f;
                    if(score>bestScore){bestScore=score;best=landing;bestMass=unsure;bestMode=SearchMode.LiftDisplay;bestLift=l;
                        reason=k.Moving?"watch lift "+k.Building+" display until it stops":"read lift "+k.Building+" display";}
                }
            if(best<0)
            {
                if(Mode==SearchMode.Belief || Mode==SearchMode.LiftDisplay){GoalReason="belief exhausted";}
                if(current==null || current.Visited)SelectNext();
                return;
            }
            Mode=bestMode;infoLift=bestLift;
            // Run while the lead is fresh or the goal is a strong one (e.g. a lift stop it watched).
            bool hot=fresh<config.HuntFreshSeconds || bestMass>=0.35f;
            DesiredSpeed=hot?(cost[best]>15f?config.ChaseSpeed*0.9f:config.InvestigateSpeed):config.SearchSpeed;
            if(best!=goalNode || (bestMode==SearchMode.LiftDisplay && GoalReason!=reason))
            {
                goalNode=best;Target=graph.Position[best];targetSince=now;current=null;
                goalMassAtChoice=bestMode==SearchMode.Belief?GoalValue(best):bestMass;
                GoalReason=bestMode==SearchMode.LiftDisplay?reason:$"{graph.Label[best]} (p={bestMass:P0})";
                goalDeadline=now+Mathf.Max(8f,cost[best]/Mathf.Max(0.5f,navigation.ScaleSpeed(Mathf.Min(DesiredSpeed,SpeedLimit)))*1.4f+6f);
            }
            GoalMass=bestMass;GoalTravelSeconds=cost[best]/Mathf.Max(0.5f,speed);
        }

        int NearestLanding(int lift)
        {
            var info=graph.Lifts[lift];int best=-1;float distance=float.PositiveInfinity;
            for(int f=0;f<info.Landing.Length;f++)
            {
                int node=info.Landing[f];if(node<0)continue;
                float c=cost[node];if(c<distance){distance=c;best=node;}
            }
            return best;
        }
    }
}
