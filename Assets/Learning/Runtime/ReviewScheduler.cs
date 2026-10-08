using System;

namespace CampusRift.Learning
{
    // Spaced review (P07-T05): a lesson mastered for the first time is due again after 1 day. Passing a review raises the
    // badge (Đồng → Bạc → Vàng) and schedules the next one after 3 and 7 days; failing keeps the badge and repeats after 1 day.
    public static class ReviewScheduler
    {
        public const int None = 0, Bronze = 1, Silver = 2, Gold = 3;
        public const int QuestionCount = 5, PassPercent = 80;
        static readonly int[] DaysAfterBadge = { 1, 3, 7, 0 };   // index = badge just reached (0 = first mastery)

        public static string BadgeName(int mastery, bool vietnamese)
        {
            switch (mastery)
            {
                case Bronze: return vietnamese ? "Đồng" : "Bronze";
                case Silver: return vietnamese ? "Bạc" : "Silver";
                case Gold: return vietnamese ? "Vàng" : "Gold";
                default: return vietnamese ? "Chưa có" : "None";
            }
        }

        public static DateTime? Due(LessonProgress p)
        {
            if (p == null || !p.rewarded || string.IsNullOrEmpty(p.nextReviewUtc) || p.mastery >= Gold) return null;
            return DateTime.TryParse(p.nextReviewUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t) ? t : (DateTime?)null;
        }
        public static bool IsDue(LessonProgress p, DateTime now) { var due = Due(p); return due.HasValue && now >= due.Value; }

        // Called when a lesson is mastered for the first time.
        public static void OnFirstMastery(LessonProgress p, DateTime now)
        { p.mastery = None; p.reviewsDone = 0; p.nextReviewUtc = now.AddDays(DaysAfterBadge[0]).ToString("o"); }

        // Returns true when the review was passed.
        public static bool Record(LessonProgress p, bool passed, DateTime now)
        {
            if (passed)
            {
                p.mastery = Math.Min(Gold, p.mastery + 1); p.reviewsDone++;
                p.nextReviewUtc = p.mastery >= Gold ? null : now.AddDays(DaysAfterBadge[p.mastery]).ToString("o");
            }
            else p.nextReviewUtc = now.AddDays(1).ToString("o");
            return passed;
        }
    }
}
