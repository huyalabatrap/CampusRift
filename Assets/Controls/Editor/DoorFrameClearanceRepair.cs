using System;
using System.IO;
using System.Linq;
using CampusRift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class DoorFrameClearanceRepair
{
    [MenuItem("Campus Rift/Controls/Repair Verified Door Edge Obstructions")]
    public static void Repair()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        var report=JsonUtility.FromJson<CampusTraversalValidation.Report>(File.ReadAllText("Artifacts/MobileControls/DoorEdges-Before.json"));
        const string root="Assets/Collision/DoorEdges";if(!AssetDatabase.IsValidFolder(root))AssetDatabase.CreateFolder("Assets/Collision","DoorEdges");
        var doors=Object.FindObjectsByType<CampusAutomaticDoor>();int changed=0;
        foreach(var group in report.trials.Where(t=>!t.pass).GroupBy(t=>t.source))
        {
            var door=doors.First(d=>d.name==group.Key);var cut=door.doorway;
            cut.Expand(new Vector3(Mathf.Abs(door.normal.x)*2.3f,0,Mathf.Abs(door.normal.z)*2.3f));
            var min=cut.min;min.y=door.doorway.min.y+.07f;var max=cut.max;max.y=min.y+2.02f;cut.SetMinMax(min,max);
            foreach(var blocker in group.SelectMany(t=>t.blockers).Distinct())
            {
                // Body is the supporting floor, never remove it to solve a side collision.
                if(blocker.EndsWith("_Body"))continue;
                var filter=Object.FindObjectsByType<MeshFilter>().FirstOrDefault(f=>f.name==blocker);if(filter==null)continue;
                var mesh=CampusDoorwayRepair.SubtractBox(filter.transform,filter.sharedMesh,cut);if(mesh==null)continue;
                string path=root+"/"+blocker+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                mesh.name=blocker;
                if(existing==null){mesh.name=blocker+"_ClearPassage";AssetDatabase.CreateAsset(mesh,path);}else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
                Undo.RecordObject(filter,"Remove verified doorway overlap");filter.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                foreach(var old in filter.GetComponents<Collider>())Undo.DestroyObjectImmediate(old);
                var collision=Undo.AddComponent<MeshCollider>(filter.gameObject);collision.sharedMesh=mesh;collision.contactOffset=.005f;
                collision.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Collision/CampusWalking.physicMaterial");changed++;
            }
            // The two upper west doors overhang their landing at the outer edges.
            // Extend the visible landing by less than one metre, rather than allowing a fall
            // into the wall lip directly beyond an otherwise clear doorway.
            if(door.name=="A7N_Door_W_28"||door.name=="A7N_Door_W_32")
            {
                string name=door.name+" Landing Edge Repair";var slab=GameObject.Find(name)??GameObject.CreatePrimitive(PrimitiveType.Cube);slab.name=name;
                slab.transform.position=new Vector3(door.doorway.center.x+.5f,door.doorway.min.y-.09f,door.doorway.center.z);
                slab.transform.localScale=new Vector3(1.25f,.18f,door.doorway.size.z);
                var body=GameObject.Find("Block_A_Body");if(body!=null)slab.GetComponent<Renderer>().sharedMaterial=body.GetComponent<Renderer>().sharedMaterial;
                slab.GetComponent<Collider>().sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Collision/CampusWalking.physicMaterial");
            }
        }
        Physics.SyncTransforms();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        Debug.Log("Repaired verified door-edge overlaps: "+changed);
    }
}
