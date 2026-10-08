using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Only operates on this package's GameReadyDragons folder.
public sealed class GameReadyDragonImporter : AssetPostprocessor
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
        if (!assetPath.StartsWith("Assets/GameReadyDragons/", StringComparison.Ordinal)) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.isReadable = GameReadyDragonSetup.Validating;
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
        ConfigureClips(importer, ReadInfo(assetPath));
    }
    public static void ConfigureClips(ModelImporter importer, ModelInfo info)
    {
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
        // Unity may expose no default takes during the initial preprocessing callback.
        if (info == null || info.clips == null || clips == null || clips.Length == 0) return;
        foreach (var clip in clips)
        {
            var metadata = info.clips.FirstOrDefault(c => clip.name.Contains(c.name));
            if (metadata != null) { clip.name = metadata.name; clip.loopTime = metadata.loop; clip.loopPose = false; }
            clip.lockRootRotation = true; clip.lockRootPositionXZ = true; clip.lockRootHeightY = true;
            clip.keepOriginalOrientation = true; clip.keepOriginalPositionXZ = true; clip.keepOriginalPositionY = true;
        }
        importer.clipAnimations = clips;
    }
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/GameReadyDragons/", StringComparison.Ordinal)) return;
        var importer = (TextureImporter)assetImporter;
        string name = Path.GetFileNameWithoutExtension(assetPath);
        importer.maxTextureSize = 4096;
        if (name.EndsWith("_Normal", StringComparison.Ordinal))
            importer.textureType = TextureImporterType.NormalMap;
        else importer.sRGBTexture = name.EndsWith("_BaseColor", StringComparison.Ordinal);
    }
}

public static class GameReadyDragonSetup
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
        public int triangles, clips, bones, unweighted_vertices, max_influences, stationary_clips, loop_setting_mismatches;
        public float bind_height_m, animation_bounds_height_m;
        public bool passed;
        public string[] clip_names;
    }

    [MenuItem("Tools/Game Ready Dragons/Build materials, controllers and prefabs")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Validating = true;
        var results = new List<ModelCheck>();
        foreach (var path in AssetDatabase.FindAssets("t:Model", new [] { "Assets/GameReadyDragons" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.SaveAndReimport();
            GameReadyDragonImporter.ConfigureClips(importer, GameReadyDragonImporter.ReadInfo(path));
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
            var idle = states["Hover_Idle"]; var walk = states["Fly_Cruise"]; var run = states["Fly_Fast"];
            machine.defaultState = idle;
            AddSpeed(idle, walk, AnimatorConditionMode.Greater, .1f);
            AddSpeed(walk, idle, AnimatorConditionMode.Less, .1f);
            AddSpeed(walk, run, AnimatorConditionMode.Greater, .65f);
            AddSpeed(run, walk, AnimatorConditionMode.Less, .65f);
            var metadataForClips = GameReadyDragonImporter.ReadInfo(path);
            foreach (var pair in states)
            {
                if (pair.Value == idle || pair.Value == walk || pair.Value == run) continue;
                controller.AddParameter(pair.Key, AnimatorControllerParameterType.Trigger);
                var transition = machine.AddAnyStateTransition(pair.Value);
                transition.hasExitTime = false; transition.duration = .15f; transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.If, 0f, pair.Key);
                string next = null;
                if (pair.Key == "Takeoff_Start") next = "Takeoff_Lift";
                else if (pair.Key == "Takeoff_Lift") next = "Takeoff_ToCruise";
                else if (pair.Key == "Takeoff_ToCruise") next = "Fly_Cruise";
                else if (pair.Key == "Land_Touchdown") next = "Land_Settle";
                else if (pair.Key == "Land_Settle") next = states.ContainsKey("Idle_Ground") ? "Idle_Ground" : "Hover_Idle";
                else if (pair.Key == "Dive_Start") next = "Dive_Loop";
                else if (pair.Key == "Dive_Recover") next = "Fly_Cruise";
                else if (pair.Key == "Spell_Start") next = "Spell_Loop";
                else if (pair.Key == "Spell_End") next = "Hover_Idle";
                else if (pair.Key == "Air_DeathStart") next = "Air_DeathFall";
                else if (pair.Key == "Air_DeathImpact" || pair.Key == "Ground_Death") next = null;
                else if (!metadataForClips.clips.First(c => c.name == id + "_" + pair.Key).loop)
                    next = pair.Key.StartsWith("Ground_") || pair.Key.StartsWith("Wing_") ? "Idle_Ground" : "Hover_Idle";
                if (next != null && states.ContainsKey(next))
                {
                    var exit = pair.Value.AddTransition(states[next]); exit.hasExitTime = true; exit.exitTime = 1f; exit.duration = .15f;
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
            var metadata = GameReadyDragonImporter.ReadInfo(path);
            bool scaleCorrect = metadata == null || Mathf.Abs(check.bind_height_m - metadata.height_m) < metadata.height_m * .02f;
            foreach (var clip in clips)
            {
                var info = metadata.clips.FirstOrDefault(c => c.name == clip.name);
                if (info == null || clip.isLooping != info.loop) check.loop_setting_mismatches++;
                clip.SampleAnimation(instance, 0f);
                var transforms = instance.GetComponentsInChildren<Transform>(true);
                var positions = transforms.Select(t => t.localPosition).ToArray();
                var rotations = transforms.Select(t => t.localRotation).ToArray();
                clip.SampleAnimation(instance, clip.length * .37f);
                bool moved = false;
                for (int i = 0; i < transforms.Length; i++)
                    if (Vector3.Distance(positions[i], transforms[i].localPosition) > .0001f || Quaternion.Angle(rotations[i], transforms[i].localRotation) > .05f) moved = true;
                if (!moved) check.stationary_clips++;
            }
            // Return the prefab to the FBX bind pose after sampling, retaining assigned components.
            var bindSource = source.GetComponentsInChildren<Transform>(true);
            var bindInstance = instance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < bindSource.Length && i < bindInstance.Length; i++)
            { bindInstance[i].localPosition = bindSource[i].localPosition; bindInstance[i].localRotation = bindSource[i].localRotation; bindInstance[i].localScale = bindSource[i].localScale; }
            check.passed = check.clips == metadata.clips.Length && check.triangles == 58000 && check.unweighted_vertices == 0 && check.max_influences <= 4 && check.bones >= 60 && scaleCorrect && check.stationary_clips == 0 && check.loop_setting_mismatches == 0;
            PrefabUtility.SaveAsPrefabAsset(instance, generated + "/" + file + ".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            results.Add(check);
        }
        Validating = false;
        foreach (var result in results) ((ModelImporter)AssetImporter.GetAtPath(result.asset)).SaveAndReimport();
        AssetDatabase.SaveAssets();
        var validation = new Validation { models = results.ToArray(), unity_version = Application.unityVersion, passed = results.Count == 3 && results.All(r => r.passed) };
        File.WriteAllText(OutputPath("Dragons_Unity_Validation.json"), JsonUtility.ToJson(validation, true));
        Debug.Log("DRAGON_VALIDATION " + JsonUtility.ToJson(validation));
        if (!validation.passed) throw new InvalidOperationException("Dragon validation failed; see Dragons_Unity_Validation.json");
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
        AssetDatabase.ExportPackage("Assets/GameReadyDragons", OutputPath("Dragons_Flight.unitypackage"), ExportPackageOptions.Recurse);
    }
}
