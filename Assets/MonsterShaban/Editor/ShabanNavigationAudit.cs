using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CampusRift;
using CampusRift.Monsters;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class ShabanNavigationAudit
{
    [Serializable] public sealed class DoorRoute
    {
        public string name;
        public int side;
        public Vector3 target;
        public Vector3 sample;
        public bool sampled, complete;
        public string status;
        public float length;
    }
    [Serializable] public sealed class Report
    {
        public int complete, disconnected, missing, vertices, viaRoomGraph;
        public List<DoorRoute> routes = new List<DoorRoute>();
    }

    [MenuItem("Campus Rift/Shaban/Audit Campus Routes")]
    public static void AuditMenu() { Debug.Log(Audit()); }
    public static string Audit()
    {
        var monster = UnityEngine.Object.FindAnyObjectByType<MonsterBrain>();
        var report = new Report { vertices = NavMesh.CalculateTriangulation().vertices.Length };
        var landmarks = new List<SearchLandmark>();
        var path = new NavMeshPath();
        foreach (var door in UnityEngine.Object.FindObjectsByType<CampusAutomaticDoor>().OrderBy(d => d.name))
        {
            var match = Regex.Match(door.name, @"^(?:Block_)?([A-Z])(?:_|\b)");
            string building = match.Success ? match.Groups[1].Value : door.name.Replace("Entrance ", "");
            var floorMatch = Regex.Match(door.name, @"_F(\d+)_");
            int floor = floorMatch.Success ? int.Parse(floorMatch.Groups[1].Value) : -1;
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 target = door.doorway.center + door.normal * (side * 1.2f);
                target.y = door.doorway.min.y + 0.15f;
                NavMeshHit hit;
                bool sampled = NavMesh.SamplePosition(target, out hit, 0.9f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - target.y) < 0.65f;
                // A door can open onto a stair flight about a metre below/above its threshold (joined by a NavMesh link).
                for (int k = 0; !sampled && k < 2; k++)
                {
                    Vector3 step = target + Vector3.up * (k == 0 ? -1.1f : 1.1f);
                    sampled = NavMesh.SamplePosition(step, out hit, 0.5f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - step.y) < 0.4f;
                }
                bool complete = sampled && NavMesh.CalculatePath(monster.transform.position, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
                string status = sampled ? path.status.ToString() : "NoSurface";
                if (sampled && !complete && monster.GetComponent<MonsterNavigation>().roomGraph != null)
                {
                    var routePoints = new List<Vector3>();
                    complete = RoomPathfinder.Find(monster.GetComponent<MonsterNavigation>().roomGraph, monster.transform.position, hit.position, routePoints);
                    if (complete) { report.viaRoomGraph++; status = "RoomGraph"; }
                }
                float length = 0;
                if (sampled) for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                var route = new DoorRoute { name = door.name, side = side, target = target, sample = sampled ? hit.position : target, sampled = sampled,
                    complete = complete, status = status, length = length };
                report.routes.Add(route);
                if (complete) report.complete++; else if (sampled) report.disconnected++; else report.missing++;
                if (sampled) landmarks.Add(new SearchLandmark { RoomID = door.name + (side > 0 ? "/inside" : "/approach"), BuildingID = building,
                    FloorID = floor, Kind = door.name.Contains("Stair") ? "Stair" : door.name.StartsWith("Entrance") ? "Entrance" : "Door", Position = hit.position });
            }
        }
        if (!Application.isPlaying)
        {
            monster.GetComponent<MonsterSearch>().landmarks = landmarks.ToArray();
            EditorUtility.SetDirty(monster.GetComponent<MonsterSearch>());
            EditorSceneManager.MarkSceneDirty(monster.gameObject.scene);
            Directory.CreateDirectory("Assets/MonsterShaban/Validation");
            File.WriteAllText("Assets/MonsterShaban/Validation/NavigationAudit.json", JsonUtility.ToJson(report, true));
            AssetDatabase.ImportAsset("Assets/MonsterShaban/Validation/NavigationAudit.json");
        }
        return $"Routes: complete={report.complete} (graph={report.viaRoomGraph}), disconnected={report.disconnected}, missing={report.missing}; vertices={report.vertices}";
    }

    [MenuItem("Campus Rift/Shaban/Rebake Campus Navigation")]
    public static void Bake()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before baking.");
        var surface = UnityEngine.Object.FindAnyObjectByType<NavMeshSurface>();
        if (surface == null) throw new InvalidOperationException("Shaban navigation surface is missing.");
        var previous = surface.navMeshData;
        surface.BuildNavMesh();
        var built = surface.navMeshData;
        if (previous != null && AssetDatabase.Contains(previous))
        {
            EditorUtility.CopySerialized(built, previous); EditorUtility.SetDirty(previous);
            surface.RemoveData(); surface.navMeshData = previous; surface.AddData();
            UnityEngine.Object.DestroyImmediate(built);
        }
        else AssetDatabase.CreateAsset(built, "Assets/MonsterShaban/CampusNavMesh.asset");
        Debug.Log(Audit());
        EditorSceneManager.SaveScene(surface.gameObject.scene); AssetDatabase.SaveAssets();
    }
}
