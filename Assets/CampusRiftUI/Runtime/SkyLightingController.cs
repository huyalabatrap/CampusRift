using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CampusRift.UI
{
    public enum SkyPreset { Default, Dusk, Night, BloodMoon, Inferno, RedEclipse }

    // Lives with settings across scenes. Only runtime copies of sky materials are edited.
    [DisallowMultipleComponent]
    public sealed class SkyLightingController : MonoBehaviour
    {
        public float Brightness {get;private set;}=1;
        SettingsManager settings;
        Scene scene;
        Material originalSky,runtimeSky;
        Light sun;
        float sunIntensity,ambientIntensity,reflectionIntensity,exposure;
        Color sunColor,ambientSky,ambientEquator,ambientGround,ambientLight,fogColor,skyTint,groundColor;
        SphericalHarmonicsL2 ambientProbe;
        bool captured,hasExposure,hasTint,hasGround;
        // The level look is layered on top of the scene's own values; the brightness setting then scales the result.
        SkyPreset preset=SkyPreset.Default;
        Quaternion sunRotation;
        Color baseSunColor,baseSkyTint,baseGround,baseFog;
        float sunScale=1,exposureScale=1,ambientScale=1;
        Color ambientTint=Color.white;
        float fireStorm;
        bool furySky;
        Material stormSky;
        public float FireStormIntensity=>fireStorm;
        Color realSkyTint=Color.white;
        public void SetFireStorm(float intensity,bool fury=false){float value=Mathf.Clamp01(intensity);if(Mathf.Abs(value-fireStorm)<.002f&&fury==furySky)return;fireStorm=value;furySky=fury;SetBrightness(Brightness);}
        public float DawnProgress{get;private set;}
        bool dawn;
        public void SetDawnProgress(float progress)
        {
            if(!dawn){SetPreset(SkyPreset.Default);dawn=true;}
            DawnProgress=Mathf.Clamp01(progress);SetFireStorm(1-DawnProgress);SetBrightness(Brightness);
            if(sun!=null){sun.color=Color.Lerp(new Color(1,.52f,.18f),new Color(1,.88f,.68f),DawnProgress);var rotation=sunRotation.eulerAngles;sun.transform.rotation=Quaternion.Euler(Mathf.Lerp(8,18,DawnProgress),rotation.y,rotation.z);}
        }
        public SkyPreset Preset=>preset;

        public void Initialize(SettingsManager manager)
        {
            if(settings!=null)return;
            settings=manager;settings.Changed+=Apply;
            SceneManager.sceneLoaded+=SceneLoaded;
            Capture();
        }
        void SceneLoaded(Scene loaded,LoadSceneMode mode)
        {if(loaded==SceneManager.GetActiveScene())Capture();}
        void Capture()
        {
            if(runtimeSky!=null)Destroy(runtimeSky);
            scene=SceneManager.GetActiveScene();
            originalSky=RenderSettings.skybox;runtimeSky=null;
            hasExposure=hasTint=hasGround=false;
            if(originalSky!=null)
            {
                runtimeSky=new Material(originalSky){name=originalSky.name+" (runtime sky)",hideFlags=HideFlags.DontSave};
                hasExposure=runtimeSky.HasProperty("_Exposure");if(hasExposure)exposure=runtimeSky.GetFloat("_Exposure");
                hasTint=runtimeSky.HasProperty("_SkyTint");if(hasTint)skyTint=runtimeSky.GetColor("_SkyTint");
                hasGround=runtimeSky.HasProperty("_GroundColor");if(hasGround)groundColor=runtimeSky.GetColor("_GroundColor");
                RenderSettings.skybox=runtimeSky;
                stormSky=Resources.Load<Material>("CampusLook/sky-inferno");
                if(runtimeSky.HasProperty("_StormTex")&&stormSky!=null)runtimeSky.SetTexture("_StormTex",stormSky.GetTexture("_MainTex"));
                if(runtimeSky.HasProperty("_Tint"))realSkyTint=runtimeSky.GetColor("_Tint");
            }
            sun=RenderSettings.sun;
            if(sun==null)
                foreach(var light in FindObjectsByType<Light>())
                    if(light.type==LightType.Directional && light.gameObject.scene==scene){sun=light;break;}
            if(sun!=null){sunIntensity=sun.intensity;sunColor=sun.color;sunRotation=sun.transform.rotation;}
            ambientIntensity=RenderSettings.ambientIntensity;reflectionIntensity=RenderSettings.reflectionIntensity;
            ambientSky=RenderSettings.ambientSkyColor;ambientEquator=RenderSettings.ambientEquatorColor;
            ambientGround=RenderSettings.ambientGroundColor;ambientLight=RenderSettings.ambientLight;
            ambientProbe=RenderSettings.ambientProbe;fogColor=RenderSettings.fogColor;
            baseSunColor=sunColor;baseSkyTint=skyTint;baseGround=groundColor;baseFog=fogColor;
            preset=SkyPreset.Default;sunScale=exposureScale=ambientScale=1;ambientTint=Color.white;
            captured=true;SetBrightness(settings.Current.SkyBrightness);
        }
        // Level look (P05-T05). Default restores the scene as authored.
        public void SetPreset(SkyPreset value)
        {
            dawn=false;DawnProgress=0;
            preset=value;
            if(!captured)return;
            // Swap only the visual sky texture. The authored sun, ambient probe, fog and gameplay
            // geometry continue through the same brightness/preset logic below.
            string comicSky = value == SkyPreset.Night ? "night" : value == SkyPreset.BloodMoon ? "blood" : value == SkyPreset.Inferno ? "inferno" : value == SkyPreset.RedEclipse ? "eclipse" : "dusk";
            var visual = Resources.Load<Material>("CampusLook/sky-" + (value==SkyPreset.Default?"day":comicSky)) ?? Resources.Load<Material>("Comic/sky-" + comicSky);
            if (visual != null && runtimeSky != null && visual.HasProperty("_MainTex"))
            {
                runtimeSky.shader = visual.shader;
                runtimeSky.CopyPropertiesFromMaterial(visual);
                hasExposure = runtimeSky.HasProperty("_Exposure"); hasTint = false; hasGround = false;
                exposure = hasExposure ? visual.GetFloat("_Exposure") : 1f;
                if(runtimeSky.HasProperty("_Tint"))realSkyTint=runtimeSky.GetColor("_Tint");
            }
            // sun colour, sky tint, ground, fog, sun/exposure/ambient scale, sun elevation (degrees, -1 keeps the scene's)
            if(runtimeSky!=null&&runtimeSky.HasProperty("_StormTex")&&stormSky!=null)runtimeSky.SetTexture("_StormTex",stormSky.GetTexture("_MainTex"));
            Color sunTint=sunColor,tint=skyTint,ground=groundColor,fog=fogColor;float sunS=1,expS=1,ambS=1,elevation=-1;Color ambTint=Color.white;
            switch(value)
            {
                case SkyPreset.Dusk:
                    ambTint=new Color(1f,.9f,.86f);
                    sunTint=new Color(1f,.84f,.7f);tint=new Color(.92f,.52f,.38f);ground=new Color(.30f,.22f,.22f);fog=new Color(.64f,.49f,.48f);
                    sunS=.95f;expS=1.15f;ambS=1.1f;elevation=22;break;
                case SkyPreset.Night:
                    ambTint=new Color(.86f,.91f,1f);
                    sunTint=new Color(.74f,.83f,1f);tint=new Color(.06f,.09f,.22f);ground=new Color(.03f,.04f,.08f);fog=new Color(.22f,.28f,.46f);
                    sunS=.56f;expS=1.3f;ambS=.95f;elevation=35;break;
                case SkyPreset.BloodMoon:
                    ambTint=new Color(1f,.92f,.85f);
                    sunTint=new Color(1f,.78f,.61f);tint=new Color(.5f,.05f,.06f);ground=new Color(.12f,.03f,.03f);fog=new Color(.4f,.21f,.29f);
                    sunS=.85f;expS=1.5f;ambS=1.18f;elevation=28;break;
                case SkyPreset.Inferno:
                    ambTint=new Color(1f,.9f,.84f);
                    sunTint=new Color(1f,.73f,.44f);tint=new Color(.9f,.25f,.06f);ground=new Color(.22f,.06f,.03f);fog=new Color(.6f,.33f,.19f);
                    sunS=.88f;expS=1.45f;ambS=1.05f;elevation=25;break;
                case SkyPreset.RedEclipse:
                    ambTint=new Color(1f,.95f,.82f);
                    sunTint=new Color(1f,.86f,.56f);tint=new Color(.32f,.02f,.04f);ground=new Color(.07f,.01f,.02f);fog=new Color(.37f,.23f,.34f);
                    sunS=.65f;expS=1.2f;ambS=.98f;elevation=45;break;
            }
            sunColor=value==SkyPreset.Default?baseSunColor:sunTint;skyTint=value==SkyPreset.Default?baseSkyTint:tint;
            groundColor=value==SkyPreset.Default?baseGround:ground;fogColor=value==SkyPreset.Default?baseFog:fog;
            sunScale=sunS;exposureScale=expS;ambientScale=ambS;ambientTint=ambTint;
            if(sun!=null)
            {
                if(elevation<0)sun.transform.rotation=sunRotation;
                else{var e=sunRotation.eulerAngles;sun.transform.rotation=Quaternion.Euler(elevation,e.y,e.z);}
            }
            SetBrightness(Brightness);
        }
        void Apply(GameSettings value)=>SetBrightness(value.SkyBrightness);
        public void SetBrightness(float brightness)
        {
            Brightness=Mathf.Clamp01(brightness);
            if(!captured)return;
            float daylight=Mathf.Lerp(.03f,1f,Brightness)*sunScale,ambient=Mathf.Lerp(.08f,1f,Brightness)*ambientScale;
            if(sun!=null)
            {sun.intensity=sunIntensity*daylight;sun.color=Color.Lerp(new Color(.5f,.62f,1f),sunColor,Brightness);}
            if(runtimeSky!=null)
            {
                if(hasExposure)runtimeSky.SetFloat("_Exposure",exposure*Mathf.Lerp(.03f,1f,Brightness)*exposureScale*(furySky?1-fireStorm*.3f:1));
                if(hasTint)runtimeSky.SetColor("_SkyTint",Color.Lerp(new Color(.12f,.18f,.35f),skyTint,Brightness));
                if(hasGround)runtimeSky.SetColor("_GroundColor",groundColor*ambient);
                if(runtimeSky.HasProperty("_Tint"))runtimeSky.SetColor("_Tint",furySky?Color.Lerp(realSkyTint,realSkyTint*new Color(1,.24f,.18f),fireStorm):realSkyTint);
                if(runtimeSky.HasProperty("_FireStorm"))runtimeSky.SetFloat("_FireStorm",fireStorm);
                if(runtimeSky.HasProperty("_DragonFury"))runtimeSky.SetFloat("_DragonFury",furySky?fireStorm:0);
            }
            RenderSettings.ambientIntensity=ambientIntensity*ambient;
            RenderSettings.ambientSkyColor=ambientSky*ambientTint*ambient;RenderSettings.ambientEquatorColor=ambientEquator*ambientTint*ambient;
            RenderSettings.ambientGroundColor=ambientGround*ambientTint*ambient;RenderSettings.ambientLight=ambientLight*ambientTint*ambient;
            var probe=ambientProbe;
            Color fireWarmth=Color.Lerp(Color.white,new Color(1.08f,.84f,.70f),fireStorm);
            for(int channel=0;channel<3;channel++)for(int coefficient=0;coefficient<9;coefficient++)
                probe[channel,coefficient]=ambientProbe[channel,coefficient]*ambient*(channel==0?ambientTint.r*fireWarmth.r:channel==1?ambientTint.g*fireWarmth.g:ambientTint.b*fireWarmth.b);
            RenderSettings.ambientProbe=probe;
            RenderSettings.reflectionIntensity=reflectionIntensity*Mathf.Lerp(.1f,1f,Brightness);
            RenderSettings.fogColor=Color.Lerp(new Color(.015f,.022f,.05f),fogColor,Brightness);
            if(fireStorm>0)
            {
                RenderSettings.ambientSkyColor*=fireWarmth;RenderSettings.ambientEquatorColor*=fireWarmth;
                RenderSettings.ambientGroundColor*=fireWarmth;RenderSettings.ambientLight*=fireWarmth;
                if(sun!=null)sun.color=Color.Lerp(sun.color,new Color(1,.72f,.5f),fireStorm*.55f);
                RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,new Color(.48f,.14f,.075f),fireStorm*.55f);
            }
        }
        void OnDestroy()
        {
            SceneManager.sceneLoaded-=SceneLoaded;if(settings!=null)settings.Changed-=Apply;
            if(captured && scene==SceneManager.GetActiveScene())
            {
                if(preset!=SkyPreset.Default)SetPreset(SkyPreset.Default);
                SetBrightness(1);
                if(RenderSettings.skybox==runtimeSky)RenderSettings.skybox=originalSky;
            }
            if(runtimeSky!=null)Destroy(runtimeSky);
        }
    }
}
