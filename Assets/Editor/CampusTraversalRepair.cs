using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CampusRift;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Repairs the supplied campus geometry without changing its imported source asset.
public static class CampusTraversalRepair
{
    const string Folder = "Assets/Collision/Traversal";
    static PhysicsMaterial Walking => AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Collision/CampusWalking.physicMaterial");

    static void Prepare()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Collision", "Traversal");
    }

    static Mesh SaveMesh(Mesh mesh, string name)
    {
        mesh.name = name;
        string path = Folder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
        return mesh;
    }

    static void Save()
    {
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Campus Rift/Repair Stair Collision Winding")]
    public static string RepairStairs()
    {
        Prepare(); int fixedCount = 0;
        foreach (var collider in Object.FindObjectsByType<MeshCollider>())
        {
            if (!collider.enabled || !collider.name.Contains("Stair") || !collider.name.Contains("Ramp") || collider.sharedMesh == null) continue;
            var source = collider.sharedMesh; var vertices = source.vertices; var triangles = source.triangles;
            // A closed ramp with inward-facing triangles has negative signed volume.
            // Subtracting the centre avoids loss of precision for distant mesh coordinates.
            Vector3 origin = source.bounds.center; double volume = 0;
            for (int i = 0; i < triangles.Length; i += 3)
                volume += Vector3.Dot(vertices[triangles[i]] - origin,
                    Vector3.Cross(vertices[triangles[i + 1]] - origin, vertices[triangles[i + 2]] - origin)) / 6.0;
            if (volume >= -1e-12) continue;
            var mesh = Object.Instantiate(source);
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var indices = mesh.GetTriangles(s);
                for (int i = 0; i < indices.Length; i += 3) { int swap = indices[i + 1]; indices[i + 1] = indices[i + 2]; indices[i + 2] = swap; }
                mesh.SetTriangles(indices, s);
            }
            mesh.RecalculateNormals();
            Undo.RecordObject(collider, "Repair inward-facing stair collision");
            collider.sharedMesh = SaveMesh(mesh, collider.name + "_OutwardCollision");
            collider.contactOffset = 0.005f; collider.sharedMaterial = Walking;
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            fixedCount++;
        }
        Save(); return "Repaired inward-facing stair colliders: " + fixedCount;
    }

    [MenuItem("Campus Rift/Repair Parking Collision and Vehicle Hulls")]
    public static string RepairParking()
    {
        Prepare(); int added = 0, convexified = 0;
        var campus = GameObject.Find("Comic_Vibrant_Elevator_System_T77");
        foreach (var filter in campus.GetComponentsInChildren<MeshFilter>())
        {
            bool vehicle = Regex.IsMatch(filter.name, @"^T60_(Car_|Supercar_|Moto_)");
            if (filter.name != "T60_Parking_Structure" && !vehicle) continue;
            var existing = filter.GetComponents<Collider>().FirstOrDefault(c => c.enabled && !c.isTrigger);
            var collider = existing as MeshCollider;
            if (existing != null && collider == null) continue;
            if (collider == null)
            {
                collider = Undo.AddComponent<MeshCollider>(filter.gameObject);
                collider.sharedMesh = filter.sharedMesh;
                collider.sharedMaterial = Walking;
                collider.contactOffset = 0.005f;
                added++;
            }
            // Detailed vehicle meshes have open cabins, wheels and handlebars. A
            // non-convex collider lets the CharacterController land inside them.
            // The garage structure must remain non-convex to preserve its rooms.
            if (vehicle && !collider.convex)
            {
                Undo.RecordObject(collider, "Make parked vehicle collision solid");
                collider.convex = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                convexified++;
            }
        }
        Save(); return "Added parking colliders: " + added + "; solid vehicle hulls: " + convexified;
    }

    [MenuItem("Campus Rift/Repair X Side Door Jump Clearance")]
    public static string RepairXSideDoorJumpClearance()
    {
        Prepare();
        var door = Object.FindObjectsByType<CampusAutomaticDoor>().FirstOrDefault(d => d.name == "Block_X_SecDoor_Side Automatic");
        var frame = Object.FindObjectsByType<MeshFilter>().FirstOrDefault(f => f.name == "Block_X_SecDoor_Side_Frame");
        if (door == null || frame == null || frame.sharedMesh == null)
            throw new InvalidOperationException("X side door or its frame is missing.");
        var collider = frame.GetComponent<MeshCollider>();
        if (collider == null) throw new InvalidOperationException("X side door frame has no MeshCollider.");

        Vector3 normal = door.normal.normalized;
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        Bounds opening = door.doorway;
        opening.Expand(new Vector3(Mathf.Abs(normal.x) * 2.3f + Mathf.Abs(tangent.x) * 0.36f,
            0f, Mathf.Abs(normal.z) * 2.3f + Mathf.Abs(tangent.z) * 0.36f));
        Vector3 minimum = opening.min;
        minimum.y = door.doorway.min.y - 0.15f;
        opening.SetMinMax(minimum, opening.max);

        // Clip collision only. Keep the existing visible frame intact.
        var cut = CampusDoorwayRepair.SubtractBox(frame.transform, frame.sharedMesh, opening);
        if (cut == null) throw new InvalidOperationException("X side door frame has no geometry in the clearance volume.");
        var mesh = SaveMesh(cut, "Block_X_SecDoor_Side_Frame_JumpClearance");
        Undo.RecordObject(collider, "Clear X side door jump passage");
        collider.sharedMesh = null;
        collider.sharedMesh = mesh;
        collider.contactOffset = 0.005f;
        collider.sharedMaterial = Walking;
        PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        Save();
        return "Cleared X side door jump passage without changing the visible frame.";
    }

    [MenuItem("Campus Rift/Complete Missing Automatic Doors")]
    public static string AddMissingDoors()
    {
        Prepare(); int count = 0;
        var campus = GameObject.Find("Comic_Vibrant_Elevator_System_T77");
        var root = GameObject.Find("Campus Automatic Doors").transform;
        var player = Object.FindAnyObjectByType<CampusExplorer>();
        foreach (var source in campus.GetComponentsInChildren<MeshRenderer>())
        {
            if (!source.enabled || !Regex.IsMatch(source.name, @"_SecDoor_(Back|Side)_Leaf$|^Block_[GI]_GndExit_Door$")) continue;
            Bounds bounds = source.bounds;
            Vector3 tangent = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
            var go = new GameObject(source.name.Replace("_Leaf", "") + " Automatic");
            Undo.RegisterCreatedObjectUndo(go, "Add automatic secondary entrance");
            go.transform.SetParent(root, false);
            var door = go.AddComponent<CampusAutomaticDoor>();
            door.player = player; door.doorway = bounds; door.normal = Vector3.Cross(Vector3.up, tangent);
            Vector3 hinge = bounds.center - tangent * Vector3.Dot(bounds.extents, tangent); hinge.y = bounds.min.y;
            var pivot = new GameObject("Hinged Leaf"); pivot.transform.SetParent(go.transform, false); pivot.transform.position = hinge;
            // A moving leaf must not be baked into the NavMesh as a wall.
            var excluded = pivot.AddComponent<NavMeshModifier>(); excluded.ignoreFromBuild = true; excluded.applyToChildren = true;
            var visual = new GameObject(source.name + " Visual");
            visual.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            visual.transform.localScale = source.transform.lossyScale; visual.transform.SetParent(pivot.transform, true);
            visual.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            visual.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
            var collider = pivot.AddComponent<BoxCollider>();
            collider.center = bounds.center - hinge; collider.size = bounds.size; collider.contactOffset = 0.005f; collider.sharedMaterial = Walking;
            door.leaves = new[] { new AutomaticDoorLeaf { transform = pivot.transform, closedPosition = pivot.transform.localPosition,
                closedRotation = pivot.transform.localRotation, swingAngle = 100f } };
            Undo.RecordObject(source, "Replace static door with moving leaf"); source.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            foreach (var old in source.GetComponents<Collider>()) { Undo.RecordObject(old, "Use moving door collider"); old.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(old); }
            count++;
        }
        Save(); return "Added automatic doors: " + count;
    }

    // Only geometry intersecting an actual doorway is clipped. Door leaves remain solid.
    [MenuItem("Campus Rift/Clear Overlapping Stair Passages")]
    public static string ClearStairPassages()
    {
        Prepare(); var repaired = new HashSet<string>();
        // These imported walls / balcony rails cross an existing stair flight or landing.
        // Remove just the pedestrian clearance along each flight, preserving side guards.
        var filters = Object.FindObjectsByType<MeshFilter>().Where(f =>
            Regex.IsMatch(f.name, @"^[A-Z]_F\d+_M\d+_Walls$|^A7N_Balcony_Rail$") &&
            f.GetComponents<Collider>().Any(c => c.enabled)).ToArray();
        foreach (var flight in CampusTraversalValidation.Flights())
        {
            var direction = Vector3.ProjectOnPlane(flight.bottom - flight.top, Vector3.up).normalized;
            var start = flight.top - direction * 0.4f; var end = flight.bottom + direction * 0.4f;
            int steps = Mathf.CeilToInt(Vector3.Distance(start, end) / 0.3f);
            for (int i = 0; i < steps; i++)
            {
                var a = Vector3.Lerp(start, end, (float)i / steps); var b = Vector3.Lerp(start, end, (float)(i + 1) / steps);
                var min = Vector3.Min(a,b) - new Vector3(0.43f, -0.03f, 0.43f);
                var max = Vector3.Max(a,b) + new Vector3(0.43f, 2.05f, 0.43f);
                var box = new Bounds(); box.SetMinMax(min,max);
                foreach (var filter in filters)
                {
                    var collider = filter.GetComponent<Collider>();
                    if (!collider.bounds.Intersects(box)) continue;
                    var mesh = CampusDoorwayRepair.SubtractBox(filter.transform, filter.sharedMesh, box);
                    if (mesh == null) continue;
                    mesh = SaveMesh(mesh, filter.name + "_StairClearance");
                    Undo.RecordObject(filter,"Clear stair passage"); filter.sharedMesh=mesh;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                    var mc=collider as MeshCollider;
                    if(mc==null){Undo.DestroyObjectImmediate(collider);mc=Undo.AddComponent<MeshCollider>(filter.gameObject);}
                    Undo.RecordObject(mc,"Clear stair collision"); mc.sharedMesh=null;mc.sharedMesh=mesh;mc.sharedMaterial=Walking;mc.contactOffset=0.005f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(mc);
                    repaired.Add(filter.name);
                }
            }
        }
        Save(); return "Cleared stair passages in " + repaired.Count + " overlapping meshes.";
    }

    [MenuItem("Campus Rift/Clear Overlapping Door Apertures")]
    public static string ClearDoorApertures()
    {
        Prepare(); var repaired = new HashSet<string>(); int apertures = 0;
        foreach (var door in Object.FindObjectsByType<CampusAutomaticDoor>())
        {
            Vector3 normal = door.normal.normalized;
            Bounds clearance = door.doorway;
            clearance.Expand(new Vector3(Mathf.Abs(normal.x) * 2.1f, 0, Mathf.Abs(normal.z) * 2.1f));
            // Preserve the walkable sill and the jambs around the existing leaf.
            var min = clearance.min; min.y += 0.025f; clearance.SetMinMax(min, clearance.max);
            bool changed = false;
            foreach (var collider in Physics.OverlapBox(clearance.center, clearance.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (collider.GetComponentInParent<CampusAutomaticDoor>() != null || collider is CharacterController) continue;
                if (!Regex.IsMatch(collider.name, @"Walls$|_Body$|JointWall|_Glass_|_WinGlass_|A7N_Door[EW]_Frame|_SecDoor_(Back|Side)_Frame$|_Arch_Plinth$")) continue;
                var filter = collider.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                Bounds opening = clearance;
                if(collider.name.Contains("_Frame") || collider.name.Contains("_Plinth"))
                { var bottom=opening.min; bottom.y=door.doorway.min.y-0.15f;opening.SetMinMax(bottom,opening.max); }
                var mesh = CampusDoorwayRepair.SubtractBox(filter.transform, filter.sharedMesh, opening);
                if (mesh == null) continue;
                mesh = SaveMesh(mesh, filter.name + "_DoorClearance");
                Undo.RecordObject(filter, "Clear overlapping doorway geometry"); filter.sharedMesh = mesh;
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                foreach (var old in filter.GetComponents<Collider>()) Undo.DestroyObjectImmediate(old);
                var collision = Undo.AddComponent<MeshCollider>(filter.gameObject); collision.sharedMesh = mesh;
                collision.contactOffset = 0.005f; collision.sharedMaterial = Walking;
                repaired.Add(filter.name); changed = true;
            }
            if (changed) apertures++;
        }
        Save(); return "Cleared " + apertures + " door apertures in " + repaired.Count + " overlapping meshes.";
    }
}
