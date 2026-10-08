#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Combat;
using CampusRift.Localization;
using CampusRift.Progression;
using CampusRift.Skills;
using CampusRift.UI;
using UnityEngine;
using Object = UnityEngine.Object;
namespace CampusRift.Learning
{
    // Study (Chapter 1, Giáo trình Tư tưởng Hồ Chí Minh) → Tu Vi → tier → player stats, through the real UI in the gameplay scene (P06-T05/T07/T08).
    // Runs on a throw-away profile so the player's real save is never touched.
    // Output: Artifacts/Learning/PlayMode.json and Artifacts/Learning/PlayMode-DONE.txt.
    public sealed class LearningPlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(); }
        readonly Report report = new Report();
        LearningEngine engine; CourseData course; CultivationService cultivation; ProfileService profile;
        LearningUI View => Object.FindAnyObjectByType<LearningUI>();
        UIStateManager State => UIStateManager.Instance;
        const string Output = "Artifacts/Learning/";
        bool running;
        GameSettings originalSettings;
        void Check(bool ok, string label) { (ok ? report.passed : report.failed).Add(label); Directory.CreateDirectory(Output); File.WriteAllText(Output + "PlayMode.json", JsonUtility.ToJson(report, true)); Debug.Log("LEARNING QA " + (ok ? "PASS " : "FAIL ") + label); }
        void Update() { if (running && State != null && State.State == UIState.Paused) State.EnterScene(true); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Output); Application.runInBackground = true; running = true;
            // Buttons are found by their English labels.
            originalSettings = SettingsManager.Instance.Current.Copy();
            var english = originalSettings.Copy(); english.Language = GameLanguage.English; SettingsManager.Instance.Apply(english, false);
            profile = ProfileService.Instance;
            profile.UseTransient(new ProfileData());
            // This fixture tests study/UI, so sandbox combat cannot kill the player between study panels.
            Object.FindAnyObjectByType<Monsters.PlayerMonsterHealth>().Revive(1,300);
            cultivation = profile.Cultivation;
            var catalog = Resources.Load<LearningCatalog>("LearningCatalog");
            engine = new LearningEngine(catalog, new ProfileLearningStore(profile), null, cultivation);
            LearningService.Instance.EditorUseEngine(engine);
            course = catalog.courses[0];
            State.EnterScene(true);
            // LevelHUD is authored by LevelDirector; a bare sandbox has no level body.
            var levelFixture = CampusRift.Levels.LevelDirector.Ensure();
            levelFixture.Begin(CampusRift.Levels.LevelCatalog.Instance.Get(1));
            levelFixture.enabled = false;
            var run = Run();
            while (true)
            {
                bool next = false; object current = null;
                try { next = run.MoveNext(); if (next) current = run.Current; }
                catch (Exception e) { Check(false, e.ToString()); break; }
                if (!next) break; yield return current;
            }
            try { profile.EndTransient(); } catch { }
            SettingsManager.Instance.Apply(originalSettings, false);
            var real = LearningService.Instance;
            if (real != null) real.EditorUseEngine(new LearningEngine(catalog, new ProfileLearningStore(profile), null, profile.Cultivation));
            running = false;
            File.WriteAllText(Output + "PlayMode-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
            Destroy(gameObject);
        }

        void Click(string label)
        {
            var b = View.content.GetComponentsInChildren<RiftButton>().FirstOrDefault(x => x.Label.text.StartsWith(label));
            if (b == null || !b.IsInteractable()) throw new InvalidOperationException("Unavailable learning button: " + label);
            b.onClick.Invoke();
        }
        IEnumerator Settle() { yield return null; yield return new WaitForSecondsRealtime(.12f); }
        // Answers every question of the running quiz through the public UI entry point (ordering needs no dragging in a test).
        void AnswerAll(bool correct)
        {
            var field = typeof(LearningUI).GetField("quiz", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            for (int i = 0; i < 40; i++)
            {
                var session = (QuizSession)field.GetValue(View);
                if (session == null || session.Complete) break;
                var q = session.Questions[session.Answered];
                var ids = new List<string>(q.Data.correctOptionIds);
                if (!correct) { if (q.Data.type == "ordering") ids.Reverse(); else if(q.Data.type=="matching") ids=q.Data.correctOptionIds.Select(p=>p.Split(':')[0]+":"+(p.EndsWith(":1")?"2":"1")).ToList(); else ids = new List<string> { q.Data.options.First(o => !q.Data.correctOptionIds.Contains(o.id)).id }; }
                View.Answer(ids);
                Click(session.Answered >= session.Questions.Count ? "VIEW RESULT" : "NEXT QUESTION");
            }
        }

        IEnumerator Run()
        {
            var player = Object.FindAnyObjectByType<CampusExplorer>(); var stats = player.GetComponent<PlayerStats>(); var health = player.GetComponent<Monsters.PlayerMonsterHealth>();
            var bridge = player.GetComponent<CultivationPlayerBridge>();
            Check(bridge != null && player.GetComponents<CultivationPlayerBridge>().Length == 1, "The player has exactly one cultivation bridge");
            bridge.Apply(); yield return null;
            Check(Mathf.Approximately(stats.MaxHealth, 100) && Mathf.Approximately(stats.Attack, 20) && Mathf.Approximately(stats.MaxSpirit, 100), "Luyện Khí 1 gives 100 health, 20 attack, 100 spirit");
            Check(cultivation.Realm == Realm.LuyenKhi && cultivation.Tier == 1 && cultivation.TuVi == 0, "A fresh profile starts at Luyện Khí 1");

            // ---- the realm screen ----
            State.Pause(); State.EditorForceCourse(); yield return Settle();
            Check(View != null && View.Screen == "Courses", "Courses opens from pause");
            var realmButton = View.content.GetComponentsInChildren<RiftButton>().FirstOrDefault(b => b.Label.text.Contains("Luyện Khí") || b.Label.text.Contains("Qi Refining"));
            Check(realmButton != null, "The course list starts with the realm summary button");
            realmButton.onClick.Invoke(); yield return Settle();
            Check(View.Screen == "Realm", "The realm screen opens");
            var texts = string.Join("\n", View.content.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => t.text));
            Check(texts.Contains("100") && texts.Contains("20") && (texts.Contains("Màn 2") || texts.Contains("Level 2")), "The realm screen shows stats and the next level to open");
            var exam = View.content.GetComponentsInChildren<RiftButton>().FirstOrDefault(b => b.Label.text.StartsWith("THI") || b.Label.text.StartsWith("BREAKTHROUGH EXAM"));
            Check(exam != null && !exam.IsInteractable(), "The exam button is off until the bottleneck");
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P06-realm.png"); yield return Settle();
            View.Close(); yield return new WaitForSecondsRealtime(.7f);

            // ---- lesson 1: reading pays 15%, the first quiz 70% ----
            State.Pause(); State.EditorForceCourse(); yield return Settle();
            var lesson1 = course.lessons[0]; Click("CHAPTER 1"); Click(lesson1.title);
            float share = StudyRewards.LessonShare(engine.Catalog, course, cultivation.Table);
            for (int page = 0; page < 10 && View.Screen == "Lesson"; page++)
            {
                var next = View.content.GetComponentsInChildren<RiftButton>().First(b => b.Label.text.StartsWith("READ & NEXT") || b.Label.text.StartsWith("COMPLETE READING"));
                next.onClick.Invoke();
            }
            yield return Settle();
            float readGain = share * StudyRewards.FirstRead;
            Check(Mathf.Abs(cultivation.Data.totalEarned - readGain) < 0.05f, "Finishing the reading pays 15/85 of the lesson share (" + cultivation.TuVi.ToString("F2") + " vs " + readGain.ToString("F2") + ")");
            Check(View.content.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("Tu Vi") || t.text.Contains("cultivation")), "The reading screen tells the player about the Tu Vi");
            float afterRead = cultivation.Data.totalEarned;
            Click("START QUIZ"); yield return Settle();
            AnswerAll(true); yield return Settle();
            float quizGain = share * StudyRewards.FirstQuiz;
            Check(Mathf.Abs(cultivation.Data.totalEarned - afterRead - quizGain) < 0.05f, "A full-marks first quiz pays 70/85 of the share (" + (cultivation.Data.totalEarned - afterRead).ToString("F2") + " vs " + quizGain.ToString("F2") + ")");
            Check(View.Screen == "Result" && engine.Progress.Lesson(lesson1.id).rewarded, "The lesson is mastered and the result screen appears");
            Click("CULTIVATION GAINED"); yield return new WaitForSecondsRealtime(1.2f);
            Check(View.content.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("+")), "The Tu Vi presentation lists the gain");
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P06-gain.png"); yield return Settle();
            float afterFirst = cultivation.Data.totalEarned;
            // A retake improves the record and pays nothing.
            Click("CONTINUE LEARNING"); Click(lesson1.title); yield return Settle();
            Click("START QUIZ"); yield return Settle(); AnswerAll(true); yield return Settle();
            Check(Mathf.Approximately(cultivation.Data.totalEarned, afterFirst), "A retake pays no further Tu Vi");
            View.Close(); yield return new WaitForSecondsRealtime(.7f);

            // ---- the rest of the chapter, then the bottleneck maths ----
            for (int i = 0; i < 6 && engine.Progress.lessons.Count(l => l.rewarded) < course.lessons.Count; i++) engine.DebugPassNext(course);
            yield return Settle();
            float expected = 500 * 0.85f;
            Check(course.lessons.Count == 4 && Mathf.Abs(TotalTuVi() - expected) < 0.5f, "A whole chapter at full marks pays 85% of the realm (" + TotalTuVi().ToString("F1") + " of " + expected + ")");
            Check(cultivation.Tier == 5 && Mathf.Abs(cultivation.TuVi - 25) < 0.5f, "That lands in Luyện Khí tier 5 (" + cultivation.Name(false) + ", " + cultivation.TuVi.ToString("F1") + ")");
            yield return null;
            Check(Mathf.Approximately(stats.MaxHealth, 140) && Mathf.Approximately(stats.Attack, 28) && Mathf.Approximately(stats.MaxSpirit, 120), "The player's stats follow the tier: 140 health, 28 attack, 120 spirit at tier 5");
            Check(Mathf.Approximately(health.maxHealth, 140), "PlayerMonsterHealth receives the new maximum");
            bridge.Apply(); bridge.Apply(); yield return null;
            Check(Mathf.Approximately(stats.MaxHealth, 140) && stats.Modifiers.Count(m => m.source == StatSource.Cultivation && m.stat == StatType.MaxHealth) == 1, "Applying the bridge again does not stack");
            var hud = Object.FindAnyObjectByType<LevelHUD>();
            Check(hud != null && cultivation.Tier == 5 && hud.BodyText.Length>0, "Current LevelHUD is present while four tiers are complete");

            cultivation.SetState(Realm.LuyenKhi, 5, 100); yield return null;
            Check(cultivation.CanAttemptBreakthrough && hud != null && cultivation.Tier == 5, "A full tier 5 is the bottleneck in the current HUD");
            State.Pause(); State.EditorForceCourse(); yield return Settle(); Click("REALM"); yield return Settle();
            exam = View.content.GetComponentsInChildren<RiftButton>().FirstOrDefault(b => b.Label.text.StartsWith("THI") || b.Label.text.StartsWith("BREAKTHROUGH EXAM"));
            Check(exam != null && exam.IsInteractable(), "The exam button turns on at the bottleneck");
            // ---- the exam through the real UI (P07-T03/T04) ----
            exam.onClick.Invoke(); yield return Settle();
            Check(View.Screen == "ExamIntro", "The exam button opens the exam introduction");
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P07-exam-intro.png"); yield return Settle();
            Click("START EXAM"); yield return Settle();
            Check(View.Screen == "Quiz", "START EXAM shows the first question");
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P07-exam-question.png"); yield return Settle();
            var quizField = typeof(LearningUI).GetField("quiz", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var examSession = (QuizSession)quizField.GetValue(View);
            Check(examSession.Kind == QuizKind.Exam && examSession.Questions.Count == 20, "The exam session holds 20 questions");
            for (int i = 0; i < 20; i++) { var q = examSession.Questions[examSession.Answered]; View.Answer(q.Data.correctOptionIds); yield return null; }
            yield return Settle();
            Check(cultivation.Realm == Realm.TrucCo && cultivation.Tier == 1, "Passing the exam through the UI breaks through to Trúc Cơ 1");
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P07-exam-result.png"); yield return Settle();
            View.Close(); yield return new WaitForSecondsRealtime(.7f);
            yield return null;
            Check(cultivation.Realm == Realm.TrucCo && Mathf.Approximately(stats.MaxHealth, 170) && Mathf.Approximately(stats.Attack, 34) && Mathf.Abs(health.DamageReduction - 0.03f) < 0.001f, "Trúc Cơ 1: 170 health, 34 attack, 3% defense");
            cultivation.SetState(Realm.DoKiep, 1, 0); yield return null;
            Check(Mathf.Approximately(stats.MaxHealth, 820) && Mathf.Approximately(stats.Attack, 164) && stats.MoveSpeedBonus <= 0.1001f, "Độ Kiếp 1: 820 health, 164 attack, run bonus capped");
            cultivation.SetState(Realm.LuyenKhi, 1, 0); yield return null;
            Check(Mathf.Approximately(stats.MaxHealth, 100) && Mathf.Approximately(stats.Attack, 20), "Going back to Luyện Khí 1 restores 100 / 20 without leftovers");

            // ---- locks ----
            var manager = GameSceneManager.Instance;
            manager.StartLevel(3); yield return null;
            Check(!manager.IsLoading && manager.LastLockReason != null, "StartLevel(3) is refused while locked: \"" + manager.LastLockReason + "\"");
            manager.StartLevel(2); yield return null;
            Check(!manager.IsLoading && manager.LastLockReason != null, "StartLevel(2) is refused at Luyện Khí 1: \"" + manager.LastLockReason + "\"");

            // ---- the shop through the UI (P08-T06) ----
            State.Pause(); State.EditorForceCourse(); yield return Settle();
            Click("ALCHEMY PAVILION"); yield return Settle();
            Check(View.Screen == "Shop", "The Courses screen opens the shop");
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P08-shop.png"); yield return Settle();
            Click("RECOVERY"); yield return Settle();
            Check(View.Screen == "ShopCategory", "The Recovery tab lists items");
            Click("Qi Recovery Pill"); yield return Settle();
            Check(View.Screen == "ShopItem", "An item opens its page");
            var confirm = View.content.GetComponentsInChildren<RiftButton>().First(bt => bt.Label.text.StartsWith("CONFIRM PURCHASE"));
            Check(!confirm.IsInteractable() && View.content.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("Linh Thạch") && t.text.Contains("more")), "Without Linh Thạch the purchase is off and says how many are missing");
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P08-shop-item.png"); yield return Settle();
            profile.Wallet.Earn(100, "test");
            View.ShowShop(); yield return Settle(); Click("RECOVERY"); yield return Settle(); Click("Qi Recovery Pill"); yield return Settle();
            Click("CONFIRM PURCHASE"); yield return Settle();
            Check(profile.Inventory.Count("hoi-khi-dan") == 1 && profile.Wallet.Balance == 75, "Confirming buys one for 25 Linh Thạch");
            View.ShowShop(); yield return Settle(); Click("TAKE INTO THE LEVEL"); yield return Settle();
            Click("Qi Recovery Pill"); yield return Settle();
            Check(profile.Inventory.CarryCount("hoi-khi-dan") == 1, "The carry list takes the item");
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P08-carry.png"); yield return Settle();
            View.ShowShop(); yield return Settle(); Click("ARTIFACTS"); yield return Settle();
            UnityEngine.ScreenCapture.CaptureScreenshot(Output + "P08-artifacts.png"); yield return Settle();
            profile.Inventory.ClearCarry();
            View.Close(); yield return new WaitForSecondsRealtime(.7f);

            // ---- saving ----
            Check(!profile.Transient || profile.Data.learning.lessons.Count(l => l.rewarded) == 4, "Lesson progress lives inside the profile");
            Check(profile.HasProgress, "The profile counts as having progress (Continue is available)");
        }

        float TotalTuVi() => cultivation.Data.totalEarned;
    }
}
#endif
