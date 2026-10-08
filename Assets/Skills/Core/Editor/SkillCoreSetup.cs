using System.Collections.Generic;
using CampusRift;
using CampusRift.Combat;
using CampusRift.Localization;
using CampusRift.Skills;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// P02: skill definitions for the three starter skills, the loadout on the player prefab and HUD wiring.
public static class SkillCoreSetup
{
    const string Data = "Assets/Skills/Core/Data/";
    const string Art = "Assets/CampusRiftUI/Art/Skills/";
    const string PlayerPrefab = "Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
    const string GameScene = "Assets/Scenes/SampleScene.unity";
    static readonly int[] RankCosts = { 0, 150, 400, 900, 1600 };

    [MenuItem("Campus Rift/V2/Setup Skill Loadout")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        var log = new List<string>();
        var hand = Definition("dai-thu-an", "Giant Hand Seal", "Đại Thủ Ấn", "HAND SEAL", Element.Tho, SkillRole.Control, 18, 30, "GiantHandSeal",
            "A giant palm slams the locked monster: heavy damage and a long stun.", "Bàn tay khổng lồ giáng xuống quái đang khóa: sát thương lớn và choáng lâu.");
        var wall = Definition("hu-khong-ket-gioi", "Void Wall", "Hư Không Kết Giới", "VOID WALL", Element.KhongGian, SkillRole.Defense, 0.35f, 15, "VoidWall",
            "Raise an energy wall that blocks monsters and projectiles. 3 charges, one returns every 12 s.", "Dựng bức tường năng lượng chặn quái và đạn. 3 lượt, hồi 1 lượt mỗi 12 giây.");
        var phantom = Definition("anh-phan-than", "Phantom Decoy", "Ảnh Phân Thân", "PHANTOM", Element.Am, SkillRole.Utility, 18, 20, "PhantomDecoy",
            "A running phantom draws monsters away for a few seconds.", "Phân thân chạy đi khiêu khích quái trong vài giây.");
        log.Add("definitions: " + hand.id + ", " + wall.id + ", " + phantom.id);

        var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
        int missing = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        if (missing > 0) log.Add("removed " + missing + " missing-script components from the player prefab");
        var handRuntime = Ensure<GiantHandRuntime>(root); handRuntime.definition = hand;
        var wallRuntime = Ensure<VoidWallRuntime>(root); wallRuntime.definition = wall;
        var phantomRuntime = Ensure<PhantomRuntime>(root); phantomRuntime.definition = phantom;
        var loadout = Ensure<SkillLoadout>(root);
        var so = new SerializedObject(loadout);
        var slots = so.FindProperty("slots"); slots.arraySize = SkillLoadout.SlotCount;
        // Default keeps the pre-V2 keys where possible: Q wall, F hand; the phantom moves from G to E.
        slots.GetArrayElementAtIndex(0).objectReferenceValue = wallRuntime;
        slots.GetArrayElementAtIndex(1).objectReferenceValue = phantomRuntime;
        slots.GetArrayElementAtIndex(2).objectReferenceValue = null;
        slots.GetArrayElementAtIndex(3).objectReferenceValue = handRuntime;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab); PrefabUtility.UnloadPrefabContents(root);
        log.Add("player prefab: 3 adapters + SkillLoadout [Q wall, E phantom, R empty, F hand]");

        var scene = EditorSceneManager.OpenScene(GameScene);
        var hud = Object.FindAnyObjectByType<GameplayHUD>();
        var template = AssetDatabase.LoadAssetAtPath<SkillSlotUI>("Assets/CampusRiftUI/Prefabs/SkillSlot.prefab");
        hud.Skills.slotTemplate = template; EditorUtility.SetDirty(hud.Skills);
        int hints = 0;
        foreach (var t in hud.GetComponentsInChildren<TMP_Text>(true))
            if (t.text.Contains("WASD") && t.text.Contains("E  Elevator")) { t.text = t.text.Replace("E  Elevator", "G  Elevator"); EditorUtility.SetDirty(t); hints++; }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        log.Add("scene: slot template " + (template != null) + ", control hints updated " + hints);

        var catalog = Resources.Load<LocalizationCatalog>("LocalizationCatalog");
        int added = 0;
        foreach (var pair in new[] { new[] { "EMPTY", "TRỐNG" }, new[] { "EMPTY SLOT", "Ô TRỐNG" },
                     new[] { "3 charges, one returns every 12 s", "3 lượt, hồi 1 lượt mỗi 12 giây" },
                     new[] { "Auto lock and cast on nearest monster", "Tự khóa và thi triển lên quái gần nhất" } })
        {
            if (catalog.entries.Exists(e => e.en == pair[0])) continue;
            catalog.entries.Add(new TranslationEntry { en = pair[0], vi = pair[1] }); added++;
        }
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        log.Add("localization entries added: " + added);
        return string.Join("\n", log);
    }

    static T Ensure<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }

    static SkillDefinition Definition(string id, string en, string vn, string shortName, Element element, SkillRole role,
        float cooldown, float spirit, string icon, string description, string descriptionVN)
    {
        string path = Data + id + ".asset";
        var d = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
        if (d == null) { d = ScriptableObject.CreateInstance<SkillDefinition>(); AssetDatabase.CreateAsset(d, path); }
        d.id = id; d.displayName = en; d.displayNameVN = vn; d.shortName = shortName;
        d.description = description; d.descriptionVN = descriptionVN;
        d.element = element; d.role = role; d.castType = CastType.Aimed; d.starter = true; d.unlockRealm = 0; d.unlockTier = 1;
        d.cooldown = cooldown; d.spiritCost = spirit;
        d.icon = AssetDatabase.LoadAssetAtPath<Sprite>(Art + icon + ".png");
        d.ranks = new SkillRank[5];
        for (int i = 0; i < 5; i++)
            d.ranks[i] = new SkillRank { effectMultiplier = 1f + 0.12f * i, cooldownMultiplier = 1f - 0.05f * i, upgradeCost = RankCosts[i], requiredRealm = i };
        EditorUtility.SetDirty(d);
        return d;
    }

    [MenuItem("Campus Rift/V2/Validate Skill Definitions")]
    public static string Validate()
    {
        var errors = new List<string>(); var ids = new HashSet<string>(); int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:SkillDefinition"))
        {
            var d = AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(guid)); count++;
            if (string.IsNullOrEmpty(d.id) || !System.Text.RegularExpressions.Regex.IsMatch(d.id, "^[a-z0-9]+(-[a-z0-9]+)*$")) errors.Add(d.name + ": id must be kebab-case");
            else if (!ids.Add(d.id)) errors.Add(d.id + ": duplicate id");
            if (string.IsNullOrEmpty(d.displayNameVN)) errors.Add(d.id + ": missing Vietnamese name");
            if (string.IsNullOrEmpty(d.displayName)) errors.Add(d.id + ": missing English name");
            if (d.ranks == null || d.ranks.Length != 5) errors.Add(d.id + ": needs 5 ranks");
        }
        string result = "SKILL DEFINITIONS " + (errors.Count == 0 ? "PASS" : "FAIL") + ": " + count + " assets " + string.Join("; ", errors);
        Debug.Log(result); return result;
    }
}
