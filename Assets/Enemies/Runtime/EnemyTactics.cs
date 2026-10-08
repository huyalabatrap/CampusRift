using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.SkyBeast;

namespace CampusRift.Enemies
{
    // A Door and its exterior Exit can describe the same opening: collapse nodes within 3 m.
    // Reserve a reachable entrance BEFORE assigning blockers, including ordinary ambushers.
    public sealed class EnemyTactics : MonoBehaviour
    {
        public sealed class Entrance { public string id; public Vector3 position; }
        public IReadOnlyList<Entrance> Entrances => entrances;
        public int BlockedCount => blockers.Count;
        public int AmbushCount => ambushers.Count;
        public Vector3 FreeEntrance { get; private set; }
        public bool WarningActive => fire != null && fire.State == FireBreathCycle.Phase.Warning;
        readonly List<Entrance> entrances = new List<Entrance>();
        readonly Dictionary<EnemyInstance, Vector3> blockers = new Dictionary<EnemyInstance, Vector3>();
        readonly Dictionary<EnemyInstance, Vector3> ambushers = new Dictionary<EnemyInstance, Vector3>();
        NavMeshPath path;
        EnemyDirector director; FireBreathCycle fire; float nextPlan;
        readonly List<Entrance> scanned=new List<Entrance>(16);
        readonly Dictionary<string,Entrance> entranceCache=new Dictionary<string,Entrance>();
        RoomGraph scanGraph;Vector3 scanFrom;int scanNode,blockDoor,blockActor;
        EnemyInstance blockBest;float blockDistance;
        EnemyInstance warningGuard;
        bool scanning,assigningWarning;
        bool explicitAmbush;
        int WarningBudget=>director.Squad.Enabled&&director.Active.Count>=6?Mathf.Min(1,BlockLimit(entrances.Count)):BlockLimit(entrances.Count);
        bool DoorCandidate(EnemyInstance e)=>!(director.Squad.Enabled&&director.Active.Count>=6&&
            director.Squad.TryInspect(e,out var order)&&order.role==SquadRole.Interceptor);
        float DoorDistance(EnemyInstance e,Vector3 door)
        {
            float distance=(e.transform.position-door).sqrMagnitude;
            if(e==warningGuard&&WarningBudget==1)return distance-20000;
            // A rear pressure unit can guard a door without opening a hole in a side wing.
            if(director.Squad.Enabled&&director.Active.Count>=6&&director.Squad.TryInspect(e,out var order)&&
                order.role!=SquadRole.Chaser&&order.role!=SquadRole.Support)distance+=5000;
            return distance;
        }
        void Awake() { director = GetComponent<EnemyDirector>(); path = new NavMeshPath(); }
        void Update()
        {
            if (fire != FireBreathCycle.Instance) { Unhook(); fire = FireBreathCycle.Instance; if (fire != null) fire.WarningStarted += QueueWarning; }
            if (Time.timeScale <= 0 || LevelDirector.Instance != null && LevelDirector.Instance.CinematicPaused) return;
            if (fire == null || !WarningActive) {blockers.Clear();warningGuard=null;}
            if(scanning){ScanStep();return;}
            if(assigningWarning){BlockStep();return;}
            if (Time.time < nextPlan) return;
            nextPlan = Time.time + 2;
            if(WarningActive)BeginScan();
            else if(director.Squad.Enabled)
            {
                explicitAmbush=false;
                ambushers.Clear();
                foreach(var a in director.Squad.Members)
                    if(a.role==SquadRole.Ambusher&&director.Squad.TryGet(a.enemy,out _))ambushers[a.enemy]=a.target;
            }
            else PlanAmbush();
        }
        void Unhook() { if (fire != null) fire.WarningStarted -= QueueWarning; }
        void QueueWarning(){nextPlan=0;scanning=assigningWarning=false;BeginScan();}
        void BeginScan()
        {
            var p=director.FindPlayer();scanGraph=ShelterGraphReference.Graph;
            if(p==null||scanGraph==null)return;
            scanFrom=p.position;scanNode=0;scanned.Clear();scanning=true;ambushers.Clear();
        }
        void ScanStep()
        {
            while(scanNode<scanGraph.Nodes.Length)
            {
                var n=scanGraph.Nodes[scanNode];
                if((n.Kind!=RoomNodeKind.Door&&n.Kind!=RoomNodeKind.Exit)||n.FloorID>1||!n.RoomID.EndsWith("/approach")||
                    Mathf.Abs(n.WorldPosition.y-scanFrom.y)>2||Vector3.ProjectOnPlane(n.WorldPosition-scanFrom,Vector3.up).sqrMagnitude>1600||
                    ShelterDetector.AtFeet(n.WorldPosition)==Shelter.Indoor){scanNode++;continue;}
                bool shelter=false;foreach(var edge in n.Neighbors)if(edge.Neighbor>=0&&edge.Neighbor<scanGraph.Nodes.Length&&ShelterDetector.AtFeet(scanGraph.Nodes[edge.Neighbor].WorldPosition)!=Shelter.Outdoor){shelter=true;break;}
                if(!shelter||!NavMesh.SamplePosition(n.WorldPosition,out var h,1.5f,NavMesh.AllAreas)){scanNode++;continue;}
                bool duplicate=false;foreach(var e in scanned)if(Vector3.Distance(e.position,h.position)<3){duplicate=true;break;}
                if(duplicate){scanNode++;continue;}
                if(!director.Squad.ReservePathQuery())return;
                scanNode++;
                if(Reachable(scanFrom,h.position))
                {
                    if(!entranceCache.TryGetValue(n.RoomID,out var entry)){entry=new Entrance{id=n.RoomID};entranceCache.Add(n.RoomID,entry);}
                    entry.position=h.position;scanned.Add(entry);
                }
            }
            scanning=false;entrances.Clear();entrances.AddRange(scanned);
            var p=director.FindPlayer();if(p==null)return;
            entrances.Sort((a,b)=>(a.position-p.position).sqrMagnitude.CompareTo((b.position-p.position).sqrMagnitude));
            FreeEntrance=entrances.Count>0?entrances[0].position:p.position;
            if(WarningActive&&ShelterDetector.AtFeet(p.position)==Shelter.Outdoor){blockers.Clear();assigningWarning=true;blockDoor=1;blockActor=0;blockBest=null;blockDistance=float.PositiveInfinity;}
        }
        void BlockStep()
        {
            if(!WarningActive){assigningWarning=false;return;}
            while(blockDoor<=WarningBudget)
            {
                while(blockActor<director.Active.Count)
                {
                    var e=director.Active[blockActor];
                    if(!Available(e,4)||!DoorCandidate(e)||blockers.ContainsKey(e)){blockActor++;continue;}
                    if(!director.Squad.ReservePathQuery())return;
                    blockActor++;if(!Reachable(e.transform.position,entrances[blockDoor].position))continue;
                    float d=DoorDistance(e,entrances[blockDoor].position);
                    if(d<blockDistance){blockDistance=d;blockBest=e;}
                }
                if(blockBest!=null&&blockBest.Alive){blockers[blockBest]=entrances[blockDoor].position;if(WarningBudget==1)warningGuard=blockBest;}
                blockDoor++;blockActor=0;blockBest=null;blockDistance=float.PositiveInfinity;
            }
            assigningWarning=false;
        }
        void OnDestroy() { Unhook(); }
        public void ResetAssignments() { blockers.Clear(); ambushers.Clear(); entrances.Clear();warningGuard=null; nextPlan = 0; scanning=assigningWarning=explicitAmbush=false; }
        public static int BlockLimit(int doors) => Mathf.Min(3, doors / 2, Mathf.Max(0, doors - 1));
        bool Reachable(Vector3 from, Vector3 to)
        {
            return NavMesh.SamplePosition(from, out var start, 2, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(start.position, to, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }
        public void FindEntrances()
        {
            entrances.Clear(); var player = director.FindPlayer(); var graph = ShelterGraphReference.Graph;
            if (player == null || graph == null) return;
            foreach (var node in graph.Nodes)
            {
                if ((node.Kind != RoomNodeKind.Door && node.Kind != RoomNodeKind.Exit) || node.FloorID > 1 ||
                    !node.RoomID.EndsWith("/approach") || ShelterDetector.AtFeet(node.WorldPosition)==Shelter.Indoor ||
                    Mathf.Abs(node.WorldPosition.y - player.position.y) > 2 ||
                    Vector3.ProjectOnPlane(node.WorldPosition - player.position, Vector3.up).sqrMagnitude > 1600) continue;
                // Only an entrance joined to shelter, never an arbitrary internal corridor door.
                bool shelter = false;
                foreach (var edge in node.Neighbors)
                    if (edge.Neighbor >= 0 && edge.Neighbor < graph.Nodes.Length &&
                        ShelterDetector.AtFeet(graph.Nodes[edge.Neighbor].WorldPosition) != Shelter.Outdoor) { shelter = true; break; }
                if (!shelter || !NavMesh.SamplePosition(node.WorldPosition, out var hit, 1.5f, NavMesh.AllAreas) || !Reachable(player.position, hit.position)) continue;
                if (entrances.Exists(e => Vector3.Distance(e.position, hit.position) < 3)) continue;
                entrances.Add(new Entrance { id = node.RoomID, position = hit.position });
            }
            entrances.Sort((a,b) => (a.position-player.position).sqrMagnitude.CompareTo((b.position-player.position).sqrMagnitude));
            FreeEntrance = entrances.Count > 0 ? entrances[0].position : player.position;
        }
        bool Available(EnemyInstance e, int tier) => e != null && e.Alive && e.scaling.aiTier >= tier && e.Brain != null &&
            e.Brain.enabled && e.Brain.State == MinionState.Chase && !director.Holds(e) && !e.Motor.Held && !(e.GetComponent<EnemyAbilityRunner>()?.Busy ?? false);
        public void PlanWarning()
        {
            blockers.Clear(); ambushers.Clear(); FindEntrances();
            var player = director.FindPlayer();
            if (player == null || ShelterDetector.AtFeet(player.position) != Shelter.Outdoor) return;
            int limit = WarningBudget;
            // Keep the nearest reachable opening entirely free. Other idle minions yield its 4 m approach.
            for (int i = 1; i <= limit; i++)
            {
                EnemyInstance best = null; float distance = float.PositiveInfinity;
                foreach (var e in director.Active)
                    if (Available(e,4) && DoorCandidate(e) && !blockers.ContainsKey(e) && Reachable(e.transform.position, entrances[i].position))
                    { float d = DoorDistance(e,entrances[i].position); if(d<distance){distance=d;best=e;} }
                if (best != null) {blockers[best] = entrances[i].position;if(limit==1)warningGuard=best;}
            }
        }
        public void PlanAmbush()
        {
            explicitAmbush=true;
            ambushers.Clear(); FindEntrances(); var player = director.FindPlayer();
            if (player == null) return;
            var movement = player.GetComponent<CampusExplorer>();
            Vector3 direction = movement != null && movement.PlanarVelocity.sqrMagnitude > .5f ? movement.PlanarVelocity.normalized : player.forward;
            foreach (var entrance in entrances)
            {
                Vector3 delta = Vector3.ProjectOnPlane(entrance.position-player.position,Vector3.up);
                if (delta.magnitude < 5 || Vector3.Dot(delta.normalized,direction) < .15f) continue;
                EnemyInstance best = null; float distance = float.PositiveInfinity;
                foreach (var e in director.Active)
                    if (Available(e,3) && !ambushers.ContainsKey(e) && !e.Brain.HearingPursuit && Reachable(e.transform.position, entrance.position))
                    { float d=(e.transform.position-entrance.position).sqrMagnitude;if(d<distance){distance=d;best=e;} }
                if (best != null) ambushers[best] = entrance.position;
                if (ambushers.Count >= 2) break;
            }
        }
        public bool TryPosition(EnemyInstance e, out Vector3 point, out bool guard)
        {
            guard = WarningActive;
            if (guard && blockers.TryGetValue(e,out point)) return true;
            if(!guard&&!explicitAmbush&&director.TierFor(e.scaling.aiTier).phasedEncirclement){point=default;return false;}
            if (!guard && (!director.Squad.TryGet(e,out var assignment)||assignment.role==SquadRole.Ambusher) && !e.Brain.HearingPursuit && ambushers.TryGetValue(e,out point) &&
                Vector3.Distance(director.PlayerTransform.position,point)>3) return true;
            point=default;return false;
        }
        public bool YieldEntrance(EnemyInstance e)
        {
            if (!WarningActive || entrances.Count==0 || blockers.ContainsKey(e)) return false;
            if (Vector3.ProjectOnPlane(e.transform.position-FreeEntrance,Vector3.up).sqrMagnitude > 16) return false;
            Vector3 away=Vector3.ProjectOnPlane(e.transform.position-FreeEntrance,Vector3.up).normalized;
            if(away.sqrMagnitude<.1f)away=Vector3.right;
            if(NavMesh.SamplePosition(FreeEntrance+away*5,out var hit,1.5f,NavMesh.AllAreas))e.Motor.MoveTo(hit.position);
            return true;
        }
    }
}
