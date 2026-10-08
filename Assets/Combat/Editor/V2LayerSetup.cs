using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// P00-T04: physics layers for V2 combat. Every existing LayerMask in the project is "Everything",
// so adding layers and moving the player/monster onto them does not change any query result.
public static class V2LayerSetup
{
    public const int Player = 6, Enemy = 7, PlayerAttack = 8, EnemyAttack = 9, SkyBeast = 10, Environment = 11;
    static readonly Dictionary<int, string> Names = new Dictionary<int, string>
    {
        { Player, "Player" }, { Enemy, "Enemy" }, { PlayerAttack, "PlayerAttack" },
        { EnemyAttack, "EnemyAttack" }, { SkyBeast, "SkyBeast" }, { Environment, "Environment" }
    };
    const string PlayerPrefab = "Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
    const string ShabanPrefab = "Assets/MonsterShaban/Monster_Shaban.prefab";

    [MenuItem("Campus Rift/V2/Setup Physics Layers")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        var log = new List<string>();
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        foreach (var pair in Names)
        {
            var slot = layers.GetArrayElementAtIndex(pair.Key);
            if (slot.stringValue == pair.Value) continue;
            if (!string.IsNullOrEmpty(slot.stringValue))
                throw new System.InvalidOperationException("Layer " + pair.Key + " is already used by " + slot.stringValue);
            slot.stringValue = pair.Value; log.Add("layer " + pair.Key + " = " + pair.Value);
        }
        tags.ApplyModifiedPropertiesWithoutUndo();

        // Attacks never hit their owner's side; sky beasts are visual only and collide with nothing.
        Physics.IgnoreLayerCollision(PlayerAttack, Player, true);
        Physics.IgnoreLayerCollision(EnemyAttack, Enemy, true);
        Physics.IgnoreLayerCollision(PlayerAttack, PlayerAttack, true);
        Physics.IgnoreLayerCollision(EnemyAttack, EnemyAttack, true);
        Physics.IgnoreLayerCollision(PlayerAttack, EnemyAttack, true);
        for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(SkyBeast, i, true);

        log.Add(AssignPrefab(PlayerPrefab, Player));
        log.Add(AssignPrefab(ShabanPrefab, Enemy));
        AssetDatabase.SaveAssets();
        return string.Join("\n", log);
    }

    // Only objects on Default move; UI (5) and other deliberate layers inside the prefab are kept.
    static string AssignPrefab(string path, int layer)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        int moved = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.gameObject.layer == 0) { t.gameObject.layer = layer; moved++; }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return path + ": " + moved + " objects -> " + Names[layer];
    }
}
