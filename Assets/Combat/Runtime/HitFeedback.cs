using System.Collections;
using UnityEngine;
using CampusRift.Monsters;
using CampusRift.Skills;

namespace CampusRift.Combat
{
    // Game feel for the player's hits (P03-T06): a 40 ms freeze of the victim's animation and a small camera
    // shake on heavy blows. It never touches Time.timeScale, because Pause/Course/Terminal own that.
    public sealed class HitFeedback : MonoBehaviour
    {
        public const float HitStopSeconds = 0.04f;
        public static HitFeedback Instance { get; private set; }
        public int HitStops { get; private set; }
        public int CameraPulses { get; private set; }
        public float LastPulse { get; private set; }
        float lastStop = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("Hit Feedback"); DontDestroyOnLoad(go); go.AddComponent<HitFeedback>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; MonsterVitality.AnyDamaged += OnDamaged;
        }
        void OnDestroy() { if (Instance == this) { MonsterVitality.AnyDamaged -= OnDamaged; Instance = null; } }

        // Only blows dealt by the player count, and only the ones worth feeling.
        void OnDamaged(MonsterVitality victim, DamageInfo info)
        {
            if (info.attacker == null || info.attacker.GetComponent<PlayerStats>() == null) return;
            bool piercing = info.skillId == "ngu-kiem-xuyen";
            bool finisher = info.skillId == "ngu-kiem-3";
            bool heavy = info.critical || piercing || finisher || info.source == DamageSource.Skill || info.amount >= victim.maxHealth * 0.15f;
            if (!heavy) return;
            // Skills bring their own reaction (the Sealed animation); a freeze on top would swallow it.
            if (info.source != DamageSource.Skill) HitStop(victim);
            var impulse = info.attacker.GetComponent<GiantHandCameraImpulse>();
            if (impulse != null)
            {
                LastPulse = info.critical || piercing ? 0.32f : 0.2f; impulse.Pulse(LastPulse); CameraPulses++;
            }
        }

        public void HitStop(MonsterVitality victim)
        {
            var animator = victim.GetComponentInChildren<Animator>();
            if (animator == null || animator.speed <= 0.001f) return;
            // A monster already frozen this instant (chain/AoE) is not stopped twice.
            if (Time.unscaledTime - lastStop < 0.015f && HitStops > 0 && animator.speed == 0) return;
            lastStop = Time.unscaledTime; HitStops++;
            StartCoroutine(Freeze(animator));
        }

        IEnumerator Freeze(Animator animator)
        {
            float original = animator.speed; animator.speed = 0f;
            float until = Time.unscaledTime + HitStopSeconds;
            while (Time.unscaledTime < until && animator != null) yield return null;
            if (animator != null) animator.speed = original;
        }
    }
}
