#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CampusRift.Monsters;
using CampusRift.UI;
using UnityEngine;

namespace CampusRift.Combat
{
    // P01-T07: the shared damage pipeline in the real gameplay scene. Writes Artifacts/Combat/Pipeline.json + Pipeline-DONE.txt.
    public sealed class DamagePipelinePlayTest : MonoBehaviour
    {
        [Serializable] sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(), notes = new List<string>(); }
        const string Output = "Artifacts/Combat/";
        readonly Report report = new Report();
        readonly List<GameObject> fixtures = new List<GameObject>();
        bool running;

        void Update() { if (running && UIStateManager.Instance != null && UIStateManager.Instance.State != UIState.Gameplay) UIStateManager.Instance.EnterScene(true); }
        void Check(bool ok, string label)
        {
            (ok ? report.passed : report.failed).Add(label);
            File.WriteAllText(Output + "Pipeline.json", JsonUtility.ToJson(report, true));
            Debug.Log("COMBAT QA " + (ok ? "PASS " : "FAIL ") + label);
        }

        MonsterVitality Dummy(string name, Element element, float health, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = name; go.layer = 7;
            go.transform.position = position; fixtures.Add(go);
            var v = go.AddComponent<MonsterVitality>(); v.Element = element; v.SetMaxHealth(health, true);
            go.AddComponent<StatusEffectHost>();
            return v;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Output); running = true; Application.runInBackground = true;
            var stamp = DateTime.Now; yield return null;
            IEnumerator run = Run();
            while (true)
            {
                bool next; object current = null;
                try { next = run.MoveNext(); if (next) current = run.Current; }
                catch (Exception e) { Check(false, "Exception: " + e); break; }
                if (!next) break; yield return current;
            }
            foreach (var f in fixtures) if (f != null) Destroy(f);
            running = false;
            File.WriteAllText(Output + "Pipeline-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
            Debug.Log("COMBAT QA DONE " + report.passed.Count + " passed / " + report.failed.Count + " failed");
        }

