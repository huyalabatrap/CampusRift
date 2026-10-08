using System.Collections.Generic;
using System.Linq;
using CampusRift.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class ShabanRoomGraphBuilder
{
    [MenuItem("Campus Rift/Shaban/Build Room Graph")]
    public static void Build()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        var monster = Object.FindAnyObjectByType<MonsterBrain>();
        var nodes = new List<RoomNode>();
        foreach (var landmark in monster.GetComponent<MonsterSearch>().landmarks)
            Add(nodes, landmark.Position, landmark.RoomID, landmark.BuildingID, landmark.FloorID);
        // Ramp and landing samples connect floors, including floors without authored room doors.
        foreach (var collider in Object.FindObjectsByType<Collider>())
        {
            string name = collider.name;
            if (!collider.enabled || !name.Contains("Stair") || !(name.Contains("Ramp") || name.Contains("Landing") || name.Contains("FlLand") || name.Contains("MidLand"))) continue;
            var meshCollider = collider as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null) continue;
            var mesh = meshCollider.sharedMesh; var vertices = mesh.vertices; var triangles = mesh.triangles;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                var a = collider.transform.TransformPoint(vertices[triangles[t]]);
                var b = collider.transform.TransformPoint(vertices[triangles[t+1]]);
                var c = collider.transform.TransformPoint(vertices[triangles[t+2]]);
                if (Vector3.Cross(b-a,c-a).normalized.y < 0.65f) continue;
                var center = (a+b+c)/3;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(center,out hit,0.65f,NavMesh.AllAreas))
                    Add(nodes,hit.position,name+"_"+t,name.StartsWith("Block_")?name.Substring(6,1):"Campus",-1);
            }
        }
        for (int x=-60;x<=60;x+=10) for(int z=-65;z<=65;z+=10)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(new Vector3(x,0.15f,z),out hit,3,NavMesh.AllAreas) && Mathf.Abs(hit.position.y)<0.8f)
                Add(nodes,hit.position,"Yard_"+x+"_"+z,"Campus",0);
        }
        int observation=AddObservationPoints(nodes,monster.GetComponent<MonsterSearch>().landmarks);
        foreach(var node in nodes)
        {
            if(node.Kind==RoomNodeKind.Observation)continue;
            if(node.RoomID.Contains("Stair"))node.Kind=RoomNodeKind.StairLanding;
            else if(node.RoomID.Contains("Entrance") || node.RoomID.Contains("GndExit") || node.RoomID.Contains("SecDoor"))node.Kind=RoomNodeKind.Exit;
            else if(node.RoomID.Contains("Door"))node.Kind=RoomNodeKind.Door;
            else if(node.BuildingID=="Campus")node.Kind=RoomNodeKind.Outdoor;
            if(node.FloorID<0)
            {
                var floor=monster.GetComponent<MonsterSearch>().landmarks.Where(l=>l.BuildingID==node.BuildingID && l.FloorID>=1)
                    .OrderBy(l=>Mathf.Abs(l.Position.y-node.WorldPosition.y)).FirstOrDefault();
                node.FloorID=floor!=null?floor.FloorID:Mathf.Max(0,Mathf.FloorToInt(node.WorldPosition.y/3.6f));
            }
        }
        var neighbors = nodes.Select(n=>new List<RoomConnection>()).ToArray();
        var path=new NavMeshPath(); int edges=0;
        for(int i=0;i<nodes.Count;i++)
        {
            var candidates=Enumerable.Range(i+1,nodes.Count-i-1).Where(j=>Vector3.Distance(nodes[i].WorldPosition,nodes[j].WorldPosition)<16
                && Mathf.Abs(nodes[i].WorldPosition.y-nodes[j].WorldPosition.y)<4.3f).ToArray();
            foreach(int j in candidates)
            {
                if(!NavMesh.CalculatePath(nodes[i].WorldPosition,nodes[j].WorldPosition,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete)continue;
                float length=0;for(int k=1;k<path.corners.Length;k++)length+=Vector3.Distance(path.corners[k-1],path.corners[k]);
                if(length>40 || length>Vector3.Distance(nodes[i].WorldPosition,nodes[j].WorldPosition)*3+4)continue;
                bool door=nodes[i].RoomID.Split('/')[0]==nodes[j].RoomID.Split('/')[0] && nodes[i].Kind==RoomNodeKind.Door;
                var type=Mathf.Abs(nodes[i].WorldPosition.y-nodes[j].WorldPosition.y)>0.8f?RoomConnectionType.Stair:
                    door?RoomConnectionType.Door:nodes[i].BuildingID=="Campus"?RoomConnectionType.Outdoor:RoomConnectionType.Corridor;
                float multiplier=type==RoomConnectionType.Stair?1.5f:type==RoomConnectionType.Door?1.2f:1f;
                neighbors[i].Add(new RoomConnection{Neighbor=j,Cost=length*multiplier,Distance=length,ConnectionType=type,FloorFrom=nodes[i].FloorID,FloorTo=nodes[j].FloorID});
                neighbors[j].Add(new RoomConnection{Neighbor=i,Cost=length*multiplier,Distance=length,ConnectionType=type,FloorFrom=nodes[j].FloorID,FloorTo=nodes[i].FloorID});edges++;
            }
        }
        for(int i=0;i<nodes.Count;i++)
        {
            nodes[i].Neighbors=neighbors[i].ToArray();
            nodes[i].StairEntry=neighbors[i].Any(e=>e.ConnectionType==RoomConnectionType.Stair && nodes[e.Neighbor].WorldPosition.y>nodes[i].WorldPosition.y+.8f);
            nodes[i].StairExit=neighbors[i].Any(e=>e.ConnectionType==RoomConnectionType.Stair && nodes[e.Neighbor].WorldPosition.y<nodes[i].WorldPosition.y-.8f);
            if(nodes[i].Kind==RoomNodeKind.Zone && neighbors[i].Count>=3)nodes[i].Kind=RoomNodeKind.Junction;
        }
        int pruned=PruneSlivers(nodes);
        if(pruned>0)Debug.Log("Shaban room graph: removed "+pruned+" generated nodes on isolated NavMesh slivers.");
        const string asset="Assets/MonsterShaban/CampusRoomGraph.asset";
        var graph=AssetDatabase.LoadAssetAtPath<RoomGraph>(asset);
        if(graph==null){graph=ScriptableObject.CreateInstance<RoomGraph>();AssetDatabase.CreateAsset(graph,asset);}
        graph.Nodes=nodes.ToArray();EditorUtility.SetDirty(graph);
        monster.GetComponent<MonsterNavigation>().roomGraph=graph;
        EditorSceneManager.SaveScene(monster.gameObject.scene);AssetDatabase.SaveAssets();
        Debug.Log("Shaban room graph: "+nodes.Count+" nodes ("+observation+" observation points), "+edges+" local connections.");
    }

    // Large rooms, halls and wide lobbies only have nodes at their doors, so hypotheses and search
    // goals could not reach their far corners. Sample every indoor walkable spot and add look-out
    // points until each one is visible (monster eye height, glazing transparent) from a node
    // within ObservationReach. Farthest-uncovered-first keeps the points few and deep inside rooms.
    const float ObservationReach=9f, SampleCell=2f;
    static readonly RaycastHit[] sight=new RaycastHit[32];

    static long Key(int x,int y,int z)=>((long)(x+4096)*8192+(y+4096))*8192+(z+4096);
    static long Cell(Vector3 p,float size,float band)=>Key(Mathf.FloorToInt(p.x/size),Mathf.FloorToInt(p.y/band),Mathf.FloorToInt(p.z/size));

    static void Index(Dictionary<long,List<int>> grid,Vector3 p,int value)
    {
        long key=Cell(p,4f,2f);
        if(!grid.TryGetValue(key,out var list))grid[key]=list=new List<int>();
        list.Add(value);
    }

    static IEnumerable<int> Near(Dictionary<long,List<int>> grid,Vector3 p,float radius)
    {
        int r=Mathf.CeilToInt(radius/4f);
        int cx=Mathf.FloorToInt(p.x/4f),cy=Mathf.FloorToInt(p.y/2f),cz=Mathf.FloorToInt(p.z/4f);
        for(int x=cx-r;x<=cx+r;x++)for(int z=cz-r;z<=cz+r;z++)for(int y=cy-1;y<=cy+1;y++)
            if(grid.TryGetValue(Key(x,y,z),out var list))foreach(int i in list)yield return i;
    }

    // Would a monster standing at `from` see a person standing at `to`?
    static bool Sees(Vector3 from,Vector3 to)
    {
        Vector3 d=to-from;
        if(Mathf.Abs(d.y)>1.3f || d.x*d.x+d.z*d.z>ObservationReach*ObservationReach)return false;
        Vector3 eye=from+Vector3.up*1.5f,target=to+Vector3.up*1.1f,delta=target-eye;
        int count=Physics.RaycastNonAlloc(eye,delta.normalized,sight,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        if(count==sight.Length)return false;
        for(int i=0;i<count;i++)
            if(!MonsterPerception.IsGlassName(sight[i].collider.name) && sight[i].collider.GetComponentInParent<CampusRift.CampusAutomaticDoor>()==null)return false;
        return true;
    }

    static bool InLiftCar(Vector3 p,CampusRift.CampusElevator[] lifts)
    {
        foreach(var lift in lifts)
        {
            if(lift.cabinSpaces==null || lift.floors==null)continue;
            foreach(var space in lift.cabinSpaces)
            {
                var flat=new Bounds(new Vector3(space.center.x,0,space.center.z),new Vector3(space.size.x+0.6f,1f,space.size.z+0.6f));
                if(flat.Contains(new Vector3(p.x,0,p.z)))return true;
            }
        }
        return false;
    }

    static int AddObservationPoints(List<RoomNode> nodes,SearchLandmark[] landmarks)
    {
        var lifts=Object.FindObjectsByType<CampusRift.CampusElevator>();
        var triangulation=NavMesh.CalculateTriangulation();
        var v=triangulation.vertices;var index=triangulation.indices;
        var cells=new Dictionary<long,Vector3>();
        for(int t=0;t<index.Length;t+=3)
        {
            Vector3 a=v[index[t]],b=v[index[t+1]],c=v[index[t+2]];
            Vector3 n=Vector3.Cross(b-a,c-a);
            if(n.sqrMagnitude<1e-6f || Mathf.Abs(n.normalized.y)<0.95f)continue; // stairs/ramps have their own nodes
            int steps=Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(n.magnitude*0.5f)),1,40);
            for(int i=0;i<=steps;i++)for(int j=0;i+j<=steps;j++)
            {
                float u=(i+1f/3f)/(steps+1f),w=(j+1f/3f)/(steps+1f);
                if(u+w>1f)continue;
                Vector3 p=a+(b-a)*u+(c-a)*w;
                long key=Cell(p,SampleCell,1f);
                if(!cells.ContainsKey(key))cells[key]=p;
            }
        }
        var candidates=new List<Vector3>();
        foreach(var p in cells.Values)
        {
            if(!Physics.Raycast(p+Vector3.up*0.3f,Vector3.up,6f,~0,QueryTriggerInteraction.Ignore))continue; // outdoors: yard grid
            if(Physics.Raycast(p+Vector3.up*0.4f,Vector3.down,out var ground,1f,~0,QueryTriggerInteraction.Ignore) &&
                (ground.collider.name.Contains("Stair") || ground.collider.name.Contains("Ramp")))continue;
            if(InLiftCar(p,lifts))continue;
            candidates.Add(p);
        }
        var nodeGrid=new Dictionary<long,List<int>>();
        for(int i=0;i<nodes.Count;i++)Index(nodeGrid,nodes[i].WorldPosition,i);
        var covered=new bool[candidates.Count];var depth=new float[candidates.Count];
        for(int i=0;i<candidates.Count;i++)
        {
            float nearest=float.PositiveInfinity;
            foreach(int n in Near(nodeGrid,candidates[i],ObservationReach))
            {
                Vector3 d=nodes[n].WorldPosition-candidates[i];
                if(Mathf.Abs(d.y)<1.3f)nearest=Mathf.Min(nearest,d.magnitude);
                if(!covered[i] && Sees(nodes[n].WorldPosition,candidates[i]))covered[i]=true;
            }
            depth[i]=nearest;
        }
        var order=Enumerable.Range(0,candidates.Count).Where(i=>!covered[i]).OrderByDescending(i=>depth[i]).ToList();
        var candidateGrid=new Dictionary<long,List<int>>();
        foreach(int i in order)Index(candidateGrid,candidates[i],i);
        var path=new NavMeshPath();int added=0;
        foreach(int i in order)
        {
            if(covered[i])continue;
            covered[i]=true;
            Vector3 p=candidates[i];
            // Must join the walkable campus by a direct local route, exactly like graph edges below
            // (not a desk top, roof deck or other island reached only by a long detour).
            bool connected=false;
            foreach(int n in Near(nodeGrid,p,16f).OrderBy(n=>(nodes[n].WorldPosition-p).sqrMagnitude).Take(8))
            {
                Vector3 q=nodes[n].WorldPosition;
                if(Mathf.Abs(q.y-p.y)>=4.3f || Vector3.Distance(p,q)>=16f)continue;
                if(!NavMesh.CalculatePath(p,q,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete)continue;
                float length=0;for(int k=1;k<path.corners.Length;k++)length+=Vector3.Distance(path.corners[k-1],path.corners[k]);
                if(length>40 || length>Vector3.Distance(p,q)*3+4)continue;
                connected=true;break;
            }
            if(!connected)continue;
            // Named after the room door it looks back to (".../room" marks a hiding spot inside a room).
            int door=Near(nodeGrid,p,15f).Where(n=>nodes[n].RoomID.EndsWith("/inside") && Sees(p,nodes[n].WorldPosition))
                .OrderBy(n=>(nodes[n].WorldPosition-p).sqrMagnitude).DefaultIfEmpty(-1).First();
            var anchor=door>=0?nodes[door]:nodes[Near(nodeGrid,p,30f).OrderBy(n=>(nodes[n].WorldPosition-p).sqrMagnitude).First()];
            string room=door>=0?anchor.RoomID.Substring(0,anchor.RoomID.Length-"/inside".Length)+"/room":anchor.RoomID.Split('/')[0]+"/obs";
            nodes.Add(new RoomNode{WorldPosition=p,RoomID=room+"_"+added,BuildingID=anchor.BuildingID,FloorID=anchor.FloorID,Kind=RoomNodeKind.Observation});
            Index(nodeGrid,p,nodes.Count-1);added++;
            foreach(int k in Near(candidateGrid,p,ObservationReach))if(!covered[k] && Sees(p,candidates[k]))covered[k]=true;
        }
        Debug.Log($"Observation points: {cells.Count} samples, {candidates.Count} indoor, {order.Count} unseen from existing nodes, {added} points added.");
        return added;
    }
    // Yard-grid samples and observation points can land on a NavMesh sliver along a wall footing.
    // Drop tiny components made only of such generated nodes (authored door/stair nodes are kept).
    static int PruneSlivers(List<RoomNode> nodes)
    {
        int count=nodes.Count;var component=Enumerable.Repeat(-1,count).ToArray();var sizes=new List<int>();
        for(int i=0;i<count;i++)
        {
            if(component[i]>=0)continue;
            var queue=new Queue<int>();queue.Enqueue(i);component[i]=sizes.Count;int size=0;
            while(queue.Count>0){int x=queue.Dequeue();size++;foreach(var e in nodes[x].Neighbors)if(component[e.Neighbor]<0){component[e.Neighbor]=sizes.Count;queue.Enqueue(e.Neighbor);}}
            sizes.Add(size);
        }
        var generated=new bool[sizes.Count];
        for(int c=0;c<sizes.Count;c++)generated[c]=sizes[c]<10;
        for(int i=0;i<count;i++)
            if(!(nodes[i].RoomID.StartsWith("Yard_") && !nodes[i].RoomID.Contains("/")) && nodes[i].Kind!=RoomNodeKind.Observation)generated[component[i]]=false;
        var map=new int[count];var kept=new List<RoomNode>(count);
        for(int i=0;i<count;i++){map[i]=generated[component[i]]?-1:kept.Count;if(map[i]>=0)kept.Add(nodes[i]);}
        int removed=count-kept.Count;
        if(removed==0)return 0;
        foreach(var node in kept)
        {
            node.Neighbors=node.Neighbors.Where(e=>map[e.Neighbor]>=0).Select(e=>{e.Neighbor=map[e.Neighbor];return e;}).ToArray();
        }
        nodes.Clear();nodes.AddRange(kept);
        return removed;
    }

    static void Add(List<RoomNode> nodes,Vector3 p,string id,string building,int floor)
    {
        if(nodes.Any(n=>(n.WorldPosition-p).sqrMagnitude<2.25f))return;
        nodes.Add(new RoomNode{WorldPosition=p,RoomID=id,BuildingID=building,FloorID=floor});
    }
}
