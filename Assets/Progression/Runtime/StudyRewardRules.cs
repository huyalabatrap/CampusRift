using UnityEngine;
using CampusRift.Learning;

namespace CampusRift.Progression
{
    // What one action was worth (returned to the UI).
    public sealed class StudyAward
    {
        public string source;
        public float requested, tuVi;                  // requested vs. actually counted (bottleneck drops the rest)
        public Realm realmBefore, realmAfter;
        public int tierBefore, tierAfter;
        public bool TierUp => tierAfter != tierBefore || realmAfter != realmBefore;
        public bool Bottleneck;
    }

    // The Tu Vi sources of plan §8.2. A lesson's share is (Tu Vi of the chapter's realm × 85%) ÷ lessons in the chapter,
    // computed from the catalog so it follows whatever content is loaded. The table's percentages (15% reading, 70% quiz)
    // are percentages of the realm, so of the 85% share reading is 15/85 and the quiz 70/85: a chapter read once and
    // passed with full marks pays exactly 85% of the realm (the rest comes from review and practice).
    public static class StudyRewards
    {
        public const float ChapterShare = 0.85f, FirstRead = 0.15f / 0.85f, FirstQuiz = 0.70f / 0.85f, Review = 0.10f, PracticePerCorrect = 2f, PracticeDailyCap = 150f, LevelClearTier = 0.05f;
        public const string SourceRead = "read", SourceQuiz = "quiz", SourceReview = "review", SourcePractice = "practice", SourceLevel = "level";

        // Chapter k (order of the course in the catalog) belongs to realm k.
        public static Realm ChapterRealm(LearningCatalog catalog, CourseData course, CultivationTable table)
        {
            int index = catalog != null ? Mathf.Max(0, catalog.courses.IndexOf(course)) : 0;
            return (Realm)Mathf.Clamp(index, 0, table.RealmCount - 1);
        }

        public static float LessonShare(LearningCatalog catalog, CourseData course, CultivationTable table)
        {
            if (course == null || course.lessons.Count == 0) return 0;
            var realm = ChapterRealm(catalog, course, table);
            return table.TuViOfRealm(realm) * ChapterShare / course.lessons.Count;
        }

        public static StudyAward Grant(CultivationService cultivation, float amount, string source)
        {
            if (cultivation == null || amount <= 0) return null;
            var award = new StudyAward { source = source, requested = amount, realmBefore = cultivation.Realm, tierBefore = cultivation.Tier };
            award.tuVi = cultivation.AddTuVi(amount, source);
            award.realmAfter = cultivation.Realm; award.tierAfter = cultivation.Tier; award.Bottleneck = cultivation.IsBottleneck;
            return award;
        }

        public static StudyAward FirstReading(CultivationService c, LearningCatalog catalog, CourseData course) =>
            Grant(c, LessonShare(catalog, course, c != null ? c.Table : CultivationTable.Instance) * FirstRead, SourceRead);

        public static StudyAward FirstQuizAttempt(CultivationService c, LearningCatalog catalog, CourseData course, float percentCorrect) =>
            Grant(c, LessonShare(catalog, course, c != null ? c.Table : CultivationTable.Instance) * FirstQuiz * Mathf.Clamp01(percentCorrect / 100f), SourceQuiz);

        public static StudyAward OnTimeReview(CultivationService c, LearningCatalog catalog, CourseData course) =>
            Grant(c, LessonShare(catalog, course, c != null ? c.Table : CultivationTable.Instance) * Review, SourceReview);

        // First clear of a level: 5% of one tier of the current realm.
        public static StudyAward FirstLevelClear(CultivationService c) =>
            c == null ? null : Grant(c, c.TuViPerTier * LevelClearTier, SourceLevel);
    }
}

