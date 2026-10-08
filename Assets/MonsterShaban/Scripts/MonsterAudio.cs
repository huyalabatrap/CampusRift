using UnityEngine;

namespace CampusRift.Monsters
{
    [RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter))]
    public sealed class MonsterAudio : MonoBehaviour
    {
        public MonsterAIConfig config;
        public bool enableOcclusion;
        [SerializeField] bool occluded;
        [SerializeField] float targetVolume;
        public bool Occluded => occluded;
        AudioSource audioSource;
        AudioLowPassFilter lowPass;
        MonsterBrain brain;
        MonsterVitality vitality;
        MonsterPerception perception;
        Transform listener;
        float nextOcclusion;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>(); lowPass = GetComponent<AudioLowPassFilter>();
            brain = GetComponent<MonsterBrain>(); perception = GetComponent<MonsterPerception>();
            vitality=GetComponent<MonsterVitality>();
            var activeListener = FindAnyObjectByType<AudioListener>();
            if (activeListener != null) listener = activeListener.transform;
            // The big monster's voice is the supplied quai-lon-1 clip (replaces the earlier loop).
            var bigVoice = Resources.Load<AudioClip>("Sfx/quai-lon-1");
            if (bigVoice != null) audioSource.clip = bigVoice;
            audioSource.spatialBlend = 1; audioSource.loop = true; audioSource.playOnAwake = false;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = config.AudioMinDistance; audioSource.maxDistance = config.AudioMaxDistance;
            audioSource.dopplerLevel = 0; audioSource.volume = 0;
            lowPass.cutoffFrequency = 22000;
        }
        void Start() { if (audioSource.clip != null) audioSource.Play(); }
        void Update()
        {
            if (Time.time >= nextOcclusion)
            {
                nextOcclusion = Time.time + 0.25f;
                // Listener position is used solely for acoustics; it is never supplied to the brain.
                occluded = enableOcclusion && listener != null && !perception.ClearLine(perception.Eye,
                    listener.position, perception.player != null ? perception.player.transform : null);
                switch (brain.CurrentState)
                {
                    case MonsterState.Attack: targetVolume = 0.95f; break;
                    case MonsterState.Chase: targetVolume = 0.78f; break;
                    case MonsterState.Investigate: case MonsterState.Search: targetVolume = 0.5f; break;
                    default: targetVolume = 0.3f; break;
                }
                if (occluded) targetVolume *= 0.72f;
                if(vitality!=null && vitality.Defeated)targetVolume=0;
            }
            float blend = 1 - Mathf.Exp(-4 * Time.deltaTime);
            audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, blend);
            audioSource.pitch = Mathf.Lerp(audioSource.pitch, brain.CurrentState == MonsterState.Chase ? 1.025f : 1, blend);
            lowPass.cutoffFrequency = Mathf.Lerp(lowPass.cutoffFrequency, occluded ? 1200 : 22000, blend);
            lowPass.enabled = occluded || lowPass.cutoffFrequency < 21500;
        }
    }
}
