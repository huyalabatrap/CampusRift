using UnityEngine;
using CampusRift.Levels;
namespace CampusRift.Audio
{
    // Two streamed voices crossfade locally; mixer remains owned by Settings / roar duck.
    public sealed class LevelMusicDirector:MonoBehaviour
    {
        public static LevelMusicDirector Instance {get;private set;}
        public string CurrentTrack {get;private set;}
        public AudioSource CurrentVoice=>voices[current];
        readonly AudioSource[] voices=new AudioSource[2];int current;float blend=1;bool story;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){LevelEvents.LevelStarted-=Started;LevelEvents.LevelStarted+=Started;}
        static void Started(LevelDefinition level){var d=LevelDirector.Instance;if(d==null)return;var m=d.GetComponent<LevelMusicDirector>()??d.gameObject.AddComponent<LevelMusicDirector>();m.story=false;m.Choose();}
        void Awake()
        {
            Instance=this;for(int i=0;i<2;i++){var go=new GameObject("P21 Music crossfade "+i);go.transform.SetParent(transform,false);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=true;s.spatialBlend=0;s.volume=0;
                var mixer=UI.SettingsManager.Instance?.Mixer;if(mixer!=null){var groups=mixer.FindMatchingGroups("Music");if(groups.Length>0)s.outputAudioMixerGroup=groups[0];}voices[i]=s;}
        }
        void Switch(string path)
        {
            if(CurrentTrack==path)return;var clip=Resources.Load<AudioClip>(path);if(clip==null)return;
            current=1-current;voices[current].Stop();voices[current].clip=clip;voices[current].volume=0;voices[current].Play();CurrentTrack=path;blend=0;
        }
        public void PlayStory(bool ending){story=true;Switch(ending?"P21/Music/dawn":"P21/Music/king");}
        public void PlayBoss(int level,bool phase2){Switch(level==5?"P12/BossPhase1":phase2?"P21/Music/king":"P12/BossPhase2");}
        void Choose()
        {
            var d=LevelDirector.Instance;if(d==null||d.Level==null)return;
            foreach(var e in d.Alive)if(e!=null&&e.Alive){var b=e.GetComponent<Enemies.BossController>();if(b!=null&&b.ActiveBoss){PlayBoss(d.Level.index,b.PhaseTwo);return;}}
            int level=d.Level.index;var sky=SkyBeast.SkyBeastScheduler.Instance;
            if(level>=8){int phase=sky!=null?sky.Phase:1;Switch(level==8?"P21/Music/giao":level==9?(phase==1?"P21/Music/bird":"P21/Music/king"):phase==1?"P21/Music/giao":phase==2?"P21/Music/bird":"P21/Music/king");}
            else Switch(level<=2?"P17/Music/dusk":"P17/Music/night");
        }
        void Update()
        {
            var d=LevelDirector.Instance;if(!story&&(d==null||d.State==LevelDirector.Phase.Idle||d.State==LevelDirector.Phase.Won||d.State==LevelDirector.Phase.Lost)){foreach(var s in voices)s.Stop();CurrentTrack=null;return;}
            if(story&&SkyBeast.P21StoryCinematic.Active==null){story=false;return;}
            if(Time.timeScale<=0)return;if(!story)Choose();
            blend=Mathf.MoveTowards(blend,1,Time.unscaledDeltaTime/1.5f);
            bool sword=SkyBeast.HeavenSwordCinematic.Active?.Playing??false;float volume=sword?.10f:.38f;
            voices[current].volume=Mathf.Sin(blend*Mathf.PI*.5f)*volume;voices[1-current].volume=Mathf.Cos(blend*Mathf.PI*.5f)*volume;if(blend>=1&&voices[1-current].isPlaying)voices[1-current].Stop();
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
