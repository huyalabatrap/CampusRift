using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CampusRift.UI
{
    // Uses URP's supported full-screen pass implementation in both RenderGraph and compatibility mode.
    public sealed class ComicInkFeature : FullScreenPassRendererFeature
    {
        public override void Create()
        {
            injectionPoint = InjectionPoint.AfterRenderingPostProcessing;
            fetchColorBuffer = true;
            bindDepthStencilAttachment = true;
            // The shader reconstructs surface normals from depth, avoiding another geometry pass.
            requirements = ScriptableRenderPassInput.Depth;
            passMaterial = Resources.Load<Material>("Comic/ComicInk");
            base.Create();
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game || !ComicRendering.Enabled) return;
            if (renderingData.cameraData.camera.gameObject.scene.name != "SampleScene") return;
            base.AddRenderPasses(renderer, ref renderingData);
        }
    }
    public static class ComicRendering
    {
        public static bool? PreviewEnabled;
        static bool PcQuality
        {
            get { var names=QualitySettings.names; int index=QualitySettings.GetQualityLevel(); return index>=0 && index<names.Length && names[index]=="PC"; }
        }
        public static bool Enabled => !Application.isMobilePlatform && !Controls.CampusInput.Mobile && PcQuality &&
            (PreviewEnabled ?? (SettingsManager.Instance == null || SettingsManager.Instance.Current.ComicEffects));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { PreviewEnabled = null; }
    }
}
