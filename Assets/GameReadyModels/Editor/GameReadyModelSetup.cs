using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Only operates on this package's GameReadyModels folder.
public sealed class GameReadyModelImporter : AssetPostprocessor
{
    [Serializable] public class ClipInfo { public string name; public bool loop; public int frames; public int fps; }
    [Serializable] public class ModelInfo { public string id; public string name; public float height_m; public ClipInfo[] clips; }
    public static ModelInfo ReadInfo(string modelPath)
    {
        string report = Path.Combine(Path.GetDirectoryName(modelPath), "report.json");
        return File.Exists(report) ? JsonUtility.FromJson<ModelInfo>(File.ReadAllText(report)) : null;
    }
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/GameReadyModels/", StringComparison.Ordinal)) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.isReadable = GameReadyModelSetup.Validating;
        importer.globalScale = 1f;
        importer.useFileUnits = true;
        importer.importNormals = ModelImporterNormals.Import;
        importer.importTangents = ModelImporterTangents.CalculateMikk;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.skinWeights = ModelImporterSkinWeights.Custom;
        importer.maxBonesPerVertex = 4;
        importer.minBoneWeight = 0f;
        importer.optimizeGameObjects = false;
        importer.preserveHierarchy = true;
        // Keep the animated skeletal Root inside the model; gameplay moves the prefab.
        importer.motionNodeName = "";
        var info = ReadInfo(assetPath);
        var clips = importer.defaultClipAnimations;
        if (info != null && info.clips != null)
        {
            foreach (var clip in clips)
            {
                var metadata = info.clips.FirstOrDefault(c => clip.name.Contains(c.name));
                if (metadata != null)
                {
                    clip.name = metadata.name;
                    clip.loopTime = metadata.loop;
                    clip.loopPose = false;
                }
                clip.lockRootRotation = true;
                clip.lockRootPositionXZ = true;
                clip.lockRootHeightY = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionXZ = true;
                clip.keepOriginalPositionY = true;
            }
            importer.clipAnimations = clips;
        }
    }
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/GameReadyModels/", StringComparison.Ordinal)) return;
        var importer = (TextureImporter)assetImporter;
        string name = Path.GetFileNameWithoutExtension(assetPath);
        importer.maxTextureSize = 4096;
        if (name.EndsWith("_Normal", StringComparison.Ordinal))
            importer.textureType = TextureImporterType.NormalMap;
        else importer.sRGBTexture = name.EndsWith("_BaseColor", StringComparison.Ordinal);
    }
}

