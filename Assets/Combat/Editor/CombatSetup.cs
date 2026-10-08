using System.Collections.Generic;
using CampusRift;
using CampusRift.Combat;
using CampusRift.Localization;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// P03: materials + Ngự Kiếm config, combat components on the player prefab, Linh Lực bar on the HUD.
public static class CombatSetup
{
    const string Data = "Assets/Combat/Data/";
    const string PlayerPrefab = "Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
    const string GameScene = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Campus Rift/V2/Setup Player Combat")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        var log = new List<string>();
        var shader = Shader.Find("Campus Rift/Speed Force Additive");
        if (shader == null) throw new System.InvalidOperationException("Speed Force Additive shader is missing.");
        var streak = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFX/SpeedForce/Textures/Streak.png");
        var blade = Material(Data + "NguKiem_Blade.mat", shader, null, 1.5f, 0.05f);
        var trail = Material(Data + "NguKiem_Trail.mat", shader, streak, 2.4f, 0.2f);
        var reticle = Material(Data + "LockReticle.mat", shader, null, 2.4f, 0.1f);
        var config = AssetDatabase.LoadAssetAtPath<NguKiemConfig>(Data + "NguKiemConfig.asset");
        if (config == null) { config = ScriptableObject.CreateInstance<NguKiemConfig>(); AssetDatabase.CreateAsset(config, Data + "NguKiemConfig.asset"); }
        config.bladeMaterial = blade; config.trailMaterial = trail; EditorUtility.SetDirty(config);
        log.Add("materials + NguKiemConfig");

        var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
        Ensure<PlayerStats>(root); Ensure<SpiritPower>(root);
        Ensure<PlayerCombat>(root).config = config;
        Ensure<DodgeAbility>(root);
        Ensure<TargetLock>(root).reticleMaterial = reticle;
        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab); PrefabUtility.UnloadPrefabContents(root);
        log.Add("player prefab: PlayerStats, SpiritPower, PlayerCombat, DodgeAbility, TargetLock");

        var scene = EditorSceneManager.OpenScene(GameScene);
        var hud = Object.FindAnyObjectByType<GameplayHUD>();
        log.Add(BuildSpiritBar(hud));
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);

        var catalog = Resources.Load<LocalizationCatalog>("LocalizationCatalog");
        int added = 0;
        foreach (var pair in new[] { new[] { "SPIRIT", "LINH LỰC" }, new[] { "ATTACK", "TẤN CÔNG" }, new[] { "DASH", "NÉ" }, new[] { "LOCK", "KHÓA" },
                     new[] { "NOT ENOUGH SPIRIT", "KHÔNG ĐỦ LINH LỰC" }, new[] { "NOT ENOUGH STAMINA", "KHÔNG ĐỦ THỂ LỰC" } })
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

    static Material Material(string path, Shader shader, Texture texture, float intensity, float core)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader; m.SetFloat("_Intensity", intensity); m.SetFloat("_CoreBoost", core);
        if (texture != null) m.SetTexture("_MainTex", texture);
        EditorUtility.SetDirty(m); return m;
    }

    // Clones the energy row (label, value, track, fill) 46 px lower and widens the readability plate.
    static string BuildSpiritBar(GameplayHUD hud)
    {
        var vitals = (RectTransform)hud.transform.Find("Vitals");
        var existing = hud.GetComponent<PlayerSpiritUI>();
        if (existing != null && vitals.Find("SpiritTrack") != null) return "spirit bar already present";
        const float drop = 46f;
        RectTransform Clone(string source, string name)
        {
            var original = (RectTransform)vitals.Find(source);
            var copy = Object.Instantiate(original, vitals); copy.name = name;
            copy.anchoredPosition = original.anchoredPosition + Vector2.down * drop; return copy;
        }
        var label = Clone("EnergyLabel", "SpiritLabel"); var value = Clone("EnergyPlaceholder", "SpiritValue");
        var track = Clone("EnergyTrack", "SpiritTrack"); var fill = Clone("EnergyPlaceholderFill", "SpiritFill");
        label.GetComponent<TMP_Text>().text = "SPIRIT"; label.GetComponent<TMP_Text>().color = new Color(.25f, .95f, .72f);
        value.GetComponent<TMP_Text>().text = "100 / 100";
        // The value column was 60 px wide for "100%"; give "100 / 100" room, right-aligned to the same edge.
        value.sizeDelta = new Vector2(140, value.sizeDelta.y); value.anchoredPosition += Vector2.left * 80;
        value.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Right;
        var image = fill.GetComponent<Image>(); image.color = new Color(.25f, .95f, .72f);
        image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; image.fillAmount = 1;
        fill.sizeDelta = new Vector2(((RectTransform)vitals.Find("EnergyTrack")).sizeDelta.x, fill.sizeDelta.y);
        image.raycastTarget = false;
        var plate = (RectTransform)vitals.Find("Readability Plate");
        if (plate != null) plate.sizeDelta += Vector2.up * drop;
        vitals.sizeDelta += Vector2.up * drop;
        var ui = existing != null ? existing : hud.gameObject.AddComponent<PlayerSpiritUI>();
        ui.Fill = image; ui.Value = value.GetComponent<TMP_Text>(); ui.Label = label.GetComponent<TMP_Text>();
        EditorUtility.SetDirty(ui);
        return "spirit bar built";
    }
}
