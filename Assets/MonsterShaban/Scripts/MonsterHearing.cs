using UnityEngine;

namespace CampusRift.Monsters
{
    public sealed class MonsterHearing : MonoBehaviour
    {
        public MonsterAIConfig config;
        public PlayerSoundEmitter source;
        [SerializeField] float lastEffectiveVolume;
        MonsterMemory memory;
        MonsterPerception perception;
        public Vector3 LastEvidencePosition { get; private set; }
        public float LastEvidenceTime { get; private set; } = -10000;
        public float LastEvidenceConfidence { get; private set; }
        const int SoundTraceCapacity = 16;
        readonly SoundTrace[] recentSounds = new SoundTrace[SoundTraceCapacity];
        int nextSoundTrace;
        struct SoundTrace
        {
            public Vector3 Position;
            public float Time;
        }
        void Awake() { memory = GetComponent<MonsterMemory>(); perception = GetComponent<MonsterPerception>(); }
        void OnEnable()
        {
            SoundEventBus.Emitted += Hear;
        }
        void OnDisable() { SoundEventBus.Emitted -= Hear; }
        public void Hear(SoundEvent sound)
        {
            if (config == null || Time.time - sound.Timestamp > 1 || sound.Timestamp > Time.time + 0.1f) return;
            float distance = Vector3.Distance(transform.position, sound.Position);
            if (distance > config.HearingRange) return;
            float falloff = Mathf.Max(1, config.HearingFalloffDistance);
            lastEffectiveVolume = sound.Loudness / (1 + distance * distance / (falloff * falloff));
            if (!perception.ClearLine(perception.Eye, sound.Position + Vector3.up, sound.Emitter))
                lastEffectiveVolume *= 0.6f;
            if (lastEffectiveVolume < config.HearingThreshold) return;
            // Keep recent audible locations for visual correlation. Player footsteps can
            // arrive between phantom footsteps without erasing the phantom's sound trail.
            recentSounds[nextSoundTrace] = new SoundTrace { Position = sound.Position, Time = sound.Timestamp };
            nextSoundTrace = (nextSoundTrace + 1) % SoundTraceCapacity;
            // A sound at the visible actor adds no new location; a different sound still matters.
            if (perception.CanSeePlayer && Vector3.Distance(sound.Position, perception.ObservedPosition) < 1.6f) return;
            LastEvidencePosition = sound.Position;
            LastEvidenceTime = sound.Timestamp;
            LastEvidenceConfidence = Mathf.Clamp01(lastEffectiveVolume);
            memory.RememberSound(sound.Position, LastEvidenceConfidence, sound.Timestamp);
        }

        public float RecentSoundMatch(Vector3 position, float radius, float maxAge)
        {
            float best = 0;
            for (int i = 0; i < recentSounds.Length; i++)
            {
                var sound = recentSounds[i];
                if (sound.Time <= 0 || Time.time - sound.Time > maxAge) continue;
                best = Mathf.Max(best, Mathf.Clamp01(1f - Vector3.Distance(sound.Position, position) / radius));
            }
            return best;
        }
    }
}
