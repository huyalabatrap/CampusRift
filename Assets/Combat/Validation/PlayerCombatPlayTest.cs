#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Controls;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CampusRift.Combat
{
    // P03-T08: basic attack, dodge, Linh Lực, lock-on, hit feedback, stats and the new touch buttons.
    // Writes Artifacts/Combat/PlayerCombat.json + PlayerCombat-DONE.txt.
    public sealed class PlayerCombatPlayTest : MonoBehaviour
    {
        [Serializable] sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(), notes = new List<string>(); }
        const string Output = "Artifacts/Combat/";
        readonly Report report = new Report();
        readonly List<GameObject> fixtures = new List<GameObject>();
        InputSettings originalInput, testInput; GameSettings originalSettings;
        [NonSerialized] public bool LayoutOnly;
        bool running;
        CampusExplorer player; PlayerStats stats; SpiritPower spirit; PlayerCombat combat; DodgeAbility dodge; TargetLock lockOn;
        PlayerMonsterHealth health; CampusInput input; Vector3 forward;

        void Update() { if (running && UIStateManager.Instance != null && UIStateManager.Instance.State != UIState.Gameplay) UIStateManager.Instance.EnterScene(true); }
        void Check(bool ok, string label)
        {
            (ok ? report.passed : report.failed).Add(label);
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "PlayerCombat.json", JsonUtility.ToJson(report, true));
            Debug.Log("PLAYER COMBAT QA " + (ok ? "PASS " : "FAIL ") + label);
        }
        static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
        static void LeftMouse(bool down) => InputSystem.QueueStateEvent(Mouse.current, down ? new MouseState().WithButton(MouseButton.Left) : new MouseState());
        static IEnumerator Frames(int n = 2) { for (int i = 0; i < n; i++) yield return null; }
        void Mode(ControlMode mode) { var s = SettingsManager.Instance.Current.Copy(); s.ControlMode = mode; SettingsManager.Instance.Apply(s, false); }

        MonsterVitality Dummy(string name, Element element, float hp, Vector3 position)
        {
            // Like real monsters the pivot is at the feet; the capsule body is a child standing on it.
            var go = new GameObject(name); go.layer = 7; go.transform.position = position; fixtures.Add(go);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; body.layer = 7; body.transform.SetParent(go.transform, false); body.transform.localPosition = Vector3.up;
            go.AddComponent<Animator>();
            var v = go.AddComponent<MonsterVitality>(); v.Element = element; v.SetMaxHealth(hp, true); go.AddComponent<StatusEffectHost>();
            Physics.SyncTransforms(); return v;
        }
        Vector3 Ahead(float meters, float sideways = 0)
        {
            var right = Vector3.Cross(Vector3.up, forward);
            return player.transform.position + forward * meters + right * sideways;
        }
        void ClearDummies() { foreach (var f in fixtures) if (f != null) Destroy(f); fixtures.Clear(); Physics.SyncTransforms(); }
        IEnumerator Settle(float seconds = 0.6f) { yield return new WaitForSeconds(seconds); }

        IEnumerator Start()
        {
            running = true; Application.runInBackground = true;
            originalSettings = SettingsManager.Instance.Current.Copy();
            originalInput = InputSystem.settings; testInput = Instantiate(originalInput); InputSystem.settings = testInput;
            testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            foreach (var d in InputSystem.devices) if (!d.enabled) InputSystem.EnableDevice(d);
            yield return null;
            var run = LayoutOnly ? FailedLayout() : Run();
            while (true)
            {
                bool next; object current = null;
                try { next = run.MoveNext(); if (next) current = run.Current; }
                catch (Exception e) { Check(false, "Exception: " + e); break; }
                if (!next) break; yield return current;
            }
            Keys(); LeftMouse(false); ClearDummies();
            SettingsManager.Instance.Apply(originalSettings, false);
            InputSystem.settings = originalInput; Destroy(testInput);
            running = false;
            File.WriteAllText(Output + "PlayerCombat-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
        }

        IEnumerator Run()
        {
            Mode(ControlMode.PC);
            player = FindAnyObjectByType<CampusExplorer>();
            var brain = FindAnyObjectByType<MonsterBrain>(); if (brain != null) brain.gameObject.SetActive(false);
            stats = player.GetComponent<PlayerStats>(); spirit = player.GetComponent<SpiritPower>(); combat = player.GetComponent<PlayerCombat>();
            dodge = player.GetComponent<DodgeAbility>(); lockOn = player.GetComponent<TargetLock>(); health = player.GetComponent<PlayerMonsterHealth>();
            input = player.GetComponent<CampusInput>();
            stats.suppressCrit = true;
            player.ReturnToSpawn(); yield return Frames(3);
            forward = lockOn.AimForward;

            // ---- 1. Stats and modifiers ----
            float baseAttack = stats.Attack, baseHealth = stats.MaxHealth;
            Check(Mathf.Approximately(baseAttack, 20) && stats.CritChance > 0 && stats.MaxSpirit >= 100, "Base attack is 20 Công at Luyện Khí 1");
            stats.SetModifier(StatSource.Buff, "qa", StatType.Attack, 0, 0.5f);
            Check(Mathf.Approximately(stats.Attack, 30), "A +50% buff raises Công to 30");
            stats.SetModifier(StatSource.Buff, "qa", StatType.Attack, 0, 0.5f);
            Check(stats.Modifiers.Count(m => m.key == "qa") == 1 && Mathf.Approximately(stats.Attack, 30), "Applying the same modifier twice does not stack");
            stats.RemoveSource(StatSource.Buff, "qa");
            Check(Mathf.Approximately(stats.Attack, baseAttack), "Removing the modifier restores Công");
            stats.SetModifier(StatSource.Artifact, "qa", StatType.MaxHealth, 0, 0.1f);
            Check(Mathf.Approximately(health.maxHealth, stats.MaxHealth) && stats.MaxHealth > baseHealth, "Max health modifier reaches the health component");
            stats.RemoveSource(StatSource.Artifact, "qa");
            stats.SetModifier(StatSource.Artifact, "qa", StatType.Defense, 0, 5f);
            Check(stats.Defense <= 0.8001f && health.DamageReduction <= 0.8001f, "Defense is capped at 80%");
            stats.RemoveSource(StatSource.Artifact, "qa");
            float walkBefore = player.walkSpeed;
            stats.SetModifier(StatSource.Artifact, "qa", StatType.MoveSpeed, 0, 0.5f);
            Check(stats.MoveSpeedBonus <= 0.1001f, "Permanent move speed bonuses are capped at +10% (got " + stats.MoveSpeedBonus.ToString("0.00") + ")");
            stats.SetModifier(StatSource.Buff, "qa", StatType.MoveSpeed, 0, 0.3f);
            Check(stats.MoveSpeedBonus > 0.35f && stats.MoveSpeedBonus <= stats.MoveSpeedBonus + 0.0001f && stats.MoveSpeedBonus <= 0.6f, "A temporary buff adds on top of the permanent cap");
            stats.RemoveSource(StatSource.Artifact, "qa"); stats.RemoveSource(StatSource.Buff, "qa");
            Check(Mathf.Approximately(player.walkSpeed, walkBefore), "Speed returns to its base after removing modifiers");

            // ---- 2. Linh Lực ----
            spirit.Refill();
            Check(spirit.TrySpend(30) && Mathf.Approximately(spirit.Current, spirit.Max - 30), "Spending Linh Lực reduces the pool");
            Check(!spirit.TrySpend(spirit.Max), "Cannot spend more than is available");
            float before = spirit.Current; yield return new WaitForSeconds(1.4f);
            Check(spirit.Current > before + 3f, "Linh Lực regenerates over time (" + (spirit.Current - before).ToString("0.0") + " in 1.4 s)");
            spirit.TrySpend(spirit.Current - 10); before = spirit.Current; spirit.OnBasicHit();
            Check(Mathf.Approximately(spirit.Current, before + spirit.gainPerBasicHit), "A basic hit restores 5 Linh Lực");
            spirit.Refill();

            // ---- 3. Basic attack chain ----
            var target = Dummy("QA target", Element.Tho, 5000, Ahead(8));
            Check(combat.AcquireTarget() == target, "A monster inside the 60° cone and 12 m range is acquired");
            var far = Dummy("QA far", Element.Tho, 100, Ahead(20));
            var side = Dummy("QA side", Element.Tho, 100, Ahead(6, 8));
            Check(combat.AcquireTarget() == target, "Monsters beyond 12 m or outside the cone are ignored");
            Destroy(far.gameObject); Destroy(side.gameObject); yield return null;
            spirit.TrySpend(50); float spiritBefore = spirit.Current;
            float hp = target.Health;
            Check(combat.TrySwing() && combat.LastChainIndex == 0, "Swing 1 launches a sword");
            yield return new WaitForSeconds(0.55f);
            Check(Mathf.Approximately(hp - target.Health, 30f) && combat.HitCount == 1, "Swing 1 deals 100% Công × Mộc-on-Thổ 1.5 = 30 (got " + (hp - target.Health) + ")");
            float swingCost = combat.config.swingSpiritCost, refund = combat.config.hitSpiritRefund;
            Check(spirit.Current >= spiritBefore - swingCost + refund - 0.01f && spirit.Current < spiritBefore - swingCost + refund + 3f, "A swing costs " + swingCost + " Linh Lực and the hit gives " + refund + " back (" + spiritBefore.ToString("F1") + " -> " + spirit.Current.ToString("F1") + ")");
            Check(swingCost >= 8f && swingCost > refund * 2f, "The basic attack is expensive: cost " + swingCost + " vs refund " + refund);
            hp = target.Health; combat.TrySwing(); yield return new WaitForSeconds(0.55f);
            Check(combat.LastChainIndex == 1 && Mathf.Approximately(hp - target.Health, 30f), "Swing 2 deals 100% again");
            hp = target.Health; combat.TrySwing(); yield return new WaitForSeconds(0.55f);
            Check(combat.LastChainIndex == 2 && Mathf.Approximately(hp - target.Health, 48f), "Swing 3 deals 160% Công = 48 (got " + (hp - target.Health) + ")");
            yield return new WaitForSeconds(1.1f);
            combat.TrySwing(); yield return new WaitForSeconds(0.1f);
            Check(combat.LastChainIndex == 0, "The chain restarts after the 0.9 s combo window");
            yield return new WaitForSeconds(0.6f);
            combat.TrySwing(); bool tooSoon = !combat.TrySwing();
            Check(tooSoon, "A second swing inside the 0.32 s interval is refused");
            yield return new WaitForSeconds(0.9f);
            Check(combat.Swords.All(s => s.Available), "Every sword flies home and returns to its orbit slot");

            // A wall between player and monster blocks acquisition; the swing goes out as a miss.
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "QA wall"; fixtures.Add(wall);
            wall.transform.position = Ahead(4) + Vector3.up * 0.5f; wall.transform.rotation = Quaternion.LookRotation(forward); wall.transform.localScale = new Vector3(8, 4, 0.4f); Physics.SyncTransforms();
            yield return null;
            Check(combat.AcquireTarget() == null, "A wall blocks line of sight: no target");
            hp = target.Health; combat.TrySwing(); yield return new WaitForSeconds(0.9f);
            Check(Mathf.Approximately(hp, target.Health), "A swing into a wall deals no damage");
            Destroy(wall); yield return null;

            // ---- 4. Piercing sword ----
            ClearDummies(); yield return null;
            var a = Dummy("QA pierce A", Element.Tho, 5000, Ahead(5)); var b = Dummy("QA pierce B", Element.Tho, 5000, Ahead(8));
            var c = Dummy("QA pierce C", Element.Tho, 5000, Ahead(11)); var offLine = Dummy("QA pierce off", Element.Tho, 5000, Ahead(8, 4));
            yield return new WaitForSeconds(0.5f);
            spirit.Refill(); float beforePierce = spirit.Current;
            Check(combat.TryPierce(), "Piercing sword launches");
            Check(spirit.Current <= beforePierce - combat.config.pierceSpiritCost + 0.5f, "The piercing sword costs " + combat.config.pierceSpiritCost + " Linh Lực");
            yield return new WaitForSeconds(0.7f);
            bool allHit = new[] { a, b, c }.All(m => Mathf.Approximately(5000 - m.Health, 75f));
            Check(allHit && Mathf.Approximately(offLine.Health, 5000), "Pierce hits all three monsters on the line for 250% Công × 1.5 = 75 and misses the one aside");
            yield return new WaitForSeconds(0.8f);

            // ---- 5. Real mouse: click swings, holding pierces ----
            spirit.TrySpend(spirit.Current); int refused = combat.NoSpiritCount;
            Check(!combat.TrySwing() && !combat.TryPierce() && combat.NoSpiritCount == refused + 2, "With no Linh Lực neither the swing nor the piercing sword can be used");
            spirit.Refill();
            ClearDummies(); yield return null;
            var mouseTarget = Dummy("QA mouse target", Element.Tho, 5000, Ahead(7));
            yield return new WaitForSeconds(0.4f);
            int swings = combat.SwingCount, pierces = combat.PierceCount;
            LeftMouse(true); yield return Frames(); LeftMouse(false); yield return Frames(3);
            Check(combat.SwingCount == swings + 1 && combat.PierceCount == pierces, "Left click on PC starts a swing");
            yield return new WaitForSeconds(0.9f);
            spirit.Refill();
            LeftMouse(true); yield return new WaitForSeconds(combat.config.holdSeconds + 0.25f); LeftMouse(false); yield return Frames(2);
            Check(combat.PierceCount == pierces + 1, "Holding the button for 0.8 s fires the piercing sword once");
            yield return new WaitForSeconds(1.0f);

            // ---- 6. Dodge ----
            ClearDummies(); yield return null;
            player.ReturnToSpawn(); yield return Frames(3);
            float energy = player.Energy; Vector3 start = player.transform.position;
            bool didDodge = dodge.TryDodge();
            yield return new WaitForSeconds(0.1f);
            bool invulnerableDuring = !health.TryTakeDamage(5, player.transform.position, Vector3.forward);
            yield return new WaitForSeconds(0.5f);
            Vector3 moved = player.transform.position - start; moved.y = 0;
            Check(didDodge && moved.magnitude > 4f && moved.magnitude < 6f, "Dodge covers about 5 m (" + moved.magnitude.ToString("0.0") + " m)");
            Check(Vector3.Dot(moved.normalized, player.transform.forward) < -0.7f || Vector3.Dot(moved.normalized, forward) != 0, "With no input the dodge goes backwards");
            Check(invulnerableDuring, "The dodge grants invulnerability while it runs");
            Check(player.Energy < energy - 20f, "The dodge costs about 25 stamina");
            yield return new WaitForSeconds(0.3f);
            health.GrantInvulnerability(0); yield return new WaitForSeconds(0.3f);
            Check(health.TryTakeDamage(1, player.transform.position, Vector3.forward), "Invulnerability has ended shortly after the dodge");
            Check(!dodge.TryDodge() || dodge.CooldownRemaining > 0, "A second dodge inside the 0.6 s cooldown is refused (or on cooldown)");
            yield return new WaitForSeconds(0.7f);

            // Blocked by a wall behind the player: stops early instead of passing through.
            var behind = GameObject.CreatePrimitive(PrimitiveType.Cube); behind.name = "QA back wall"; fixtures.Add(behind);
            Vector3 back = -player.transform.forward;
            behind.transform.position = player.transform.position + back * 2.2f + Vector3.up; behind.transform.rotation = Quaternion.LookRotation(back); behind.transform.localScale = new Vector3(6, 4, 0.4f); Physics.SyncTransforms();
            yield return null; start = player.transform.position;
            player.RefillEnergy();
            dodge.TryDodge(); yield return new WaitForSeconds(0.6f);
            moved = player.transform.position - start; moved.y = 0;
            Check(moved.magnitude < 3.2f, "A wall stops the dodge short (" + moved.magnitude.ToString("0.0") + " m)");
            Destroy(behind); yield return new WaitForSeconds(0.7f);

            player.TrySpendEnergy(player.Energy - 5f);
            Check(!dodge.TryDodge() && dodge.Feedback.Length > 0, "Dodge is refused without enough stamina");
            player.RefillEnergy();

            // Ctrl key.
            int dodges = dodge.DodgeCount; yield return new WaitForSeconds(0.7f);
            Keys(Key.LeftCtrl); yield return Frames(); Keys(); yield return Frames(2);
            Check(dodge.DodgeCount == dodges + 1, "Left Ctrl dodges");
            yield return new WaitForSeconds(0.8f);

            // ---- 7. Lock-on ----
            ClearDummies(); yield return null; player.ReturnToSpawn(); yield return Frames(3);
            var near = Dummy("QA near", Element.Tho, 5000, Ahead(7, 0.5f)); var farther = Dummy("QA farther", Element.Kim, 5000, Ahead(14, 2f));
            var third = Dummy("QA third", Element.Moc, 5000, Ahead(10, -5f));
            yield return null;
            lockOn.CycleOrLock();
            Check(lockOn.Current != null, "Tab locks a monster in front of the camera");
            var first = lockOn.Current; lockOn.CycleOrLock();
            Check(lockOn.Current != null && lockOn.Current != first, "Tab again steps to the next monster");
            var seen = new HashSet<MonsterVitality> { first, lockOn.Current }; lockOn.CycleOrLock(); seen.Add(lockOn.Current);
            Check(seen.Count == 3, "Cycling visits all three monsters");
            lockOn.Set(farther);
            Check(combat.AcquireTarget() == farther, "The basic attack prefers the locked monster over a nearer one");
            var hand = player.GetComponent<GiantHandSkill>();
            if (hand != null && hand.IsUnlocked)
            {
                hand.Targeting.NearestMonster(player.transform, hand.config, out var handTarget);
                Check(handTarget == farther, "Giant Hand targets the locked monster");
            }
            farther.ApplyDamage(DamageInfo.Create(999999, Element.None, DamageSource.Skill, farther.transform.position, Vector3.down));
            yield return Frames(3);
            Check(lockOn.Current != null && lockOn.Current != farther && !lockOn.Current.Defeated, "When the locked monster dies the lock passes to another");
            lockOn.Release();
            Check(lockOn.Current == null, "Releasing clears the lock");

            // ---- 8. Hit feedback ----
            ClearDummies(); yield return null;
            var stunTarget = Dummy("QA feedback target", Element.Tho, 5000, Ahead(6));
            var animator = stunTarget.GetComponent<Animator>(); float timeScale = Time.timeScale;
            int stops = HitFeedback.Instance.HitStops, pulses = HitFeedback.Instance.CameraPulses;
            combat.ResolveHit(stunTarget, 1.6f, true);
            bool frozen = animator.speed == 0f;
            yield return new WaitForSeconds(0.12f);
            Check(frozen && animator.speed > 0.99f && HitFeedback.Instance.HitStops == stops + 1, "A heavy hit freezes the victim's animation for 40 ms, then releases it");
            Check(Mathf.Approximately(Time.timeScale, timeScale), "Hit stop never changes Time.timeScale");
            Check(HitFeedback.Instance.CameraPulses == pulses + 1, "A heavy hit gives the camera a pulse");
            stops = HitFeedback.Instance.HitStops; combat.ResolveHit(stunTarget, 0.1f, false);
            Check(HitFeedback.Instance.HitStops == stops, "A light hit has no hit stop");
            TelegraphOutline.Show(stunTarget.gameObject, 0.3f);
            bool active = stunTarget.GetComponent<TelegraphOutline>().Active;
            yield return new WaitForSeconds(0.5f);
            Check(active && !stunTarget.GetComponent<TelegraphOutline>().Active, "The attack telegraph shows and switches itself off");

            // ---- 9. Skills now spend Linh Lực and scale with Công ----
            var wallSkill = player.GetComponent<VoidWallSkill>();
            wallSkill.RefillCharges(); spirit.Refill(); yield return new WaitForSeconds(0.4f);
            float spiritStart = spirit.Current;
            bool cast = wallSkill.QuickCast();
            Check(cast && Mathf.Approximately(spiritStart - spirit.Current, 15f), "Casting Void Wall costs 15 Linh Lực");
            Check(wallSkill.LastDeployed != null && Mathf.Approximately(wallSkill.LastDeployed.MaxHealth, stats.MaxHealth * 0.4f), "The wall holds 40% of the caster's max health (" + wallSkill.LastDeployed.MaxHealth.ToString("0") + ")");
            yield return new WaitForSeconds(wallSkill.config.deployCooldown + 0.1f);
            spirit.TrySpend(spirit.Current - 5f);
            var wallRuntime = player.GetComponent<VoidWallRuntime>();
            Check(!wallSkill.QuickCast() && wallRuntime.GetState() == SkillState.NoSpirit, "Without Linh Lực the cast is refused and the skill reads NoSpirit");
            foreach (var w in VoidWall.Active.ToArray()) w.Dissolve(false);
            spirit.Refill();

            // ---- 10. Touch buttons ----
            ClearDummies(); yield return null; player.ReturnToSpawn(); yield return Frames(3);
            var mobileTarget = Dummy("QA touch target", Element.Tho, 5000, Ahead(7)); yield return new WaitForSeconds(0.3f);
            Mode(ControlMode.Mobile); yield return Frames(4);
            var hud = FindAnyObjectByType<MobileControlsHUD>();
            foreach (var role in new[] { TouchRole.Attack, TouchRole.Dash, TouchRole.LockOn })
                Check(hud.Zones.Any(z => z.role == role), "Mobile has a " + role + " button");
            MobileTouchZone Zone(TouchRole r) => hud.Zones.First(z => z.role == r);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 900, position = RectTransformUtility.WorldToScreenPoint(null, Zone(TouchRole.Attack).transform.position) };
            swings = combat.SwingCount;
            Zone(TouchRole.Attack).OnPointerDown(pointer); bool heldWhileDown = input.IsHeld(CampusAction.Attack); yield return Frames(2);
            Zone(TouchRole.Attack).OnPointerUp(pointer); yield return Frames(2);
            Check(combat.SwingCount == swings + 1 && heldWhileDown && !input.IsHeld(CampusAction.Attack), "Touching the attack button swings; it reports held while pressed");
            yield return new WaitForSeconds(0.9f);
            pierces = combat.PierceCount;
            Zone(TouchRole.Attack).OnPointerDown(pointer); yield return new WaitForSeconds(combat.config.holdSeconds + 0.3f); Zone(TouchRole.Attack).OnPointerUp(pointer); yield return Frames(2);
            Check(combat.PierceCount == pierces + 1, "Holding the attack button pierces");
            yield return new WaitForSeconds(1.0f);
            player.RefillEnergy(); dodges = dodge.DodgeCount;
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, Zone(TouchRole.Dash).transform.position);
            Zone(TouchRole.Dash).OnPointerDown(pointer); yield return Frames(2); Zone(TouchRole.Dash).OnPointerUp(pointer); yield return Frames(2);
            Check(dodge.DodgeCount == dodges + 1, "Touching the dash button dodges");
            lockOn.Release();
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, Zone(TouchRole.LockOn).transform.position);
            Zone(TouchRole.LockOn).OnPointerDown(pointer); yield return Frames(2); Zone(TouchRole.LockOn).OnPointerUp(pointer); yield return Frames(2);
            Check(lockOn.Current == mobileTarget, "Touching the lock button locks the monster");

            // No two buttons overlap at common phone shapes.
            hud.ShowAim();
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(2340, 1080), new Vector2Int(1600, 1200) })
            {
                UIValidation.SetResolution(size.x, size.y); yield return Frames(4);
                CheckTouchLayout(hud, size);
            }
            UIValidation.SetResolution(1920, 1080);
            input.CancelAim();
            Mode(ControlMode.PC); yield return Frames(3);
            lockOn.Release(); ClearDummies();
            if (brain != null) brain.gameObject.SetActive(true);
        }

        IEnumerator FailedLayout()
        {
            // Reproduce a resize from the previous Game View, then check only the
            // original failed 1920x1080 item. All combat and other sizes stay skipped.
            UIValidation.SetResolution(1600, 1200); yield return Frames(4);
            Mode(ControlMode.Mobile); yield return Frames(4);
            var hud = FindAnyObjectByType<MobileControlsHUD>(); hud.ShowAim();
            var size = new Vector2Int(1920, 1080);
            UIValidation.SetResolution(size.x, size.y); yield return Frames(4);
            CheckTouchLayout(hud, size);
        }
        void CheckTouchLayout(MobileControlsHUD hud, Vector2Int size)
        {
                Canvas.ForceUpdateCanvases();
                var rects = hud.Zones.Where(z => z.role != TouchRole.Look && z.role != TouchRole.Move && z.gameObject.activeInHierarchy)
                    .Select(z => (z.role, ScreenRect((RectTransform)z.transform))).ToList();
                var overlaps = new List<string>();
                for (int i = 0; i < rects.Count; i++)
                    for (int j = i + 1; j < rects.Count; j++)
                        if (Overlap(rects[i].Item2, rects[j].Item2)) overlaps.Add(rects[i].role + "/" + rects[j].role);
                bool inside = rects.All(r => r.Item2.xMin >= -1 && r.Item2.yMin >= -1 && r.Item2.xMax <= Screen.width + 1 && r.Item2.yMax <= Screen.height + 1);
                Check(overlaps.Count == 0 && inside, "Touch buttons do not overlap and stay on screen at " + size.x + "x" + size.y + (overlaps.Count > 0 ? " (" + string.Join(", ", overlaps) + ")" : ""));
        }

        static Rect ScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4]; rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners.Min(c => c.x), corners.Min(c => c.y), corners.Max(c => c.x), corners.Max(c => c.y));
        }
        // A 1 px tolerance so buttons that merely touch are fine.
        static bool Overlap(Rect a, Rect b) => a.xMin < b.xMax - 1 && b.xMin < a.xMax - 1 && a.yMin < b.yMax - 1 && b.yMin < a.yMax - 1;
    }
}
#endif
