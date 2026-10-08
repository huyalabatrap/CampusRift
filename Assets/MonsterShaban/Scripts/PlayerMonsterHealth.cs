using UnityEngine;
using UnityEngine.Events;
using CampusRift.Combat;

namespace CampusRift.Monsters
{
    [DisallowMultipleComponent]
    public sealed class PlayerMonsterHealth : MonoBehaviour, IDamageable
    {
        [Min(1)] public float maxHealth = 100;
        [SerializeField] float currentHealth = 100;
        // Invulnerability after an accepted melee/projectile/skill hit. Short enough that a swarm still lands.
        [Min(0)] public float hitInvulnerability = 0.25f;
        public UnityEvent<float> Damaged = new UnityEvent<float>();
        public UnityEvent Defeated = new UnityEvent();
        public UnityEvent<float, float> HealthChanged = new UnityEvent<float, float>();
        public bool respawnOnDefeat = true;
        public bool showLegacyHUD = false;
        public float CurrentHealth => currentHealth;
        public int DamageCount { get; private set; }
        // Fraction of incoming damage removed (0..0.8); PlayerStats supplies it from realm, artifacts and buffs.
        public float DamageReduction { get; set; }
        public Element Element => Element.None;
        public bool IsDead => currentHealth <= 0;
        public Transform Anchor => transform;
        public bool Invulnerable => Time.time < protectedUntil;
        // Raised only for accepted physical hits (melee/projectile), before death can pause the game or respawn the player.
        public event System.Action<Vector3, Vector3> ImpactReceived;
        // Raised for every accepted hit, including environment and reaction damage.
        public event System.Action<DamageInfo> DamageReceived;
        // A handler returning true cancels this death (Hộ Mệnh Phù); it must restore health itself.
        public event System.Func<DamageInfo, bool> BeforeDefeat;
        float protectedUntil, flashUntil;
        CampusRift.Skills.GoldenBellRuntime goldenBell;
        PlayerStats stats;
        void Awake(){goldenBell=GetComponent<CampusRift.Skills.GoldenBellRuntime>();stats=GetComponent<PlayerStats>();}

        public void SetProgressionMaxHealth(float value)
        {
            value=Mathf.Max(1,value);
            if(Mathf.Approximately(value,maxHealth))return;
            float fraction=currentHealth/Mathf.Max(1,maxHealth);
            maxHealth=value;currentHealth=Mathf.Clamp01(fraction)*maxHealth;
            HealthChanged.Invoke(currentHealth,maxHealth);
        }

        public void TakeDamage(float amount)
        {
            TryTakeDamage(amount, transform.position + Vector3.up * 1.1f, -transform.forward);
        }

        public bool TryTakeDamage(float amount, Vector3 point, Vector3 direction) =>
            ApplyDamage(DamageInfo.Create(amount, Element.None, DamageSource.Melee, point, direction));

        public void GrantInvulnerability(float seconds) { protectedUntil = Mathf.Max(protectedUntil, Time.time + Mathf.Max(0, seconds)); }

        public void Heal(float amount)
        {
            if (amount <= 0 || currentHealth <= 0) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged.Invoke(currentHealth, maxHealth);
        }

        public bool ApplyDamage(DamageInfo info)
        {
            if (CampusRift.Progression.DevMode.Invincible) return false;
            if(CampusRift.SkyBeast.HeavenSwordCinematic.Active!=null&&CampusRift.SkyBeast.HeavenSwordCinematic.Active.Playing)return false;
            if (info.amount <= 0 || currentHealth <= 0) return false;
            if (!info.ignoreInvulnerability && Time.time < protectedUntil) return false;
            float incoming=info.amount;
            if(info.source==DamageSource.Environment&&info.element==Element.Hoa&&stats!=null)incoming*=1-stats.FireResistance;
            bool fireHazard=CampusRift.SkyBeast.FireBreathCycle.IsFireHazard(info);
            // Fixed P13 hazard values use the combined fire-resistance cap. Bell contributes
            // its 60% resistance while active, avoiding a second shield/defense reduction.
            float amount = fireHazard?incoming:DamageCalculator.AfterDefense(incoming, DamageReduction);
            if(goldenBell!=null&&!fireHazard)amount=goldenBell.Absorb(amount,info);
            amount=Mathf.Min(currentHealth,amount);currentHealth = Mathf.Max(0, currentHealth - amount); DamageCount++;
            // Environment ticks (Thiên Hỏa) arrive every 0.5 s and must neither be blocked by nor grant invulnerability.
            if (!info.ignoreInvulnerability) protectedUntil = Mathf.Max(protectedUntil, Time.time + hitInvulnerability);
            flashUntil = Time.time + 0.7f;
            if (info.source == DamageSource.Melee || info.source == DamageSource.Projectile) ImpactReceived?.Invoke(info.point, info.direction);
            info.amount=Mathf.Min(incoming,amount);DamageReceived?.Invoke(info);
            HealthChanged.Invoke(currentHealth, maxHealth);
            Damaged.Invoke(amount);
            if (currentHealth <= 0)
            {
                if (BeforeDefeat != null)
                    foreach (System.Func<DamageInfo, bool> handler in BeforeDefeat.GetInvocationList())
                        if (handler(info) && currentHealth > 0) { HealthChanged.Invoke(currentHealth, maxHealth); return true; }
                Defeated.Invoke();
                if (!respawnOnDefeat) return true;
                GetComponent<CampusExplorer>()?.ReturnToSpawn();
                currentHealth = maxHealth; protectedUntil = Time.time + 3;
                HealthChanged.Invoke(currentHealth, maxHealth);
            }
            return true;
        }

        // Revives from zero without passing through ApplyDamage (used by BeforeDefeat handlers).
        public void Revive(float fraction, float invulnerableSeconds)
        {
            currentHealth = Mathf.Clamp(maxHealth * fraction, 1, maxHealth);
            GrantInvulnerability(invulnerableSeconds);
            HealthChanged.Invoke(currentHealth, maxHealth);
        }

        void OnGUI()
        {
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return;
#else
            if (!showLegacyHUD) return;
            var old = GUI.color;
            GUI.color = Color.Lerp(Color.white,new Color(1,0.25f,0.25f),Mathf.Clamp01((flashUntil-Time.time)/0.7f));
            GUI.Box(new Rect(12,12,156,27),$"HEALTH  {currentHealth:0} / {maxHealth:0}");
            GUI.color = old;
#endif
        }
    }
}
