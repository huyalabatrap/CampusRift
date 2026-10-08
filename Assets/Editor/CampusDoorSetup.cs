using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CampusRift;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CampusDoorSetup
{
    const string Folder = "Assets/AutomaticDoors";
    static Mesh container;

    [MenuItem("Campus Rift/Set Up Automatic Doors")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (GameObject.Find("Campus Automatic Doors") != null) throw new InvalidOperationException("Automatic doors are already configured.");
        var scenery = GameObject.Find("Comic_Vibrant_Elevator_System_T77");
        var player = UnityEngine.Object.FindAnyObjectByType<CampusExplorer>();
        if (scenery == null || player == null) throw new InvalidOperationException("Campus and player are required.");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "AutomaticDoors");
        container = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/DoorMeshes.asset");
        var all = scenery.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name, t => t.gameObject);
        var root = new GameObject("Campus Automatic Doors");
        Undo.RegisterCreatedObjectUndo(root, "Set up automatic doors");
        int entrances = 0, interior = 0, stairs = 0;
        foreach (string block in new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "T", "V", "X" })
        {
            var marker = all["P0_" + block + "_EntryClearance"];
            var pane = all["Block_" + block + "_Glass_Door_Pane"].GetComponent<Renderer>();
            Vector3 center = marker.transform.TransformPoint(marker.GetComponent<MeshFilter>().sharedMesh.bounds.center);
            center.y = 1.23f;
            center.z = pane.bounds.center.z;
            var door = NewDoor(root.transform, "Entrance " + block, player, new Bounds(center, new Vector3(2.16f, 2.42f, 0.08f)), Vector3.back);
            door.openingDistance = 2f;
            door.keepingOpenDistance = 2.6f;
            var leaves = new List<AutomaticDoorLeaf>();
            for (int side = -1; side <= 1; side += 2)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = side < 0 ? "Glass Left" : "Glass Right";
                leaf.transform.SetParent(door.transform, false);
                leaf.transform.position = center + Vector3.right * (side * 0.54f);
                leaf.transform.localScale = new Vector3(1.07f, 2.42f, 0.055f);
                leaf.GetComponent<Renderer>().sharedMaterial = pane.sharedMaterial;
                leaf.GetComponent<Collider>().contactOffset = 0.005f;
                leaves.Add(new AutomaticDoorLeaf { transform = leaf.transform, closedPosition = leaf.transform.localPosition,
                    closedRotation = leaf.transform.localRotation, slideOffset = Vector3.right * (side * 1.12f) });
            }
            door.leaves = leaves.ToArray();
            entrances++;
        }

        foreach (var source in all.Values)
        {
            if (!source.activeInHierarchy || source.GetComponent<MeshFilter>() == null) continue;
            if (Regex.IsMatch(source.name, @"^[A-Z]_F\d+_M\d+_Doors$"))
            {
                var parts = GetParts(source);
                var panels = parts.bounds.Where(p => p.Value.size.y > 1.5f && Mathf.Min(p.Value.size.x, p.Value.size.z) < 0.25f
                    && Mathf.Max(p.Value.size.x, p.Value.size.z) > 0.55f).OrderBy(p => p.Key).ToArray();
                if (panels.Length == 0) continue;
                var assignment = new Dictionary<int, int>();
                foreach (var part in parts.bounds)
                {
                    int closest = 0;
                    float best = float.MaxValue;
                    for (int i = 0; i < panels.Length; i++)
                    {
                        float distance = (panels[i].Value.center - part.Value.center).sqrMagnitude;
                        if (distance < best) { best = distance; closest = i; }
                    }
                    assignment[part.Key] = closest;
                }
                for (int i = 0; i < panels.Length; i++)
                {
                    var bounds = panels[i].Value;
                    Vector3 tangent = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
                    Vector3 normal = Vector3.Cross(Vector3.up, tangent);
                    var door = NewDoor(root.transform, source.name + " " + (i + 1), player, bounds, normal);
                    Vector3 hinge = bounds.center - tangent * (Vector3.Dot(bounds.size, tangent) * 0.5f);
                    hinge.y = bounds.min.y;
                    var mesh = ExtractMesh(source, parts, assignment, i, hinge);
                    var leaf = MakeLeaf(source.name + " Leaf", door.transform, mesh, source.GetComponent<Renderer>().sharedMaterials, hinge);
                    var collider = leaf.AddComponent<BoxCollider>();
                    collider.center = bounds.center - hinge; collider.size = bounds.size; collider.contactOffset = 0.005f;
                    door.leaves = new[] { new AutomaticDoorLeaf { transform = leaf.transform, closedPosition = leaf.transform.localPosition,
                        closedRotation = Quaternion.identity, swingAngle = 95f } };
                    interior++;
                }
                HideSource(source);
            }
            else if (Regex.IsMatch(source.name, @"_FireDoor_Leaf_\d+$") || source.name.EndsWith("_GndExit_Leaf")
                || Regex.IsMatch(source.name, @"^A7N_Door_[EW]_\d+$"))
            {
                string frameName = source.name.Replace("_Leaf", "_Frame");
                if (source.name.StartsWith("A7N_Door_")) frameName = source.name.Replace("A7N_Door_E_", "A7N_DoorE_Frame_").Replace("A7N_Door_W_", "A7N_DoorW_Frame_");
                GameObject frame;
                if (!all.TryGetValue(frameName, out frame)) continue;
                Bounds frameBounds = frame.GetComponent<Renderer>().bounds;
                Bounds leafBounds = source.GetComponent<Renderer>().bounds;
                Vector3 closedCenter = frameBounds.center;
                closedCenter.y = frameBounds.min.y + leafBounds.extents.y;
                Vector3 shift = closedCenter - leafBounds.center;
                leafBounds.center = closedCenter;
                Vector3 tangent = leafBounds.size.x > leafBounds.size.z ? Vector3.right : Vector3.forward;
                Vector3 normal = Vector3.Cross(Vector3.up, tangent);
                var door = NewDoor(root.transform, source.name.Replace("_Leaf", ""), player, leafBounds, normal);
                Vector3 hinge = leafBounds.center - tangent * (Vector3.Dot(leafBounds.size, tangent) * 0.5f);
                hinge.y = leafBounds.min.y;
                var pivot = new GameObject("Hinged Leaf");
                pivot.transform.SetParent(door.transform, false); pivot.transform.position = hinge;
                // A moving leaf must not be baked into the NavMesh as a wall.
                var excluded = pivot.AddComponent<NavMeshModifier>(); excluded.ignoreFromBuild = true; excluded.applyToChildren = true;
                CloneVisual(source, pivot.transform, shift);
                GameObject handle;
                if (all.TryGetValue(source.name.Replace("_Leaf", "_PanicBar"), out handle))
                { CloneVisual(handle, pivot.transform, shift); HideSource(handle); }
                var collider = pivot.AddComponent<BoxCollider>();
                collider.center = leafBounds.center - hinge; collider.size = leafBounds.size; collider.contactOffset = 0.005f;
                door.leaves = new[] { new AutomaticDoorLeaf { transform = pivot.transform, closedPosition = pivot.transform.localPosition,
                    closedRotation = Quaternion.identity, swingAngle = 100f } };
                HideSource(source);
                stairs++;
            }
        }
        AssetDatabase.SaveAssets();
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        File.WriteAllText(Folder + "/SetupReport.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { entrances, interior, stairs,
            total = entrances + interior + stairs }, Newtonsoft.Json.Formatting.Indented));
        AssetDatabase.ImportAsset(Folder + "/SetupReport.json");
        Debug.Log("Automatic doors ready: " + entrances + " entrances, " + interior + " interior doors, " + stairs + " stair/hall doors.");
    }

    static CampusAutomaticDoor NewDoor(Transform parent, string name, CampusExplorer player, Bounds doorway, Vector3 normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var door = go.AddComponent<CampusAutomaticDoor>();
        door.player = player; door.doorway = doorway; door.normal = normal;
        return door;
    }

    static void HideSource(GameObject source)
    {
        var renderer = source.GetComponent<Renderer>();
        if (renderer != null) { Undo.RecordObject(renderer, "Use animated door"); renderer.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
        foreach (var collider in source.GetComponents<Collider>())
        { Undo.RecordObject(collider, "Use animated door collision"); collider.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(collider); }
    }

    static void CloneVisual(GameObject source, Transform parent, Vector3 shift)
    {
        var go = new GameObject(source.name + " Visual");
        go.transform.SetPositionAndRotation(source.transform.position + shift, source.transform.rotation);
        go.transform.localScale = source.transform.lossyScale;
        go.transform.SetParent(parent, true);
        go.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
    }

    static GameObject MakeLeaf(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 hinge)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false); go.transform.position = hinge;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = materials;
        return go;
    }

    sealed class Parts
    {
        public Mesh mesh;
        public Vector3[] world;
        public int[] group;
        public Dictionary<int, Bounds> bounds;
    }

    static Parts GetParts(GameObject source)
    {
        Mesh mesh = source.GetComponent<MeshFilter>().sharedMesh;
        var world = mesh.vertices.Select(v => source.transform.TransformPoint(v)).ToArray();
        var parent = Enumerable.Range(0, world.Length).ToArray();
        Func<int, int> find = i => { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; };
        Action<int, int> union = (a, b) => parent[find(a)] = find(b);
        var welded = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < world.Length; i++)
        {
            var key = Vector3Int.RoundToInt(world[i] * 10000f);
            int previous;
            if (welded.TryGetValue(key, out previous)) union(i, previous); else welded.Add(key, i);
        }
        var triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3) { union(triangles[i], triangles[i + 1]); union(triangles[i], triangles[i + 2]); }
        var bounds = new Dictionary<int, Bounds>();
        for (int i = 0; i < world.Length; i++)
        {
            parent[i] = find(i); Bounds b;
            if (!bounds.TryGetValue(parent[i], out b)) b = new Bounds(world[i], Vector3.zero); else b.Encapsulate(world[i]);
            bounds[parent[i]] = b;
        }
        return new Parts { mesh = mesh, world = world, group = parent, bounds = bounds };
    }

    static Mesh ExtractMesh(GameObject source, Parts data, Dictionary<int, int> assignment, int group, Vector3 origin)
    {
        var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>();
        var submeshes = new List<int[]>();
        var sourceNormals = data.mesh.normals; var sourceUV = data.mesh.uv;
        Matrix4x4 matrix = source.transform.localToWorldMatrix.inverse.transpose;
        for (int s = 0; s < data.mesh.subMeshCount; s++)
        {
            var indices = new List<int>(); var triangles = data.mesh.GetTriangles(s);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                if (assignment[data.group[triangles[t]]] != group) continue;
                for (int j = 0; j < 3; j++)
                {
                    int i = triangles[t + j]; indices.Add(vertices.Count);
                    vertices.Add(data.world[i] - origin);
                    normals.Add(sourceNormals.Length > i ? matrix.MultiplyVector(sourceNormals[i]).normalized : Vector3.up);
                    uv.Add(sourceUV.Length > i ? sourceUV[i] : Vector2.zero);
                }
            }
            submeshes.Add(indices.ToArray());
        }
        var mesh = new Mesh { name = source.name + "_Leaf_" + group };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv);
        mesh.subMeshCount = submeshes.Count;
        for (int s = 0; s < submeshes.Count; s++) mesh.SetTriangles(submeshes[s], s);
        mesh.RecalculateBounds();
        if (container == null) { AssetDatabase.CreateAsset(mesh, Folder + "/DoorMeshes.asset"); container = mesh; }
        else AssetDatabase.AddObjectToAsset(mesh, container);
        return mesh;
    }
}