        IEnumerator Run()
        {
            var player = FindAnyObjectByType<PlayerMonsterHealth>();
            var brain = FindAnyObjectByType<MonsterBrain>();
            var shaban = brain != null ? brain.GetComponent<MonsterVitality>() : null;
            // Keep Shaban from interfering; its vitality is still exercised directly.
            if (brain != null) { brain.enabled = false; var c = brain.GetComponent<MonsterCombat>(); if (c != null) c.enabled = false; }
            player.GetComponent<CampusExplorer>().ReturnToSpawn();
            Vector3 origin = player.transform.position + player.transform.forward * 6f;

            // 1. Element multipliers through the calculator and a brain-less vitality.
            var kim = Dummy("QA Kim target", Element.Kim, 1000, origin);
            var rng = new System.Random(3);
            kim.ApplyDamage(DamageCalculator.Compute(100, 1f, Element.Hoa, kim, 0, 1.5f, rng));
            Check(Mathf.Approximately(kim.Health, 850), "Hỏa on Kim deals ×1.5 on a monster without MonsterBrain (1000 → 850)");
            kim.ApplyDamage(DamageCalculator.Compute(100, 1f, Element.Moc, kim, 0, 1.5f, rng));
            Check(Mathf.Approximately(kim.Health, 775), "Mộc on Kim is resisted ×0.75 (850 → 775)");

            // 2. Defense and armor break.
            kim.defense = 0.2f;
            kim.ApplyDamage(DamageInfo.Create(100, Element.None, DamageSource.Skill, origin, Vector3.down));
            Check(Mathf.Approximately(kim.Health, 695), "20% defense removes a fifth of the hit (775 → 695)");
            kim.GetComponent<StatusEffectHost>().Apply(StatusType.ArmorBreak, 8);
            kim.ApplyDamage(DamageInfo.Create(100, Element.None, DamageSource.Skill, origin, Vector3.down));
            Check(Mathf.Approximately(kim.Health, 595), "Armor break cancels up to 30% defense (695 → 595)");

            // 3. Death is raised exactly once.
            int defeated = 0; kim.DefeatedOnce += () => defeated++;
            kim.ApplyDamage(DamageInfo.Create(5000, Element.None, DamageSource.Skill, origin, Vector3.down));
            bool second = kim.ApplyDamage(DamageInfo.Create(10, Element.None, DamageSource.Skill, origin, Vector3.down));
            Check(kim.Defeated && defeated == 1 && !second, "Lethal hit defeats once; a dead monster ignores further damage");

            // 4. Player invulnerability rules (plan §14.5).
            player.GrantInvulnerability(0); yield return new WaitForSeconds(0.6f);
            int impacts = 0; Action<Vector3, Vector3> onImpact = (p, d) => impacts++; player.ImpactReceived += onImpact;
            float hp = player.CurrentHealth;
            bool first = player.TryTakeDamage(5, player.transform.position, Vector3.forward);
            yield return new WaitForSeconds(0.1f);
            bool blocked = !player.TryTakeDamage(5, player.transform.position, Vector3.forward);
            yield return new WaitForSeconds(0.25f);
            bool later = player.TryTakeDamage(5, player.transform.position, Vector3.forward);
            Check(first && blocked && later && Mathf.Approximately(player.CurrentHealth, hp - 10), "Melee: second hit within 0.25 s is blocked, a hit 0.35 s later lands");
            yield return new WaitForSeconds(0.3f);
            hp = player.CurrentHealth; impacts = 0;
            var fire = DamageInfo.Create(2, Element.Hoa, DamageSource.Environment, player.transform.position, Vector3.down);
            bool e1 = player.ApplyDamage(fire), e2 = player.ApplyDamage(fire);
            bool meleeAfterFire = player.TryTakeDamage(1, player.transform.position, Vector3.forward);
            Check(e1 && e2 && meleeAfterFire && Mathf.Approximately(player.CurrentHealth, hp - 5), "Environment ticks bypass and never grant invulnerability");
            Check(impacts == 1, "Blood/impact feedback fires for physical hits only, not for fire ticks");
            yield return new WaitForSeconds(0.3f);
            player.GrantInvulnerability(1f); hp = player.CurrentHealth;
            bool dodged = !player.TryTakeDamage(5, player.transform.position, Vector3.forward);
            bool burned = player.ApplyDamage(fire);
            Check(dodged && burned && Mathf.Approximately(player.CurrentHealth, hp - 2), "Granted invulnerability (dodge) blocks melee but not environment damage");
            player.ImpactReceived -= onImpact;

            // 5. Defense on the player and Hộ Mệnh Phù style death cancel.
            player.GrantInvulnerability(0); yield return new WaitForSeconds(0.3f);
            player.DamageReduction = 0.5f; hp = player.CurrentHealth;
            player.ApplyDamage(DamageInfo.Create(10, Element.None, DamageSource.Environment, player.transform.position, Vector3.down));
            Check(Mathf.Approximately(player.CurrentHealth, hp - 5), "Player damage reduction removes its fraction");
            player.DamageReduction = 0;
            int defeatedEvents = 0; UnityEngine.Events.UnityAction onDefeat = () => defeatedEvents++; player.Defeated.AddListener(onDefeat);
            Func<DamageInfo, bool> talisman = info => { player.Revive(0.4f, 2f); return true; };
            player.BeforeDefeat += talisman;
            player.ApplyDamage(DamageInfo.Create(99999, Element.None, DamageSource.Environment, player.transform.position, Vector3.down));
            player.BeforeDefeat -= talisman; player.Defeated.RemoveListener(onDefeat);
            Check(defeatedEvents == 0 && Mathf.Approximately(player.CurrentHealth, player.maxHealth * 0.4f) && player.Invulnerable,
                "BeforeDefeat handler cancels death and revives at 40% with invulnerability");
            player.Heal(player.maxHealth);

            // 6. Status effects.
            var target = Dummy("QA status target", Element.Tho, 1000, origin + Vector3.right * 3);
            var status = target.GetComponent<StatusEffectHost>();
            status.Apply(StatusType.Burn, 1.1f, 10);
            yield return new WaitForSeconds(1.3f);
            Check(Mathf.Approximately(target.Health, 980) && !status.Has(StatusType.Burn), "Burn ticks every 0.5 s (2 × 10 in 1.1 s) then expires");
            status.Apply(StatusType.Freeze, 0.5f);
            bool frozen = status.Immobilized && status.SpeedMultiplier == 0;
            yield return new WaitForSeconds(0.7f);
            Check(frozen && !status.Has(StatusType.Freeze) && status.SpeedMultiplier == 1, "Freeze holds the monster (speed 0) and expires on time");
            status.Apply(StatusType.Chill, 1f, 0.4f);
            Check(Mathf.Approximately(status.SpeedMultiplier, 0.6f), "Chill 40% leaves 60% speed");
            status.Apply(StatusType.Shock, 0.1f);
            Check(status.Remaining(StatusType.Shock) >= 0.45f, "Shock lasts at least 0.5 s");
            status.Clear();
            Check(!status.Has(StatusType.Chill) && !status.Has(StatusType.Shock), "Clear removes every status (pool reuse)");

            var boss = Dummy("QA boss", Element.Am, 1000, origin + Vector3.left * 3);
            boss.resistHardControl = true;
            var bossStatus = boss.GetComponent<StatusEffectHost>();
            bossStatus.Apply(StatusType.Freeze, 2.5f);
            Check(!bossStatus.Has(StatusType.Freeze) && Mathf.Approximately(bossStatus.SpeedMultiplier, 0.5f), "Bosses turn freeze into a 50% slow");
            bossStatus.Apply(StatusType.Stun, 3f);
            Check(boss.SuppressionRemaining <= 1.01f && boss.Suppressed, "Boss stun is capped at 1 s");

            // 7. Damage numbers and health bars are pooled.
            var numbers = DamageNumberPool.Instance; var bars = EnemyHealthBars.Instance;
            Check(numbers != null && bars != null, "Damage numbers and health bars bootstrap automatically");
            var punch = Dummy("QA pool target", Element.Moc, 1000000, origin + Vector3.up * 0.1f);
            for (int i = 0; i < 200; i++) punch.ApplyDamage(DamageInfo.Create(3, Element.Kim, DamageSource.Skill, origin, Vector3.down));
            yield return null;
            Check(numbers != null && numbers.CreatedCount <= DamageNumberPool.Capacity && numbers.ActiveCount > 0, "200 hits reuse at most 64 damage numbers (created " + (numbers != null ? numbers.CreatedCount : -1) + ")");
            int barObjects = bars != null ? bars.CreatedCount : -1;
            for (int i = 0; i < 50; i++) punch.ApplyDamage(DamageInfo.Create(3, Element.Kim, DamageSource.Skill, origin, Vector3.down));
            yield return null;
            Check(bars != null && bars.IsShowing(punch) && bars.CreatedCount == barObjects, "A damaged monster shows one reused health bar");
            yield return new WaitForSeconds(0.9f);
            Check(numbers.ActiveCount == 0, "Numbers fade out after 0.8 s");

            // 8. Giant Hand's legacy path still works on Shaban.
            if (shaban != null)
            {
                float before = shaban.Health;
                bool sealedHit = shaban.ReceiveSeal(60, 1f);
                Check(sealedHit && Mathf.Approximately(shaban.Health, before - 60) && shaban.Suppressed, "ReceiveSeal still deals raw 60 damage and staggers Shaban");
            }
            else Check(false, "Shaban present in SampleScene");
            if (brain != null) { brain.enabled = true; var c = brain.GetComponent<MonsterCombat>(); if (c != null) c.enabled = true; }
        }
    }
}
#endif
