#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRift.Enemies
{
    // P04-T07: the light monsters in the real gameplay scene. Writes Artifacts/Enemies/MinionCombat.json + MinionCombat-DONE.txt.
    public sealed class MinionCombatPlayTest : MonoBehaviour
    {
        [Serializable] sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(), notes = new List<string>(); }
        const string Output = "Artifacts/Enemies/";
        readonly Report report = new Report();
        EnemyArchetype goblin, archer; EnemyPool pool; EnemyDirector director;
        CampusExplorer player; PlayerMonsterHealth health; PlayerStats stats; bool running;
        readonly List<(float time, DamageInfo info)> damageLog = new List<(float, DamageInfo)>();

        void Update() { if (running && UIStateManager.Instance != null && UIStateManager.Instance.State != UIState.Gameplay) UIStateManager.Instance.EnterScene(true); }
        void Check(bool ok, string label)
        {
            (ok ? report.passed : report.failed).Add(label);
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "MinionCombat.json", JsonUtility.ToJson(report, true));
            Debug.Log("MINION QA " + (ok ? "PASS " : "FAIL ") + label);
        }
        void Note(string text) { report.notes.Add(text); Debug.Log("MINION QA NOTE " + text); }
        static IEnumerator Frames(int n = 2) { for (int i = 0; i < n; i++) yield return null; }

        // A NavMesh point around the player; tries neighbouring angles when the first is off the mesh.
        bool RingPoint(float radius, float angle, out Vector3 point)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = (angle + i * 30f) * Mathf.Deg2Rad;
                Vector3 want = player.transform.position + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius;
                if (NavMesh.SamplePosition(want, out var hit, 2f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - player.transform.position.y) < 1.5f) { point = hit.position; return true; }
            }
            point = default; return false;
        }

        EnemyInstance Spawn(EnemyArchetype a, float radius, float angle, int tier, float health = 1)
        {
            if (!RingPoint(radius, angle, out var p)) return null;
            var scaling = EnemyScaling.Default; scaling.aiTier = tier; scaling.health = health;
            return pool.Spawn(a, p, scaling);
        }

        IEnumerator Start()
        {
            running = true; Application.runInBackground = true;
            var run = Run();
            while (true)
            {
                bool next; object current = null;
                try { next = run.MoveNext(); if (next) current = run.Current; }
                catch (Exception e) { Check(false, "Exception: " + e); break; }
                if (!next) break; yield return current;
            }
            if (pool != null) pool.ReleaseAll();
            if (stats != null) stats.RemoveSource(StatSource.Buff, "qa-hp");
            running = false;
            File.WriteAllText(Output + "MinionCombat-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
        }

        IEnumerator Run()
        {
            goblin = AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");
            archer = AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/doc-nhan.asset");
            player = FindAnyObjectByType<CampusExplorer>(); health = player.GetComponent<PlayerMonsterHealth>(); stats = player.GetComponent<PlayerStats>();
            var brain = FindAnyObjectByType<MonsterBrain>(); if (brain != null) brain.gameObject.SetActive(false);
            // Enough health that the swarm cannot end the run; damage is still recorded exactly.
            stats.SetModifier(StatSource.Buff, "qa-hp", StatType.MaxHealth, 90000, 0); health.Heal(1e6f);
            stats.suppressCrit = true;
            health.DamageReceived += info => damageLog.Add((Time.time, info));
            pool = EnemyPool.Ensure(); director = EnemyDirector.Ensure();
            player.ReturnToSpawn(); yield return Frames(3);

            Check(goblin != null && archer != null && goblin.prefab != null && archer.prefab != null, "Both archetypes and their prefabs load");
            var one = Spawn(goblin, 12, 0, 0);
            Check(one != null && one.Brain.State == MinionState.Spawn && one.Motor.OnMesh, "A monster spawns on the NavMesh and starts in the Spawn state");
            Check(Mathf.Approximately(one.Vitality.maxHealth, 60) && one.Vitality.Element == Element.Moc && one.gameObject.layer == 7, "Tiểu Yêu has 60 health, element Thổ and sits on the Enemy layer");
            yield return new WaitForSeconds(one.Animation.SpawnSeconds+.2f);
            Check(one.Brain.State == MinionState.Chase, "After the supplied Spawn clip it emerges and starts chasing");
            pool.ReleaseAll(); yield return Frames(2);

            // ---- 1. The horde: 10 Tiểu Yêu + 3 Độc Nhãn, tier 0 ----
            director.ResetCounters(); damageLog.Clear();
            var horde = new List<EnemyInstance>();
            for (int i = 0; i < 10; i++) { var e = Spawn(goblin, 13 + i % 3 * 2, i * 36, 0); if (e != null) horde.Add(e); }
            for (int i = 0; i < 3; i++) { var e = Spawn(archer, 17 + i * 2, 20 + i * 120, 0); if (e != null) horde.Add(e); }
            Check(horde.Count == 13, "13 monsters spawn (10 melee + 3 archers), got " + horde.Count);
            int strikeEvents = 0, windups = 0;
            var windupDurations = new List<float>();
            foreach (var m in horde)
            {
                m.Brain.WindupStarted += b => windups++;
                m.Brain.Struck += b => { strikeEvents++; windupDurations.Add(Time.time - b.WindupStartedAt); };
            }
            int maxWinding = 0, maxWindingRanged = 0; float arrived = -1, start = Time.time; float maxAi = 0;
            while (Time.time - start < 40f)
            {
                yield return null;
                int winding = 0, windingRanged = 0, near = 0;
                foreach (var m in horde)
                {
                    if (!m.Alive) continue;
                    bool busy = m.Brain.State == MinionState.Windup || m.Brain.State == MinionState.Strike;
                    if (busy) { if (m.IsMelee) winding++; else windingRanged++; }
                    float d = Vector3.ProjectOnPlane(m.transform.position - player.transform.position, Vector3.up).magnitude;
                    if (m.IsMelee ? d < 3.2f : d < 14f) near++;
                }
                maxWinding = Mathf.Max(maxWinding, winding); maxWindingRanged = Mathf.Max(maxWindingRanged, windingRanged);
                maxAi = Mathf.Max(maxAi, director.AiMilliseconds);
                if (arrived < 0 && near >= horde.Count) arrived = Time.time - start;
                if (Time.time - start > 22 && arrived >= 0 && windups >= 12) break;
            }
            Check(arrived >= 0 && arrived < 30f, "Every monster reaches the player within 30 s (took " + arrived.ToString("0.0") + " s)");
            Check(maxWinding <= 2 && director.MaxMeleeTokensSeen <= 2, "At tier 0 at most 2 melee monsters wind up or strike at once (saw " + maxWinding + ", tokens " + director.MaxMeleeTokensSeen + ")");
            Check(maxWindingRanged <= 2 && director.MaxRangedTokensSeen <= 2, "At most 2 archers shoot at once (saw " + maxWindingRanged + ")");
            Check(windups >= 6, "The horde attacks repeatedly (" + windups + " wind-ups)");
            float shortest = windupDurations.Count > 0 ? windupDurations.Min() : 0;
            Check(windupDurations.Count > 0 && shortest >= 0.75f, "Every blow is preceded by the 0.8 s warning (shortest " + shortest.ToString("0.00") + " s)");
            var melee = damageLog.Where(d => d.info.source == DamageSource.Melee).ToList(); var shots = damageLog.Where(d => d.info.source == DamageSource.Projectile).ToList();
            Check(melee.Count > 0 && melee.All(d => Mathf.Approximately(d.info.amount, 8f)), "Tiểu Yêu blows land for 8 damage (" + melee.Count + " landed)");
            Check(shots.Count > 0 && shots.All(d => Mathf.Approximately(d.info.amount, 8.5f)), "Poison bolts land for 8.5 damage (" + shots.Count + " landed)");
            Check(damageLog.All(d => d.info.attacker != null && d.info.element != Element.None), "Every hit carries its attacker and element");
            Note("AI time with 13 monsters: max " + maxAi.ToString("0.000") + " ms/frame");
            Check(maxAi < 2f, "AI cost with 13 monsters stays under 2 ms per frame (" + maxAi.ToString("0.000") + " ms)");

            // ---- 2. Interrupting a wind-up ----
            pool.ReleaseAll(); yield return Frames(2); director.ResetCounters();
            var duel = Spawn(goblin, 3, 0, 0);
            float until = Time.time + 12; while (duel.Brain.State != MinionState.Windup && Time.time < until) yield return null;
            Check(duel.Brain.State == MinionState.Windup && director.Holds(duel), "A close monster starts a wind-up and holds an attack token");
            duel.Status.Apply(StatusType.Freeze, 1.2f);
            yield return new WaitForSeconds(0.3f);
            Check(duel.Brain.Interrupts == 1 && !director.Holds(duel) && duel.Brain.State != MinionState.Windup, "Freezing a monster mid wind-up cancels the blow and returns its token");
            yield return new WaitForSeconds(1.2f);

            // ---- 3. Tier 1 surrounds the player ----
            pool.ReleaseAll(); yield return Frames(2); director.ResetCounters(); damageLog.Clear();
            var ring = new List<EnemyInstance>();
            for (int i = 0; i < 8; i++) { var e = Spawn(goblin, 9, i * 10, 1); if (e != null) ring.Add(e); }
            yield return new WaitForSeconds(9f);
            var angles = ring.Where(e => e.Alive).Select(e => { Vector3 d = e.transform.position - player.transform.position; return Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f); }).OrderBy(a => a).ToList();
            float largestGap = 0; for (int i = 0; i < angles.Count; i++) largestGap = Mathf.Max(largestGap, Mathf.Repeat(angles[(i + 1) % angles.Count] - angles[i], 360f));
            if (angles.Count == 1) largestGap = 360;
            Note("Tier 1 ring: " + angles.Count + " monsters, largest angular gap " + largestGap.ToString("0") + "°");
            Check(angles.Count == 8 && largestGap < 140f, "At tier 1 eight monsters spread around the player (largest gap " + largestGap.ToString("0") + "°)");
            Check(director.MaxMeleeTokensSeen <= 3, "Tier 1 allows at most 3 simultaneous attackers (saw " + director.MaxMeleeTokensSeen + ")");
            Check(ring.All(e => Mathf.Approximately(e.Brain.LastWindupSeconds, 0.6f) || e.Brain.LastWindupSeconds == 0), "Tier 1 wind-up is 0.6 s");

            // ---- 4. Death, events and reuse ----
            pool.ReleaseAll(); yield return Frames(2);
            int died = 0; int weight = 0; Action<EnemyInstance> onDied = e => { died++; weight += e.SwordIntentWeight; };
            EnemyDirector.EnemyDied += onDied;
            var victims = new List<EnemyInstance>();
            for (int i = 0; i < 5; i++) { var e = Spawn(goblin, 14, i * 70, 0); if (e != null) victims.Add(e); }
            yield return new WaitForSeconds(0.2f);
            foreach (var v in victims) v.Vitality.ApplyDamage(DamageInfo.Create(99999, Element.None, DamageSource.Skill, v.transform.position, Vector3.down, gameObject));
            yield return null;
            Check(died == 5 && weight == 5 && victims.All(v => v.Vitality.Defeated), "Killing five monsters raises EnemyDied five times with their Thiên Kiếm weight");
            Check(victims.All(v => !v.GetComponent<Collider>().enabled), "A dead monster stops blocking movement");
            yield return new WaitForSeconds(victims[0].Animation.DeathSeconds+.7f);
            Check(victims.All(v => !v.gameObject.activeSelf), "After the death animation and fade every corpse returns to the pool");
            EnemyDirector.EnemyDied -= onDied;
            int created = pool.CreatedCount;
            for (int wave = 0; wave < 5; wave++)
            {
                var w = new List<EnemyInstance>();
                for (int i = 0; i < 5; i++) { var e = Spawn(goblin, 14, i * 70 + wave * 9, 0); if (e != null) w.Add(e); }
                yield return new WaitForSeconds(0.15f);
                foreach (var v in w) v.Vitality.ApplyDamage(DamageInfo.Create(99999, Element.None, DamageSource.Skill, v.transform.position, Vector3.down, gameObject));
                yield return new WaitForSeconds(w[0].Animation.DeathSeconds+.7f);
            }
            Check(pool.CreatedCount <= created + 1 && pool.ActiveCount == 0, "Five more waves of five reuse the pooled monsters (created " + pool.CreatedCount + ")");
            int objects = FindObjectsByType<Transform>(FindObjectsInactive.Include).Length;
            for (int i = 0; i < 100; i++)
            {
                var batch = new List<EnemyInstance>();
                for (int k = 0; k < 5; k++) { var e = Spawn(goblin, 14, k * 70, 0); if (e != null) batch.Add(e); }
                if (i == 50 && batch.Count > 0) { batch[0].Status.Apply(StatusType.Freeze, 30); batch[0].Status.Apply(StatusType.Burn, 30, 5); batch[0].Vitality.ApplyDamage(DamageInfo.Create(30, Element.None, DamageSource.Skill, Vector3.zero, Vector3.down, gameObject)); }
                pool.ReleaseAll();
                if (i % 20 == 0) yield return null;
            }
            yield return null;
            int objectsAfter = FindObjectsByType<Transform>(FindObjectsInactive.Include).Length;
            Check(objectsAfter <= objects + 20 && pool.CreatedCount <= created + 1, "100 spawn/release cycles do not grow the scene (" + objects + " → " + objectsAfter + " transforms)");
            var reused = Spawn(goblin, 12, 0, 0);
            Check(reused != null && !reused.Status.Has(StatusType.Freeze) && !reused.Status.Has(StatusType.Burn) && Mathf.Approximately(reused.Vitality.Health, 60) && !reused.Vitality.Defeated &&
                  reused.GetComponent<Collider>().enabled, "A recycled monster has no leftover status, damage, or disabled collider");
            yield return new WaitForSeconds(reused.Animation.SpawnSeconds+.2f);
            Check(reused.Brain.State == MinionState.Chase && Mathf.Abs(reused.Brain.GetComponentInChildren<Animator>().transform.localScale.x - 1f) < 0.05f, "A recycled monster emerges at full size (" + reused.Brain.GetComponentInChildren<Animator>().transform.localScale.x.ToString("0.000") + ")");
            pool.ReleaseAll(); yield return Frames(2);

            // ---- 5. Poison bolts ----
            damageLog.Clear(); var bolts = EnemyProjectilePool.Ensure();
            Vector3 aim = player.GetComponent<TargetLock>().AimForward;
            Vector3 chest = player.transform.position + Vector3.up * 1.1f;
            int walls = EnemyProjectile.ImpactsOnWalls, onPlayer = EnemyProjectile.ImpactsOnPlayer;
            var wallSkill = player.GetComponent<VoidWallSkill>(); wallSkill.RefillCharges(); player.GetComponent<SpiritPower>().Refill();
            yield return new WaitForSeconds(0.4f);
            bool placed = wallSkill.QuickCast(); yield return new WaitForSeconds(0.5f);
            bolts.Fire(chest + aim * 9f, -aim, 14f, 10f, Element.Moc, null);
            yield return new WaitForSeconds(1.2f);
            Check(placed && wallSkill.LastDeployed != null && EnemyProjectile.ImpactsOnWalls == walls + 1 && EnemyProjectile.ImpactsOnPlayer == onPlayer && !damageLog.Any(),
                "A Void Wall stops a poison bolt and takes the damage instead of the player");
            foreach (var w in VoidWall.Active.ToArray()) w.Dissolve(false);
            yield return Frames(2);
            bolts.Fire(chest + aim * 9f, -aim, 14f, 10f, Element.Moc, null);
            yield return new WaitForSeconds(0.9f);
            Check(EnemyProjectile.ImpactsOnPlayer == onPlayer + 1 && damageLog.Count == 1 && Mathf.Approximately(damageLog[0].info.amount, 10f) && damageLog[0].info.source == DamageSource.Projectile,
                "An open bolt hits the player for 10 projectile damage");
            health.GrantInvulnerability(0); yield return new WaitForSeconds(0.4f);
            onPlayer = EnemyProjectile.ImpactsOnPlayer; damageLog.Clear();
            Vector3 origin = chest + aim * 10f; bolts.Fire(origin, -aim, 14f, 10f, Element.Moc, null);
            yield return new WaitForSeconds(0.25f);
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false; player.transform.position += Vector3.Cross(Vector3.up, aim) * 3f; cc.enabled = true; Physics.SyncTransforms();
            yield return new WaitForSeconds(1.2f);
            Check(EnemyProjectile.ImpactsOnPlayer == onPlayer && !damageLog.Any(), "Stepping aside lets a bolt pass without damage");
            player.ReturnToSpawn(); yield return Frames(3);
            int shotsBefore = bolts.CreatedCount;
            for (int i = 0; i < 60; i++) { bolts.Fire(chest + Vector3.up * 30f, Vector3.up, 20f, 1f, Element.Moc, null); if (i % 6 == 5) yield return new WaitForSeconds(0.1f); }
            yield return new WaitForSeconds(4.2f);
            Check(bolts.CreatedCount <= EnemyProjectilePool.Capacity && bolts.ActiveCount == 0, "60 bolts reuse at most 24 pooled projectiles (created " + bolts.CreatedCount + ") and all expire");

            // ---- 6. Stairs ----
            pool.ReleaseAll(); yield return Frames(2);
            Vector3 topFloor = new Vector3(-26.2f, 8f, 17.6f);
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false; player.transform.position = topFloor + Vector3.up * 0.1f; controller.enabled = true; Physics.SyncTransforms();
            yield return Frames(3);
            var climber = pool.Spawn(goblin, new Vector3(-26.2f, 0f, 17.6f), EnemyScaling.Default);
            if (climber != null)
            {
                float t0 = Time.time; float best = float.MaxValue;
                while (Time.time - t0 < 45f)
                {
                    yield return null;
                    Vector3 d = climber.transform.position - player.transform.position;
                    best = Mathf.Min(best, new Vector2(d.x, d.z).magnitude + Mathf.Abs(d.y));
                    if (Mathf.Abs(d.y) < 1.5f && new Vector2(d.x, d.z).magnitude < 3f) break;
                }
                Note("Stairs: closest approach " + best.ToString("0.0") + " m after " + (Time.time - t0).ToString("0.0") + " s");
                Check(best < 4f, "A Tiểu Yêu climbs the stairs from the ground floor to the player eight metres up (closest " + best.ToString("0.0") + " m)");
            }
            else Check(false, "The stair test could spawn its monster");
            pool.ReleaseAll(); player.ReturnToSpawn(); yield return Frames(3);
            if (brain != null) brain.gameObject.SetActive(true);
        }
    }
}
#endif
