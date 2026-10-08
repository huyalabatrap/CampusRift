using System;
using UnityEngine;
using UnityEngine.Audio;
namespace CampusRift.UI
{
    [Serializable]
    public sealed class GameSettings
    {
        public float MasterVolume=.8f, MusicVolume=.7f, SFXVolume=.8f, MonsterVolume=1, UIVolume=.8f;
        public float MouseSensitivity=1, CameraSensitivity=1, FieldOfView=60;
        public float SkyBrightness=1f;
        public bool ComicEffects=false;
        public bool ReduceSkillFlashes;
        public bool ReduceCameraShake;
        public bool Subtitles=true, SlowReading;
        public int TextSize;
        public AccessiblePalette AccessibleColors;
        public bool LocalTelemetryEnabled, TelemetryConsentAsked;
        public bool InvertY, Fullscreen=true, VSync=true;
        public int ResolutionWidth, ResolutionHeight, Quality;
        public CampusRift.Controls.ControlMode ControlMode;
        public Localization.GameLanguage Language=Localization.GameLanguage.Vietnamese;
        public float TouchSensitivity=1, MobileControlScale=1, MobileOpacity=.85f;
        public bool MobileHaptics=true, JoystickAutoSprint=false;
        public bool BoostToggle=false, MobileBoostSeparated;
        public GameSettings Copy() { return (GameSettings)MemberwiseClone(); }
    }
    public sealed class SettingsManager : MonoBehaviour
    {
        const string Key="CampusRift.Settings.v1";
        public static SettingsManager Instance {get;private set;}
        public AudioMixer Mixer;
        public GameSettings Current {get;private set;}
        public SkyLightingController Sky {get;private set;}
        public event Action<GameSettings> Changed;
        public static readonly string[] VolumeParameters={"MasterVolume","MusicVolume","SFXVolume","MonsterVolume","UIVolume"};
        void Awake()
        {
            if(Instance!=null && Instance!=this){enabled=false;return;}
            Instance=this;Current=Defaults();
            if(PlayerPrefs.HasKey(Key))
            {
                try {JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Key),Current);}catch(ArgumentException){Current=Defaults();}
            }
            bool migrateBoost=!Current.MobileBoostSeparated;
            Validate(Current);
            if(migrateBoost)Save();
        }
        void Start()
        {
            Sky=GetComponent<SkyLightingController>() ?? gameObject.AddComponent<SkyLightingController>();
            Sky.Initialize(this);
            ApplyAudio(Current);if(PlayerPrefs.HasKey(Key))ApplyVideo(Current);Changed?.Invoke(Current.Copy());
        }
        public void PreviewSky(float brightness) { if(Sky!=null)Sky.SetBrightness(brightness); }
        public void RestoreSkyPreview() { PreviewSky(Current.SkyBrightness); }
        void OnDestroy(){if(Instance==this)Instance=null;}
        public static GameSettings Defaults()
        {return new GameSettings{ControlMode=Application.isMobilePlatform?CampusRift.Controls.ControlMode.Mobile:CampusRift.Controls.ControlMode.PC,ResolutionWidth=Screen.width,ResolutionHeight=Screen.height,Fullscreen=Screen.fullScreen,Quality=QualitySettings.GetQualityLevel(),VSync=QualitySettings.vSyncCount>0};}
        public void Apply(GameSettings draft,bool save=true)
        {
            Current=draft.Copy();Validate(Current);ApplyAudio(Current);ApplyVideo(Current);Changed?.Invoke(Current.Copy());
            if(save)Save();
        }
        public void Save() {PlayerPrefs.SetString(Key,JsonUtility.ToJson(Current));PlayerPrefs.Save();}
        public GameSettings ReadSaved()
        {
            var data=Defaults();if(PlayerPrefs.HasKey(Key))JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Key),data);Validate(data);return data;
        }
        static void Validate(GameSettings s)
        {
            // One-time migration: old profiles used automatic/toggle sprint by default.
            // Once marked, preserve the player's explicit opt-in on future loads.
            if(!s.MobileBoostSeparated){s.JoystickAutoSprint=false;s.BoostToggle=false;s.MobileBoostSeparated=true;}
            if(!Enum.IsDefined(typeof(Localization.GameLanguage),s.Language))s.Language=Localization.GameLanguage.Vietnamese;
            s.SkyBrightness=float.IsNaN(s.SkyBrightness)?1f:Mathf.Clamp01(s.SkyBrightness);
            if(!Enum.IsDefined(typeof(CampusRift.Controls.ControlMode),s.ControlMode))s.ControlMode=Defaults().ControlMode;
            s.TextSize=Mathf.Clamp(s.TextSize,0,2);
            if(!Enum.IsDefined(typeof(AccessiblePalette),s.AccessibleColors))s.AccessibleColors=AccessiblePalette.Default;
            s.TouchSensitivity=Mathf.Clamp(s.TouchSensitivity,.3f,2.5f);s.MobileControlScale=Mathf.Clamp(s.MobileControlScale,.85f,1.2f);s.MobileOpacity=Mathf.Clamp(s.MobileOpacity,.35f,1);
            s.MasterVolume=Mathf.Clamp01(s.MasterVolume);s.MusicVolume=Mathf.Clamp01(s.MusicVolume);s.SFXVolume=Mathf.Clamp01(s.SFXVolume);s.MonsterVolume=Mathf.Clamp01(s.MonsterVolume);s.UIVolume=Mathf.Clamp01(s.UIVolume);
            s.MouseSensitivity=Mathf.Clamp(s.MouseSensitivity,.25f,3);s.CameraSensitivity=Mathf.Clamp(s.CameraSensitivity,.25f,3);s.FieldOfView=Mathf.Clamp(s.FieldOfView,45,90);
            s.Quality=Mathf.Clamp(s.Quality,0,Mathf.Max(0,QualitySettings.names.Length-1));
            if(s.ResolutionWidth<640 || s.ResolutionHeight<480){s.ResolutionWidth=Screen.width;s.ResolutionHeight=Screen.height;}
        }
        public void ApplyAudio(GameSettings s)
        {
            if(Mixer==null)return;
            float[] values={s.MasterVolume,s.MusicVolume,s.SFXVolume,s.MonsterVolume,s.UIVolume};
            for(int i=0;i<values.Length;i++)Mixer.SetFloat(VolumeParameters[i],values[i]<=.0001f?-80:20*Mathf.Log10(values[i]));
        }
        static void ApplyVideo(GameSettings s)
        {
            if(QualitySettings.GetQualityLevel()!=s.Quality)QualitySettings.SetQualityLevel(s.Quality,true);
            QualitySettings.vSyncCount=s.VSync?1:0;
            if(!Application.isMobilePlatform && (Screen.width!=s.ResolutionWidth || Screen.height!=s.ResolutionHeight || Screen.fullScreen!=s.Fullscreen))
                Screen.SetResolution(s.ResolutionWidth,s.ResolutionHeight,s.Fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        }
    }
}
