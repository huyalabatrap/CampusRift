using CampusRift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FlashlightSetup
{
    const string Root = "Assets/VFX/Flashlight/";
    const string ModelPath = Root + "Model/Flashlight.fbx";
    const string PrefabPath = "Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Campus Rift/VFX/Setup Player Flashlight")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        // Quaternius colours are linear glTF factors; URP material colours are authored in gamma.
        var body = Lit("Flashlight Body", new Color(.219f, .15f, .044f).gamma, .55f, .55f);
        var grip = Lit("Flashlight Grip", new Color(.026f, .026f, .026f).gamma, 0, .3f);
        var trim = Lit("Flashlight Trim", new Color(.148f, .101f, .031f).gamma, .6f, .6f);
        var lens = Lit("Flashlight Lens", new Color(.8f, .85f, .9f), 0, .95f);
        lens.EnableKeyword("_EMISSION"); lens.SetColor("_EmissionColor", Color.black);
        lens.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.isReadable = true;                    // PlayerFlashlight reads the lens submesh to orient the model
        importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Yellow"), body);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Black"), grip);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "LightBlue"), lens);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "DarkYellow"), trim);
        importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var bounds = model.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
        var scale = model.GetComponentInChildren<MeshFilter>().transform.lossyScale;
        Debug.Log("Flashlight model bounds " + Vector3.Scale(bounds.size, scale).ToString("0.000") + " (expected ~0.24 m long)");

        var cookiePath = Root + "FlashlightCookie.png";
        var cookieImporter = (TextureImporter)AssetImporter.GetAtPath(cookiePath);
        cookieImporter.textureType = TextureImporterType.Cookie; cookieImporter.alphaSource = TextureImporterAlphaSource.FromGrayScale;
        cookieImporter.wrapMode = TextureWrapMode.Clamp; cookieImporter.mipmapEnabled = false;
        cookieImporter.SaveAndReimport();
        var cookie = AssetDatabase.LoadAssetAtPath<Texture2D>(cookiePath);

        void Configure(GameObject player)
        {
            var light = player.GetComponent<PlayerFlashlight>();
            if (light == null) light = player.AddComponent<PlayerFlashlight>();
            light.model = model; light.lensMaterial = lens; light.cookie = cookie;
            light.intensity = 9; light.range = 18; light.spotAngle = 70; light.innerSpotAngle = 34; light.downPitch = 8;
            light.fillIntensity = .35f; light.fillRange = 3.2f;
            EditorUtility.SetDirty(light);
        }
        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        Configure(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath); PrefabUtility.UnloadPrefabContents(prefab);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Configure(Object.FindAnyObjectByType<CampusExplorer>().gameObject);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CAMPUS RIFT: Player flashlight configured.");
    }

    static Material Lit(string name, Color color, float metallic, float smoothness)
    {
        string path = Root + "Model/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }
}
