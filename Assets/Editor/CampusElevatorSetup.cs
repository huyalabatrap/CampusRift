using System;
using System.Collections.Generic;
using System.Linq;
using CampusRift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CampusElevatorSetup
{
    const string AssetRoot = "Assets/Elevators";
    const string MeshAsset = AssetRoot + "/DoorMeshes.asset";
    static Mesh meshContainer;

    [MenuItem("Campus Rift/Set Up Elevators")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before setting up elevators.");
        if (GameObject.Find("Campus Elevators") != null) throw new InvalidOperationException("Campus Elevators is already configured.");
        var scenery = GameObject.Find("Comic_Vibrant_Elevator_System_T77");
        var player = UnityEngine.Object.FindAnyObjectByType<CampusExplorer>();
        if (scenery == null || player == null) throw new InvalidOperationException("The campus model and Campus Explorer must be present.");
        EnsureFolder(AssetRoot);
        meshContainer = AssetDatabase.LoadAssetAtPath<Mesh>(MeshAsset);
        var source = scenery.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name, t => t.gameObject);
        var system = new GameObject("Campus Elevators");
        Undo.RegisterCreatedObjectUndo(system, "Set up campus elevators");
        var elevators = new List<CampusElevator>();

        foreach (string id in new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "T", "V", "X" })
        {
            string prefix = "ELEVATOR_" + id + "_";
            GameObject originalCabin;
            if (!source.TryGetValue(prefix + "Cabin", out originalCabin)) continue;
            var doors = source.Values.Where(g => g.name.StartsWith(prefix + "Door_F", StringComparison.Ordinal))
                .OrderBy(g => g.name, StringComparer.Ordinal).ToArray();
            if (doors.Length == 0) continue;

            Bounds cabinBounds = originalCabin.GetComponent<Renderer>().bounds;
            Bounds firstDoorBounds = doors[0].GetComponent<Renderer>().bounds;
            Vector3 right = firstDoorBounds.size.x > firstDoorBounds.size.z ? Vector3.right : Vector3.forward;
            Vector3 outward = firstDoorBounds.center - cabinBounds.center;
            outward.y = 0;
            outward = Mathf.Abs(outward.x) > Mathf.Abs(outward.z)
                ? Vector3.right * Mathf.Sign(outward.x) : Vector3.forward * Mathf.Sign(outward.z);
            var panelCenters = GetDoorCenters(doors[0], right);

            var root = new GameObject("Elevator " + id);
            root.transform.SetParent(system.transform, false);
            var elevator = root.AddComponent<CampusElevator>();
            elevator.building = id;
            elevator.player = player;
            elevator.displayMaterial = GetDisplayMaterial();
            elevator.outward = outward;
            elevator.floors = new ElevatorLanding[doors.Length];

            for (int f = 0; f < doors.Length; f++)
            {
                string suffix = "F" + (f + 1).ToString("00");
                var lobby = source[prefix + "Lobby_" + suffix].GetComponent<Renderer>().bounds;
                var doorBounds = doors[f].GetComponent<Renderer>().bounds;
                var floor = new ElevatorLanding();
                floor.height = lobby.center.y;
                var landing = new GameObject("Landing " + suffix);
                landing.transform.SetParent(root.transform, false);
                var centers = panelCenters.Select(p => new Vector3(p.x, doorBounds.center.y, p.z)).ToArray();
                floor.entrances = centers.Select(p => new Vector3(p.x, floor.height, p.z)).ToArray();
                floor.doorways = centers.Select(p => new Bounds(p, SizeAlong(right, outward, 1.20f, 2.18f, 0.68f))).ToArray();
                floor.doors = MakeDoorLeaves(doors[f], landing.transform, centers, right, Vector3.zero, id + "_" + suffix);
                floor.display = MakeDisplay(landing.transform, id, firstDoorBounds.center + Vector3.up * (floor.height - lobby.center.y), outward);
                floor.display.transform.position = doorBounds.center + Vector3.up * 1.39f + outward * 0.11f;
                elevator.floors[f] = floor;
                HideSource(doors[f]);
                GameObject indicator;
                if (source.TryGetValue(prefix + "Indicator_" + suffix, out indicator)) HideSource(indicator);
            }

            var platform = new GameObject("Cabin Platform");
            platform.transform.SetParent(root.transform, false);
            platform.transform.position = new Vector3(cabinBounds.center.x, elevator.floors[0].height, cabinBounds.center.z);
            elevator.cabin = platform.transform;
            elevator.cabinBasePosition = platform.transform.position;
            var body = platform.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            elevator.cabinBody = body;

            var visual = new GameObject("Original Cabin Visual");
            visual.transform.SetPositionAndRotation(originalCabin.transform.position, originalCabin.transform.rotation);
            visual.transform.localScale = originalCabin.transform.lossyScale;
            visual.transform.SetParent(platform.transform, true);
            visual.AddComponent<MeshFilter>().sharedMesh = originalCabin.GetComponent<MeshFilter>().sharedMesh;
            visual.AddComponent<MeshRenderer>().sharedMaterials = originalCabin.GetComponent<MeshRenderer>().sharedMaterials;
            HideSource(originalCabin);

            elevator.cabinSpaces = new Bounds[panelCenters.Length];
            float totalWidth = Vector3.Dot(cabinBounds.size, right);
            float roomWidth = totalWidth / panelCenters.Length - 0.12f;
            float backCoordinate = Vector3.Dot(cabinBounds.center, outward) - Vector3.Dot(cabinBounds.extents, Abs(outward)) + 0.06f;
            float frontCoordinate = Vector3.Dot(firstDoorBounds.center, outward) - 0.10f;
            float depth = frontCoordinate - backCoordinate;
            float centerCoordinate = (backCoordinate + frontCoordinate) * 0.5f;
            for (int car = 0; car < panelCenters.Length; car++)
            {
                Vector3 center = cabinBounds.center;
                center += right * Vector3.Dot(panelCenters[car] - center, right);
                center += outward * (centerCoordinate - Vector3.Dot(center, outward));
                center.y = elevator.floors[0].height;
                AddBox(platform.transform, "Floor " + car, center + Vector3.up * 0.035f, SizeAlong(right, outward, roomWidth, 0.08f, depth));
                AddBox(platform.transform, "Ceiling " + car, center + Vector3.up * 2.27f, SizeAlong(right, outward, roomWidth, 0.08f, depth));
                AddBox(platform.transform, "Back Wall " + car, center - outward * (depth * 0.5f) + Vector3.up * 1.15f,
                    SizeAlong(right, outward, roomWidth, 2.3f, 0.10f));
                foreach (float side in new[] { -1f, 1f })
                    AddBox(platform.transform, "Side Wall " + car + " " + side, center + right * (side * roomWidth * 0.5f) + Vector3.up * 1.15f,
                        SizeAlong(right, outward, 0.08f, 2.3f, depth));
                elevator.cabinSpaces[car] = new Bounds(center + Vector3.up * 1.05f,
                    SizeAlong(right, outward, roomWidth - 0.20f, 2.1f, depth - 0.45f));
                var lamp = new GameObject("Cabin Light " + car);
                lamp.transform.SetParent(platform.transform, false);
                lamp.transform.position = center + Vector3.up * 1.95f;
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point; light.range = 3.2f; light.intensity = 0.8f;
                light.color = new Color(0.88f, 0.95f, 1f); light.shadows = LightShadows.None;
            }
            elevator.cabinDoors = MakeDoorLeaves(doors[0], platform.transform, panelCenters, right, -outward * 0.16f, id + "_Cabin");
            var audio = platform.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 1; audio.minDistance = 1; audio.maxDistance = 14; audio.volume = 0.65f;
            elevators.Add(elevator);
        }

        var interaction = player.GetComponent<ElevatorInteraction>();
        if (interaction == null) interaction = Undo.AddComponent<ElevatorInteraction>(player.gameObject);
        interaction.elevators = elevators.ToArray();
        PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
        EditorUtility.SetDirty(interaction);
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(system.scene);
        EditorSceneManager.SaveScene(system.scene);
        Debug.Log("Elevators configured: " + elevators.Count + " banks, " + elevators.Sum(e => e.FloorCount) + " landings.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    static void HideSource(GameObject source)
    {
        var renderer = source.GetComponent<Renderer>();
        if (renderer != null) { Undo.RecordObject(renderer, "Use interactive elevator geometry"); renderer.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
        foreach (var collider in source.GetComponents<Collider>())
        { Undo.RecordObject(collider, "Use interactive elevator collision"); collider.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(collider); }
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    static Vector3 SizeAlong(Vector3 right, Vector3 outward, float width, float height, float depth)
        => Abs(right) * width + Vector3.up * height + Abs(outward) * depth;

    static void AddBox(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.AddComponent<BoxCollider>().size = size;
    }

    static TextMesh MakeDisplay(Transform parent, string building, Vector3 position, Vector3 outward)
    {
        var go = new GameObject("Floor Display");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-outward, Vector3.up));
        var text = go.AddComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.GetComponent<MeshRenderer>().sharedMaterial = GetDisplayMaterial();
        text.text = building + "  01"; text.fontSize = 48; text.characterSize = 0.035f;
        text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
        text.color = new Color(0.2f, 0.85f, 1f);
        return text;
    }

    public static Material GetDisplayMaterial()
    {
        const string path = AssetRoot + "/FloorDisplay.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            EnsureFolder(AssetRoot);
            material = new Material(Shader.Find("Campus Rift/World Text"));
            material.name = "Floor Display";
            material.mainTexture = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf").material.mainTexture;
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    sealed class MeshParts
    {
        public Mesh mesh;
        public Vector3[] worldVertices;
        public int[] roots;
        public Dictionary<int, Bounds> bounds;
    }

    static MeshParts GetParts(GameObject source)
    {
        Mesh mesh = source.GetComponent<MeshFilter>().sharedMesh;
        var vertices = mesh.vertices;
        var parent = Enumerable.Range(0, vertices.Length).ToArray();
        Func<int, int> find = null;
        find = i => { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; };
        Action<int, int> union = (a, b) => parent[find(a)] = find(b);
        var welded = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i] * 1000000f;
            var key = new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
            int existing;
            if (welded.TryGetValue(key, out existing)) union(i, existing); else welded.Add(key, i);
        }
        var triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3) { union(triangles[i], triangles[i + 1]); union(triangles[i], triangles[i + 2]); }
        var world = vertices.Select(v => source.transform.TransformPoint(v)).ToArray();
        var bounds = new Dictionary<int, Bounds>();
        for (int i = 0; i < vertices.Length; i++)
        {
            parent[i] = find(i);
            Bounds b;
            if (!bounds.TryGetValue(parent[i], out b)) b = new Bounds(world[i], Vector3.zero); else b.Encapsulate(world[i]);
            bounds[parent[i]] = b;
        }
        return new MeshParts { mesh = mesh, worldVertices = world, roots = parent, bounds = bounds };
    }

    static Vector3[] GetDoorCenters(GameObject source, Vector3 right)
    {
        var panels = GetParts(source).bounds.Values.Where(b => b.size.y > 2f && Vector3.Dot(b.size, right) > 0.35f)
            .OrderBy(b => Vector3.Dot(b.center, right)).ToArray();
        if (panels.Length == 0 || panels.Length % 2 != 0) throw new InvalidOperationException("Unexpected door geometry: " + source.name);
        var result = new Vector3[panels.Length / 2];
        for (int i = 0; i < result.Length; i++) result[i] = (panels[i * 2].center + panels[i * 2 + 1].center) * 0.5f;
        return result;
    }

    static ElevatorDoorLeaf[] MakeDoorLeaves(GameObject source, Transform parent, Vector3[] centers, Vector3 right, Vector3 offset, string label)
    {
        var data = GetParts(source);
        var assignments = new Dictionary<int, int>();
        foreach (var part in data.bounds)
        {
            int car = 0; float best = float.MaxValue;
            for (int i = 0; i < centers.Length; i++)
            {
                float distance = Mathf.Abs(Vector3.Dot(part.Value.center - centers[i], right));
                if (distance < best) { best = distance; car = i; }
            }
            assignments[part.Key] = car * 2 + (Vector3.Dot(part.Value.center - centers[car], right) >= 0 ? 1 : 0);
        }
        var output = new ElevatorDoorLeaf[centers.Length * 2];
        var sourceNormals = data.mesh.normals;
        var sourceUV = data.mesh.uv;
        Matrix4x4 normalMatrix = source.transform.localToWorldMatrix.inverse.transpose;
        for (int group = 0; group < output.Length; group++)
        {
            var indexMap = new Dictionary<int, int>();
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>();
            var submeshes = new List<int[]>();
            Vector3 anchor = centers[group / 2];
            for (int s = 0; s < data.mesh.subMeshCount; s++)
            {
                var sourceTriangles = data.mesh.GetTriangles(s); var triangles = new List<int>();
                for (int i = 0; i < sourceTriangles.Length; i += 3)
                {
                    if (assignments[data.roots[sourceTriangles[i]]] != group) continue;
                    for (int j = 0; j < 3; j++)
                    {
                        int original = sourceTriangles[i + j], mapped;
                        if (!indexMap.TryGetValue(original, out mapped))
                        {
                            mapped = vertices.Count; indexMap.Add(original, mapped);
                            vertices.Add(data.worldVertices[original] - anchor);
                            normals.Add(sourceNormals.Length > original ? normalMatrix.MultiplyVector(sourceNormals[original]).normalized : Vector3.up);
                            uv.Add(sourceUV.Length > original ? sourceUV[original] : Vector2.zero);
                        }
                        triangles.Add(mapped);
                    }
                }
                submeshes.Add(triangles.ToArray());
            }
            var mesh = new Mesh { name = label + "_Leaf_" + group };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv);
            mesh.subMeshCount = submeshes.Count;
            for (int s = 0; s < submeshes.Count; s++) mesh.SetTriangles(submeshes[s], s);
            mesh.RecalculateBounds();
            if (meshContainer == null) { AssetDatabase.CreateAsset(mesh, MeshAsset); meshContainer = mesh; }
            else AssetDatabase.AddObjectToAsset(mesh, meshContainer);
            var go = new GameObject("Door " + (group / 2 + 1) + (group % 2 == 0 ? " Left" : " Right"));
            go.transform.SetParent(parent, false); go.transform.position = anchor + offset;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            var box = go.AddComponent<BoxCollider>(); box.center = mesh.bounds.center; box.size = mesh.bounds.size;
            output[group] = new ElevatorDoorLeaf { transform = go.transform, closedLocalPosition = go.transform.localPosition,
                openOffset = parent.InverseTransformVector(right * (group % 2 == 0 ? -0.64f : 0.64f)) };
        }
        return output;
    }
}
