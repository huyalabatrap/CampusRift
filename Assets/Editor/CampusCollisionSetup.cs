using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CampusCollisionSetup
{
    const string SourceName = "Comic_Vibrant_Elevator_System_T77";
    const string Folder = "Assets/Collision";

    [MenuItem("Campus Rift/Clean Scene and Rebuild Colliders")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var source = GameObject.Find(SourceName);
        if (source == null) throw new InvalidOperationException("Campus model is missing.");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Collision");
        EditorSceneManager.SaveScene(source.scene);
        Directory.CreateDirectory("Backups");
        string backup = "Backups/SampleScene-before-collider-cleanup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".unity";
        File.Copy(source.scene.path, backup, false);

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Clean campus and rebuild collision");
        var removedHelpers = new List<string>();
        var skipped = new List<string>();
        var rebuilt = new List<string>();
        var objects = source.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
        var names = new HashSet<string>(objects.Select(g => g.name));
        int oldCount = source.GetComponentsInChildren<Collider>(true).Length;
        int boxes = 0, meshes = 0, newSurfaces = 0;
        var material = GetWalkingMaterial();
        int removedEntranceObstructions = ClearEntranceObstructions();

        foreach (var go in objects)
        {
            if (IsAuthoringHelper(go))
            {
                Undo.RecordObject(go, "Remove authoring helper from gameplay");
                go.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(go);
                removedHelpers.Add(go.name);
            }
            var previous = go.GetComponents<Collider>();
            bool hadCollider = previous.Length > 0;
            var renderer = go.GetComponent<MeshRenderer>();
            var filter = go.GetComponent<MeshFilter>();
            bool keep = go.activeInHierarchy && renderer != null && renderer.enabled && filter != null
                && filter.sharedMesh != null && (hadCollider || IsStructure(go.name))
                && !IsDecoration(go.name) && !HasStairRamp(go.name, names);
            foreach (var collider in previous) Undo.DestroyObjectImmediate(collider);
            if (!keep)
            {
                if (hadCollider) skipped.Add(go.name);
                continue;
            }

            Collider replacement;
            Mesh mesh = filter.sharedMesh;
            if (IsSolidBox(mesh))
            {
                var box = Undo.AddComponent<BoxCollider>(go);
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size;
                replacement = box;
                boxes++;
            }
            else
            {
                var shape = Undo.AddComponent<MeshCollider>(go);
                // The repaired side-door collision has more jump clearance than its visual mesh.
                shape.sharedMesh = go.name == "Block_X_SecDoor_Side_Frame"
                    ? AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Collision/Traversal/Block_X_SecDoor_Side_Frame_JumpClearance.asset") ?? mesh
                    : mesh;
                // Vehicle shells need a solid hull so a landing capsule cannot enter their
                // open interiors. Keep the building and parking structure non-convex.
                shape.convex = Regex.IsMatch(go.name, @"^T60_(Car_|Supercar_|Moto_)");
                replacement = shape;
                meshes++;
            }
            replacement.sharedMaterial = material;
            replacement.contactOffset = 0.005f;
            if (!hadCollider) newSurfaces++;
            rebuilt.Add(go.name);
        }

        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(source.scene);
        EditorSceneManager.SaveScene(source.scene);
        Undo.CollapseUndoOperations(undo);
        var report = new
        {
            backup, helpersRemovedFromGameplay = removedHelpers.Count, removedHelpers, removedEntranceObstructions,
            previousColliders = oldCount, boxColliders = boxes, meshColliders = meshes,
            addedMissingSurfaces = newSurfaces, skippedPreviousColliders = skipped,
            rebuiltSources = rebuilt,
            note = "Source FBX stays intact. Authoring helpers are inactive prefab overrides. Moving elevator collision is maintained by CampusElevator."
        };
        File.WriteAllText(Folder + "/CleanupReport.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        AssetDatabase.ImportAsset(Folder + "/CleanupReport.json");
        AssetDatabase.SaveAssets();
        Debug.Log("Campus collision rebuilt: " + boxes + " boxes, " + meshes + " meshes; " + removedHelpers.Count + " authoring helpers removed from gameplay.");
    }

    public static int ClearEntranceObstructions()
    {
        var source = GameObject.Find(SourceName);
        int removed = 0;
        foreach (var transform in source.GetComponentsInChildren<Transform>(true))
        {
            if (!Regex.IsMatch(transform.name, @"^Floodlight_(Mast_3|Crossbar_3|Lamp_3_.+)$")) continue;
            if (!transform.gameObject.activeSelf) continue;
            Undo.RecordObject(transform.gameObject, "Clear E entrance approach");
            transform.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject);
            removed++;
        }
        return removed;
    }

    static bool IsAuthoringHelper(GameObject go)
    {
        string n = go.name;
        if (n.StartsWith("P0_") || n.StartsWith("QA_") || n.StartsWith("SNAP_")) return true;
        if (Regex.IsMatch(n, @"^T\d+_Asset_")) return true;
        var renderer = go.GetComponent<Renderer>();
        // Modular kit samples are parked outside the campus. Placed furniture instances stay.
        return n.StartsWith("KIT_") && !n.Contains("|Dupli|") && renderer != null
            && renderer.bounds.center.x < -75f && renderer.bounds.center.z < -75f;
    }

    static bool IsDecoration(string name)
    {
        if (name.StartsWith("ELEVATOR_")) return name.Contains("CallButton") || name.Contains("Indicator");
        return Regex.IsMatch(name, @"(_Sign|_Txt_|_Text|_Plate_|_Emblem|_Logo|_Light|_PanicBar|_Handle|_Downpipe|_Downspout|_AC_|_RFID|_Mullion|_Stay_|_AwningSash|_Soil|_Transom|_Drainage_Grate)")
            || name.StartsWith("Courtyard_Grid_") || name.StartsWith("TreeGrate_");
    }

    static bool IsStructure(string name)
    {
        return Regex.IsMatch(name, @"^Block_[A-Z]_Body$")
            || Regex.IsMatch(name, @"(_Wall|_Partitions|_Slab|_Floor|_Shell|_Landing|_Terrace|_Platform|_Ramp|_BridgeSill|_SillPad|_ApronStep)")
            || name.Contains("_Canopy_Col_") && !name.Contains("Baseplate");
    }

    static bool HasStairRamp(string name, HashSet<string> names)
    {
        if (!name.Contains("Stair") || !name.Contains("_Step_")) return false;
        string prefix = name.Substring(0, name.IndexOf("_Step_", StringComparison.Ordinal)) + "_Ramp_";
        return names.Any(n => n.StartsWith(prefix, StringComparison.Ordinal));
    }

    static bool IsSolidBox(Mesh mesh)
    {
        // A bounds box is valid only for an actual closed cuboid, never a combined room or frame.
        if (mesh.vertexCount != 24 || mesh.triangles.Length != 36) return false;
        Bounds b = mesh.bounds;
        if (b.size.x <= 0 || b.size.y <= 0 || b.size.z <= 0) return false;
        int corners = 0;
        Vector3 tolerance = b.size * 0.0001f + Vector3.one * 0.0000001f;
        foreach (var v in mesh.vertices)
        {
            int corner = 0;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(v[axis] - b.min[axis]) <= tolerance[axis]) continue;
                if (Mathf.Abs(v[axis] - b.max[axis]) > tolerance[axis]) return false;
                corner |= 1 << axis;
            }
            corners |= 1 << corner;
        }
        return corners == 255;
    }

    static PhysicsMaterial GetWalkingMaterial()
    {
        const string path = Folder + "/CampusWalking.physicMaterial";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (material == null)
        {
            material = new PhysicsMaterial("Campus Walking")
            {
                dynamicFriction = 0, staticFriction = 0, bounciness = 0,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
}
