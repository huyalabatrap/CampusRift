using System.Linq;
using CampusRift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SpeedForceSetup
{
    const string Root = "Assets/VFX/SpeedForce/";
    const string PrefabPath = "Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Campus Rift/VFX/Setup Speed Force Boost")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "Textures" }))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = true; importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "SpeedForceAdditive.shader");
        var bolt = MaterialFor(shader, "SF_Bolt", "BoltLine", 6f, .9f);
        var crackle = MaterialFor(shader, "SF_Crackle", "CrackleSheet", 3.2f, .7f);
        var fork = MaterialFor(shader, "SF_Fork", "ForkSheet", 4f, .8f);
        var spark = MaterialFor(shader, "SF_Spark", "Streak", 3.5f, .5f);
        var flare = MaterialFor(shader, "SF_Flare", "Flare", 1.3f, .5f);
        var lines = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/SpeedLines.png");
        var clips = new[] { "laserSmall_000", "laserSmall_001", "laserSmall_002" }.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/" + n + ".ogg")).Where(c => c != null).ToArray();

        void Configure(GameObject player)
        {
            var vfx = player.GetComponent<SpeedForceVFX>();
            if (vfx == null) vfx = player.AddComponent<SpeedForceVFX>();
            vfx.boltMaterial = bolt; vfx.crackleMaterial = crackle; vfx.forkMaterial = fork; vfx.sparkMaterial = spark; vfx.flareMaterial = flare;
            vfx.speedLinesTexture = lines; vfx.ignitionClips = clips;
            EditorUtility.SetDirty(vfx);
            // Afterimages stay, re-tinted from lightning gold to rift violet and a little lighter.
            var trail = player.GetComponent<CharacterAfterimageTrail>();
            if (trail != null)
            {
                trail.freshColor = new Color(1.6f, 1.05f, .3f, 1); trail.fadedColor = new Color(.7f, .3f, 1.4f, 1);
                trail.opacity = .28f; trail.lifetime = .3f;
                EditorUtility.SetDirty(trail);
            }
        }

        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        Configure(prefab);
        PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);

        var scene = EditorSceneManager.OpenScene(ScenePath);
        var explorer = Object.FindAnyObjectByType<CampusExplorer>();
        Configure(explorer.gameObject);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CAMPUS RIFT: Speed Force boost VFX configured on " + explorer.name + " and " + PrefabPath);
    }

    static Material MaterialFor(Shader shader, string name, string texture, float intensity, float core)
    {
        string path = Root + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/" + texture + ".png"));
        material.SetFloat("_Intensity", intensity); material.SetFloat("_CoreBoost", core);
        EditorUtility.SetDirty(material);
        return material;
    }
}
