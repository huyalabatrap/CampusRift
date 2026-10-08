#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Progression;
using UnityEngine;
namespace CampusRift.Learning
{
    // P07 rules without any UI: content integrity, graders, breakthrough exam, spaced review and quick practice,
    // driven with a fake clock and an in-memory save so nothing of the player's data is touched.
    // Output: Artifacts/Learning/Breakthrough.json and Artifacts/Learning/Breakthrough-DONE.txt.
    public sealed class BreakthroughExamPlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(); }
        sealed class MemoryStore : ILearningStore
        {
            public LearningProgress Data = new LearningProgress();
            public LearningProgress Load() => JsonUtility.FromJson<LearningProgress>(JsonUtility.ToJson(Data));
            public void Save(LearningProgress data) { Data = JsonUtility.FromJson<LearningProgress>(JsonUtility.ToJson(data)); }
        }
        readonly Report report = new Report();
        const string Output = "Artifacts/Learning/";
        static readonly DateTime T0 = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        float clockSeconds;
        void Check(bool ok, string label) { (ok ? report.passed : report.failed).Add(label); Directory.CreateDirectory(Output); File.WriteAllText(Output + "Breakthrough.json", JsonUtility.ToJson(report, true)); Debug.Log("EXAM QA " + (ok ? "PASS " : "FAIL ") + label); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Output);
            try { Run(); } catch (Exception e) { Check(false, e.ToString()); }
            File.WriteAllText(Output + "Breakthrough-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
            yield return null;
            Destroy(gameObject);
        }

        static void AnswerCorrectly(QuizSession s) { while (!s.Complete) s.Answer(s.Questions[s.Answered].Data.correctOptionIds); }
        // Answers so that exactly `wrong` questions are wrong (ordering: reversed, other types: first wrong option).
        static void AnswerWithMistakes(QuizSession s, int wrong)
        {
            int i = 0;
            while (!s.Complete)
            {
                var q = s.Questions[s.Answered].Data;
                if (i++ < wrong)
                {
                    if (q.type == "ordering") { var r = new List<string>(q.correctOptionIds); r.Reverse(); s.Answer(r); }
                    else if(q.type=="matching") s.Answer(q.correctOptionIds.Select(p=>p.Split(':')[0]+":"+(p.EndsWith(":1")?"2":"1")).ToArray());
                    else s.Answer(q.options.First(o => !q.correctOptionIds.Contains(o.id)).id);
                }
                else s.Answer(q.correctOptionIds);
            }
        }

        void Run()
        {
            var catalog = Resources.Load<LearningCatalog>("LearningCatalog");
            // ---- content (T07/T08/T10) ----
            Check(catalog.courses.Count == 6, "The catalog holds the six textbook chapters");
            Check(!catalog.courses.Any(c => c.id.Contains("algorithm")), "No Algorithms course is left in the catalog");
            bool chapters = true, lessons = true, sources = true; string detail = "";
            foreach (var c in catalog.courses)
            {
                if (BreakthroughExam.Pool(c).Count < 40) { chapters = false; detail += c.id + " "; }
                foreach (var l in c.lessons)
                {
                    var qs = l.questionBank.questions.Where(q => q.lessonId == l.id).ToList();
                    if (qs.Count < 15) { lessons = false; detail += l.id + " "; }
                    foreach (var q in qs) if (string.IsNullOrWhiteSpace(q.explanation) || string.IsNullOrWhiteSpace(q.source) || !q.source.StartsWith("GT 2021, tr.")) sources = false;
                }
            }
            Check(chapters, "Every chapter has at least 40 exam questions " + detail);
            Check(lessons, "Every lesson has at least 15 questions " + detail);
            Check(sources, "Every question has an explanation and a textbook page");
            for (int i = 0; i < catalog.courses.Count; i++) if (catalog.courses[i].chapterIndex != i + 1) { Check(false, "Chapter order"); break; }
            Check(catalog.courses.Select((c, i) => c.chapterIndex == i + 1).All(x => x), "Chapter k is course number k (chapter k opens realm k)");

            // ---- graders (T02) ----
            var tf = catalog.courses.SelectMany(c => c.lessons).SelectMany(l => l.questionBank.questions).First(q => q.type == "true-false");
            var ord = catalog.courses.SelectMany(c => c.lessons).SelectMany(l => l.questionBank.questions).First(q => q.type == "ordering" && q.options.Count >= 3);
            var single = catalog.courses.SelectMany(c => c.lessons).SelectMany(l => l.questionBank.questions).First(q => q.type == "single-choice");
            var grader = GraderRegistry.Default;
            Check(grader.Grade(single, single.correctOptionIds) && !grader.Grade(single, new[] { single.options.First(o => !single.correctOptionIds.Contains(o.id)).id }), "Single choice: right and wrong");
            Check(grader.Grade(tf, tf.correctOptionIds) && !grader.Grade(tf, new[] { tf.correctOptionIds[0] == "true" ? "false" : "true" }), "True/false: right and wrong");
            Check(grader.Grade(ord, ord.correctOptionIds), "Ordering: the right order is accepted");
            var swapped = new List<string>(ord.correctOptionIds); var t = swapped[0]; swapped[0] = swapped[1]; swapped[1] = t;
            Check(!grader.Grade(ord, swapped), "Ordering: one wrong position is a wrong answer");
            Check(!grader.Grade(ord, ord.correctOptionIds.Take(ord.correctOptionIds.Count - 1).ToList()), "Ordering: an incomplete arrangement is wrong");
            Check(!grader.Supports(new QuestionData { type = "unknown-type", options = single.options, correctOptionIds = single.correctOptionIds }), "An unknown question type is refused, not guessed");

            // ---- lesson quiz ----
            var course = catalog.courses[0]; var lesson = course.lessons[0];
            var store = new MemoryStore(); var clock = new FakeLearningClock(T0);
            var cult = new CultivationService(new CultivationData());
            int linh = 0;
            var engine = new LearningEngine(catalog, store, 7, cult, clock) { LinhThachSink = n => linh += n };
            for (int p = 0; p < lesson.pages.Count; p++) engine.ReadPage(course, lesson, p);
            var quiz = engine.StartQuiz(course, lesson);
            Check(quiz.Questions.Count == 10, "A lesson quiz asks 10 questions");
            AnswerWithMistakes(quiz, 3); engine.Submit(quiz);
            Check(!engine.Progress.Lesson(lesson.id).rewarded, "70% is below the pass mark: the lesson is not mastered");
            quiz = engine.StartQuiz(course, lesson); AnswerCorrectly(quiz); engine.Submit(quiz);
            Check(engine.Progress.Lesson(lesson.id).rewarded, "Full marks masters the lesson");

            // ---- spaced review Bronze → Silver → Gold (T05) ----
            var pr = engine.Progress.Lesson(lesson.id);
            Check(pr.mastery == ReviewScheduler.None && engine.DueReviewCount == 0, "A new mastery has no badge and is not due at once");
            clock.Advance(TimeSpan.FromHours(23)); Check(engine.DueReviewCount == 0, "Not due after 23 hours");
            clock.Advance(TimeSpan.FromHours(2)); Check(engine.DueReviewCount == 1, "Due after one day");
            var review = engine.StartReview(course, lesson); Check(review.Questions.Count == 5, "A review asks 5 questions");
            AnswerWithMistakes(review, 3); engine.SubmitReview(review);
            Check(pr.mastery == ReviewScheduler.None && engine.DueReviewCount == 0, "A failed review keeps the badge and waits another day");
            clock.Advance(TimeSpan.FromDays(1.1));
            review = engine.StartReview(course, lesson); AnswerCorrectly(review); float tuBefore = cult.Data.totalEarned; engine.SubmitReview(review);
            Check(pr.mastery == ReviewScheduler.Bronze && cult.Data.totalEarned > tuBefore, "A passed review gives the Bronze badge and a little Tu Vi");
            clock.Advance(TimeSpan.FromDays(2.9)); Check(engine.DueReviewCount == 0, "Silver review is not due after 3 days minus a bit");
            clock.Advance(TimeSpan.FromDays(0.2));
            review = engine.StartReview(course, lesson); AnswerCorrectly(review); engine.SubmitReview(review);
            Check(pr.mastery == ReviewScheduler.Silver, "The second review gives Silver");
            clock.Advance(TimeSpan.FromDays(7.1));
            review = engine.StartReview(course, lesson); AnswerCorrectly(review); engine.SubmitReview(review);
            Check(pr.mastery == ReviewScheduler.Gold && engine.DueReviewCount == 0, "The third review gives Gold and ends the schedule");

            // ---- the clock cannot go backwards ----
            var before = engine.Now; clock.Now = before.AddDays(-30);
            Check(engine.Now >= before, "Setting the system clock back does not move the game clock back");
            clock.Now = before;

            // ---- quick practice (T06) ----
            var pool = engine.PracticePool();
            Check(pool.Count > 0 && pool.All(q => q.lessonId == lesson.id), "Practice only offers questions of lessons already read");
            var practice = engine.StartPractice(); Check(practice.Questions.Count == 10, "Practice asks 10 questions");
            var practiceIds = new HashSet<string>(practice.Questions.Select(q => q.Data.id));
            AnswerCorrectly(practice); float practiceBefore = cult.Data.totalEarned; var award = engine.SubmitPractice(practice);
            Check(Mathf.Abs(cult.Data.totalEarned - practiceBefore - 20) < 0.01f, "10 correct answers give 20 Tu Vi");
            var next = engine.PracticePool();
            Check(!next.Any(q => practiceIds.Contains(q.id)), "Questions answered correctly are not offered again within 24 hours");
            clock.Advance(TimeSpan.FromHours(25));
            Check(engine.PracticePool().Any(q => practiceIds.Contains(q.id)), "They come back after 24 hours");
            // The daily cap: 150 Tu Vi.
            engine.Progress.practiceDate = engine.Now.ToString("yyyy-MM-dd"); engine.Progress.practiceTuVi = 148;
            practice = engine.StartPractice(); AnswerCorrectly(practice); float capBefore = cult.Data.totalEarned; engine.SubmitPractice(practice);
            Check(Mathf.Abs(cult.Data.totalEarned - capBefore - 2) < 0.01f && engine.PracticeTuViLeft < 0.01f, "Practice stops paying at 150 Tu Vi a day");

            // ---- breakthrough exam (T04) ----
            var exam0 = catalog.courses[0];
            Check(!engine.CanTakeExam(exam0, out var why) && !string.IsNullOrEmpty(why), "The exam is closed before the bottleneck: " + why);
            cult.SetState(Realm.LuyenKhi, 5, 100);
            Check(engine.CanTakeExam(exam0, out _), "The exam opens at the bottleneck");
            Check(!engine.CanTakeExam(catalog.courses[1], out _), "Another chapter's exam stays closed");
            var exam = engine.StartExam(exam0, () => clockSeconds);
            Check(exam.Session.Questions.Count == 20 && exam0.examMinutes == 20 && exam0.examPassPercent == 80, "The exam has 20 questions, 20 minutes and an 80% pass mark");
            Check(engine.Progress.Exam(exam0.id).inProgress, "The exam is marked in progress at the start");
            AnswerWithMistakes(exam.Session, 5); var fail = engine.SubmitExam(exam);
            Check(!fail.passed && !fail.breakthrough && linh == 0 && cult.Realm == Realm.LuyenKhi, "75% fails: no breakthrough and no Linh Thạch");
            Check(fail.retryAt.HasValue && !engine.CanTakeExam(exam0, out var lockMsg) && engine.ExamLockedUntil(exam0).HasValue, "A failed exam locks the retry");
            clock.Advance(TimeSpan.FromMinutes(29)); Check(!engine.CanTakeExam(exam0, out _), "Still locked after 29 minutes");
            clock.Advance(TimeSpan.FromMinutes(2)); Check(engine.CanTakeExam(exam0, out _), "Unlocked after 30 minutes");
            // Abandoning counts as failing.
            exam = engine.StartExam(exam0, () => clockSeconds); var ab = engine.AbandonExam(exam);
            Check(ab.abandoned && !engine.CanTakeExam(exam0, out _) && !engine.Progress.Exam(exam0.id).inProgress, "Leaving the exam is a failed attempt with a 30-minute lock");
            clock.Advance(TimeSpan.FromMinutes(31));
            // Restarting the game in the middle of an exam counts as failing.
            exam = engine.StartExam(exam0, () => clockSeconds);
            var afterCrash = new LearningEngine(catalog, store, 7, cult, clock);
            Check(!afterCrash.CanTakeExam(exam0, out _) && !afterCrash.Progress.Exam(exam0.id).inProgress, "Quitting mid-exam counts as a failure after restart");
            clock.Advance(TimeSpan.FromMinutes(31)); engine = afterCrash; engine.LinhThachSink = n => linh += n;
            // Timeout: answers so far are graded, the rest are wrong.
            clockSeconds = 0; exam = engine.StartExam(exam0, () => clockSeconds);
            exam.Session.Answer(exam.Session.Questions[0].Data.correctOptionIds); clockSeconds = 20 * 60 + 1;
            Check(exam.Expired && exam.Remaining == 0, "The exam clock runs out after 20 minutes");
            var timeout = engine.SubmitExam(exam);
            Check(timeout.timedOut && !timeout.passed && timeout.total == 20 && timeout.correct == 1, "Time out: unanswered questions count as wrong");
            clock.Advance(TimeSpan.FromMinutes(31));
            // Pass.
            clockSeconds = 0; exam = engine.StartExam(exam0, () => clockSeconds);
            AnswerWithMistakes(exam.Session, 4); var pass = engine.SubmitExam(exam);
            Check(pass.passed && pass.breakthrough && cult.Realm == Realm.TrucCo && cult.Tier == 1, "80% passes: Trúc Cơ 1");
            Check(linh == BreakthroughExam.RewardLinhThach && pass.linhThach == 300, "A pass pays 300 Linh Thạch");
            Check(engine.Progress.Exam(exam0.id).passed && engine.ExamLockedUntil(exam0) == null, "The pass is recorded and clears the lock");

            var saved = JsonUtility.FromJson<LearningProgress>(JsonUtility.ToJson(store.Data));
            Check(saved.exams.Count == 1 && saved.lessons.Count > 0 && !string.IsNullOrEmpty(saved.lastSeenUtc), "Exam, review and clock data survive a save round-trip");
        }
    }
}
#endif
