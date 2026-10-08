using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CampusRift.Enemies;
using CampusRift.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// P05-T01/T02: data checks for the ten levels, plus a NavMesh check of every rift zone and spawn point.
public static class LevelValidation
{
    static readonly int[] Totals = { 6, 10, 14, 18, 23, 26, 30, 32, 36, 45 };
    static readonly int[] WaveCounts = { 2, 2, 3, 3, 3, 4, 4, 1, 2, 3 };

    [MenuItem("Campus Rift/V2/Validate Levels")]
    public static string Validate() => Run(false);

    // Same checks; also moves every zone onto the NavMesh (height and up to 3 m sideways) and saves the assets.
    [MenuItem("Campus Rift/V2/Snap Rift Zones To NavMesh")]
    public static string Snap() => Run(true);

    static string Run(bool snap)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var report = new StringBuilder(); int failures = 0, passes = 0;
        void Check(bool ok, string label) { if (ok) passes++; else { failures++; report.AppendLine("FAIL " + label); } }

        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Levels/Resources/LevelCatalog.asset");
        Check(catalog != null && catalog.Count == 10, "catalog has 10 levels");
        if (catalog == null) return report.ToString();

        var shaban = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MonsterShaban/Monster_Shaban.prefab");
        int agentType = shaban != null ? shaban.GetComponent<NavMeshAgent>().agentTypeID : 0;
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity") scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };

        float prevHp = 0, prevDmg = 0, prevSpeed = 0; int prevAi = -1; int snapped = 0;
        for (int i = 0; i < catalog.levels.Length; i++)
        {
            var level = catalog.levels[i]; string tag = "L" + (i + 1) + ": ";
            Check(level != null, tag + "asset present"); if (level == null) continue;
            Check(level.index == i + 1, tag + "index");
            int total=level.TotalMonsters+(level.index==3?1:0)+level.bosses.Count;
            Check(total == Totals[i], tag + "total including authored elite/boss " + total + " == " + Totals[i]);
            Check(level.waves.Count == WaveCounts[i], tag + "wave count " + level.waves.Count + " == " + WaveCounts[i]);
            Check(level.healthMultiplier >= prevHp && level.damageMultiplier >= prevDmg && level.speedMultiplier >= prevSpeed && level.aiTier >= prevAi, tag + "multipliers do not decrease");
            prevHp = level.healthMultiplier; prevDmg = level.damageMultiplier; prevSpeed = level.speedMultiplier; prevAi = level.aiTier;
            bool entriesOk = true;
            foreach (var wave in level.waves) foreach (var entry in wave.entries) if (entry.archetype == null || entry.count < 1) entriesOk = false;
            Check(entriesOk, tag + "all wave entries have an archetype");
            Check(level.zones.Count >= 3, tag + "at least 3 rift zones (" + level.zones.Count + ")");
            var ids = new HashSet<string>(); bool idsOk = true;
            foreach (var z in level.zones) if (!ids.Add(z.id)) idsOk = false;
            Check(idsOk, tag + "zone ids unique");
            bool refsOk = true;
            foreach (var wave in level.waves) foreach (var id in wave.riftZones) if (!ids.Contains(id)) refsOk = false;
            Check(refsOk, tag + "wave zone references exist");
            Check(level.stars != null && level.stars.Length == 3, tag + "3 star conditions");

            for (int z = 0; z < level.zones.Count; z++)
            {
                var zone = level.zones[z];
                bool found = NavMesh.SamplePosition(zone.position, out var hit, 3f, filter);
                if (found && snap && (hit.position - zone.position).sqrMagnitude > 0.0025f) { zone.position = hit.position; level.zones[z] = zone; snapped++; }
                Check(found && (snap || (hit.position - zone.position).magnitude < 1.5f), tag + "zone " + zone.id + " on NavMesh" + (found ? " (off by " + (hit.position - zone.position).magnitude.ToString("F1") + " m)" : " (no NavMesh)"));
            }
            bool spawnFound = NavMesh.SamplePosition(level.spawnPoint, out var spawnHit, 2f, filter);
            Check(spawnFound, tag + "player spawn near NavMesh");
            if (snap) EditorUtility.SetDirty(level);
        }
        if (snap) AssetDatabase.SaveAssets();
        string summary = "levels: " + passes + " passed, " + failures + " failed" + (snap ? ", snapped " + snapped : "");
        Directory.CreateDirectory("Artifacts/Levels");
        File.WriteAllText("Artifacts/Levels/LevelData.txt", summary + "\n" + report);
        return summary + (failures > 0 ? "\n" + report : "");
    }
}
