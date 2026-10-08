using System.Collections.Generic;
using UnityEngine;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    public sealed class SkyBeastAudioDuck : MonoBehaviour
    {
        float until,musicOriginal,current;bool ducked;readonly Dictionary<AudioSource,float> ambient=new Dictionary<AudioSource,float>();
        void Start(){if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed+=Changed;}
        void Changed(GameSettings settings){if(ducked)musicOriginal=settings.MusicVolume<=.0001f?-80:20*Mathf.Log10(settings.MusicVolume);}
        public static SkyBeastAudioDuck Ensure(){var c=FindAnyObjectByType<SkyBeastAudioDuck>();return c!=null?c:new GameObject("Dragon Audio Duck").AddComponent<SkyBeastAudioDuck>();}
        public void Duck(float seconds){until=Mathf.Max(until,Time.unscaledTime+seconds);if(ducked)return;ducked=true;
            var mixer=SettingsManager.Instance?.Mixer;if(mixer!=null)mixer.GetFloat("MusicVolume",out musicOriginal);current=0;
            foreach(var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))if(source.name.ToLowerInvariant().Contains("ambient")||source.name.ToLowerInvariant().Contains("ambience")){ambient[source]=source.volume;}}
        void Update(){if(!ducked)return;float target=Time.unscaledTime<until?1:0;current=Mathf.MoveTowards(current,target,Time.unscaledDeltaTime*(target>0?6:1.5f));
            var mixer=SettingsManager.Instance?.Mixer;if(mixer!=null)mixer.SetFloat("MusicVolume",musicOriginal-10*current);
            foreach(var pair in ambient)if(pair.Key!=null)pair.Key.volume=pair.Value*(1-.65f*current);
            if(target==0&&current<=0){ducked=false;ambient.Clear();}}
        void OnDisable(){if(ducked){SettingsManager.Instance?.Mixer?.SetFloat("MusicVolume",musicOriginal);foreach(var p in ambient)if(p.Key!=null)p.Key.volume=p.Value;}ambient.Clear();ducked=false;}
        void OnDestroy(){if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed-=Changed;}
    }
}
