using System;
using System.Collections.Generic;
using CampusRift.Progression;

namespace CampusRift.Learning
{
    // What one finished quiz, review, practice or exam paid in Linh Thạch.
    public sealed class LinhThachAward
    {
        public int firstCorrect, retryCorrect, lesson, review, exam, total;
        public int answersFirst, answersRetry;     // how many answers paid
        public bool retryCapReached, paused;
        public string notice;                       // shown to the player ("Đọc kỹ câu hỏi")
    }

    // Linh Thạch rules of plan §9.1 (earning) and §9.2 (anti-farming). Pure logic over the learning save and the wallet;
    // the engine feeds it results and the clock, so it is tested with a fake clock.
    public sealed class StudyEconomy
    {
        readonly Wallet wallet;
        readonly EconomyConfig config;
        public StudyEconomy(Wallet wallet, EconomyConfig config = null) { this.wallet = wallet; this.config = config != null ? config : EconomyConfig.Instance; }
        public EconomyConfig Config => config;

        static DateTime? Parse(string value) => DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t) ? t : (DateTime?)null;
        public bool IsPaused(LearningProgress p, DateTime now) { var until = Parse(p.linhPausedUntilUtc); return until.HasValue && until.Value > now; }

        // Pays the answers of a finished session. Must run before the correct-answer log is updated with these answers.
        public LinhThachAward PayAnswers(LearningProgress p, QuizResult result, DateTime now, int maximumPayment=int.MaxValue, Func<int,int> payout=null)
        {
            var award = new LinhThachAward();
            if (DevMode.Active) return award;
            string today = now.ToString("yyyy-MM-dd");
            if (p.linhDate != today) { p.linhDate = today; p.linhRetryToday = 0; }
            bool paused = IsPaused(p, now); int streak = 0;
            var lastCorrect = new Dictionary<string, DateTime>();
            foreach (var e in p.correctLog) { var t = Parse(e.utc); if (t.HasValue) lastCorrect[e.question] = t.Value; }
            foreach (var a in result.answers)
            {
                streak = a.seconds < config.fastSeconds ? streak + 1 : 0;
                if (!paused && streak >= config.fastStreak)
                {
                    paused = true; p.linhPausedUntilUtc = now.AddMinutes(config.fastPauseMinutes).ToString("o");
                    award.paused = true; award.notice = "Đọc kỹ câu hỏi";
                }
                if (!a.correct) continue;
                if (paused) { award.paused = true; continue; }
                int remainingBudget=Math.Max(0,maximumPayment-award.firstCorrect-award.retryCorrect);
                if(remainingBudget==0)continue;
                if (!p.solved.Contains(a.questionId))
                {
                    p.solved.Add(a.questionId); award.firstCorrect += Math.Min(config.firstCorrect,remainingBudget); award.answersFirst++;
                }
                else
                {
                    if (lastCorrect.TryGetValue(a.questionId, out var when) && now - when < TimeSpan.FromHours(config.retryLockHours)) continue;
                    int room = Math.Max(0, config.retryDailyCap - p.linhRetryToday);
                    int pay = Math.Min(config.retryCorrect, Math.Min(room,remainingBudget));
                    if (pay < config.retryCorrect) award.retryCapReached = true;
                    if (pay <= 0) continue;
                    p.linhRetryToday += pay; award.retryCorrect += pay; award.answersRetry++;
                }
            }
            award.total = award.firstCorrect + award.retryCorrect;
            if (award.total > 0){if(payout!=null)award.total=payout(award.total);else wallet.Earn(award.total, "answers");}
            return award;
        }

        // Finishing a lesson: 30 from 80%, 50 for full marks; a better result later only pays the difference.
        public int PayLesson(LessonProgress lesson, float percent, bool passed)
        {
            if (DevMode.Active) return 0;
            int target = !passed ? 0 : percent >= 100f - 0.001f ? config.lessonPerfect : config.lessonPassed;
            int pay = target - lesson.linhPaid;
            if (pay <= 0) return 0;
            lesson.linhPaid = target; return wallet.Earn(pay, "lesson");
        }

        public int PayReview() => wallet.Earn(config.reviewOnTime, "review");
        public int PayExam() => wallet.Earn(config.examPassed, "exam");
        public int PayDaily(int amount) => wallet.Earn(amount, "daily-study");
    }
}
