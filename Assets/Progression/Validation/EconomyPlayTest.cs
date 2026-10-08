#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Learning;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.UI;

namespace CampusRift.Progression
{
    // P08: Linh Thạch rules, anti-farming, shop, inventory, items, buffs, Hộ Mệnh Phù and artifacts. Runs in the gameplay scene on a
    // throw-away profile with a fake study clock, so neither the player's save nor real time matters.
    // Output: Artifacts/Economy/Economy.json and Artifacts/Economy/Economy-DONE.txt.
    public sealed class EconomyPlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(); }
        readonly Report report = new Report();
        const string Output = "Artifacts/Economy/";
        static readonly DateTime T0 = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
        float studyTime;      // seconds, drives QuizSession answer timing
        readonly Dictionary<LessonData, int> originalQuizSize = new Dictionary<LessonData, int>();
        FakeLearningClock clock;
        ProfileService profile;
        LearningEngine engine;
        LearningCatalog catalog;
        void Check(bool ok, string label) { (ok ? report.passed : report.failed).Add(label); Directory.CreateDirectory(Output); File.WriteAllText(Output + "Economy.json", JsonUtility.ToJson(report, true)); Debug.Log("ECONOMY QA " + (ok ? "PASS " : "FAIL ") + label); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Output); Application.runInBackground = true;
            profile = ProfileService.Instance; profile.UseTransient(new ProfileData());
            var run = Run();
            while (true)
            {
                bool next = false; object current = null;
                try { next = run.MoveNext(); if (next) current = run.Current; }
                catch (Exception e) { Check(false, e.ToString()); break; }
                if (!next) break; yield return current;
            }
            foreach (var pair in originalQuizSize) pair.Key.quizSize = pair.Value;
            try { profile.EndTransient(); } catch { }
            File.WriteAllText(Output + "Economy-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
            Destroy(gameObject);
        }

        // Answers a quiz, `seconds` apart; all right or with `wrong` mistakes first.
        void Play(QuizSession s, float seconds, int wrong = 0)
        {
            s.TimeSource = () => studyTime; s.ResetTimer(); int i = 0;
            while (!s.Complete)
            {
                studyTime += seconds; var q = s.Questions[s.Answered].Data;
                if (i++ < wrong)
                {
                    if (q.type == "ordering") { var r = new List<string>(q.correctOptionIds); r.Reverse(); s.Answer(r); }
                    else if(q.type=="matching") s.Answer(q.correctOptionIds.Select(p=>p.Split(':')[0]+":"+(p.EndsWith(":1")?"2":"1")).ToArray());
                    else s.Answer(q.options.First(o => !q.correctOptionIds.Contains(o.id)).id);
                }
                else s.Answer(q.correctOptionIds);
            }
        }
        void Read(CourseData c, LessonData l) { for (int p = 0; p < l.pages.Count; p++) engine.ReadPage(c, l, p); }
        int Balance => profile.Wallet.Balance;

        IEnumerator Run()
        {
            catalog = Resources.Load<LearningCatalog>("LearningCatalog");
            var course = catalog.courses[0]; var l1 = course.lessons[0]; var l2 = course.lessons[1];
            clock = new FakeLearningClock(T0);
            var cult = profile.Cultivation;
            engine = new LearningEngine(catalog, new ProfileLearningStore(profile), 5, cult, clock) { Economy = new StudyEconomy(profile.Wallet) };
            // Every quiz asks the whole bank, so the amounts below are exact whichever questions a random draw would pick.
            foreach (var lesson in course.lessons) { originalQuizSize[lesson] = lesson.quizSize; lesson.quizSize = lesson.questionBank.questions.Count(q => q.lessonId == lesson.id); }
            int n1 = l1.quizSize, n2 = l2.quizSize;
            var config = EconomyConfig.Instance;
            Check(config.firstCorrect == 5 && config.retryCorrect == 2 && config.lessonPassed == 30 && config.lessonPerfect == 50 && config.reviewOnTime == 40 && config.examPassed == 300
                && config.levelClearPerIndex == 40 && config.perStar == 20 && config.retryDailyCap == 300, "The reward numbers of plan §9.1 are in EconomyConfig");

            // ---- wallet ----
            Check(Balance == 0, "A new profile has no Linh Thạch");
            Check(!profile.Wallet.TrySpend(1) && Balance == 0, "Spending more than the balance fails and changes nothing");
            profile.Wallet.Earn(10, "test"); Check(Balance == 10 && profile.Wallet.TrySpend(4) && Balance == 6, "Earn and spend keep the balance");
            profile.Wallet.TrySpend(6); Check(Balance == 0 && profile.Data.wallet.lifetimeEarned == 10 && profile.Data.wallet.lifetimeSpent == 10, "Lifetime totals are kept");

            // ---- lesson quiz: 10 first-time answers + full-marks lesson ----
            Read(course, l1);
            var quiz = engine.StartQuiz(course, l1); Play(quiz, 4f); engine.Submit(quiz);
            var pay = engine.LastLinhThach;
            Check(pay.firstCorrect == 5 * n1 && pay.lesson == 50 && Balance == 5 * n1 + 80, "Daily read +30; a perfect first quiz pays 5 per answer and 50 for the lesson (" + Balance + " for " + n1 + " questions)");
            quiz = engine.StartQuiz(course, l1); Play(quiz, 4f); engine.Submit(quiz);
            Check(Balance == 5 * n1 + 110 && engine.LastLinhThach.total == 0, "Daily20 correct +30 once; immediate repeat quiz pays nothing");
            clock.Advance(TimeSpan.FromHours(25));
            quiz = engine.StartQuiz(course, l1); Play(quiz, 4f); engine.Submit(quiz);
            Check(engine.LastLinhThach.retryCorrect == 2 * n1 && Balance == 7 * n1 + 110, "After 24 hours each correct answer pays 2 again (" + engine.LastLinhThach.retryCorrect + ")");
            clock.Advance(TimeSpan.FromHours(25));
            // Daily cap of the repeat income: 300.
            engine.Progress.linhDate = engine.Now.ToString("yyyy-MM-dd"); engine.Progress.linhRetryToday = 296;
            quiz = engine.StartQuiz(course, l1); Play(quiz, 4f); engine.Submit(quiz);
            Check(engine.LastLinhThach.retryCorrect == 4 && engine.LastLinhThach.retryCapReached, "Repeat rewards stop at 300 Linh Thạch a day (" + engine.LastLinhThach.retryCorrect + ")");
            clock.Advance(TimeSpan.FromHours(25));
            quiz = engine.StartQuiz(course, l1); Play(quiz, 4f); engine.Submit(quiz);
            Check(engine.LastLinhThach.retryCorrect == 2 * n1 && engine.Progress.linhRetryToday == 2 * n1, "The next UTC day starts with an empty counter");
            // A better score only pays the difference: lesson 2 first passes with 80%, then reaches 100%.
            Read(course, l2); quiz = engine.StartQuiz(course, l2); Play(quiz, 4f, 2); engine.Submit(quiz);
            Check(engine.LastLinhThach.lesson == 30 && engine.LastLinhThach.firstCorrect == 5 * (n2 - 2), "Lesson done at 80%+ pays 30 (+ 5 per first correct answer)");
            clock.Advance(TimeSpan.FromHours(25));
            quiz = engine.StartQuiz(course, l2); Play(quiz, 4f); engine.Submit(quiz);
            Check(engine.LastLinhThach.lesson == 20, "Reaching 100% later pays the 20 that are missing");

            // ---- too-fast answering ----
            var l3 = course.lessons[2]; Read(course, l3);
            quiz = engine.StartQuiz(course, l3); Play(quiz, 0.5f); engine.Submit(quiz);
            var fast = engine.LastLinhThach;
            Check(fast.paused && !string.IsNullOrEmpty(fast.notice) && fast.firstCorrect == 4 * 5, "Five answers in a row under 1.5 s pause the rewards (paid " + fast.firstCorrect + ")");
            Check(fast.notice == "Đọc kỹ câu hỏi", "The reminder reads \"Đọc kỹ câu hỏi\"");
            clock.Advance(TimeSpan.FromSeconds(30));
            var l4 = course.lessons[3]; Read(course, l4);
            quiz = engine.StartQuiz(course, l4); Play(quiz, 4f); engine.Submit(quiz);
            Check(engine.LastLinhThach.firstCorrect == 0 && engine.LastLinhThach.paused, "Answers during the two minutes of pause pay nothing");
            clock.Advance(TimeSpan.FromMinutes(3));
            quiz = engine.StartQuiz(course, l3); Play(quiz, 4f); engine.Submit(quiz);
            Check(engine.LastLinhThach.firstCorrect > 0 && !engine.LastLinhThach.paused, "After the pause the rewards return (" + engine.LastLinhThach.firstCorrect + ")");

            // ---- spaced review and exam ----
            clock.Advance(TimeSpan.FromDays(2));
            var review = engine.StartReview(course, l1); Play(review, 4f); engine.SubmitReview(review);
            Check(engine.LastLinhThach.review == 40, "An on-time review pays 40");
            cult.SetState(Realm.LuyenKhi, 5, 100);
            var exam = engine.StartExam(course, () => studyTime); exam.Session.TimeSource = () => studyTime; exam.Session.ResetTimer();
            while (!exam.Session.Complete) { studyTime += 4; exam.Session.Answer(exam.Session.Questions[exam.Session.Answered].Data.correctOptionIds); }
            long beforeExam = Balance; var outcome = engine.SubmitExam(exam);
            Check(outcome.passed && engine.LastLinhThach.exam == 300 && Balance >= beforeExam + 300, "The breakthrough exam pays 300 Linh Thạch through the wallet");

            // ---- levels ----
            var levels = profile.Levels; var level1 = LevelCatalog.Instance.Get(1); var level3 = LevelCatalog.Instance.Get(3);
            long b0 = Balance;
            levels.RecordClear(level1, 100f, 0, out _);
            Check(levels.LastLinhThach == 60 && Balance == b0 + 60, "First clear of level 1: 40 × 1 plus 20 for the completion star (" + levels.LastLinhThach + ")");
            levels.RecordClear(level1, 90f, 0, out _); Check(levels.LastLinhThach == 0, "Clearing it again pays nothing");
            levels.RecordClear(level1, 80f, LevelProgressService.StarTime, out _); Check(levels.LastLinhThach == 20, "A star reached for the first time pays 20");
            levels.RecordClear(level3, 100f, LevelProgressService.StarTime | LevelProgressService.StarChallenge, out _);
            Check(levels.LastLinhThach == 120 + 60, "Level 3 first clear with three stars: 40 × 3 + 3 × 20");

            // ---- items catalog ----
            var items = ItemCatalog.Instance;
            Check(items.items.Count == 16 && items.artifacts.Count == 5, "Sixteen items and five artifacts are installed");
            (string id, int price, int max, Realm from)[] table =
            {
                ("hoi-khi-dan", 25, 5, Realm.LuyenKhi), ("hoi-xuan-dan", 60, 3, Realm.TrucCo), ("tu-linh-dan", 40, 3, Realm.LuyenKhi), ("ho-menh-phu", 200, 1, Realm.KetDan),
                ("cuong-luc-dan", 80, 2, Realm.TrucCo), ("kim-cuong-phu", 80, 2, Realm.TrucCo), ("than-hanh-phu", 50, 2, Realm.LuyenKhi),
                ("ti-hoa-chau", 120, 2, Realm.HoaThan), ("bang-tam-phu", 90, 2, Realm.HoaThan), ("kiem-tam-dan", 150, 1, Realm.HoaThan)
            };
            bool tableOk = true; foreach (var row in table) { var i = items.Item(row.id); if (i == null || i.price != row.price || i.maxPerLevel != row.max || i.availableFrom != row.from) { tableOk = false; Debug.Log("Mismatch " + row.id); } }
            Check(tableOk, "Prices, per-level limits and realms match plan §9.3");
            Check(items.items.All(i => !string.IsNullOrEmpty(i.nameEN) && !string.IsNullOrEmpty(i.nameVN) && !string.IsNullOrEmpty(i.descriptionEN) && !string.IsNullOrEmpty(i.descriptionVN)), "Every item has English and Vietnamese texts");
            Check(items.items.All(i => ItemIcons.Get(i) != null), "Every item has an icon");

            // ---- shop ----
            cult.SetState(Realm.LuyenKhi, 1, 0);
            var shop = new ShopService(profile);
            var qi = items.Item("hoi-khi-dan"); var spring = items.Item("hoi-xuan-dan");
            Check(shop.Buy(spring, 1) == PurchaseResult.Locked && !shop.IsUnlocked(spring) && !string.IsNullOrEmpty(shop.LockReason(spring, true)), "An item of a higher realm is locked with a reason: " + shop.LockReason(spring, true));
            profile.Wallet.TrySpend(Balance); profile.Wallet.Earn(60, "test");
            Check(shop.Buy(qi, 3) == PurchaseResult.NotEnough && shop.Missing(qi, 3) == 15 && profile.Inventory.Count(qi.id) == 0, "Not enough Linh Thạch: nothing is bought, 15 are missing");
            Check(shop.Buy(qi, 2) == PurchaseResult.Ok && Balance == 10 && profile.Inventory.Count(qi.id) == 2, "Buying 2 Hồi Khí Đan costs exactly 50");
            profile.Wallet.Earn(500, "test");
            cult.SetState(Realm.KetDan, 1, 0);
            foreach (var pair in new[] { ("hoi-xuan-dan", 3), ("tu-linh-dan", 3), ("ho-menh-phu", 1), ("cuong-luc-dan", 2) }) shop.Buy(items.Item(pair.Item1), pair.Item2);
            var inv = profile.Inventory;
            Check(inv.Count("hoi-xuan-dan") == 3 && inv.Count("ho-menh-phu") == 1, "Purchases land in the inventory");
            var saved = JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(profile.Data));
            Check(saved.inventory.Count == profile.Data.inventory.Count && saved.wallet.linhThach == Balance, "Inventory and wallet survive a save round-trip");

            // ---- carrying ----
            Check(inv.SlotCount == 3, "Three item slots to start with");
            Check(inv.SetCarry(qi, 9) == 2, "Cannot carry more than owned");
            Check(inv.SetCarry(items.Item("hoi-xuan-dan"), 9) == 3, "Cannot carry more than the per-level limit");
            Check(inv.SetCarry(items.Item("ho-menh-phu"), 1) == 1, "A third kind fits the third slot");
            Check(inv.SetCarry(items.Item("tu-linh-dan"), 1) == 0 && inv.CarriedKinds == 3, "A fourth kind is refused: no free slot");
            inv.SetCarry(qi, 0); inv.SetCarry(items.Item("tu-linh-dan"), 2);

            // ---- artifacts ----
            var artifacts = profile.Artifacts; var kim = items.Artifact("phi-kiem"); var pouch = items.Artifact("tui-can-khon");
            cult.SetState(Realm.LuyenKhi, 1, 0); profile.Wallet.TrySpend(Balance); profile.Wallet.Earn(5000, "test");
            Check(artifacts.TryUpgrade(kim) && artifacts.Level(kim.id) == 1 && Balance == 4700, "Phi Kiếm level 1 costs 300");
            Check(!artifacts.TryUpgrade(kim) && artifacts.Level(kim.id) == 1, "Level 2 is refused at Luyện Khí: the level cannot exceed the realms reached");
            cult.SetState(Realm.TrucCo, 1, 0);
            Check(artifacts.TryUpgrade(kim) && Balance == 4100, "At Trúc Cơ level 2 costs 600");
            Check(artifacts.TryUpgrade(pouch) && artifacts.ItemSlots() == 4 && inv.SlotCount == 4, "Túi Càn Khôn adds an item slot");
            Check(pouch.MaxLevel == 2, "Túi Càn Khôn has two levels");

            // ---- the player ----
            var player = FindAnyObjectByType<CampusExplorer>(); var stats = player.GetComponent<PlayerStats>(); var health = player.GetComponent<PlayerMonsterHealth>();
            var pi = player.GetComponent<PlayerItems>(); var buffs = player.GetComponent<BuffSystem>(); var spirit = player.GetComponent<SpiritPower>();
            Check(pi != null && buffs != null, "The player has PlayerItems and BuffSystem");
            buffs.enabled = false; float now = 0; pi.Clock = () => now;
            cult.SetState(Realm.LuyenKhi, 1, 0); yield return null;
            float hpBase = stats.MaxHealth, atkBase = stats.Attack;
            // Artifacts change the stats: Phi Kiếm level 2 = +12%.
            Check(Mathf.Abs(stats.Attack / 20f - 1.12f) < 0.001f, "Phi Kiếm level 2 raises attack by 12% (" + stats.Attack + ")");
            profile.Wallet.Earn(1000, "test");
            cult.SetState(Realm.TrucCo, 1, 0); yield return null;
            var mirror = items.Artifact("ho-tam-kinh"); artifacts.TryUpgrade(mirror); yield return null;
            Check(Mathf.Abs(stats.MaxHealth - 170 * 1.05f) < 0.01f, "Hộ Tâm Kính level 1 raises max health by 5% (" + stats.MaxHealth + ")");
            Check(stats.Modifiers.Count(m => m.source == StatSource.Artifact && m.stat == StatType.MaxHealth) == 1, "Artifact modifiers are keyed, not stacked");

            // Start a level with a full bag.
            inv.ClearCarry(); inv.Add("hoi-khi-dan", 3); inv.Add("kim-cuong-phu", 2); inv.Add("cuong-luc-dan", 0);
            cult.SetState(Realm.HoaThan, 1, 0); yield return null;
            foreach (var id in new[] { "ti-hoa-chau", "bang-tam-phu", "kiem-tam-dan", "than-hanh-phu" }) inv.Add(id, 2);
            inv.SetCarry(items.Item("hoi-khi-dan"), 3); inv.SetCarry(items.Item("hoi-xuan-dan"), 3); inv.SetCarry(items.Item("ho-menh-phu"), 1); inv.SetCarry(items.Item("kim-cuong-phu"), 2);
            pi.BeginLevel(); yield return null;
            Check(pi.Bag.Slots.Count == 4, "The bag holds the carried items (4 slots with the pouch)");
            var bar = FindAnyObjectByType<ItemBarUI>(FindObjectsInactive.Include);
            Check(bar != null, "The HUD has an item bar");
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P08-hud.png"); yield return new WaitForSecondsRealtime(.3f);

            void Hurt(float fraction) { var info = DamageInfo.Create(health.maxHealth * fraction, Element.None, DamageSource.Environment, player.transform.position, Vector3.zero); health.ApplyDamage(info); }
            health.Revive(.5f, 0); float hp0 = health.CurrentHealth;
            int slotQi = 0, slotSpring = 1, slotAmulet = 2, slotDiamond = 3;
            var result = pi.Use(slotQi);
            Check(result == ItemUseResult.Used && Mathf.Abs(health.CurrentHealth - (hp0 + health.maxHealth * .2f)) < 0.5f, "Hồi Khí Đan heals 20% of max health at once");
            Check(pi.Bag.Slots[slotQi].remaining == 2 && inv.Count("hoi-khi-dan") == 4, "Using an item takes one from the bag and the inventory");
            Check(pi.Use(slotQi) == ItemUseResult.Cooldown, "A second item within one second is refused (shared cooldown)");
            now += 1.1f;
            health.Revive(.3f, 0); hp0 = health.CurrentHealth;
            result = pi.Use(slotSpring); float hp1 = health.CurrentHealth;
            buffs.Tick(1.5f); float hp2 = health.CurrentHealth; buffs.Tick(1.6f);
            Check(result == ItemUseResult.Used && Mathf.Abs(health.CurrentHealth - (hp0 + health.maxHealth * .45f)) < 0.5f && hp1 <= hp0 + 0.01f && hp2 > hp1, "Hồi Xuân Đan heals 45% over 3 seconds, not at once");
            now += 1.1f; health.Revive(1f, 0); Check(pi.Use(slotSpring) == ItemUseResult.NothingToDo && pi.Bag.Slots[slotSpring].remaining == 2, "A heal at full health is refused, the item is not wasted");

            // Buffs.
            now += 1.1f;
            Check(pi.Use(slotDiamond) == ItemUseResult.Used && Mathf.Abs(stats.DamageTaken - 0.65f) < 0.001f, "Kim Cương Phù: 35% less damage taken");
            buffs.Apply(items.Item("kim-cuong-phu")); Check(Mathf.Abs(stats.DamageTaken - 0.65f) < 0.001f && buffs.Active.Count == 1, "Using it again refreshes the timer and does not stack");
            buffs.Tick(20f); float left = buffs.Remaining("kim-cuong-phu"); buffs.Apply(items.Item("kim-cuong-phu"));
            Check(Mathf.Abs(buffs.Remaining("kim-cuong-phu") - 45f) < 0.01f && left < 30f, "Refreshing restores the full 45 seconds");
            buffs.Tick(46f); Check(Mathf.Abs(stats.DamageTaken - 1f) < 0.001f && buffs.Active.Count == 0, "When the buff ends the stat returns exactly");
            buffs.Apply(items.Item("cuong-luc-dan")); Check(Mathf.Abs(stats.DamageDealt - 1.3f) < 0.001f, "Cuồng Lực Đan: +30% damage");
            buffs.Tick(61f); Check(Mathf.Abs(stats.DamageDealt - 1f) < 0.001f, "…for 60 seconds");
            float speedBefore = stats.MoveSpeedBonus; buffs.Apply(items.Item("than-hanh-phu"));
            Check(Mathf.Abs(stats.MoveSpeedBonus - speedBefore - 0.3f) < 0.001f, "Thần Hành Phù: +30% run speed"); buffs.Tick(41f);
            Check(Mathf.Abs(stats.MoveSpeedBonus - speedBefore) < 0.001f, "…for 40 seconds");
            buffs.Apply(items.Item("ti-hoa-chau")); Check(Mathf.Abs(stats.FireResistance - 0.5f) < 0.001f, "Tị Hỏa Châu: 50% fire resistance");
            buffs.Apply(items.Item("bang-tam-phu")); Check(Mathf.Abs(stats.FireResistance - 0.75f) < 0.001f && buffs.EmberImmune, "Băng Tâm Phù adds 25% and immunity to embers");
            stats.SetModifier(StatSource.Skill, "test", StatType.FireResistance, 0.3f, 0);
            Check(stats.FireResistance <= 0.8f + 0.0001f && Mathf.Abs(stats.FireResistance - 0.8f) < 0.001f, "Fire resistance never exceeds 80%");
            stats.SetModifier(StatSource.Skill, "test", StatType.DamageTaken, -2f, 0); Check(stats.DamageTaken >= 0.2f - 0.0001f, "Damage taken never falls below −80%");
            stats.RemoveSource(StatSource.Skill, "test"); buffs.ClearAll();
            buffs.Apply(items.Item("kiem-tam-dan"));
            Check(Mathf.Abs(buffs.SwordChannelSpeed - 0.4f) < 0.001f && buffs.ConsumeUninterrupted() && !buffs.ConsumeUninterrupted(), "Kiếm Tâm Đan: 40% faster channelling and one uninterrupted cast");
            buffs.ClearAll();

            // Spirit potion.
            var spiritItem = items.Item("tu-linh-dan"); inv.Add("tu-linh-dan", 0);
            for (int guard = 0; guard < 20 && spirit.Current > 0.5f; guard++) spirit.TrySpend(Mathf.Min(spirit.Current, 50f));
            var effectHost = items.Item("tu-linh-dan"); float max = spirit.Max;
            pi.Bag.Slots.Add(new LevelBag.Slot { item = effectHost, remaining = 1 }); now += 1.1f;
            Check(pi.Use(pi.Bag.Slots.Count - 1) == ItemUseResult.Used && Mathf.Abs(spirit.Current - max * .5f) < 1f, "Tụ Linh Đan restores 50% of spirit");

            // Hộ Mệnh Phù.
            health.Revive(1f, 0);
            var lethal = DamageInfo.Create(health.maxHealth * 10f, Element.None, DamageSource.Environment, player.transform.position, Vector3.zero);
            bool defeated = false; health.Defeated.AddListener(() => defeated = true); health.respawnOnDefeat = false;
            health.ApplyDamage(lethal);
            Check(!defeated && !health.IsDead && Mathf.Abs(health.CurrentHealth - health.maxHealth * .4f) < 1f, "Hộ Mệnh Phù cancels a lethal hit and restores 40% health (" + health.CurrentHealth + ")");
            Check(health.Invulnerable, "…and grants invulnerability for 2 seconds");
            Check(pi.ReviveUsed && pi.Bag.Slots[slotAmulet].remaining == 0 && inv.Count("ho-menh-phu") == 0, "The talisman is used up");
            var handler = typeof(PlayerItems).GetMethod("OnBeforeDefeat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Check(!(bool)handler.Invoke(pi, new object[] { lethal }), "A second death in the same level is not prevented");
            Check(pi.Use(slotAmulet) == ItemUseResult.EmptySlot, "An empty slot does nothing");

            // Unused items stay after the level.
            int keptQi = inv.Count("hoi-khi-dan"), keptWood = inv.Count("hoi-xuan-dan");
            pi.BeginLevel();
            Check(inv.Count("hoi-khi-dan") == keptQi && inv.Count("hoi-xuan-dan") == keptWood, "Items not used stay in the inventory after the level");
            Check(pi.Bag.Slots.All(s => s.remaining <= Math.Min(s.item.maxPerLevel, inv.Count(s.item.id))), "A new attempt never carries more than the limit or the stock");
            var over = inv.BeginLevel(); Check(over.Slots.All(s => s.remaining <= s.item.maxPerLevel), "The level bag respects the per-level limit");
            P20EconomyChecks.Run(player.gameObject,profile,Check);
            buffs.enabled=true;
            // No real-money path.
            Check(!typeof(ShopService).GetMethods().Any(m => m.Name.ToLowerInvariant().Contains("real") || m.Name.ToLowerInvariant().Contains("iap")), "The shop has no real-money purchase method");
        }
    }
}
#endif
