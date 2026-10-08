using UnityEngine;

namespace CampusRift.Monsters
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerMonsterHealth))]
    public sealed class PlayerBloodFeedback : MonoBehaviour
    {
        public ParticleSystem bloodPrefab;
        [Range(1, 8)] public int poolSize = 4;
        public int BurstCount { get; private set; }
        public int LiveParticles
        {
            get
            {
                int count = 0;
                if (emitters != null)
                    foreach (var emitter in emitters)
                        foreach (var system in emitter.GetComponentsInChildren<ParticleSystem>()) count += system.particleCount;
                return count;
            }
        }

        PlayerMonsterHealth health;
        [System.NonSerialized] ParticleSystem[] emitters;
        [System.NonSerialized] int next;
        void Awake() { health = GetComponent<PlayerMonsterHealth>(); }
        void OnEnable() { health.ImpactReceived += Emit; }
        void OnDisable()
        {
            health.ImpactReceived -= Emit;
            if (emitters != null) foreach (var emitter in emitters)
                if (emitter != null) emitter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void Emit(Vector3 point, Vector3 direction)
        {
            if (bloodPrefab == null) return;
            if (emitters == null || emitters.Length == 0)
            {
                next = 0;
                emitters = new ParticleSystem[Mathf.Clamp(poolSize, 1, 8)];
                for (int i = 0; i < emitters.Length; i++)
                {
                    emitters[i] = Instantiate(bloodPrefab, transform);
                    emitters[i].name = "Blood impact (pooled) " + i;
                    emitters[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            var effect = emitters[next]; next = (next + 1) % emitters.Length;
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            effect.transform.SetPositionAndRotation(point, Quaternion.LookRotation((direction + Vector3.up * 0.35f).normalized));
            effect.Play(true);
            // Emit immediately even if the lethal hit opens Game Over and pauses this frame.
            effect.Simulate(1f / 60f, true, false, false);
            effect.Play(true);
            BurstCount++;
        }
    }
}
