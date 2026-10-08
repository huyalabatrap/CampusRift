using UnityEngine;
using CampusRift.UI;
namespace CampusRift.AR
{
    // Small local procedural cues; sources belong to the encounter, not a persistent global pool.
    public sealed class ARCombatAudio:MonoBehaviour
    {
        AudioClip chime,whistle,hum;AudioSource ui,rift;ARBattlefield field;bool paused;
        public static float Volume=>SettingsManager.Instance!=null?SettingsManager.Instance.Current.MasterVolume*SettingsManager.Instance.Current.SFXVolume: .64f;
        public static void Spatial(AudioSource source,float scale=1)
        {source.playOnAwake=false;source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Logarithmic;source.minDistance=Mathf.Clamp(.5f*scale,.15f,.5f);source.maxDistance=Mathf.Max(8,12*scale);source.dopplerLevel=0;source.spatialize=true;}
        void Awake(){field=GetComponent<ARBattlefield>();chime=Tone("Seal chime",.18f,false);whistle=Tone("Incoming energy",1.2f,true);hum=Tone("Rift hum",2,false,140);ui=gameObject.AddComponent<AudioSource>();ui.playOnAwake=false;ui.spatialBlend=0;}
        AudioClip Tone(string name,float seconds,bool rising,float frequency=880)
        {const int rate=24000;var samples=new float[Mathf.CeilToInt(rate*seconds)];float phase=0;for(int i=0;i<samples.Length;i++){float t=i/(float)samples.Length;phase+=2*Mathf.PI*(rising?Mathf.Lerp(320,1500,t):frequency)/rate;samples[i]=(Mathf.Sin(phase)+.25f*Mathf.Sin(phase*2.01f))*Mathf.Sin(Mathf.PI*t)*(rising?.15f:.2f);}var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;}
        public void Chime(){ui.PlayOneShot(chime,Volume);}
        public void Warn(AudioSource source){source.volume=Volume;source.clip=whistle;source.Play();}
        public void Rift(Transform target,float scale){rift=target.gameObject.AddComponent<AudioSource>();Spatial(rift,scale);rift.loop=true;rift.clip=hum;rift.volume=Volume*.3f;rift.Play();paused=false;}
        public AudioSource SecondaryRift(Transform target,float scale){var audio=target.gameObject.AddComponent<AudioSource>();Spatial(audio,scale);audio.loop=true;audio.clip=hum;audio.volume=Volume*.3f;audio.Play();return audio;}
        void Update(){if(rift==null)return;rift.volume=Volume*.3f;if(field.Paused&&!paused){rift.Pause();paused=true;}else if(!field.Paused&&paused){rift.UnPause();paused=false;}}
        void OnDestroy(){if(chime!=null)Destroy(chime);if(whistle!=null)Destroy(whistle);if(hum!=null)Destroy(hum);}
    }
}
