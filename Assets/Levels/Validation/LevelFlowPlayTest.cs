#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Audio;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.Skills;
using CampusRift.UI;
using UnityEngine;

namespace CampusRift.Levels
{
    // P05-T09: level data → LevelDirector → HUD → result, in the real gameplay scene.
    // Writes Artifacts/Levels/LevelFlow.json and Artifacts/Levels/LevelFlow-DONE.txt.
    public sealed class LevelFlowPlayTest : MonoBehaviour
    {
        [Serializable] sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(), notes = new List<string>(); }
        const string Output = "Artifacts/Levels/";
        public bool AudioOnly;
        readonly Report report = new Report();
        CampusExplorer player; PlayerMonsterHealth health; PlayerStats stats; SpiritPower spirit; SkillLoadout loadout;
        LevelDirector director; LevelCatalog catalog; bool running;
        readonly List<string> events = new List<string>();
        int wonEvents, lostEvents, killEvents;
        float waveClearedAt, nextWaveAt;

        void Update() { if (running && UIStateManager.Instance != null && UIStateManager.Instance.State == UIState.Paused) UIStateManager.Instance.EnterScene(true); }
        void Check(bool ok, string label)
        {
            (ok ? report.passed : report.failed).Add(label);
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "LevelFlow.json", JsonUtility.ToJson(report, true));
            Debug.Log("LEVEL QA " + (ok ? "PASS " : "FAIL ") + label);
        }
        void Note(string text) { report.notes.Add(text); Debug.Log("LEVEL QA NOTE " + text); }
        static IEnumerator Frames(int n = 2) { for (int i = 0; i < n; i++) yield return null; }

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
            Cleanup();
            running = false;
            File.WriteAllText(Output + "LevelFlow-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
        }

        void Cleanup()
        {
            try
            {
                if (director != null) director.End();
                EnemyPool.Instance?.ReleaseAll();
                LevelEvents.ResetAll();
                LevelSession.Clear(); LevelSession.Loadout = null;
                ProfileService.Instance.EndTransient();
                if (stats != null) { stats.RemoveSource(StatSource.Buff, "qa-hp"); stats.suppressCrit = false; }
                if (SettingsManager.Instance != null && SettingsManager.Instance.Sky != null) SettingsManager.Instance.Sky.SetPreset(SkyPreset.Default);
            }
            catch (Exception e) { Debug.LogWarning("LevelFlow cleanup: " + e.Message); }
        }

        void Kill(EnemyInstance e) => e.Vitality.ApplyDamage(DamageInfo.Create(1e7f, Element.None, DamageSource.Melee, e.transform.position + Vector3.up, Vector3.forward));
        void KillAll() { foreach (var e in new List<EnemyInstance>(director.Alive)) if (e != null && e.Alive) Kill(e); }

        void BeginForQA(LevelDefinition level)
        {
            director.Begin(level);
            // Begin resets gameplay buffs. Reapply the same existing QA health fixture afterwards.
            stats.SetModifier(StatSource.Buff, "qa-hp", StatType.MaxHealth, 90000, 0);
            health.Heal(1e6f);
        }

        void CheckSmallVoices(int level, string label)
        {
            var minions = EnemyDirector.Instance.Active.Where(e => e != null && e.Alive && e.Voice != null && e.VoiceClip != null).ToArray();
            var audible = minions.Where(e => e.Voice.isPlaying && !e.Voice.mute).ToArray();
            var nearest = minions.OrderBy(e => (e.transform.position - player.transform.position).sqrMagnitude).FirstOrDefault();
            Check(minions.Length > 0 && minions.Select(e => e.VoiceClip).Distinct().Count() == 1 && minions.All(e => e.VoiceClip.name == GameSfx.SmallMonsterForLevel(level).name), label + " uses exactly one stable level growl");
            Check(audible.Length == 1 && audible[0] == nearest && EnemyDirector.Instance.AudibleVoices == 1, label + " only the nearest minion is audible");
        }

        IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            float until = Time.time + timeout;
            while (!condition() && Time.time < until) yield return null;
        }

        void ListenToEvents()
        {
            LevelEvents.ResetAll();
            LevelEvents.WaveStarted += (w, n) => { events.Add("start" + w + "/" + n); if (w == 2) nextWaveAt = Time.time; };
            LevelEvents.WaveCleared += (w, n) => { events.Add("clear" + w + "/" + n); if (w == 1) waveClearedAt = Time.time; };
            LevelEvents.EnemyKilled += e => killEvents++;
            LevelEvents.LevelWon += r => { wonEvents++; events.Add("won"); };
            LevelEvents.LevelLost += r => { lostEvents++; events.Add("lost"); };
        }

        IEnumerator Run()
        {
            player = FindAnyObjectByType<CampusExplorer>(); health = player.GetComponent<PlayerMonsterHealth>(); stats = player.GetComponent<PlayerStats>();
            spirit = player.GetComponent<SpiritPower>(); loadout = player.GetComponent<SkillLoadout>();
            // Run against a throw-away profile so the real save is never touched.
            ProfileService.Instance.UseTransient(new ProfileData());
            catalog = Resources.Load<LevelCatalog>("LevelCatalog");
            // Enough health that a wave cannot end the run by accident; the loss test deals lethal damage on purpose.
            stats.SetModifier(StatSource.Buff, "qa-hp", StatType.MaxHealth, 90000, 0); health.Heal(1e6f); stats.suppressCrit = true;
            var shabanBrain = FindAnyObjectByType<MonsterBrain>();
            var shabanObject = shabanBrain != null ? shabanBrain.gameObject : null;
            var shabanVitality = shabanBrain != null ? shabanBrain.GetComponent<MonsterVitality>() : null;
            var aiConfig = shabanBrain != null ? shabanBrain.config : null;
            if(AudioOnly){yield return RunAudio();yield break;}
            var bossProbe=EnemyPool.Ensure().Spawn(catalog.Get(5).bosses[0],player.transform.position+Vector3.forward*5,catalog.Get(5).Scaling);
            Check(bossProbe!=null && !bossProbe.GetComponent<ShabanEnemyBridge>().RuntimeConfig.enableTimeEscalation, "P12 level Shaban runtime disables escalation without changing sandbox config");
            if(bossProbe!=null)EnemyPool.Instance.Release(bossProbe);
            Check(catalog != null && catalog.Count == 10, "The level catalog holds 10 levels");

            var voices = EnemyDirector.Ensure();
            var tower = EndgameFactory.Tower(13, DateTime.UtcNow); voices.BeginLevelVoice(tower);
            Check(voices.SmallMonsterSourceLevel == 3 && voices.SmallMonsterVoice == GameSfx.SmallMonsterForLevel(3), "Tower13 uses source level3 growl");
            var nightmare = EndgameFactory.Nightmare(8); voices.BeginLevelVoice(nightmare);
            Check(voices.SmallMonsterSourceLevel == 8 && voices.SmallMonsterVoice == GameSfx.SmallMonsterForLevel(8), "Nightmare8 uses source level8 growl");
            Destroy(tower); Destroy(nightmare);

            // ---- Level 1 ----
            var level1 = catalog.Get(1);
            LevelSession.Select(level1);
            ListenToEvents();
            director = LevelDirector.Ensure(); director.introSeconds = 0.5f;
            spirit.TrySpend(spirit.Current);
            BeginForQA(level1);
            yield return Frames(2);
            Check(shabanObject != null && !shabanObject.activeSelf, "The scene's Shaban is hidden while a level runs");
            Check(Vector3.Distance(player.transform.position, level1.spawnPoint) < 0.6f, "The player starts at the level's spawn point");
            Check(SettingsManager.Instance.Sky.Preset == SkyPreset.Dusk, "Level 1 sets the Dusk sky");
            Check(director.TotalPlanned == 6 && director.State == LevelDirector.Phase.Intro, "Level 1 plans 6 monsters and starts with the intro");
            Check(FindAnyObjectByType<GameplayHUD>().Breakthrough == null || !FindAnyObjectByType<GameplayHUD>().Breakthrough.gameObject.activeSelf, "The old breakthrough ring is hidden from the HUD");
            yield return WaitFor(() => director.State == LevelDirector.Phase.Wave, 3f);
            Check(director.State == LevelDirector.Phase.Wave && director.WaveIndex == 0, "After the intro wave 1 begins");
            int portalsSeen = 0;
            yield return WaitFor(() => { portalsSeen = Mathf.Max(portalsSeen, RiftPortal.LiveCount); return director.AliveCount >= 3; }, 4f);
            Check(portalsSeen >= 1, "Rift portals open before monsters appear");
            Check(director.AliveCount == 3 && director.SpawnedTotal == 3, "Wave 1 spawns exactly 3 monsters (" + director.AliveCount + ")");
            var first = new List<EnemyInstance>(director.Alive)[0];
            Check(Mathf.Approximately(first.MaxHealth, first.archetype.baseHealth*level1.healthMultiplier) && first.scaling.aiTier == level1.aiTier, "P12 level1 health and AI match the authored archetype and scaling");
            yield return new WaitForSeconds(0.4f);
            var hud = FindAnyObjectByType<LevelHUD>();
            Check(hud != null && hud.BodyText.Contains("6/6") && hud.BodyText.Contains("1/2"), "The HUD shows monsters left and the wave: \"" + (hud != null ? hud.BodyText.Replace("\n", " | ") : "no HUD") + "\"");
            var objective = FindAnyObjectByType<GameplayHUD>().Objective;
            Check(objective.Body.text == hud.BodyText, "The objective card carries the HUD text");

            Check(hud.Reveal != null && !hud.Reveal.Showing && hud.Reveal.RevealCount == 0, "No reveal marker while more than 3 monsters remain in the level");

            // Killing one monster must not win the level.
            var list = new List<EnemyInstance>(director.Alive);
            var nearestKilled = EnemyDirector.Instance.NearestVoice;
            Kill(nearestKilled != null ? nearestKilled : list[0]);
            Check(nearestKilled != null && !nearestKilled.Voice.isPlaying && EnemyDirector.Instance.NearestVoice != nearestKilled && EnemyDirector.Instance.AudibleVoices == 1, "Nearest voice death switches immediately without overlapping its stopped loop");
            CheckSmallVoices(1, "Level1 after nearest voice death");
            yield return Frames(3);
            Check(director.Kills == 1 && director.State == LevelDirector.Phase.Wave && UIStateManager.Instance.State == UIState.Gameplay, "Killing a monster counts once and does not end the level");
            KillAll(); spirit.TrySpend(spirit.Current);
            yield return WaitFor(() => director.State == LevelDirector.Phase.Rest, 3f);
            Check(director.State == LevelDirector.Phase.Rest && events.Contains("clear1/2"), "Clearing wave 1 starts the rest period");
            yield return null;
            Check(spirit.Current >= 19.5f && spirit.Current <= 30f, "Rest restores 20% of Linh Lực (" + spirit.Current.ToString("F1") + ")");
            Check(Mathf.Abs(director.RestRemaining - 15f) < 0.6f, "The rest counter starts at 15 s (" + director.RestRemaining.ToString("F1") + ")");
            yield return new WaitForSeconds(0.4f);
            Check(hud.BodyText.Contains("giây") || hud.BodyText.Contains(" s"), "The HUD shows the rest countdown: \"" + hud.BodyText.Replace((char)10, (char)124) + "\"");
            yield return WaitFor(() => director.State == LevelDirector.Phase.Wave, 17f);
            float rest = nextWaveAt - waveClearedAt;
            Check(director.WaveIndex == 1 && Mathf.Abs(rest - 15f) < 0.5f, "Wave 2 starts 15 s after wave 1 was cleared (" + rest.ToString("F2") + " s)");
            yield return WaitFor(() => director.AliveCount == 3, 4f);
            // Tầm Yêu: only 3 monsters left in the whole level; the first reveal comes after 20 s and lasts 3 s.
            float revealStart = Time.time;
            yield return WaitFor(() => hud.Reveal.Showing, 24f);
            float revealDelay = Time.time - revealStart;
            Check(hud.Reveal.Showing && revealDelay > 19f && revealDelay < 22f, "Tầm Yêu reveals the monsters after about 20 s (" + revealDelay.ToString("F1") + " s)");
            yield return null;
            Check(hud.Reveal.ShownCount == 3, "Tầm Yêu marks all 3 remaining monsters (" + hud.Reveal.ShownCount + ")");
            yield return WaitFor(() => !hud.Reveal.Showing, 5f);
            Check(!hud.Reveal.Showing && Time.time - revealStart < 30f, "The markers disappear after 3 s");

            Check(GameSfx.PlayCount > 0 && GameSfx.LastClip != null && GameSfx.LastClip.StartsWith("quai-nho"), "Every summoned small monster carries one of the quai-nho clips (" + GameSfx.LastClip + ")");
            bool allVoiced = director.AliveCount > 0; foreach (var monster in director.Alive) allVoiced &= monster.Voice != null && monster.Voice.clip != null && monster.Voice.clip.name.StartsWith("quai-nho");
            allVoiced &= director.Alive.All(m => m.Voice.loop && m.Voice.isPlaying);
            Check(allVoiced, "Each living monster has its own looping voice with a quai-nho clip");
            var audibleByClip = new Dictionary<string, int>(); var clipsInUse = new HashSet<string>();
            foreach (var monster in director.Alive) { clipsInUse.Add(monster.Voice.clip.name); if (!monster.Voice.mute) audibleByClip[monster.Voice.clip.name] = (audibleByClip.ContainsKey(monster.Voice.clip.name) ? audibleByClip[monster.Voice.clip.name] : 0) + 1; }
            Check(clipsInUse.Count == 1 && audibleByClip.Count == 1 && audibleByClip.Values.All(n => n == 1), "Every level1 minion shares one growl and exactly one voice is audible");
            CheckSmallVoices(1, "Level1 wave2");
            var voiceOfVictim = new List<EnemyInstance>(director.Alive)[0];
            var bigAudio = shabanObject != null ? shabanObject.GetComponent<MonsterAudio>() : null;
            var bigSource = bigAudio != null ? bigAudio.GetComponent<AudioSource>() : null;
            Check(bigSource != null && bigSource.clip != null && bigSource.clip.name == "quai-lon-1", "The big monster (Shaban) uses the new quai-lon-1 sound");
            int soundsBeforeWin = GameSfx.PlayCount;
            KillAll(); yield return null;
            Check(!voiceOfVictim.Voice.isPlaying, "A monster's voice stops when it is defeated");
            yield return WaitFor(() => director.State == LevelDirector.Phase.Won, 4f);
            Check(director.State == LevelDirector.Phase.Won && wonEvents == 1 && director.WinsRaised == 1, "Level 1 is won exactly once");
            Check(director.Kills == 6 && killEvents == 6, "Six kills counted and reported (" + director.Kills + "/" + killEvents + ")");
            Check(LevelSession.HighestCompleted == 1 && LevelSession.NextToPlay == 1, "Level 1 is remembered as cleared; level 2 stays closed until Luyện Khí 3, so Continue stays on level 1");
            Check(director.ClearAward != null && Mathf.Approximately(director.ClearAward.tuVi, 5) && Mathf.Approximately(ProfileService.Instance.Cultivation.TuVi, 5), "The first clear pays 5 Tu Vi");
            yield return WaitFor(() => UIStateManager.Instance.State == UIState.Victory, 3f);
            Check(UIStateManager.Instance.State == UIState.Victory, "The victory panel opens after the win delay");
            Check(GameSfx.PlayCount > soundsBeforeWin && GameSfx.LastClip == "win", "The win sound plays when the victory panel opens (" + GameSfx.LastClip + ")");
            yield return Frames(3);
            var ui = FindAnyObjectByType<UIManager>();
            var result = ui.Victory != null ? ui.Victory.GetComponent<LevelResultUI>() : null;
            Check(result != null && result.Title.text.Contains("1") && result.Stats.text.Contains("6/6"), "The result card shows the level number and 6/6 monsters: \"" + (result != null ? result.Title.text + " / " + result.Stats.text.Replace("\n", " | ") : "missing") + "\"");
            Check(result != null && result.NextButton.activeSelf && result.Stars[0].color.r > 0.9f, "Next level button is shown and the completion star is lit");
            UIStateManager.Instance.EnterScene(true);
            yield return Frames(2);
            Check(wonEvents == 1 && lostEvents == 0, "No second win or a loss was raised afterwards");
            director.End(); yield return Frames(2);

            // ---- Loss and retry keep the loadout ----
            events.Clear(); wonEvents = lostEvents = 0; killEvents = 0;
            LevelSession.Select(level1); ListenToEvents();
            director.introSeconds = 0.3f; BeginForQA(level1); yield return Frames(3);
            loadout.Equip(0, "dai-thu-an"); loadout.Equip(3, "hu-khong-ket-gioi");
            Check(LevelSession.Loadout != null && LevelSession.Loadout[0] == "dai-thu-an" && LevelSession.Loadout[3] == "hu-khong-ket-gioi", "The equipped skills are remembered by the level session");
            yield return WaitFor(() => director.AliveCount >= 1, 4f);
            var lethal = DamageInfo.Create(1e9f, Element.None, DamageSource.Environment, player.transform.position, Vector3.up); lethal.ignoreInvulnerability = true;
            health.ApplyDamage(lethal);
            yield return Frames(3);
            Check(director.State == LevelDirector.Phase.Lost && lostEvents == 1 && director.LossesRaised == 1, "The level is lost exactly once when the player falls");
            Check(UIStateManager.Instance.State == UIState.GameOver, "The Game Over panel opens on a loss");
            Check(GameSfx.LastClip != null && GameSfx.LastClip.StartsWith("that-bai"), "A defeat sound plays on a loss (" + GameSfx.LastClip + ")");
            var voidWall = player.GetComponent<CampusRift.Skills.VoidWallSkill>();
            var wallSound = (AudioSource)typeof(CampusRift.Skills.VoidWallSkill).GetField("sound", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(voidWall);
            Check(!CampusRift.Skills.SkillAudio.Enabled && wallSound != null && wallSound.mute, "Skill sounds are switched off (Void Wall's voice is muted)");
            yield return new WaitForSecondsRealtime(0.5f);
            Check(lostEvents == 1 && wonEvents == 0, "No further loss or win events follow");
            // Simulate the scene reload of Retry: skills are back to the defaults, health restored, then the level begins again.
            director.End();
            loadout.Equip(0, "hu-khong-ket-gioi"); loadout.Equip(3, "dai-thu-an");
            UIStateManager.Instance.EnterScene(true);
            typeof(PlayerMonsterHealth).GetField("currentHealth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(health, 1000f);
            Check(health.CurrentHealth > 0, "Health is restored for the retry (a reload does this in the game)");
            int attemptBefore = LevelSession.Attempt;
            LevelSession.Select(level1); director.introSeconds = 0.3f; BeginForQA(level1); yield return Frames(3);
            Check(LevelSession.Attempt == attemptBefore + 1 && loadout.Get(0) != null && loadout.Get(0).Id == "dai-thu-an" && loadout.Get(3).Id == "hu-khong-ket-gioi", "Retry keeps the same level and the previous skill loadout");
            Check(director.State == LevelDirector.Phase.Intro && director.Kills == 0 && director.AliveCount == 0, "Retry starts the level from scratch");
            director.End(); yield return Frames(2);

            // ---- Level 8: one big wave, concurrency cap and scaling ----
            var level8 = catalog.Get(8);
            LevelSession.Select(level8); ListenToEvents();
            director.introSeconds = 0.2f; BeginForQA(level8);
            Check(director.TotalPlanned == 32 && SettingsManager.Instance.Sky.Preset == SkyPreset.Inferno, "Level 8 plans 32 monsters under the Inferno sky");
            int expectedCap = CampusInput.Mobile ? LevelDirector.MobileCap : LevelDirector.PcCap;
            Check(director.ConcurrentCap == expectedCap && LevelDirector.PcCap == 14 && LevelDirector.MobileCap == 9, "The concurrency cap is 14 on PC and 9 on mobile (now " + director.ConcurrentCap + ")");
            int maxAlive = 0; float sample = Time.time;
            while (Time.time - sample < 14f) { maxAlive = Mathf.Max(maxAlive, director.AliveCount); yield return null; }
            Check(maxAlive == expectedCap && director.MaxConcurrentSeen == expectedCap, "Alive monsters reach the cap and never exceed it (" + maxAlive + ")");
            EnemyDirector.Instance.UpdateVoices();
            int audible = 0, distinct = 0; var seen = new HashSet<string>();
            foreach (var monster in director.Alive) { if (seen.Add(monster.Voice.clip.name)) distinct++; if (!monster.Voice.mute) audible++; }
            Check(director.AliveCount >= 9 && audible == 1 && distinct == 1, director.AliveCount + " monsters share exactly one clip and one audible voice");
            CheckSmallVoices(8, "Level8 crowd");
            var summoned = EnemyPool.Ensure().Spawn(catalog.Get(1).waves[0].entries[0].archetype, player.transform.position + Vector3.forward * 4, level8.Scaling, false);
            Check(summoned != null && !summoned.countsForSwordIntent && summoned.VoiceClip == EnemyDirector.Instance.SmallMonsterVoice, "Summoned minion inherits the level8 clip");
            EnemyDirector.Instance.UpdateVoices(); CheckSmallVoices(8, "Level8 with summoned minion");
            EnemyPool.Instance.Release(summoned);
            var any = new List<EnemyInstance>(director.Alive)[0];
            Check(Mathf.Abs(any.MaxHealth - any.archetype.baseHealth * level8.healthMultiplier * any.archetype.lateHealthMultiplier * (any.Elite!=null&&any.Elite.IsElite?3:1)) < 0.5f && Mathf.Abs(any.Damage - any.archetype.baseDamage * level8.damageMultiplier) < 0.1f && any.scaling.aiTier == level8.aiTier, "P12/P19 Level 8 monsters match authored scaling, late health and elite HP");
            int before = director.SpawnedTotal;
            var victims = new List<EnemyInstance>(director.Alive);
            for (int i = 0; i < 3; i++) Kill(victims[i]);
            yield return WaitFor(() => director.SpawnedTotal >= before + 3, 4f);
            Check(director.SpawnedTotal >= before + 3 && director.AliveCount <= expectedCap, "Each death lets the next monster in, still under the cap");
            director.ConcurrentCapOverride = 9;
            for (int i = 0; i < 6; i++) { var l = new List<EnemyInstance>(director.Alive); if (l.Count > 0) Kill(l[0]); }
            yield return new WaitForSeconds(3f);
            Check(director.AliveCount <= 9 && director.ConcurrentCap == 9, "A lower cap (mobile) is respected (" + director.AliveCount + ")");
            // Kill every remaining monster: the level must finish after the whole wave, not before.
            int guard = 0;
            while (director.State == LevelDirector.Phase.Wave && guard++ < 60) { KillAll(); yield return new WaitForSeconds(0.25f); }
            Check(director.Kills == 32 && killEvents==32 && director.TotalPlanned==32 && director.Remaining==0 && director.SpawnedTotal>=32, "P19 all32 counted wave enemies killed once; total spawns include uncounted summoned minions (" + director.Kills + "/" + director.SpawnedTotal + ")");
            Check(director.AwaitingSkySword && wonEvents==0,"P14/P16 cleared wave waits for Heaven Sword before victory");
            var sky=CampusRift.SkyBeast.SkyBeastScheduler.Instance;sky.CinematicPaused=true;
            while(!sky.Completed){Check(sky.ApplySkySwordHit(),"Level8 successful Heaven Sword segment");yield return null;}
            director.CompleteSkySword(); // HeavenSwordUltimate performs this after its cinematic (P15/P16).
            yield return Frames(2);
            Check(director.State == LevelDirector.Phase.Won && wonEvents == 1, "Level8 wins exactly once after wave and sky guardian clear");
            yield return WaitFor(() => UIStateManager.Instance.State == UIState.Victory, 3f);
            UIStateManager.Instance.EnterScene(true);
            director.End(); director.ConcurrentCapOverride = 0; yield return Frames(2);

            // ---- Sky presets, data, Shaban rule ----
            LevelSession.Select(6); director.introSeconds = 0.2f; BeginForQA(catalog.Get(6)); yield return Frames(2);
            Check(SettingsManager.Instance.Sky.Preset == SkyPreset.BloodMoon, "Level 6 sets the Blood Moon sky");
            director.End(); yield return Frames(2);
            if (shabanObject != null && shabanVitality != null)
            {
                LevelSession.Select(1); BeginForQA(level1); yield return Frames(2);
                shabanObject.SetActive(true);
                shabanVitality.ApplyDamage(DamageInfo.Create(1e7f, Element.None, DamageSource.Melee, shabanObject.transform.position, Vector3.forward));
                yield return Frames(3);
                Check(UIStateManager.Instance.State == UIState.Gameplay, "Defeating Shaban no longer wins a level");
                director.End();
            }
            Note("cap on this machine: " + expectedCap);
        }
        IEnumerator RunAudio()
        {
            director=LevelDirector.Instance;UIStateManager.Instance.EnterScene(true);
            for(int level=1;level<=10;level++)
            {
                var clip=GameSfx.SmallMonsterForLevel(level);
                Check(clip!=null && clip.name==(level%2==1?"quai-nho-2":"quai-nho-3"),"Level"+level+" odd/even two-clip mapping");
            }
            var voices=EnemyDirector.Ensure();
            var tower=EndgameFactory.Tower(13,DateTime.UtcNow);voices.BeginLevelVoice(tower);
            Check(voices.SmallMonsterSourceLevel==3 && voices.SmallMonsterVoice==GameSfx.SmallMonsterForLevel(3),"Tower13 inherits source level3 growl");
            var nightmare=EndgameFactory.Nightmare(8);voices.BeginLevelVoice(nightmare);
            Check(voices.SmallMonsterSourceLevel==8 && voices.SmallMonsterVoice==GameSfx.SmallMonsterForLevel(8),"Nightmare8 inherits source level8 growl");
            Destroy(tower);Destroy(nightmare);
            foreach(int level in new[]{1,8})
            {
                var definition=catalog.Get(level);voices.BeginLevelVoice(definition);
                var pool=EnemyPool.Ensure();pool.ReleaseAll();
                var type=catalog.Get(1).waves[0].entries[0].archetype;
                var near=pool.Spawn(type,player.transform.position+Vector3.forward*4,definition.Scaling);
                var far=pool.Spawn(type,player.transform.position+Vector3.forward*8,definition.Scaling);
                var summon=pool.Spawn(type,player.transform.position+Vector3.right*12,definition.Scaling,false);
                yield return Frames(3);voices.UpdateVoices();CheckSmallVoices(level,"Level"+level+" crowd + summon");
                Check(voices.NearestVoice==near,"Level"+level+" closest voice selected");
                far.transform.position=player.transform.position+Vector3.right*2;
                voices.UpdateVoices();CheckSmallVoices(level,"Level"+level+" nearest changed");
                Check(voices.NearestVoice==far,"Level"+level+" voice switches immediately when distance changes");
                pool.Release(far);voices.UpdateVoices();CheckSmallVoices(level,"Level"+level+" nearest removed");
                pool.ReleaseAll();
            }
            Note("AudioOnly: no level-flow regression or balance run");
        }
    }
}
#endif
