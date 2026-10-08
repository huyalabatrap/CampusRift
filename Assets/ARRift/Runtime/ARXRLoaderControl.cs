using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace CampusRift.AR
{
    // Scene owns XR and its pipeline. Ordinary scenes never initialize either.
    [DefaultExecutionOrder(-1000)]
    public sealed class ARXRLoaderControl : MonoBehaviour
    {
        public RenderPipelineAsset pipeline;
        public Material runeMaterial;
        public Behaviour[] sessionComponents;
        public bool Ready { get; private set; }
        public string Failure { get; private set; }
        XRManagerSettings manager;
        RenderPipelineAsset previousPipeline;
        RenderPipelineAsset sessionPipeline;
        public UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset SessionPipeline=>sessionPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        bool ownsLoader, changedPipeline;
        ScreenOrientation previousOrientation;
        bool changedOrientation;

        void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            previousOrientation = Screen.orientation;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            changedOrientation = true;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            gameObject.AddComponent<ARDeviceDiagnostics>();
#endif
        }

        IEnumerator Start()
        {
            SetComponents(false);
            var consent=GetComponent<ARSessionBootstrap>();
            while(consent!=null&&!consent.Accepted)yield return null;
            // Wait for Android's asynchronous surface rotation before ARCore
            // initializes and caches display geometry for this session.
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            while (Screen.width < Screen.height || Screen.orientation != ScreenOrientation.LandscapeLeft)
                yield return null;
#endif
            previousPipeline = QualitySettings.renderPipeline;
            if (pipeline != null) { sessionPipeline=Instantiate(pipeline);sessionPipeline.name="AR session pipeline";QualitySettings.renderPipeline = sessionPipeline; changedPipeline = true; }
            SetComponents(false);
            // Let the rendering pipeline become active before InitializeLoader.
            yield return null;
            foreach (var background in FindObjectsByType<ARCameraBackground>(FindObjectsSortMode.None))
            {
                var camera = background.GetComponent<Camera>();
                camera.rect = new Rect(0, 0, 1, 1);
                camera.targetTexture = null;
                camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().SetRenderer(0);
            }
            manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null) { Failure = "XR settings unavailable"; yield break; }
            if (manager.activeLoader != null) { Failure = "XR already owned by another session"; yield break; }
            ownsLoader = true;
            yield return manager.InitializeLoader();
            if (manager.activeLoader == null) { Failure = "AR provider unavailable"; yield break; }
            yield return ARSession.CheckAvailability();
            if(ARSession.state==ARSessionState.Unsupported){Failure="ARCore unsupported";Shutdown();yield break;}
            manager.StartSubsystems();
            SetComponents(true);
            Ready = true;
        }

        void SetComponents(bool value)
        {
            if (sessionComponents == null) return;
            foreach (var component in sessionComponents) if (component != null) component.enabled = value;
        }

        public void Shutdown()
        {
            StopAllCoroutines();
            Ready = false;
            // ARAnchor.OnDisable removes itself from the provider: run it while the
            // anchor manager and subsystem are still active, before stopping XR.
            var anchors = FindObjectsByType<ARAnchor>(FindObjectsSortMode.None);
            foreach (var anchor in anchors) if (anchor.enabled) anchor.enabled = false;
            SetComponents(false);
            if (ownsLoader && manager != null && manager.isInitializationComplete)
            {
                if (manager.activeLoader != null) manager.StopSubsystems();
                manager.DeinitializeLoader();
            }
            ownsLoader = false;
            if (changedPipeline) QualitySettings.renderPipeline = previousPipeline;
            changedPipeline = false;
            if(sessionPipeline!=null)Destroy(sessionPipeline);sessionPipeline=null;
            if (changedOrientation) Screen.orientation = previousOrientation;
            changedOrientation = false;
        }
        void OnDisable() { Shutdown(); }
        void OnDestroy() { Shutdown(); }
    }
}
