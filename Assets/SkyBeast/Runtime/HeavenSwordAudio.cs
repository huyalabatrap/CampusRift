using UnityEngine;
namespace CampusRift.SkyBeast
{
    public sealed class HeavenSwordAudio : MonoBehaviour
    {
        public static HeavenSwordAudio Instance {get;private set;}
        AudioSource sfx,music;HeavenSwordConfig config;
        public int StepsPlayed {get;private set;}
        public static void Attach(GameObject go,HeavenSwordConfig cfg){var a=go.AddComponent<HeavenSwordAudio>();a.config=cfg;a.Build();}
        void Build()
        {
            Instance=this;sfx=gameObject.AddComponent<AudioSource>();sfx.playOnAwake=false;sfx.spatialBlend=0;sfx.volume=.7f;
            sfx.outputAudioMixerGroup=config.sounds.output;
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.volume=.5f;
            var groups=UI.SettingsManager.Instance?.Mixer?.FindMatchingGroups("Music");if(groups!=null&&groups.Length>0)music.outputAudioMixerGroup=groups[0];
        }
        public void Ready(){if(config.ready!=null)sfx.PlayOneShot(config.ready,.75f);}
        public void Step(int step)
        {
            var g=config.sounds;var clip=step==0?g.cast:step==1?g.charge:step==2?g.rift:step==3?g.descent:step==4?g.impact:step==5?g.aftershock:g.dissipate;
            if(clip!=null){sfx.PlayOneShot(clip,step==4?1:.65f);StepsPlayed++;}
        }
        public void Victory(){music.clip=config.victory;if(music.clip!=null)music.Play();}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
