using UnityEngine;
using UnityEngine.Audio;
namespace CampusRift.UI
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class UIAudioManager : MonoBehaviour
    {
        public static UIAudioManager Instance { get; private set; }
        public AudioClip HoverSound, ClickSound, BackSound;
        public AudioMixerGroup Output;
        AudioSource source;
        float nextHover;
        void Awake()
        {
            if(Instance!=null && Instance!=this){enabled=false;return;}
            Instance = this; source = GetComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0;
            source.ignoreListenerPause = true; source.outputAudioMixerGroup = Output;
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public void Hover() { if (Time.unscaledTime < nextHover) return; nextHover = Time.unscaledTime + .075f; Play(HoverSound); }
        public void Click() { Play(ClickSound); }
        public void Back() { Play(BackSound); }
        void Play(AudioClip clip) { if (clip != null && source != null) source.PlayOneShot(clip); }
    }
}
