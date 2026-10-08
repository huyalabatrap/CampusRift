from pathlib import Path
def write(p,s):Path(p).write_text(s,encoding='utf-8')
write('Assets/SkyBeast/Runtime/SkyBeastEnding.cs','''using System.Collections;
using UnityEngine;
namespace CampusRift.SkyBeast
{
    // Preserve P12's entry point; P21 owns the reveal and restoration.
    public static class SkyBeastEnding
    {
        public static bool Skipped=>P21StoryCinematic.LastSkipped;
        public static bool Playing=>P21StoryCinematic.Active!=null&&P21StoryCinematic.Active.Playing;
        public static void Cancel()=>P21StoryCinematic.CancelActive();
        internal static void SuppressHud(){}
        public static IEnumerator Play(){yield return P21StoryCinematic.Play(false);}
    }
    public sealed class SkyBeastEndingHudGuard:MonoBehaviour{}
}
''')
write('Assets/Audio/Runtime/LevelMusicDirector.cs','''using UnityEngine;
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
''')
p=Path('Assets/SkyBeast/Runtime/P21StoryCinematic.cs');s=p.read_text(encoding='utf-8');s=s.replace('public static P21StoryCinematic Active {get;private set;}','public static P21StoryCinematic Active {get;private set;}\n        public static bool LastSkipped {get;private set;}').replace('Playing=true;Ending=ending;','Playing=true;Ending=ending;LastSkipped=false;').replace('Playing=false;if(Active==this)','Playing=false;LastSkipped=Skipped;if(Active==this)');write(str(p),s)
p=Path('Assets/SkyBeast/Runtime/SkyBeastIdentity.cs');s=p.read_text(encoding='utf-8').replace('Accent.localBounds=new Bounds(Vector3.up*3,Vector3.one*32);','var bounds=mesh.bounds;bounds.Expand(4);Accent.localBounds=bounds;');write(str(p),s)
p=Path('Assets/Enemies/Runtime/Boss/BossController.cs')
backup=Path('task/p21/BACKUP.txt').read_text(encoding='utf-8');dest=Path(backup)/p;dest.parent.mkdir(parents=True,exist_ok=True)
if not dest.exists():dest.write_bytes(p.read_bytes())
s=p.read_text(encoding='utf-8');start=s.index('            var clip=Resources.Load<AudioClip>("P12/"');end=s.index('\n        }',start)
s=s[:start]+'            Audio.LevelMusicDirector.Instance?.PlayBoss(owner.scaling.level,phase2);'+s[end:];s=s.replace('Animator animator;Vector3 modelRest;AudioSource music;','Animator animator;Vector3 modelRest;').replace('void OnDisable(){Cancel();if(music!=null)music.Stop();}','void OnDisable(){Cancel();}');write(str(p),s)
with Path('task/p21/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n## M1 · runtime / Timeline\n- Thêm accents1skinnedmesh/cự thú, chín sừng/mane020, cánh/đuôi026, vảy023; giữ model/rig/LOD/data.\n- Timeline5clip/màn8/9/10, Manual unscaled với hit/audio/Render P15; màn9 đảo bên, màn10 rộng hơn, cuối10 thêm1,5s.\n- Reveal020 7s/3,5s sau, HUD/input/sky/camera restore; dawn20s→credits có skip. Title/reveal flag thêm vào profile tương thích save cũ.\n- Music2voice crossfade1,5s/mixer Settings/duck; thay nguồn BossController (backup bổ sung). Chưa compile/capture; nguồn/credits đang hoàn thiện.\n')
