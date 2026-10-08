using System.Collections.Generic;
using UnityEngine;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARSealPractice : MonoBehaviour
    {
        public sealed class Note {public string label;public double at;public bool judged,cuePlayed;public string grade;}
        public readonly List<Note> Notes=new List<Note>();
        public int Points {get;private set;}public int Perfect {get;private set;}public int Great {get;private set;}public int Miss {get;private set;}
        public string Judgment {get;private set;}="";public float JudgmentUntil {get;private set;}
        public double Elapsed {get;private set;}public float Accuracy=>Notes.Count==0?0:(float)(Perfect+Great)/Notes.Count;
        public bool Running {get;private set;}public const double Duration=90,PerfectWindow=.09,GreatWindow=.20;
        ARBattlefield field;ARMonsterDirector director;ARSkillCaster caster;AudioSource music;double origin,pausedAt;bool paused,musicStarted;
        long lastIntent=-1;int lastEpoch=-1;
        void Awake()
        {
            field=GetComponent<ARBattlefield>();director=GetComponent<ARMonsterDirector>();caster=GetComponent<ARSkillCaster>();
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.spatialBlend=0;music.clip=Resources.Load<AudioClip>("P17/Music/dusk");
            var mixer=SettingsManager.Instance?.Mixer;if(mixer!=null){var groups=mixer.FindMatchingGroups("Music");if(groups.Length>0)music.outputAudioMixerGroup=groups[0];}
        }
        void Start(){director.BattleStarted+=Begin;field.Removing+=Clear;}
        void Begin()
        {
            Clear();if(!field.ModeSession.Has(ARModeFeature.Rhythm))return;
            Running=true;lastIntent=-1;lastEpoch=-1;Points=Perfect=Great=Miss=0;Notes.Clear();Elapsed=0;Judgment="";
            // 100 BPM grid: 4, 3, then 2 beats between seals. Every block teaches all six gestures.
            var random=new System.Random(field.ModeSession.Seed);double at=3.6;int index=0;var order=(string[])GestureSkillMapper.Labels.Clone();
            while(at<Duration-1)
            {
                if(index>=6&&index%6==0)for(int i=5;i>0;i--){int j=random.Next(i+1);var swap=order[i];order[i]=order[j];order[j]=swap;}
                Notes.Add(new Note{label=order[index%6],at=at});index++;at+=at<24?2.4:at<54?1.8:1.2;
            }
            origin=AudioSettings.dspTime+.25;paused=false;musicStarted=false;caster.sequences.Cancel();caster.gestures.RequireRelease();
        }
        bool Blocked=>field.Paused||caster.source.Recovering||!caster.source.Ready||!caster.source.SamplingActive||caster.Practice||field.CheckLoad;
        void Update()
        {
            if(!Running)return;field.ModeSession.ObservePolicy();if(field.Root==null||director.Finished){Clear();return;}
            if(Blocked){if(musicStarted&&!paused){paused=true;pausedAt=AudioSettings.dspTime;music.Pause();}return;}
            if(!musicStarted){musicStarted=true;origin=AudioSettings.dspTime+.25;if(music.clip!=null)music.PlayScheduled(origin);}
            if(paused){origin+=AudioSettings.dspTime-pausedAt;paused=false;music.UnPause();}
            Elapsed=System.Math.Max(0,AudioSettings.dspTime-origin);
            var settings=SettingsManager.Instance;music.volume=music.outputAudioMixerGroup!=null?.38f:.38f*(settings!=null?settings.Current.MasterVolume*settings.Current.MusicVolume:1);
            foreach(var note in Notes)
            {if(!note.cuePlayed&&Elapsed>=note.at){note.cuePlayed=true;GetComponent<ARCombatAudio>()?.Chime();}if(!note.judged&&Elapsed>note.at+GreatWindow)Judge(note,"Miss");}
            if(Elapsed>=Duration){Running=false;music.Stop();director.Finish(true);}
        }
        public bool Consume(GestureIntent intent)
        {
            if(!field.ModeSession.Has(ARModeFeature.Rhythm))return false;
            if(!Running||Blocked)return true;
            if(intent.epoch==lastEpoch&&intent.id<=lastIntent)return true;lastEpoch=intent.epoch;lastIntent=intent.id;
            double at=AudioSettings.dspTime-origin-System.Math.Max(0,(GestureRecognizerBridge.Now-intent.triggerConsumeMs)*.001);
            Note nearest=null;double best=GreatWindow+.000001;
            foreach(var note in Notes){if(note.judged)continue;double delta=System.Math.Abs(note.at-at);if(delta<best){best=delta;nearest=note;}}
            if(nearest==null){Judgment=LevelHUD.Vietnamese?"Chờ ô ấn tới vòng nhịp":"Wait for a seal to reach the beat ring";JudgmentUntil=Time.unscaledTime+1;return true;}
            Judge(nearest,nearest.label!=intent.label?"Miss":best<=PerfectWindow?"Perfect":"Great");return true;
        }
        void Judge(Note note,string grade)
        {
            note.judged=true;note.grade=grade;Judgment=grade;JudgmentUntil=Time.unscaledTime+.7f;
            if(grade=="Perfect"){Perfect++;Points+=1000;ARHaptics.Pulse();GetComponent<ARCombatAudio>()?.Chime();}
            else if(grade=="Great"){Great++;Points+=600;ARHaptics.Pulse();}else Miss++;
        }
        void Clear(){Running=false;paused=musicStarted=false;music.Stop();}
        void OnDestroy(){if(director!=null)director.BattleStarted-=Begin;if(field!=null)field.Removing-=Clear;}
    }
}
