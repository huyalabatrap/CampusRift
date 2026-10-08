#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.ARCore;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;

namespace CampusRift.AR.Editor
{
    public static class ARRiftSetup
    {
        const string Root = "Assets/ARRift/";
        [MenuItem("Campus Rift/AR/Install M1")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first");
            UIFoundationBuilder.ConfigureMobileDisplay();
            foreach (var dir in new[] { "Settings", "Scenes", "Prefabs" }) System.IO.Directory.CreateDirectory(Root + dir);
            AssetDatabase.Refresh();
            var settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(Root + "Settings/XRSettings.asset");
            if (settings == null) { settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>(); AssetDatabase.CreateAsset(settings, Root + "Settings/XRSettings.asset"); }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, settings, true);
            Configure(settings, BuildTargetGroup.Android, "UnityEngine.XR.ARCore.ARCoreLoader");
            Configure(settings, BuildTargetGroup.Standalone, "UnityEngine.XR.Simulation.SimulationLoader");
            var core = ARCoreSettings.GetOrCreateSettings();
            if (!AssetDatabase.Contains(core)) AssetDatabase.CreateAsset(core, Root + "Settings/ARCoreSettings.asset");
            core.requirement = ARCoreSettings.Requirement.Optional; core.depth = ARCoreSettings.Requirement.Optional;
            ARCoreSettings.currentSettings = core;

            string rendererPath = Root + "Settings/AR_Renderer.asset", pipelinePath = Root + "Settings/AR_RPAsset.asset";
            if (AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath) == null) AssetDatabase.CopyAsset("Assets/Settings/Mobile_Renderer.asset", rendererPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            foreach (var old in renderer.rendererFeatures) if (old != null) Object.DestroyImmediate(old, true);
            renderer.rendererFeatures.Clear();
            var background = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>(); background.name = "AR Background";
            AssetDatabase.AddObjectToAsset(background, renderer); renderer.rendererFeatures.Add(background); renderer.SetDirty();
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath) == null) AssetDatabase.CopyAsset("Assets/Settings/Mobile_RPAsset.asset", pipelinePath);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            var serialized = new SerializedObject(pipeline); var list = serialized.FindProperty("m_RendererDataList"); list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.FindProperty("m_DefaultRendererIndex").intValue = 0; serialized.ApplyModifiedPropertiesWithoutUndo();

            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Settings/PlaneGrid.mat");
            if (material == null) { material = new Material(Shader.Find("Campus Rift/AR/Plane Grid")); AssetDatabase.CreateAsset(material, Root + "Settings/PlaneGrid.mat"); }
            var plane = new GameObject("AR detected plane", typeof(MeshFilter), typeof(MeshRenderer), typeof(ARPlane), typeof(ARPlaneMeshVisualizer));
            plane.GetComponent<MeshRenderer>().sharedMaterial = material;
            var prefab = PrefabUtility.SaveAsPrefabAsset(plane, Root + "Prefabs/DetectedPlane.prefab"); Object.DestroyImmediate(plane);
            EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(pipeline); EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/DetectedPlane.prefab");
            var owner = new GameObject("AR Session"); var session = owner.AddComponent<ARSession>(); session.enabled = false;
            var arInput = owner.AddComponent<ARInputManager>(); arInput.enabled = false;
            var control = owner.AddComponent<ARXRLoaderControl>(); control.pipeline = pipeline;
            var originObject = new GameObject("XR Origin"); var origin = originObject.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset"); offset.transform.SetParent(originObject.transform, false); origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device; origin.CameraYOffset = 0;
            var cameraObject = new GameObject("AR Camera"); cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(offset.transform, false);
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .05f; camera.farClipPlane = 40; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            cameraObject.AddComponent<AudioListener>(); origin.Camera = camera;
            var driver = cameraObject.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new UnityEngine.InputSystem.InputActionProperty(new UnityEngine.InputSystem.InputAction("Position", UnityEngine.InputSystem.InputActionType.Value, "<XRHMD>/centerEyePosition"));
            driver.rotationInput = new UnityEngine.InputSystem.InputActionProperty(new UnityEngine.InputSystem.InputAction("Rotation", UnityEngine.InputSystem.InputActionType.Value, "<XRHMD>/centerEyeRotation"));
            driver.trackingStateInput = new UnityEngine.InputSystem.InputActionProperty(new UnityEngine.InputSystem.InputAction("Tracking State", UnityEngine.InputSystem.InputActionType.Value, "<XRHMD>/trackingState"));
            driver.positionInput.action.AddBinding("<HandheldARInputDevice>/devicePosition");
            driver.rotationInput.action.AddBinding("<HandheldARInputDevice>/deviceRotation");
            driver.trackingStateInput.action.AddBinding("<HandheldARInputDevice>/trackingState");
            var cameraManager = cameraObject.AddComponent<ARCameraManager>(); cameraManager.enabled = false;
            cameraManager.requestedFacingDirection = CameraFacingDirection.World;
            cameraManager.autoFocusRequested = true;
            cameraManager.requestedLightEstimation = LightEstimation.AmbientIntensity | LightEstimation.AmbientColor | LightEstimation.MainLightDirection | LightEstimation.MainLightIntensity | LightEstimation.AmbientSphericalHarmonics;
            var cameraBackground = cameraObject.AddComponent<ARCameraBackground>(); cameraBackground.enabled = false;
            cameraObject.AddComponent<AROcclusionManager>().enabled = false;
            camera.GetUniversalAdditionalCameraData().SetRenderer(0);
            var planes = originObject.AddComponent<ARPlaneManager>(); planes.enabled = false; planes.requestedDetectionMode = PlaneDetectionMode.Horizontal; planes.planePrefab = prefab;
            var raycast = originObject.AddComponent<ARRaycastManager>(); raycast.enabled = false;
            var anchors = originObject.AddComponent<ARAnchorManager>(); anchors.enabled = false;
            control.sessionComponents = new Behaviour[] { arInput, cameraManager, cameraBackground, planes, raycast, anchors, session };
            var light = new GameObject("AR estimated sunlight").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1; light.shadows = LightShadows.Soft; light.transform.rotation = Quaternion.Euler(45, -30, 0);
            EditorSceneManager.SaveScene(scene, Root + "Scenes/ARRiftBattle.unity");
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(x => x.path == scene.path)) scenes.Add(new EditorBuildSettingsScene(scene.path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        static void Configure(XRGeneralSettingsPerBuildTarget all, BuildTargetGroup group, string loader)
        {
            if (!all.HasSettingsForBuildTarget(group)) all.CreateDefaultSettingsForBuildTarget(group);
            if (!all.HasManagerSettingsForBuildTarget(group)) all.CreateDefaultManagerSettingsForBuildTarget(group);
            var settings = all.SettingsForBuildTarget(group); settings.InitManagerOnStart = false;
            settings.Manager.automaticLoading = false; settings.Manager.automaticRunning = false;
            if (!XRPackageMetadataStore.AssignLoader(settings.Manager, loader, group)) throw new System.InvalidOperationException("Cannot assign " + loader);
            EditorUtility.SetDirty(settings); EditorUtility.SetDirty(settings.Manager);
        }
    }
}
#endif
