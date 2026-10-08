using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CampusDoorwayRepair
{
    struct Vertex
    {
        public Vector3 world;
        public Vector3 normal;
        public Vector2 uv;
        public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex
        { world = Vector3.Lerp(a.world, b.world, t), normal = Vector3.Lerp(a.normal, b.normal, t).normalized, uv = Vector2.Lerp(a.uv, b.uv, t) };
    }

    [MenuItem("Campus Rift/Repair Overlapping T Entrance")]
    public static void RepairTEntrance()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        var scenery = GameObject.Find("Comic_Vibrant_Elevator_System_T77");
        var targets = new HashSet<string> { "Block_T_Glass_F_Fl1", "Block_F_Glass_B_Fl1", "Block_X_WinGlass_Fl1_B0", "Block_X_Glass_F_Fl1",
            "Block_F_Body", "Block_X_Body", "Block_F_Arch_Plinth", "Block_X_Arch_Plinth" };
        // This doorway is shared by overlapping elevations of F, T and X in the supplied model.
        Bounds clearance = new Bounds(new Vector3(-30f, 1.23f, -10f), new Vector3(2.2f, 2.5f, 0.7f));
        var repaired = new List<string>();
        foreach (var filter in scenery.GetComponentsInChildren<MeshFilter>())
        {
            if (!targets.Contains(filter.name)) continue;
            Mesh source = filter.sharedMesh;
            Mesh mesh = SubtractBox(filter.transform, source, clearance);
            if (mesh == null) continue;
            mesh.name = filter.name + "_ClearEntrance";
            string path = "Assets/Collision/" + mesh.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
            Undo.RecordObject(filter, "Clear overlapping entrance glass");
            filter.sharedMesh = mesh;
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            foreach (var collider in filter.GetComponents<Collider>()) Undo.DestroyObjectImmediate(collider);
            var collision = Undo.AddComponent<MeshCollider>(filter.gameObject);
            collision.sharedMesh = mesh;
            collision.contactOffset = 0.005f;
            collision.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Collision/CampusWalking.physicMaterial");
            repaired.Add(filter.name);
        }
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scenery.scene);
        EditorSceneManager.SaveScene(scenery.scene);
        Debug.Log("Cleared shared T doorway: " + string.Join(", ", repaired));
    }

    public static Mesh SubtractBox(Transform transform, Mesh source, Bounds box)
    {
        var positions = source.vertices; var sourceNormals = source.normals; var sourceUV = source.uv;
        var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>();
        var submeshes = new List<int[]>();
        int affected = 0;
        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            var indices = new List<int>(); var triangles = source.GetTriangles(sub);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                var remaining = new List<Vertex>();
                for (int j = 0; j < 3; j++)
                {
                    int index = triangles[t + j];
                    remaining.Add(new Vertex { world = transform.TransformPoint(positions[index]),
                        normal = sourceNormals.Length > index ? sourceNormals[index] : Vector3.up,
                        uv = sourceUV.Length > index ? sourceUV[index] : Vector2.zero });
                }
                for (int plane = 0; plane < 6 && remaining.Count >= 3; plane++)
                {
                    int axis = plane / 2;
                    float edge = plane % 2 == 0 ? box.min[axis] : box.max[axis];
                    float sign = plane % 2 == 0 ? 1 : -1;
                    var inside = new List<Vertex>(); var outside = new List<Vertex>();
                    for (int i = 0; i < remaining.Count; i++)
                    {
                        Vertex a = remaining[i], b = remaining[(i + 1) % remaining.Count];
                        float da = sign * (a.world[axis] - edge), db = sign * (b.world[axis] - edge);
                        if (da >= 0) inside.Add(a); else outside.Add(a);
                        if ((da >= 0) != (db >= 0))
                        {
                            Vertex crossing = Vertex.Lerp(a, b, da / (da - db));
                            inside.Add(crossing); outside.Add(crossing);
                        }
                    }
                    Emit(outside, transform, vertices, normals, uv, indices);
                    remaining = inside;
                }
                if (remaining.Count >= 3) affected++;
            }
            submeshes.Add(indices.ToArray());
        }
        if (affected == 0) return null;
        var mesh = new Mesh { indexFormat = vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv);
        mesh.subMeshCount = submeshes.Count;
        for (int i = 0; i < submeshes.Count; i++) mesh.SetTriangles(submeshes[i], i);
        mesh.RecalculateBounds();
        return mesh;
    }

    static void Emit(List<Vertex> polygon, Transform transform, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> indices)
    {
        for (int i = 1; i + 1 < polygon.Count; i++)
        {
            if (Vector3.Cross(polygon[i].world - polygon[0].world, polygon[i + 1].world - polygon[0].world).sqrMagnitude < 0.0000000001f) continue;
            foreach (var v in new[] { polygon[0], polygon[i], polygon[i + 1] })
            {
                indices.Add(vertices.Count); vertices.Add(transform.InverseTransformPoint(v.world)); normals.Add(v.normal); uv.Add(v.uv);
            }
        }
    }
}
