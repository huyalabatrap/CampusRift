#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CampusRift.SkyBeast
{
    public static class P13LookFix
    {
        public static void Apply()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.72f,.75f,.8f);
            RenderSettings.ambientEquatorColor=new Color(.60f,.62f,.64f);
            RenderSettings.ambientGroundColor=new Color(.40f,.42f,.44f);
            var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.7f,.73f,.77f));RenderSettings.ambientProbe=probe;
            var sun=RenderSettings.sun;
            if(sun==null)foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){sun=light;break;}
            if(sun!=null){sun.color=new Color(1,.975f,.92f);sun.intensity=1.25f;RenderSettings.sun=sun;EditorUtility.SetDirty(sun);}
            foreach(var id in new[]{"Faded-ivory-plaster","Faded-ochre-plaster"})
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/CampusLook/Materials/"+id+".mat");
                m.SetFloat("_NormalScale",.85f);m.SetFloat("_Age",.40f);
                m.SetColor("_BaseColor",id.Contains("ochre")?new Color(1.45f,1.38f,1.15f):new Color(1.35f,1.34f,1.28f));EditorUtility.SetDirty(m);
            }
            var brick=AssetDatabase.LoadAssetAtPath<Material>("Assets/CampusLook/Materials/Red-brick-courtyard.mat");brick.SetColor("_BaseColor",new Color(1.22f,1.05f,.94f));EditorUtility.SetDirty(brick);
            var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/CampusLook/Materials/Dusty-window-glass.mat");glass.shader=Shader.Find("Campus Rift/Dusty Window");glass.SetColor("_BaseColor",new Color(.4f,.52f,.55f,.24f));glass.renderQueue=3000;EditorUtility.SetDirty(glass);
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if(profile.TryGet<ColorAdjustments>(out var color))color.postExposure.Override(.55f);
            if(profile.TryGet<WhiteBalance>(out var white)){white.temperature.Override(0);white.tint.Override(0);}
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        }
    }
}
#endif
