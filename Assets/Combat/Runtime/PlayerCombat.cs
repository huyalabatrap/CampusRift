using System;
using UnityEngine;
using CampusRift.Controls;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // Ngự Kiếm Thuật: three jade flying swords. Click for a 3-hit chain (100 / 100 / 160% Công), hold 0.8 s
    // for a piercing sword (250% Công) that cuts every monster in a line (plan §6).
    [DisallowMultipleComponent, DefaultExecutionOrder(-20)]
    public sealed class PlayerCombat : MonoBehaviour
    {
        public NguKiemConfig config;
        public event Action<MonsterVitality, DamageInfo> Hit;
        public int SwingCount { get; private set; }
        public int PierceCount { get; private set; }
        public int HitCount { get; private set; }
        // Swings refused because Linh Lực ran out.
        public int NoSpiritCount { get; private set; }
        public int ChainIndex => chain;
        public int LastChainIndex { get; private set; } = -1;
        public float LastDamage { get; private set; }
        public MonsterVitality LastTarget { get; private set; }
        public bool Charging => holdStart >= 0 && !pierceFired && input != null && input.IsHeld(CampusAction.Attack);
        public float ChargeFraction => Charging ? Mathf.Clamp01((Time.time - holdStart) / config.holdSeconds) : 0f;
        public FlyingSword[] Swords => swords;

        PlayerStats stats; SpiritPower spirit; CampusInput input; CampusExplorer explorer; TargetLock lockOn;
        PlayerMonsterHealth health; PlayerSoundEmitter sound;
        CampusRift.Skills.MartialAvatarRuntime avatar;
        FlyingSword[] swords = new FlyingSword[0];
        Transform swordRoot;
        readonly System.Random rng = new System.Random();
        int chain, nextSword;
        float nextSwingAt, lastSwingAt = -10, holdStart = -1;
        bool pierceFired;

        void Awake()
        {
            stats = GetComponent<PlayerStats>(); spirit = GetComponent<SpiritPower>(); input = GetComponent<CampusInput>();
            explorer = GetComponent<CampusExplorer>(); lockOn = GetComponent<TargetLock>(); health = GetComponent<PlayerMonsterHealth>();
            sound = GetComponent<PlayerSoundEmitter>();
            avatar=GetComponent<CampusRift.Skills.MartialAvatarRuntime>();
        }

        void Start()
        {
            if (config == null) { enabled = false; return; }
            swords = new FlyingSword[config.swordCount];
            var root = swordRoot = new GameObject("Ngu Kiem Swords").transform;
            for (int i = 0; i < swords.Length; i++)
            {
                var go = new GameObject(); go.transform.SetParent(root, false);
                swords[i] = go.AddComponent<FlyingSword>(); swords[i].Initialize(this, i, config);
            }
        }

        void OnDestroy() { if (swordRoot != null) Destroy(swordRoot.gameObject); }

        // Where sword `slot` hovers: behind the shoulders, bobbing gently.
        public Vector3 SlotPoint(int slot)
        {
            int n = Mathf.Max(1, swords.Length > 0 ? swords.Length : config != null ? config.swordCount : 3);
            float t = n == 1 ? 0 : slot / (float)(n - 1) * 2f - 1f;
            Vector3 local = new Vector3(t * 0.8f, 1.65f - Mathf.Abs(t) * 0.2f, -0.35f);
            float bob = Mathf.Sin(Time.time * 2.1f + slot * 1.7f) * 0.06f;
            return transform.TransformPoint(local) + Vector3.up * bob;
        }

        bool CanAct
        {
            get
            {
                if (config == null || input == null || !input.Allowed) return false;
                if (health != null && health.IsDead) return false;
                if (explorer != null && explorer.RidingElevator != null) return false;
                return true;
            }
        }

        void Update()
        {
            if (!CanAct) { holdStart = -1; return; }
            bool pressed = input.Pressed(CampusAction.Attack), held = input.IsHeld(CampusAction.Attack);
            if (pressed) { holdStart = Time.time; pierceFired = false; TrySwing(); }
            if (held && holdStart >= 0 && !pierceFired && Time.time - holdStart >= config.holdSeconds) { pierceFired = true; TryPierce(); }
            if (!held) holdStart = -1;
        }

        FlyingSword FreeSword()
        {
            for (int i = 0; i < swords.Length; i++)
            {
                var candidate = swords[(nextSword + i) % swords.Length];
                if (candidate != null && candidate.Available) { nextSword = (nextSword + i + 1) % swords.Length; return candidate; }
            }
            return null;
        }

        Vector3 AimForward => lockOn != null ? lockOn.AimForward : transform.forward;

        // Locked monster first; otherwise the best monster inside the 60° cone in front of the camera.
        public MonsterVitality AcquireTarget()
        {
            Vector3 eye = transform.position + Vector3.up * 1.2f;
            if (lockOn != null && lockOn.Current != null && lockOn.IsValid(lockOn.Current)) return lockOn.Current;
            MonsterVitality best = null; float bestScore = float.MaxValue;
            Vector3 forward = AimForward; float half = config.coneAngle * 0.5f;
            foreach (var m in MonsterVitality.Active)
            {
                if (m == null || m.Defeated || !m.isActiveAndEnabled) continue;
                Vector3 chest = CombatLine.Chest(m); Vector3 delta = chest - eye;
                float distance = Vector3.ProjectOnPlane(delta, Vector3.up).magnitude;
                if (distance > config.range) continue;
                float angle = Vector3.Angle(forward, Vector3.ProjectOnPlane(delta, Vector3.up));
                if (angle > half && distance > 1.5f) continue;
                if (!CombatLine.Clear(eye, chest, transform)) continue;
                float score = angle / half * 0.6f + distance / config.range * 0.4f;
                if (score < bestScore) { bestScore = score; best = m; }
            }
            return best;
        }

        public bool TrySwing()
        {
            if (config == null || Time.time < nextSwingAt) return false;
            var sword = avatar!=null&&avatar.Active?null:FreeSword(); if (sword == null && !(avatar!=null&&avatar.Active)) return false;
            if (spirit != null && !spirit.TrySpend(config.swingSpiritCost)) { NoSpiritCount++; return false; }
            if (Time.time - lastSwingAt > config.comboWindow) chain = 0;
            var target = AcquireTarget();
            int last = config.chainPercent.Length - 1;
            float percent = config.chainPercent[Mathf.Min(chain, last)]; bool heavy = chain >= last;
            if(avatar!=null&&avatar.Active){avatar.Sweep(percent);}
            else if (target != null)
            {
                sword.LaunchAt(target, percent, heavy);
                if (explorer != null) explorer.FaceDirection(target.transform.position - transform.position);
            }
            else
            {
                Vector3 dir = AimForward + Vector3.up * 0.05f;
                sword.LaunchMiss(dir);
                if (explorer != null) explorer.FaceDirection(AimForward);
            }
            SwingCount++; LastChainIndex = chain; lastSwingAt = Time.time; nextSwingAt = Time.time + config.swingInterval;
            chain = (chain + 1) % config.chainPercent.Length;
            if (sound != null) sound.Combat();
            return true;
        }

        public bool TryPierce()
        {
            if (config == null) return false;
            var sword = avatar!=null&&avatar.Active?null:FreeSword(); if (sword == null && !(avatar!=null&&avatar.Active)) return false;
            if (spirit != null && !spirit.TrySpend(config.pierceSpiritCost)) { NoSpiritCount++; return false; }
            var target = AcquireTarget();
            Vector3 dir = target != null ? Vector3.ProjectOnPlane(CombatLine.Chest(target) - transform.position, Vector3.up) : AimForward;
            if (dir.sqrMagnitude < 0.01f) dir = AimForward;
            if(avatar!=null&&avatar.Active)avatar.Sweep(config.piercePercent);else sword.LaunchPierce(transform.position + Vector3.up * 1.2f, dir, config.piercePercent);
            if (explorer != null) explorer.FaceDirection(dir);
            PierceCount++; nextSwingAt = Time.time + config.swingInterval;
            if (sound != null) sound.Combat();
            return true;
        }

        // Called by a sword that reaches a monster. Damage = Công × percent × element × crit.
        public void ResolveHit(MonsterVitality target, float percent, bool heavy)
        {
            if (target == null || target.Defeated || stats == null) return;
            var info = DamageCalculator.Compute(stats.Attack * stats.DamageDealt, percent, config.element, target,
                stats.EffectiveCritChance, stats.CritDamage, rng, DamageSource.Projectile);
            info.attacker = gameObject; info.point = CombatLine.Chest(target);
            info.direction = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up).normalized;
            info.skillId = percent >= config.piercePercent ? "ngu-kiem-xuyen" : (heavy ? "ngu-kiem-3" : "ngu-kiem");
            info.isHeavy = heavy || percent >= config.piercePercent;
            if (!target.ApplyDamage(info)) return;
            HitCount++; LastDamage = info.amount; LastTarget = target;
            if (spirit != null) spirit.Restore(config.hitSpiritRefund);
            Hit?.Invoke(target, info);
        }
        public bool ResolveAvatarHit(MonsterVitality target,DamageInfo info)
        {if(target==null||target.Defeated||!target.ApplyDamage(info))return false;HitCount++;LastDamage=info.amount;LastTarget=target;spirit?.Restore(config.hitSpiritRefund);Hit?.Invoke(target,info);return true;}
    }
}
