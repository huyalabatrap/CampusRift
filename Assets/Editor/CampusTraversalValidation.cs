using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CampusRift;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.Collections;
using UnityEditor;

public static class CampusTraversalValidation
{
    [Serializable] public sealed class Flight
    {
        public string source;
        public Vector3 top, bottom;
    }
    [Serializable] public sealed class Trial
    {
        public string source, direction;
        public Vector3 start, target, actual;
        public bool pass;
        public string[] blockers;
    }
    [Serializable] public sealed class Report
    { public int passed, failed; public List<Trial> trials = new List<Trial>(); }

    public static List<Flight> Flights()
    {
        var result = new List<Flight>();
        foreach (var collider in Object.FindObjectsByType<MeshCollider>())
        {
            if (!collider.enabled || !collider.name.Contains("Stair") || !collider.name.Contains("Ramp") || collider.sharedMesh == null) continue;
            var mesh = collider.sharedMesh; ReadMesh(mesh, out var vertices, out var indices);
            var groups = new Dictionary<string,List<Vector3>>();
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=collider.transform.TransformPoint(vertices[indices[i]]);
                var b=collider.transform.TransformPoint(vertices[indices[i+1]]);
                var c=collider.transform.TransformPoint(vertices[indices[i+2]]);
                var normal=Vector3.Cross(b-a,c-a).normalized;
                float low=Mathf.Min(a.y,b.y,c.y),high=Mathf.Max(a.y,b.y,c.y);
                if(normal.y<0.45f || normal.y>0.98f || high-low<0.6f)continue;
                string key=$"{low:F2}/{high:F2}/{normal.x:F2}/{normal.z:F2}";
                if(!groups.TryGetValue(key,out var points)){points=new List<Vector3>();groups[key]=points;}
                foreach(var p in new[]{a,b,c})if(!points.Any(v=>(v-p).sqrMagnitude<0.00001f))points.Add(p);
            }
            foreach(var points in groups.Values)
            {
                float low=points.Min(p=>p.y),high=points.Max(p=>p.y);
                var lows=points.Where(p=>p.y<low+0.02f).ToArray();var highs=points.Where(p=>p.y>high-0.02f).ToArray();
                result.Add(new Flight {source=collider.name,top=highs.Aggregate(Vector3.zero,(sum,p)=>sum+p)/highs.Length,
                    bottom=lows.Aggregate(Vector3.zero,(sum,p)=>sum+p)/lows.Length});
            }
        }
        return result.OrderBy(f=>f.source).ThenBy(f=>f.bottom.y).ToList();
    }

    // Editor validation reads the same stored geometry without enabling runtime Read/Write
    // or replacing colliders. Unity's Editor snapshot API supports non-readable meshes.
    static void ReadMesh(Mesh mesh, out Vector3[] vertices, out int[] indices)
    {
        if(mesh.isReadable){vertices=mesh.vertices;indices=mesh.triangles;return;}
        using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(mesh))
        {
            var data=snapshot[0];
            using(var points=new NativeArray<Vector3>(data.vertexCount,Allocator.Temp))
            {data.GetVertices(points);vertices=points.ToArray();}
            var triangles=new List<int>();
            for(int submesh=0;submesh<data.subMeshCount;submesh++)
                using(var parts=new NativeArray<int>(data.GetSubMesh(submesh).indexCount,Allocator.Temp))
                {data.GetIndices(parts,submesh,true);triangles.AddRange(parts.ToArray());}
            indices=triangles.ToArray();
        }
    }

    public static string RunStairs(string label,int offset=0,int count=1000,bool allFlights=false,bool sprint=false,bool backward=false)
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run the validation in Play Mode.");
        var player=Object.FindAnyObjectByType<CampusExplorer>();var cc=player.GetComponent<CharacterController>();
        player.enabled=false;var monster=GameObject.Find("Monster_Shaban");if(monster!=null)monster.SetActive(false);
        var probe=player.GetComponent<CampusTraversalProbe>();if(probe==null)probe=player.gameObject.AddComponent<CampusTraversalProbe>();
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var move=(Action<Vector2,bool,bool,float>)Delegate.CreateDelegate(typeof(Action<Vector2,bool,bool,float>),player,typeof(CampusExplorer).GetMethod("MoveCharacter",flags));
        var planar=typeof(CampusExplorer).GetField("planarVelocity",flags);var vertical=typeof(CampusExplorer).GetField("verticalVelocity",flags);
        typeof(CampusExplorer).GetField("yaw",flags).SetValue(player,0f);
        var report=new Report();var doors=Object.FindObjectsByType<CampusAutomaticDoor>();
        var flights=Flights();
        var selected=(allFlights?flights:flights.GroupBy(f=>System.Text.RegularExpressions.Regex.Replace(f.source,@"_Ramp.*$",""))
            .SelectMany(g=>new[]{g.First(),g.OrderBy(f=>Mathf.Abs(f.bottom.y-7)).First(),g.Last()}).Distinct()).Skip(offset).Take(count).ToArray();
        foreach(var flight in selected)
        foreach(bool down in new[]{true,false})
        {
            var direction=Vector3.ProjectOnPlane(flight.bottom-flight.top,Vector3.up).normalized;
            var start=down?flight.top-direction*0.42f:flight.bottom+direction*0.42f;
            var target=down?flight.bottom+direction*0.32f:flight.top-direction*0.32f;
            Vector3 travel=Vector3.ProjectOnPlane(target-start,Vector3.up).normalized;
            typeof(CampusExplorer).GetField("yaw",flags).SetValue(player,backward?Quaternion.LookRotation(-travel).eulerAngles.y:0f);
            Vector2 input=backward?new Vector2(0,-1):new Vector2(travel.x,travel.z);
            cc.enabled=false;player.transform.position=start+Vector3.up*0.06f;cc.enabled=true;
            planar.SetValue(player,Vector3.zero);vertical.SetValue(player,-2f);Physics.SyncTransforms();
            for(int i=0;i<20;i++)move(Vector2.zero,false,false,0.016667f);
            probe.blockers.Clear();float length=Vector3.ProjectOnPlane(target-start,Vector3.up).magnitude;
            bool pass=false;
            for(int i=0;i<240;i++)
            {
                foreach(var door in doors)if((door.doorway.center-player.transform.position).sqrMagnitude<36)door.Tick(0.016667f);
                move(input,sprint,false,0.016667f);Physics.SyncTransforms();
                if(Vector3.Dot(player.transform.position-start,travel)>length-0.12f && Mathf.Abs(player.transform.position.y-target.y)<0.4f){pass=true;break;}
            }
            var trial=new Trial {source=flight.source,direction=down?"down":"up",start=start,target=target,actual=player.transform.position,pass=pass,blockers=probe.blockers.ToArray()};
            report.trials.Add(trial);if(pass)report.passed++;else report.failed++;
        }
        Directory.CreateDirectory("Assets/TraversalFixes");
        File.WriteAllText("Assets/TraversalFixes/"+label+"-Stairs.json",JsonUtility.ToJson(report,true));
        return $"{label}: {report.passed} passed, {report.failed} failed; selected flights {selected.Length}";
    }

    public static string RunDoors(string label)
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run in Play Mode.");
        var player=Object.FindAnyObjectByType<CampusExplorer>();player.enabled=false;
        var cc=player.GetComponent<CharacterController>();var monster=GameObject.Find("Monster_Shaban");if(monster!=null)monster.SetActive(false);
        var probe=player.GetComponent<CampusTraversalProbe>();if(probe==null)probe=player.gameObject.AddComponent<CampusTraversalProbe>();
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var move=(Action<Vector2,bool,bool,float>)Delegate.CreateDelegate(typeof(Action<Vector2,bool,bool,float>),player,typeof(CampusExplorer).GetMethod("MoveCharacter",flags));
        var report=new Report();var doors=Object.FindObjectsByType<CampusAutomaticDoor>();
        foreach(var door in doors)
        foreach(int side in new[]{-1,1})
        {
            var start=door.doorway.center+door.normal*(side*0.85f);start.y=door.doorway.min.y+0.12f;
            var target=door.doorway.center-door.normal*(side*0.85f);target.y=start.y;
            var travel=-door.normal*side;
            cc.enabled=false;player.transform.position=start;cc.enabled=true;
            typeof(CampusExplorer).GetField("planarVelocity",flags).SetValue(player,Vector3.zero);
            typeof(CampusExplorer).GetField("verticalVelocity",flags).SetValue(player,-2f);
            typeof(CampusExplorer).GetField("yaw",flags).SetValue(player,0f);Physics.SyncTransforms();probe.blockers.Clear();bool pass=false;
            for(int i=0;i<150;i++)
            {
                foreach(var d in doors)if((d.doorway.center-player.transform.position).sqrMagnitude<40)d.Tick(1f/60);
                move(new Vector2(travel.x,travel.z),false,false,1f/60);Physics.SyncTransforms();
                if(Vector3.Dot(player.transform.position-start,travel)>1.55f && Mathf.Abs(player.transform.position.y-target.y)<0.65f){pass=true;break;}
            }
            report.trials.Add(new Trial{source=door.name,direction=side.ToString(),start=start,target=target,actual=player.transform.position,pass=pass,blockers=probe.blockers.ToArray()});
            if(pass)report.passed++;else report.failed++;
        }
        File.WriteAllText("Assets/TraversalFixes/"+label+"-Doors.json",JsonUtility.ToJson(report,true));
        return $"Doors: {report.passed} pass, {report.failed} fail";
    }
}
