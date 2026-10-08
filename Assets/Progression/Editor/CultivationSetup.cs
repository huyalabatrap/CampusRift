using System;
using System.Collections.Generic;
using System.IO;
using CampusRift;
using CampusRift.Learning;
using CampusRift.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// P06-T03/T04: writes the cultivation table and moves the player from the old learning bridge to the cultivation bridge.
public static class CultivationSetup
{
    const string TablePath = "Assets/Progression/Resources/CultivationTable.asset";
    const string PrefabPath = "Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";

    [MenuItem("Campus Rift/V2/Setup Cultivation")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var log = new List<string>();
        Directory.CreateDirectory("Assets/Progression/Resources");
        var table = AssetDatabase.LoadAssetAtPath<CultivationTable>(TablePath);
        var fresh = CultivationTable.CreateDefault();
        if (table == null) { table = ScriptableObject.CreateInstance<CultivationTable>(); AssetDatabase.CreateAsset(table, TablePath); }
        table.rows = fresh.rows; table.runBonusPerRealm = fresh.runBonusPerRealm; table.runBonusCap = fresh.runBonusCap;
        EditorUtility.SetDirty(table); UnityEngine.Object.DestroyImmediate(fresh);
        log.Add("table: " + table.rows.Length + " realms");

        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try { log.Add("prefab: " + Convert(prefab)); PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        foreach (var path in new[] { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/MainMenu.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            var player = UnityEngine.Object.FindAnyObjectByType<CampusExplorer>();
            if (player != null) { log.Add(path + ": " + Convert(player.gameObject)); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        return string.Join("\n", log);
    }

    static string Convert(GameObject player)
    {
        var text = new List<string>();
        if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player) > 0) text.Add("removed missing scripts");
        if (player.GetComponent<CultivationPlayerBridge>() == null) { player.AddComponent<CultivationPlayerBridge>(); text.Add("added cultivation bridge"); }
        var gate = player.GetComponent<LearningSkillGate>();
        if (gate != null && gate.bindings.Length > 0) { gate.bindings = new SkillBinding[0]; text.Add("cleared skill gate bindings"); }
        return string.Join(", ", text);
    }
}
