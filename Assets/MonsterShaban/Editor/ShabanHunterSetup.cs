using CampusRift.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShabanHunterSetup
{
    public static string AddInterception()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        var brain=Object.FindAnyObjectByType<MonsterBrain>();
        var planner=brain.GetComponent<InterceptionPlanner>();if(planner==null)planner=Undo.AddComponent<InterceptionPlanner>(brain.gameObject);
        planner.config=brain.config;EditorUtility.SetDirty(planner);
        PrefabUtility.ApplyPrefabInstance(brain.gameObject,InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(brain.gameObject.scene);EditorSceneManager.SaveScene(brain.gameObject.scene);AssetDatabase.SaveAssets();
        return "Interception planner added to the existing Monster_Shaban prefab.";
    }

    // Belief-driven multi-floor hunting: adds the belief and lift awareness to the existing
    // prefab (the brain also adds them at runtime for older instances).
    [MenuItem("Campus Rift/Shaban/Add Multi-Floor Hunting")]
    public static string AddMultiFloorHunting()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        var brain=Object.FindAnyObjectByType<MonsterBrain>();
        if(brain==null)throw new System.InvalidOperationException("Open the gameplay scene with Monster_Shaban first.");
        var lifts=brain.GetComponent<MonsterElevatorAwareness>();if(lifts==null)lifts=Undo.AddComponent<MonsterElevatorAwareness>(brain.gameObject);
        lifts.config=brain.config;EditorUtility.SetDirty(lifts);
        var belief=brain.GetComponent<MonsterBelief>();if(belief==null)belief=Undo.AddComponent<MonsterBelief>(brain.gameObject);
        belief.config=brain.config;EditorUtility.SetDirty(belief);
        PrefabUtility.ApplyPrefabInstance(brain.gameObject,InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(brain.gameObject.scene);EditorSceneManager.SaveScene(brain.gameObject.scene);AssetDatabase.SaveAssets();
        return "Belief and elevator awareness added to Monster_Shaban.";
    }
}