public static class GameReadyModelSetup
{
    public static bool Validating;
    static string OutputPath(string name)
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-gameReadyOutput");
        string folder = index >= 0 && index + 1 < args.Length ? args[index+1] : Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, name);
    }
    [Serializable] public class Validation { public ModelCheck[] models; public string unity_version; public bool passed; }
    [Serializable] public class ModelCheck
    {
        public string id, asset;
        public int triangles, clips, bones, unweighted_vertices, max_influences;
        public float bind_height_m, animation_bounds_height_m;
        public bool passed;
        public string[] clip_names;
    }

    [MenuItem("Tools/Game Ready Models/Build materials, controllers and prefabs")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Validating = true;
        var results = new List<ModelCheck>();
        foreach (var path in AssetDatabase.FindAssets("t:Model", new [] { "Assets/GameReadyModels" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.SaveAndReimport();
            string folder = Path.GetDirectoryName(path).Replace('\\','/');
            string file = Path.GetFileNameWithoutExtension(path);
            string id = file.Substring(0, 3);
            string generated = folder + "/Generated";
            Directory.CreateDirectory(generated);
            AssetDatabase.Refresh();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).OrderBy(c => c.name).ToArray();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            bool urp = shader != null && UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            if (!urp) shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No Lit or Standard shader available.");
            string materialPath = generated + "/" + file + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            material.shader = shader;
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Textures/" + id + "_BaseColor.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Textures/" + id + "_Normal.png");
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Textures/" + id + "_MetallicSmoothness.png");
            var occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Textures/" + id + "_Occlusion.png");
            if (color != null) material.SetTexture(urp ? "_BaseMap" : "_MainTex", color);
            if (normal != null) { material.SetTexture("_BumpMap", normal); material.EnableKeyword("_NORMALMAP"); }
            if (metallic != null) { material.SetTexture("_MetallicGlossMap", metallic); material.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP"); }
            if (occlusion != null) { material.SetTexture("_OcclusionMap", occlusion); material.EnableKeyword("_OCCLUSIONMAP"); }
            EditorUtility.SetDirty(material);
            string controllerPath = generated + "/" + file + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            controller.parameters = new AnimatorControllerParameter[0];
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var states = new Dictionary<string, AnimatorState>();
            foreach (var clip in clips)
            {
                string shortName = clip.name.StartsWith(id + "_") ? clip.name.Substring(4) : clip.name;
                var state = machine.AddState(shortName); state.motion = clip;
                states[shortName] = state;
            }
            if (!states.ContainsKey("Idle_Breathe") || !states.ContainsKey("Walk_Forward") || !states.ContainsKey("Run_Forward"))
                throw new InvalidOperationException("Missing locomotion clips in " + path);
            var idle = states["Idle_Breathe"]; var walk = states["Walk_Forward"]; var run = states["Run_Forward"];
            machine.defaultState = idle;
            AddSpeed(idle, walk, AnimatorConditionMode.Greater, .1f);
            AddSpeed(walk, idle, AnimatorConditionMode.Less, .1f);
            AddSpeed(walk, run, AnimatorConditionMode.Greater, .65f);
            AddSpeed(run, walk, AnimatorConditionMode.Less, .65f);
            foreach (var pair in states)
            {
                if (pair.Value == idle || pair.Value == walk || pair.Value == run) continue;
                controller.AddParameter(pair.Key, AnimatorControllerParameterType.Trigger);
                var transition = machine.AddAnyStateTransition(pair.Value);
                transition.hasExitTime = false; transition.duration = .12f; transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.If, 0f, pair.Key);
                if (!pair.Key.StartsWith("Death_") && pair.Key != "Despawn")
                {
                    var exit = pair.Value.AddTransition(idle); exit.hasExitTime = true; exit.exitTime = 1f; exit.duration = .12f;
                }
            }
            EditorUtility.SetDirty(controller);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = file;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            var check = new ModelCheck { id = id, asset = path, clips = clips.Length, clip_names = clips.Select(c => c.name).ToArray() };
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                check.bones += renderer.bones.Length;
                var mesh = renderer.sharedMesh;
                for (int s = 0; s < mesh.subMeshCount; s++) check.triangles += (int)mesh.GetIndexCount(s) / 3;
                var influence = mesh.GetBonesPerVertex();
                foreach (byte count in influence) { if (count == 0) check.unweighted_vertices++; check.max_influences = Math.Max(check.max_influences, count); }
                influence.Dispose();
                float low = float.PositiveInfinity, high = float.NegativeInfinity;
                var bounds = mesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    float y = renderer.transform.TransformPoint(point).y;
                    low = Mathf.Min(low, y); high = Mathf.Max(high, y);
                }
                check.bind_height_m = Mathf.Max(check.bind_height_m, high - low);
                check.animation_bounds_height_m = Mathf.Max(check.animation_bounds_height_m, renderer.bounds.size.y);
            }
            var metadata = GameReadyModelImporter.ReadInfo(path);
            bool scaleCorrect = metadata == null || Mathf.Abs(check.bind_height_m - metadata.height_m) < metadata.height_m * .02f;
            check.passed = check.clips == 28 && check.triangles >= 30000 && check.triangles <= 60000 && check.unweighted_vertices == 0 && check.max_influences <= 4 && check.bones >= 30 && scaleCorrect;
            PrefabUtility.SaveAsPrefabAsset(instance, generated + "/" + file + ".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            results.Add(check);
        }
        Validating = false;
        foreach (var result in results) ((ModelImporter)AssetImporter.GetAtPath(result.asset)).SaveAndReimport();
        AssetDatabase.SaveAssets();
        var validation = new Validation { models = results.ToArray(), unity_version = Application.unityVersion, passed = results.Count == 7 && results.All(r => r.passed) };
        File.WriteAllText(OutputPath("Unity_Validation.json"), JsonUtility.ToJson(validation, true));
        Debug.Log("GAME_READY_VALIDATION " + JsonUtility.ToJson(validation));
        if (!validation.passed) throw new InvalidOperationException("Game Ready validation failed; see Unity_Validation.json");
    }
    static void AddSpeed(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float value)
    {
        var transition = from.AddTransition(to); transition.hasExitTime = false; transition.duration = .15f;
        transition.AddCondition(mode, value, "Speed");
    }
    // Used in the isolated QA project; emits a portable package with prefab dependencies.
    public static void ValidateAndExport()
    {
        Build();
        AssetDatabase.ExportPackage("Assets/GameReadyModels", OutputPath("GameReadyModels.unitypackage"), ExportPackageOptions.Recurse);
    }
}
