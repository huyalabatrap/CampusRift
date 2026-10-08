using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CampusRift.Learning;
using CampusRift.Levels;
using CampusRift.Progression;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// P06-T08: rules of cultivation, Tu Vi sharing, the profile file and level locks, all without entering Play Mode.
public static class CultivationValidation
{
    sealed class MemoryProfileStore : IProfileStore
    {
        public ProfileData Data = new ProfileData();
        public ProfileData Load() => Data;
        public void Save(ProfileData data) { Data = data; }
    }

    [MenuItem("Campus Rift/V2/Validate Cultivation")]
    public static string Validate()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var report = new StringBuilder(); int passed = 0, failed = 0;
        void Check(bool ok, string label) { if (ok) passed++; else { failed++; report.AppendLine("FAIL " + label); } }
        string temp = Path.Combine(Path.GetTempPath(), "campusrift-cultivation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Table(Check); Rules(Check); Sharing(Check); Store(Check, temp); Migration(Check, temp); Locks(Check);
        }
        catch (Exception e) { failed++; report.AppendLine("EXCEPTION " + e); }
        finally { try { Directory.Delete(temp, true); } catch { } }
        string summary = "cultivation: " + passed + " passed, " + failed + " failed";
        Directory.CreateDirectory("Artifacts/Progression");
        File.WriteAllText("Artifacts/Progression/Cultivation.txt", summary + "\n" + report);
        return summary + (failed > 0 ? "\n" + report : "");
    }

    static CultivationService Fresh(CultivationTable table = null) => new CultivationService(new CultivationData(), table);

    static void Table(Action<bool, string> check)
    {
        var table = CultivationTable.Instance;
        check(table.RealmCount == 7, "seven realms");
        int[] perTier = { 100, 150, 225, 340, 510, 760, 1140 };
        bool tuVi = true; for (int i = 0; i < 7; i++) tuVi &= table.TuViPerTier((Realm)i) == perTier[i];
        check(tuVi, "Tu Vi per tier 100/150/225/340/510/760/1140");
        check(Mathf.Approximately(table.Health(Realm.LuyenKhi, 1), 100) && Mathf.Approximately(table.Attack(Realm.LuyenKhi, 1), 20), "Luyện Khí 1 = 100 health / 20 attack");
        check(Mathf.Approximately(table.Health(Realm.DoKiep, 1), 820) && Mathf.Approximately(table.Attack(Realm.DoKiep, 1), 164), "Độ Kiếp 1 = 820 health / 164 attack");
        check(Mathf.Approximately(table.Health(Realm.LuyenKhi, 5), 140) && Mathf.Approximately(table.Health(Realm.LuyenKhi, 3), 120), "tiers interpolate linearly (140 at tier 5, 120 at tier 3)");
        check(Mathf.Approximately(table.Defense(Realm.TrucCo), 0.03f) && Mathf.Approximately(table.Defense(Realm.DoKiep), 0.18f), "defense 3% Trúc Cơ … 18% Độ Kiếp");
        check(Mathf.Approximately(table.RunBonus(Realm.LuyenKhi), 0) && table.RunBonus(Realm.DoKiep) <= 0.1001f && table.RunBonus(Realm.KetDan) > table.RunBonus(Realm.TrucCo), "run bonus grows per realm and stays within +10%");
        var asset = AssetDatabase.LoadAssetAtPath<CultivationTable>("Assets/Progression/Resources/CultivationTable.asset");
        check(asset != null && asset.rows.Length == 7 && asset.TuViPerTier(Realm.DoKiep) == 1140, "the shipped asset has the same numbers");
    }

    static void Rules(Action<bool, string> check)
    {
        var c = Fresh(); int tierEvents = 0, realmEvents = 0; float lastGain = 0;
        c.TierChanged += (r, t) => tierEvents++; c.RealmChanged += r => realmEvents++; c.TuViAdded += (g, s) => lastGain = g;
        check(c.Realm == Realm.LuyenKhi && c.Tier == 1 && c.TuVi == 0, "starts at Luyện Khí 1 with no Tu Vi");
        c.AddTuVi(99, "test"); check(c.Tier == 1 && Mathf.Approximately(c.TuVi, 99) && tierEvents == 0, "99 of 100 stays in tier 1");
        c.AddTuVi(1, "test"); check(c.Tier == 2 && c.TuVi == 0 && tierEvents == 1, "filling a tier raises the next one");
        c.AddTuVi(250, "test"); check(c.Tier == 4 && Mathf.Approximately(c.TuVi, 50), "one big gain passes several tiers (tier 4, 50/100)");
        c.SetState(Realm.LuyenKhi, 1, 0);
        float gained = c.AddTuVi(1000, "test");
        check(c.Tier == 5 && c.IsBottleneck && Mathf.Approximately(gained, 500) && Mathf.Approximately(lastGain, 500), "1000 Tu Vi from the start pays 500 and stops at the bottleneck");
        check(c.AddTuVi(50, "test") == 0 && c.IsBottleneck, "Tu Vi at the bottleneck is dropped");
        check(c.CanAttemptBreakthrough, "the breakthrough exam opens at the bottleneck");
        check(c.CompleteBreakthrough() && c.Realm == Realm.TrucCo && c.Tier == 1 && c.TuVi == 0 && realmEvents == 1, "passing the exam starts Trúc Cơ 1 with an empty bar");
        check(!c.CompleteBreakthrough(), "no second breakthrough without a full bottleneck");
        c.SetState(Realm.DoKiep, 5, 1140);
        check(c.IsBottleneck && c.IsLastRealm && !c.CanAttemptBreakthrough && !c.CompleteBreakthrough() && c.AddTuVi(10, "x") == 0, "Độ Kiếp is the last realm: full tier 5 just stops");
        c.SetState(Realm.KetDan, 3, 10);
        check(c.AtLeast(2, 3) && c.AtLeast(1, 5) && !c.AtLeast(2, 4) && !c.AtLeast(3, 1), "AtLeast compares realm then tier");
        check(c.AddTuVi(-5, "x") == 0 && c.AddTuVi(float.NaN, "x") == 0, "negative and NaN amounts are ignored");
    }

    static CourseData Course(int lessons, string id)
    {
        var course = ScriptableObject.CreateInstance<CourseData>(); course.id = id;
        for (int i = 0; i < lessons; i++) { var l = ScriptableObject.CreateInstance<LessonData>(); l.id = id + "-" + i; course.lessons.Add(l); }
        return course;
    }
    static void Free(CourseData course) { foreach (var l in course.lessons) Object.DestroyImmediate(l); Object.DestroyImmediate(course); }

    static void Sharing(Action<bool, string> check)
    {
        var table = CultivationTable.Instance; var catalog = ScriptableObject.CreateInstance<LearningCatalog>();
        var a = Course(3, "a"); var b = Course(4, "b"); var c = Course(5, "c");
        catalog.courses.Add(a); catalog.courses.Add(b); catalog.courses.Add(c);
        check(Mathf.Approximately(StudyRewards.LessonShare(catalog, a, table), 500 * 0.85f / 3), "chapter 1 with 3 lessons: 500 × 85% ÷ 3");
        check(Mathf.Approximately(StudyRewards.LessonShare(catalog, b, table), 750 * 0.85f / 4), "chapter 2 with 4 lessons: 750 × 85% ÷ 4");
        check(Mathf.Approximately(StudyRewards.LessonShare(catalog, c, table), 1125 * 0.85f / 5), "chapter 3 with 5 lessons: 1125 × 85% ÷ 5");
        foreach (var course in new[] { a, b, c })
        {
            var service = Fresh(); service.SetState((Realm)catalog.courses.IndexOf(course), 1, 0);
            float total = 0; foreach (var l in course.lessons)
            { total += StudyRewards.FirstReading(service, catalog, course).requested; total += StudyRewards.FirstQuizAttempt(service, catalog, course, 100).requested; }
            float realm = table.TuViOfRealm((Realm)catalog.courses.IndexOf(course));
            check(Mathf.Abs(total - realm * 0.85f) < 0.01f, course.id + ": a full-marks chapter pays 85% of its realm (" + total.ToString("F1") + " of " + realm + ")");
        }
        var quiz = Fresh(); float half = StudyRewards.FirstQuizAttempt(quiz, catalog, a, 50).requested;
        check(Mathf.Approximately(half, StudyRewards.LessonShare(catalog, a, table) * (0.70f / 0.85f) * 0.5f), "the first quiz pays 70% share scaled by the score");
        check(StudyRewards.FirstQuizAttempt(quiz, catalog, a, 0) == null, "a zero score pays nothing");
        var level = Fresh(); var award = StudyRewards.FirstLevelClear(level);
        check(award != null && Mathf.Approximately(award.tuVi, 5), "first level clear pays 5% of a tier (5 at Luyện Khí)");
        Free(a); Free(b); Free(c); Object.DestroyImmediate(catalog);
    }

    static void Store(Action<bool, string> check, string dir)
    {
        string path = Path.Combine(dir, "campusrift-v2.json");
        var store = new JsonProfileStore(path);
        var data = new ProfileData(); data.cultivation.realm = 2; data.cultivation.tier = 3; data.cultivation.tuVi = 77; data.wallet.linhThach = 560;
        data.learning.lessons.Add(new LessonProgress { id = "ch1-b1", pagesRead = 4, bestPercent = 90, readRewarded = true });
        data.Level(1, true).cleared = true; data.Level(1, true).stars = 5; data.Level(1, true).bestTime = 212.4f;
        store.Save(data);
        var loaded = new JsonProfileStore(path).Load();
        check(loaded.version == 2 && loaded.cultivation.realm == 2 && loaded.cultivation.tier == 3 && Mathf.Approximately(loaded.cultivation.tuVi, 77) && loaded.wallet.linhThach == 560, "save → load keeps cultivation and wallet");
        check(loaded.learning.lessons.Count == 1 && loaded.learning.lessons[0].readRewarded && loaded.Level(1, false) != null && loaded.Level(1, false).stars == 5, "lessons and level results survive");
        data.cultivation.tuVi = 88; store.Save(data);
        check(File.Exists(path + ".bak"), "the second save leaves a .bak");
        File.WriteAllText(path, "corrupt");
        var recover = new JsonProfileStore(path); var fromBak = recover.Load();
        check(recover.RecoveredFromBackup && Mathf.Approximately(fromBak.cultivation.tuVi, 77) && !string.IsNullOrEmpty(recover.RecoveryMessage), "a damaged main file is recovered from the .bak with a warning");
        File.WriteAllText(path, "corrupt"); File.WriteAllText(path + ".bak", "corrupt");
        var blocked = new JsonProfileStore(path); blocked.Load(); bool refused = false;
        try { blocked.Save(new ProfileData()); } catch (IOException) { refused = true; }
        check(refused && File.ReadAllText(path) == "corrupt", "two damaged files are preserved, never overwritten");
        string newer = Path.Combine(dir, "newer.json");
        File.WriteAllText(newer, JsonUtility.ToJson(new ProfileData { version = 3 }));
        var future = new JsonProfileStore(newer); future.Load(); bool futureRefused = false;
        try { future.Save(new ProfileData()); } catch (IOException) { futureRefused = true; }
        check(futureRefused, "a save from a newer version is not overwritten");
        var empty = new JsonProfileStore(Path.Combine(dir, "none.json")).Load();
        check(empty.cultivation.realm == 0 && empty.cultivation.tier == 1 && empty.levels.Count == 0, "no file gives a fresh profile");
    }

    static string Hash(string file) { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(file))); }

    static void Migration(Action<bool, string> check, string dir)
    {
        string legacy = Path.Combine(dir, "learning-v1.json"), backup = Path.Combine(dir, "learning-v1.backup.json"), v2 = Path.Combine(dir, "campusrift-v2.json");
        if (File.Exists(v2)) File.Delete(v2); if (File.Exists(v2 + ".bak")) File.Delete(v2 + ".bak");
        File.WriteAllText(legacy, "{\"version\":1,\"breakthroughs\":3}");
        string before = Hash(legacy);
        var go = new GameObject("profile test"); var service = go.AddComponent<ProfileService>();
        service.Initialize(new JsonProfileStore(v2), legacy);
        check(File.Exists(backup) && Hash(backup) == before && Hash(legacy) == before, "the old save is copied to learning-v1.backup.json and left untouched");
        check(service.Data.migration.noticePending && service.Data.cultivation.totalEarned == 0 && service.Data.cultivation.tier == 1, "the V2 profile starts new and asks for the one-time notice");
        check(File.Exists(v2), "the new profile file is written at once");
        service.Data.migration.noticePending = false; service.Flush(); Object.DestroyImmediate(go);
        var go2 = new GameObject("profile test 2"); var again = go2.AddComponent<ProfileService>();
        again.Initialize(new JsonProfileStore(v2), legacy);
        check(!again.Data.migration.noticePending, "the notice does not come back after it was dismissed");
        File.WriteAllText(legacy, "{\"version\":1,\"breakthroughs\":9}");
        Object.DestroyImmediate(go2);
        check(Hash(backup) == before, "an existing backup is never overwritten");
    }

    static void Locks(Action<bool, string> check)
    {
        var go = new GameObject("profile locks"); var service = go.AddComponent<ProfileService>();
        service.Initialize(new MemoryProfileStore(), null);
        var levels = service.Levels; var cultivation = service.Cultivation;
        string reason;
        check(levels.IsUnlocked(1, out reason) && reason == null, "level 1 is open from the start");
        check(!levels.IsUnlocked(2, out reason) && reason != null && (reason.Contains("Luyện Khí 3") || reason.Contains("Qi Refining 3")), "level 2 needs Luyện Khí 3: \"" + reason + "\"");
        var level1 = LevelCatalog.Instance.Get(1); var level2 = LevelCatalog.Instance.Get(2);
        bool first = levels.RecordClear(level1, 200f, 0, out var award);
        check(first && award != null && Mathf.Approximately(award.tuVi, 5) && levels.IsCleared(1) && levels.Stars(1) == 1, "first clear is recorded with one star and pays 5 Tu Vi");
        check(!levels.IsUnlocked(2, out reason), "clearing level 1 is not enough without the realm");
        cultivation.SetState(Realm.LuyenKhi, 3, 0);
        check(levels.IsUnlocked(2, out reason) && reason == null, "Luyện Khí 3 plus level 1 cleared opens level 2");
        first = levels.RecordClear(level1, 150f, LevelProgressService.StarTime, out award);
        check(!first && award == null && Mathf.Approximately(levels.BestTime(1), 150f) && levels.Stars(1) == 3 && levels.StarCount(1) == 2, "a replay pays nothing, keeps the best time and adds stars");
        check(!levels.IsUnlocked(3, out reason) && reason != null, "level 3 stays locked: \"" + reason + "\"");
        cultivation.SetState(Realm.TrucCo, 1, 0);
        check(!levels.IsUnlocked(3, out reason) && reason.Contains("2"), "level 3 also needs level 2 cleared: \"" + reason + "\"");
        levels.RecordClear(level2, 300f, 0, out award);
        check(levels.IsUnlocked(3, out reason), "Trúc Cơ 1 plus level 2 cleared opens level 3");
        check(levels.HighestCleared == 2 && levels.NextPlayable() == 3, "next playable level is 3");
        cultivation.SetState(Realm.LuyenKhi, 1, 0);
        check(levels.NextPlayable() == 1 || levels.NextPlayable() == 2, "with a lower realm the next playable level falls back to an open one (" + levels.NextPlayable() + ")");
        Object.DestroyImmediate(go);
    }
}
