using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRift.Monsters
{
    // Read-only runtime view of the static campus topology, shared by every monster:
    // RoomGraph nodes in compact arrays, a spatial index, travel-cost queries and the
    // elevator landings (the RoomGraph asset has none). Built from level data only; it
    // never holds any information about the player.
    public sealed class CampusNavGraph
    {
        public sealed class Lift
        {
            public CampusElevator Elevator;
            public int[] Landing;   // node per floor, -1 when that lobby has no NavMesh
            public Vector3[] Lobby; // walkable point in front of the landing doors, per floor
            public float[,] Seconds; // door-to-door ride time between floors
            public float Wait;       // expected wait for a car whose position is unknown
        }

        public sealed class Scratch
        {
            internal int[] heapNode;
            internal float[] heapKey;
            internal readonly List<int> query = new List<int>(64);
        }

        public int Count { get; private set; }
        public Vector3[] Position { get; private set; }
        public RoomNodeKind[] Kind { get; private set; }
        public string[] Label { get; private set; }
        // Full adjacency (CSR) for travel costs; a sparse, direction-diverse subset for belief motion.
        public int[] EdgeStart { get; private set; }
        public int[] EdgeTo { get; private set; }
        public float[] EdgeLength { get; private set; }
        public int[] MoveStart { get; private set; }
        public int[] MoveTo { get; private set; }
        public float[] MoveLength { get; private set; }
        public Lift[] Lifts { get; private set; }
        // Inside a room behind a door (RoomGraph "…/inside" nodes): where people hide.
        public bool[] RoomInterior { get; private set; }
        public int[] LiftOfNode { get; private set; }
        public int[] LiftFloorOfNode { get; private set; }

        const float CellSize = 4f, BandHeight = 2f;
        readonly Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>(4096);
        static readonly Dictionary<(RoomGraph, int), CampusNavGraph> cache = new Dictionary<(RoomGraph, int), CampusNavGraph>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => cache.Clear();

        public static CampusNavGraph For(RoomGraph graph, int agentTypeID)
        {
            if (graph == null || graph.Nodes == null || graph.Nodes.Length == 0) return null;
            var key = (graph, agentTypeID);
            if (cache.TryGetValue(key, out var view) && view.StillValid()) return view;
            view = new CampusNavGraph();
            view.Build(graph, agentTypeID);
            cache[key] = view;
            return view;
        }

        bool StillValid()
        {
            foreach (var lift in Lifts) if (lift.Elevator == null) return false; // scene was reloaded
            return Lifts.Length > 0 || Object.FindAnyObjectByType<CampusElevator>() == null;
        }

        void Build(RoomGraph graph, int agentTypeID)
        {
            var nodes = graph.Nodes;
            var positions = new List<Vector3>(nodes.Length + 128);
            var kinds = new List<RoomNodeKind>(nodes.Length + 128);
            var labels = new List<string>(nodes.Length + 128);
            for (int i = 0; i < nodes.Length; i++)
            {
                positions.Add(nodes[i].WorldPosition); kinds.Add(nodes[i].Kind); labels.Add(nodes[i].RoomID);
                Index(i, nodes[i].WorldPosition);
            }
            Position = positions.ToArray(); // graph nodes only, while landings are being linked
            var extra = new List<(int a, int b, float length)>();
            var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = NavMesh.AllAreas };
            var lifts = new List<Lift>();
            var elevators = new List<CampusElevator>(Object.FindObjectsByType<CampusElevator>());
            elevators.Sort((x, y) => string.CompareOrdinal(x.building, y.building));
            var nearby = new List<int>(32);
            var path = new NavMeshPath();
            foreach (var elevator in elevators)
            {
                if (elevator.floors == null || elevator.floors.Length == 0) continue;
                var lift = new Lift { Elevator = elevator, Landing = new int[elevator.FloorCount], Lobby = new Vector3[elevator.FloorCount] };
                for (int f = 0; f < elevator.FloorCount; f++)
                {
                    var landing = elevator.floors[f];
                    Vector3 center = Vector3.zero;
                    foreach (var entrance in landing.entrances) center += entrance;
                    center /= Mathf.Max(1, landing.entrances.Length);
                    center.y = landing.height;
                    Vector3 lobby = center + elevator.outward * 1.4f;
                    lift.Lobby[f] = lobby; lift.Landing[f] = -1;
                    if (!NavMesh.SamplePosition(lobby + Vector3.up * 0.1f, out var hit, 1.2f, filter) || Mathf.Abs(hit.position.y - landing.height) > 0.6f) continue;
                    int node = positions.Count;
                    lift.Lobby[f] = hit.position; lift.Landing[f] = node;
                    positions.Add(hit.position); kinds.Add(RoomNodeKind.ElevatorLanding);
                    labels.Add("Lift " + elevator.building + " F" + (f + 1).ToString("00"));
                    // Straight walkable lines to nearby rooms/doors; a few measured paths for L-shaped lobbies.
                    nearby.Clear(); QueryRadius(hit.position, 14f, 1f, nearby, nodes.Length);
                    nearby.Sort((x, y) => (positions[x] - hit.position).sqrMagnitude.CompareTo((positions[y] - hit.position).sqrMagnitude));
                    int linked = 0;
                    for (int k = 0; k < nearby.Count && k < 24 && linked < 8; k++)
                    {
                        if (NavMesh.Raycast(hit.position, positions[nearby[k]], out _, filter)) continue;
                        extra.Add((node, nearby[k], Vector3.Distance(hit.position, positions[nearby[k]]))); linked++;
                    }
                    for (int k = 0; k < nearby.Count && k < 5 && linked == 0; k++)
                    {
                        if (!NavMesh.CalculatePath(hit.position, positions[nearby[k]], filter, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                        float length = PathLength(path);
                        if (length > Vector3.Distance(hit.position, positions[nearby[k]]) * 3 + 4) continue;
                        extra.Add((node, nearby[k], length));
                    }
                    Index(node, hit.position);
                }
                int floors = elevator.FloorCount;
                lift.Seconds = new float[floors, floors];
                float wait = 0;
                for (int a = 0; a < floors; a++)
                    for (int b = 0; b < floors; b++) { lift.Seconds[a, b] = a == b ? 0f : RideSeconds(elevator, a, b); wait += lift.Seconds[a, b]; }
                lift.Wait = floors > 1 ? wait / (floors * (floors - 1)) : 0f;
                lifts.Add(lift);
            }
            Count = positions.Count;
            Position = positions.ToArray(); Kind = kinds.ToArray(); Label = labels.ToArray();
            Lifts = lifts.ToArray();
            RoomInterior = new bool[Count];
            for (int i = 0; i < Count; i++)
                RoomInterior[i] = Label[i] != null && !Label[i].StartsWith("Entrance") && (Label[i].EndsWith("/inside") || Label[i].Contains("/room_"));
            LiftOfNode = new int[Count]; LiftFloorOfNode = new int[Count];
            for (int i = 0; i < Count; i++) { LiftOfNode[i] = -1; LiftFloorOfNode[i] = -1; }
            for (int l = 0; l < Lifts.Length; l++)
                for (int f = 0; f < Lifts[l].Landing.Length; f++)
                    if (Lifts[l].Landing[f] >= 0) { LiftOfNode[Lifts[l].Landing[f]] = l; LiftFloorOfNode[Lifts[l].Landing[f]] = f; }

            var adjacency = new List<(int to, float length)>[Count];
            for (int i = 0; i < Count; i++) adjacency[i] = new List<(int, float)>();
            for (int i = 0; i < nodes.Length; i++)
                foreach (var edge in nodes[i].Neighbors)
                {
                    if (edge.Neighbor < 0 || edge.Neighbor >= nodes.Length || edge.Neighbor == i) continue;
                    adjacency[i].Add((edge.Neighbor, edge.Distance > 0 ? edge.Distance : Mathf.Max(0.1f, edge.Cost)));
                }
            foreach (var e in extra) { adjacency[e.a].Add((e.b, e.length)); adjacency[e.b].Add((e.a, e.length)); }

            EdgeStart = new int[Count + 1];
            int total = 0;
            for (int i = 0; i < Count; i++) { EdgeStart[i] = total; total += adjacency[i].Count; }
            EdgeStart[Count] = total;
            EdgeTo = new int[total]; EdgeLength = new float[total];
            for (int i = 0, w = 0; i < Count; i++)
                foreach (var e in adjacency[i]) { EdgeTo[w] = e.to; EdgeLength[w] = e.length; w++; }

            // Belief motion: shortest neighbours first, keeping only directions not already covered,
            // so a hypothesis advances along corridors/stairs instead of hopping across the floor.
            var moveStart = new int[Count + 1];
            var moveTo = new List<int>(Count * 8); var moveLength = new List<float>(Count * 8);
            var accepted = new List<Vector3>(12);
            float cone = Mathf.Cos(35f * Mathf.Deg2Rad);
            for (int i = 0; i < Count; i++)
            {
                moveStart[i] = moveTo.Count;
                adjacency[i].Sort((x, y) => x.length.CompareTo(y.length));
                accepted.Clear();
                foreach (var e in adjacency[i])
                {
                    Vector3 d = Position[e.to] - Position[i];
                    if (d.sqrMagnitude < 0.01f) continue;
                    Vector3 dir = d.normalized;
                    bool keep = Kind[e.to] == RoomNodeKind.ElevatorLanding || Kind[i] == RoomNodeKind.ElevatorLanding;
                    if (!keep)
                    {
                        keep = true;
                        foreach (var a in accepted) if (Vector3.Dot(a, dir) > cone) { keep = false; break; }
                    }
                    if (!keep) continue;
                    accepted.Add(dir); moveTo.Add(e.to); moveLength.Add(Mathf.Max(0.3f, e.length));
                    if (accepted.Count >= 10) break;
                }
            }
            moveStart[Count] = moveTo.Count;
            MoveStart = moveStart; MoveTo = moveTo.ToArray(); MoveLength = moveLength.ToArray();
        }

        static float PathLength(NavMeshPath path)
        {
            float length = 0; var corners = path.corners;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            return length;
        }

        static long Key(int x, int y, int z) => ((long)(x + 4096) * 8192 + (y + 4096)) * 8192 + (z + 4096);

        void Index(int node, Vector3 p)
        {
            long key = Key(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.y / BandHeight), Mathf.FloorToInt(p.z / CellSize));
            if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<int>(4);
            list.Add(node);
        }

        // Nodes within a horizontal radius and vertical tolerance (unordered).
        public void QueryRadius(Vector3 p, float radius, float verticalTolerance, List<int> result, int limit = int.MaxValue)
        {
            int x0 = Mathf.FloorToInt((p.x - radius) / CellSize), x1 = Mathf.FloorToInt((p.x + radius) / CellSize);
            int z0 = Mathf.FloorToInt((p.z - radius) / CellSize), z1 = Mathf.FloorToInt((p.z + radius) / CellSize);
            int y0 = Mathf.FloorToInt((p.y - verticalTolerance) / BandHeight), y1 = Mathf.FloorToInt((p.y + verticalTolerance) / BandHeight);
            float r2 = radius * radius;
            for (int x = x0; x <= x1; x++)
                for (int z = z0; z <= z1; z++)
                    for (int y = y0; y <= y1; y++)
                    {
                        if (!cells.TryGetValue(Key(x, y, z), out var list)) continue;
                        foreach (int n in list)
                        {
                            if (n >= limit) continue;
                            Vector3 d = Position[n] - p;
                            if (Mathf.Abs(d.y) > verticalTolerance || d.x * d.x + d.z * d.z > r2) continue;
                            result.Add(n);
                        }
                    }
        }

        public int Nearest(Vector3 p, float verticalTolerance = 1.8f, float maxRadius = 16f)
        {
            int best = -1; float bestSq = maxRadius * maxRadius;
            int cx = Mathf.FloorToInt(p.x / CellSize), cz = Mathf.FloorToInt(p.z / CellSize);
            int y0 = Mathf.FloorToInt((p.y - verticalTolerance) / BandHeight), y1 = Mathf.FloorToInt((p.y + verticalTolerance) / BandHeight);
            int rings = Mathf.CeilToInt(maxRadius / CellSize);
            for (int r = 0; r <= rings; r++)
            {
                if (best >= 0 && (r - 1) * CellSize > Mathf.Sqrt(bestSq)) break;
                for (int x = cx - r; x <= cx + r; x++)
                    for (int z = cz - r; z <= cz + r; z++)
                    {
                        if (Mathf.Abs(x - cx) != r && Mathf.Abs(z - cz) != r) continue;
                        for (int y = y0; y <= y1; y++)
                        {
                            if (!cells.TryGetValue(Key(x, y, z), out var list)) continue;
                            foreach (int n in list)
                            {
                                Vector3 d = Position[n] - p;
                                if (Mathf.Abs(d.y) > verticalTolerance) continue;
                                float sq = d.sqrMagnitude;
                                if (sq < bestSq) { bestSq = sq; best = n; }
                            }
                        }
                    }
            }
            return best;
        }

        // Nearest node that the point reaches in a straight walkable line when possible,
        // so evidence behind a wall is not attached to the room on the other side.
        public int NearestConnected(Vector3 p, int agentTypeID, Scratch scratch, float verticalTolerance = 1.8f, float radius = 12f)
        {
            var list = scratch.query; list.Clear();
            QueryRadius(p, radius, verticalTolerance, list);
            if (list.Count == 0) return Nearest(p, verticalTolerance, radius * 1.5f);
            var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = NavMesh.AllAreas };
            bool onMesh = NavMesh.SamplePosition(p, out var start, 1.2f, filter);
            // Up to six nearest candidates by selection (no allocation; called on every sighting).
            int nearest = -1;
            for (int round = 0; round < 6 && list.Count > 0; round++)
            {
                int bestIndex = 0; float best = float.PositiveInfinity;
                for (int i = 0; i < list.Count; i++)
                {
                    float d = (Position[list[i]] - p).sqrMagnitude;
                    if (d < best) { best = d; bestIndex = i; }
                }
                int node = list[bestIndex];
                if (nearest < 0) nearest = node;
                if (!onMesh || !NavMesh.Raycast(start.position, Position[node], out _, filter)) return onMesh ? node : nearest;
                list[bestIndex] = list[list.Count - 1]; list.RemoveAt(list.Count - 1);
            }
            return nearest;
        }

        public Scratch CreateScratch() => new Scratch { heapNode = new int[EdgeTo.Length + Count + 8], heapKey = new float[EdgeTo.Length + Count + 8] };

        // Door closing + acceleration/cruise/braking + door opening + a moment to step in (seconds).
        public static float RideSeconds(CampusElevator e, int from, int to)
        {
            float h = Mathf.Abs(e.floors[to].height - e.floors[from].height);
            float v = Mathf.Max(0.1f, e.travelSpeed), a = Mathf.Max(0.1f, e.acceleration);
            float travel = h < v * v / a ? 2f * Mathf.Sqrt(h / a) : h / v + v / a;
            return e.doorDuration * 2f + travel + 0.8f;
        }

        // Dijkstra in metres over walkable connections. With liftSpeed > 0, a lift ride is also an edge
        // between landings of the same lift, costed as (expected wait + ride + boarding) at that pace.
        public void TravelCosts(Vector3 from, float[] cost, Scratch scratch, float maxCost = float.PositiveInfinity, float liftSpeed = 0f)
        {
            for (int i = 0; i < Count; i++) cost[i] = float.PositiveInfinity;
            int size = 0;
            var sources = scratch.query; sources.Clear();
            QueryRadius(from, 8f, 1.8f, sources);
            if (sources.Count == 0) { int n = Nearest(from, 2.5f, 24f); if (n >= 0) sources.Add(n); }
            foreach (int s in sources)
            {
                float c = Vector3.Distance(from, Position[s]) * 1.25f;
                if (c < cost[s]) { cost[s] = c; Push(scratch, ref size, s, c); }
            }
            while (size > 0)
            {
                Pop(scratch, ref size, out int node, out float key);
                if (key > cost[node] || key > maxCost) continue;
                for (int e = EdgeStart[node], end = EdgeStart[node + 1]; e < end; e++)
                {
                    int to = EdgeTo[e]; float next = key + EdgeLength[e];
                    if (next >= cost[to]) continue;
                    cost[to] = next;
                    if (size < scratch.heapNode.Length) Push(scratch, ref size, to, next);
                }
                int lift = liftSpeed > 0f ? LiftOfNode[node] : -1;
                if (lift < 0) continue;
                var info = Lifts[lift]; int floor = LiftFloorOfNode[node];
                for (int f = 0; f < info.Landing.Length; f++)
                {
                    int to = info.Landing[f];
                    if (to < 0 || f == floor) continue;
                    float next = key + (info.Wait + info.Seconds[floor, f] + 3f) * liftSpeed;
                    if (next >= cost[to]) continue;
                    cost[to] = next;
                    if (size < scratch.heapNode.Length) Push(scratch, ref size, to, next);
                }
            }
        }

        static void Push(Scratch s, ref int size, int node, float key)
        {
            int i = size++;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (s.heapKey[parent] <= key) break;
                s.heapNode[i] = s.heapNode[parent]; s.heapKey[i] = s.heapKey[parent]; i = parent;
            }
            s.heapNode[i] = node; s.heapKey[i] = key;
        }

        static void Pop(Scratch s, ref int size, out int node, out float key)
        {
            node = s.heapNode[0]; key = s.heapKey[0];
            int lastNode = s.heapNode[--size]; float lastKey = s.heapKey[size];
            int i = 0;
            while (true)
            {
                int child = i * 2 + 1;
                if (child >= size) break;
                if (child + 1 < size && s.heapKey[child + 1] < s.heapKey[child]) child++;
                if (lastKey <= s.heapKey[child]) break;
                s.heapNode[i] = s.heapNode[child]; s.heapKey[i] = s.heapKey[child]; i = child;
            }
            if (size > 0) { s.heapNode[i] = lastNode; s.heapKey[i] = lastKey; }
        }

        public int LiftIndex(CampusElevator elevator)
        {
            for (int i = 0; i < Lifts.Length; i++) if (Lifts[i].Elevator == elevator) return i;
            return -1;
        }

        // Floor of a lift closest to a height (the lift's own landing heights define floors).
        public static int FloorAt(CampusElevator lift, float height)
        {
            int best = 0; float distance = float.PositiveInfinity;
            for (int f = 0; f < lift.FloorCount; f++)
            {
                float d = Mathf.Abs(lift.floors[f].height - height);
                if (d < distance) { distance = d; best = f; }
            }
            return best;
        }
    }
}
