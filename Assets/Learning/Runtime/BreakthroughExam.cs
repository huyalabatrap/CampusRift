using System;
using System.Collections.Generic;

namespace CampusRift.Learning
{
    // One sitting of the breakthrough exam (P07-T04). The clock runs on unscaled time so pausing the game does not stop it;
    // the caller supplies that time so the rules stay testable.
    public sealed class BreakthroughExam
    {
        public const int RewardLinhThach = 300, RetryMinutes = 30;
        public readonly CourseData Course;
        public readonly QuizSession Session;
        public readonly float StartedAt;
        readonly Func<float> now;
        internal bool Committed;
        public BreakthroughExam(CourseData course, QuizSession session, Func<float> unscaledNow)
        { Course = course; Session = session; now = unscaledNow; StartedAt = unscaledNow(); }
        public float TotalSeconds => Course.examMinutes * 60f;
        public float Remaining => Math.Max(0, StartedAt + TotalSeconds - now());
        public bool Expired => Remaining <= 0;

        // The exam pool: the chapter's exam bank plus the questions of every lesson in the chapter, each question once.
        public static List<QuestionData> Pool(CourseData course)
        {
            var seen = new HashSet<string>(); var list = new List<QuestionData>();
            void Add(QuestionBankData bank)
            {
                if (bank == null) return;
                foreach (var q in bank.questions) if (q != null && !string.IsNullOrEmpty(q.id) && GraderRegistry.Default.Supports(q) && seen.Add(q.id)) list.Add(q);
            }
            Add(course.examBank);
            foreach (var l in course.lessons) if (l != null) Add(l.questionBank);
            return list;
        }
    }

    public sealed class ExamOutcome
    {
        public bool passed, abandoned, timedOut;
        public float percent;
        public int correct, total;
        public bool breakthrough;
        public int linhThach;
        public DateTime? retryAt;
        public QuizResult result;
    }
}
