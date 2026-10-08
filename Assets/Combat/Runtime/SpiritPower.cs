using System;
using UnityEngine;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // Linh Lực: the resource skills spend. Regenerates over time and gains a little from every basic hit.
    [DisallowMultipleComponent, DefaultExecutionOrder(-90)]
    public sealed class SpiritPower : MonoBehaviour
    {
        AR.ARCombatContext ar;float SessionNow=>ar!=null?ar.Now:Time.time;float Scale=>ar!=null?ar.scale:1;

        [Min(0)] public float gainPerBasicHit = 5f;
        // Regeneration pauses briefly after spending so a cast is felt.
        [Min(0)] public float regenDelay = 0.4f;
        PlayerStats stats;
        PlayerMonsterHealth health;
        float current, regenAt;
        bool initialised;
        public event Action Changed;
        public float Max => stats != null ? stats.MaxSpirit : 100f;
        public float Current => Progression.DevMode.Active ? Max : current;
        public float Fraction => Mathf.Clamp01(Current / Mathf.Max(1f, Max));

        void Awake()
        {
            ar=GetComponent<AR.ARCombatContext>();
            stats = GetComponent<PlayerStats>(); health = GetComponent<PlayerMonsterHealth>();
            if (stats != null) stats.Changed += StatsChanged;
            current = Max; initialised = true;
        }
        void OnDestroy() { if (stats != null) stats.Changed -= StatsChanged; }

        // Keep the fill fraction when the maximum changes (levelling up, artifacts).
        float lastMax = -1;
        void StatsChanged()
        {
            if (!initialised) return;
            float max = Max;
            if (lastMax > 0 && !Mathf.Approximately(lastMax, max)) current = Mathf.Clamp(current / lastMax * max, 0, max);
            lastMax = max; Changed?.Invoke();
        }

        public bool CanAfford(float cost) => Progression.DevMode.Active || cost <= 0 || current + 0.0001f >= cost;

        public bool TrySpend(float cost)
        {
            if (Progression.DevMode.Active || cost <= 0) return true;
            if (!CanAfford(cost)) return false;
            current = Mathf.Max(0, current - cost); regenAt = SessionNow + regenDelay; Changed?.Invoke(); return true;
        }

        // Incoming AR damage drains the available resource even when it cannot pay the whole hit.
        public void Drain(float amount)
        {if(Progression.DevMode.Invincible||amount<=0)return;current=Mathf.Max(0,current-amount);regenAt=SessionNow+regenDelay;Changed?.Invoke();}

        public void Restore(float amount)
        {
            if (amount <= 0) return;
            float before = current; current = Mathf.Min(Max, current + amount);
            if (current != before) Changed?.Invoke();
        }

        public void OnBasicHit() => Restore(gainPerBasicHit);
        public void Refill() { current = Max; Changed?.Invoke(); }

        void Update()
        {
            if(ar!=null&&ar.Paused)return;
            if (lastMax < 0) lastMax = Max;
            if (health != null && health.IsDead) return;
            var ui = UI.UIStateManager.Instance;
            if (ar == null && ui != null && ui.State != UI.UIState.Gameplay && ui.State != UI.UIState.Modal) return;
            if (SessionNow < regenAt || current >= Max) return;
            float regen = stats != null ? stats.SpiritRegen : 4f;
            float before = current; current = Mathf.Min(Max, current + regen * Time.deltaTime);
            if (!Mathf.Approximately(before, current)) Changed?.Invoke();
        }
    }
}
