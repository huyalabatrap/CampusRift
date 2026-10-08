using System.Collections.Generic;
using System.Text;
using CampusRift;
using CampusRift.Monsters;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Closes the gaps in Shaban's NavMesh that the player can cross but the agent cannot:
// - automatic door leaves that were baked as walls (secondary/ground exits added after the bake);
// - stair flights steeper than the agent's slope limit (block C), bridged by NavMesh links;
// - doors that open onto a stair flight about a metre below the threshold (A7N west doors).
// Links are plain scene objects under "Shaban Navigation Links"; rebuilding recreates them.
public static class ShabanNavigationRepair
{
    const string LinksRoot = "Shaban Navigation Links";

    [MenuItem("Campus Rift/Shaban/Repair Navigation Gaps (links + door leaves)")]
    public static void RepairMenu() { Debug.Log(ExcludeDoorLeaves() + "\n" + BuildLinks()); }

    static bool Excluded(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            var modifier = p.GetComponent<NavMeshModifier>();
            if (modifier != null && modifier.ignoreFromBuild && (modifier.applyToChildren || p == t)) return true;
        }
        return false;
    }

    // Moving door leaves must never be baked: a closed leaf would cut the doorway.
    public static string ExcludeDoorLeaves()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        var fixedDoors = new List<string>();
        foreach (var door in Object.FindObjectsByType<CampusAutomaticDoor>())
            foreach (var leaf in door.leaves)
            {
                if (leaf.transform == null || Excluded(leaf.transform)) continue;
                var modifier = Undo.AddComponent<NavMeshModifier>(leaf.transform.gameObject);
                modifier.ignoreFromBuild = true; modifier.applyToChildren = true;
                EditorUtility.SetDirty(modifier);
                fixedDoors.Add(door.name);
            }
        if (fixedDoors.Count > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return "Door leaves excluded from the bake: " + fixedDoors.Count + (fixedDoors.Count > 0 ? " (" + string.Join(", ", fixedDoors) + ")" : "");
    }

    static NavMeshQueryFilter Filter(out int agentType)
    {
        var monster = Object.FindAnyObjectByType<MonsterBrain>();
        agentType = monster.GetComponent<NavMeshAgent>().agentTypeID;
        return new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
    }

    static bool Sample(Vector3 point, float height, float radius, NavMeshQueryFilter filter, out Vector3 result)
    {
        result = point;
        if (!NavMesh.SamplePosition(point, out var hit, radius, filter) || Mathf.Abs(hit.position.y - height) > 0.35f) return false;
        result = hit.position; return true;
    }

    // Already joined by a walkable route that is not a long detour?
    static bool Connected(Vector3 a, Vector3 b, NavMeshQueryFilter filter, NavMeshPath path, float slack)
    {
        if (!NavMesh.CalculatePath(a, b, filter, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        float length = 0; var corners = path.corners;
        for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
        return length <= Vector3.Distance(a, b) * slack + 3f;
    }

    public static string BuildLinks()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        var filter = Filter(out int agentType);
        var old = GameObject.Find(LinksRoot);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(LinksRoot);
        Undo.RegisterCreatedObjectUndo(root, "Build Shaban navigation links");
        var path = new NavMeshPath();
        var report = new StringBuilder();
        int flights = 0, drops = 0;

        // 1) Stair flights with no NavMesh on them (too steep for the agent), whose landings exist.
        foreach (var collider in Object.FindObjectsByType<MeshCollider>())
        {
            if (!collider.enabled || !collider.name.Contains("Stair") || !collider.name.Contains("Ramp")) continue;
            Bounds b = collider.bounds;
            bool alongZ = b.size.z >= b.size.x;
            Vector3 axis = alongZ ? Vector3.forward : Vector3.right;
            float half = (alongZ ? b.extents.z : b.extents.x) - 0.08f;
            if (!SurfaceAt(collider, b.center - axis * half, b, out Vector3 endA) || !SurfaceAt(collider, b.center + axis * half, b, out Vector3 endB)) continue;
            if (Mathf.Abs(endA.y - endB.y) < 0.6f) continue; // not a flight
            Vector3 low = endA.y < endB.y ? endA : endB, high = endA.y < endB.y ? endB : endA;
            Vector3 up = (high - low); up.y = 0; up.Normalize();
            // Walkable already? Then the NavMesh covers the middle of the flight.
            Vector3 middle = (low + high) * 0.5f;
            if (SurfaceAt(collider, middle, b, out Vector3 mid) && NavMesh.SamplePosition(mid, out var onFlight, 0.2f, filter) && Mathf.Abs(onFlight.position.y - mid.y) < 0.2f) continue;
            if (!Sample(low - up * 0.45f, low.y, 0.5f, filter, out Vector3 bottom) || !Sample(high + up * 0.45f, high.y, 0.5f, filter, out Vector3 top)) continue;
            if (Connected(bottom, top, filter, path, 3f)) continue;
            float width = Mathf.Clamp((alongZ ? b.size.x : b.size.z) - 0.3f, 0.6f, 1.4f);
            AddLink(root.transform, "Flight " + collider.name, bottom, top, width, agentType);
            report.AppendLine($"flight {collider.name}: {bottom:F2} <-> {top:F2}");
            flights++;
        }

        // 2) Doors opening onto a surface about a metre below (or above) the threshold on one side.
        foreach (var door in Object.FindObjectsByType<CampusAutomaticDoor>())
        {
            float threshold = door.doorway.min.y;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 far = door.doorway.center + door.normal * (side * 1.0f); far.y = threshold;
                Vector3 near = door.doorway.center - door.normal * (side * 1.0f); near.y = threshold;
                if (Sample(far, threshold, 0.4f, filter, out _)) continue;                     // level with the door: fine
                if (!Sample(near, threshold, 0.4f, filter, out Vector3 floor)) continue;        // no floor on the other side either
                Vector3 landing = default; bool found = false;
                foreach (float dy in new[] { -1.1f, 1.1f, -0.8f, 0.8f, -1.3f })
                    if (NavMesh.SamplePosition(far + Vector3.up * dy, out var hit, 0.45f, filter) && Mathf.Abs(hit.position.y - (threshold + dy)) < 0.4f &&
                        Mathf.Abs(hit.position.y - threshold) > 0.45f && Mathf.Abs(hit.position.y - threshold) < 1.45f)
                    { landing = hit.position; found = true; break; }
                if (!found || Connected(floor, landing, filter, path, 4f)) continue;
                AddLink(root.transform, "Door drop " + door.name, floor, landing, Mathf.Clamp(Vector3.Dot(door.doorway.size, Abs(Vector3.Cross(Vector3.up, door.normal))) - 0.4f, 0.6f, 1.2f), agentType);
                report.AppendLine($"door {door.name}: {floor:F2} <-> {landing:F2}");
                drops++;
            }
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        return $"Navigation links: {flights} steep flights, {drops} door drops\n{report}";
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static bool SurfaceAt(Collider collider, Vector3 point, Bounds bounds, out Vector3 surface)
    {
        surface = point;
        var ray = new Ray(new Vector3(point.x, bounds.max.y + 0.5f, point.z), Vector3.down);
        if (!collider.Raycast(ray, out var hit, bounds.size.y + 1f)) return false;
        surface = hit.point; return true;
    }

    static void AddLink(Transform root, string name, Vector3 a, Vector3 b, float width, int agentType)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = (a + b) * 0.5f;
        var link = go.AddComponent<NavMeshLink>();
        link.agentTypeID = agentType;
        link.startPoint = a - go.transform.position;
        link.endPoint = b - go.transform.position;
        link.width = width;
        link.bidirectional = true;
        link.autoUpdate = false;
        link.area = 0;
    }
}
