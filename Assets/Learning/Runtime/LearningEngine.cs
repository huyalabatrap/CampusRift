using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CampusRift.Progression;
namespace CampusRift.Learning
{
    // Rules are independent of UI, scene objects, disk and input devices. Tu Vi (plan §8.2) goes to the optional
    // CultivationService; without one the engine still records reading and quiz progress.
    public sealed partial class LearningEngine
    {
        public const int PracticeQuestions = 10, PracticeMinutes = 5;
        public const float PracticeTuViPerAnswer = 2f, PracticeDailyCap = 150f;
        public LearningCatalog Catalog {get;}
        LearningProgress progress;
        public LearningProgress Progress { get => store is ProfileLearningStore ? store.Load() : progress; private set => progress = value; }
        public CultivationService Cultivation {get;}
        public event Action Changed;
        public event Action<StudyAward> Awarded;
        // Set by the profile so a passed exam pays 300 Linh Thạch (the shop arrives with P08).
        public Action<int> LinhThachSink;
        // Linh Thạch rules (P08); null in tests that only care about learning.
        public StudyEconomy Economy;
        // What the latest submission paid, for the result screen.
        public LinhThachAward LastLinhThach {get;private set;}
        public Func<float> TimeSource;
        public string SaveError {get;private set;}
        readonly ILearningStore store;
        readonly ILearningClock clock;
        readonly System.Random random;
        QuizSession active;
        public LearningEngine(LearningCatalog catalog,ILearningStore store,int? seed=null,CultivationService cultivation=null,ILearningClock clock=null)
        {
            Catalog=catalog;this.store=store;Cultivation=cultivation;this.clock=clock??new SystemLearningClock();
            Progress=store.Load();random=seed.HasValue?new System.Random(seed.Value):new System.Random();
            ExpireInterruptedExams();
        }

        // ---------------------------------------------------------------- time
        // The clock never goes backwards: a player who sets the system time back keeps the latest time the game has seen.
        public DateTime Now
        {
            get
            {
                var t=clock.UtcNow;
                if(DateTime.TryParse(Progress.lastSeenUtc,null,System.Globalization.DateTimeStyles.RoundtripKind,out var last) && last>t)return last;
                Progress.lastSeenUtc=t.ToString("o");return t;
            }
        }
        QuizSession Timed(QuizSession s){if(TimeSource!=null){s.TimeSource=TimeSource;s.ResetTimer();}s.AnsweredRecord+=RecordNotebookAnswer;return s;}
        // Linh Thạch of the answers, paid before the correct-answer log learns about them (the 24-hour rule needs the old log).
        LinhThachAward PayAnswers(QuizResult result){LastLinhThach=Economy!=null?Economy.PayAnswers(Progress,result,Now):new LinhThachAward();return LastLinhThach;}
        static string Stamp(DateTime t)=>t.ToString("o");
        static DateTime? Parse(string value)=>DateTime.TryParse(value,null,System.Globalization.DateTimeStyles.RoundtripKind,out var t)?t:(DateTime?)null;

        // ---------------------------------------------------------------- lessons
        // Lessons open in order of the chapter: a lesson needs its prerequisites mastered, nothing else.
        public bool CourseAvailable(CourseData c)=>Catalog.courses.Contains(c);
        public bool LessonCompleted(LessonData l) => l != null && (DevMode.Active || Progress.Lesson(l.id).completed);
        public CourseData CurrentExamCourse => Catalog.courses.Find(c => CanTakeExam(c, out _)) ?? Catalog.courses.Find(c => ChapterRealm(c) == Cultivation?.RealmIndex);
        public bool Available(CourseData c,LessonData l)=>CourseAvailable(c) && c.lessons.Contains(l) &&
            (DevMode.Active || l.prerequisiteLessonIds.All(id=>Progress.Lesson(id).rewarded));
        // Returns the Tu Vi granted by finishing the last page for the first time, otherwise null.
        public StudyAward ReadPage(CourseData c,LessonData l,int page)
        {
            if(!Available(c,l) || page<0 || page>=l.pages.Count)throw new InvalidOperationException("Lesson unavailable.");
            var p=Progress.Lesson(l.id);
            if(!DevMode.Active && page>p.pagesRead)throw new InvalidOperationException("Read the preceding page first.");
            p.pagesRead=Math.Max(p.pagesRead,page+1);p.completed=p.pagesRead>=l.pages.Count;
            StudyAward award=null;
            if(p.completed && !p.readRewarded){p.readRewarded=true;award=StudyRewards.FirstReading(Cultivation,Catalog,c);Daily.Lesson();}
            Save();if(award!=null)Awarded?.Invoke(award);return award;
        }
        public QuizSession StartQuiz(CourseData c,LessonData l)
        {
            if(!Available(c,l) || !DevMode.Active && !Progress.Lesson(l.id).completed)throw new InvalidOperationException("Complete the lesson first.");
            active=new QuizSession(c,l,Progress.Lesson(l.id).lastQuestionOrder,random,GraderRegistry.Default);Timed(active);
            Progress.Lesson(l.id).lastQuestionOrder=active.Signature;Save();return active;
        }
        // The first attempt pays 70/85 of the lesson's share scaled by the score; later attempts only improve the record.
        public StudyAward Submit(QuizSession session)
        {
            if(session==null || session!=active || session.Committed || session.Kind!=QuizKind.Lesson)throw new InvalidOperationException("Quiz expired or already submitted.");
            var result=session.Result();session.Committed=true;active=null;
            var p=Progress.Lesson(session.Lesson.id);p.attempts++;p.lastResult=result;p.bestPercent=Math.Max(p.bestPercent,result.Percent);
            if(result.passed && !p.rewarded){p.rewarded=true;ReviewScheduler.OnFirstMastery(p,Now);}
            PayAnswers(result);if(Economy!=null){LastLinhThach.lesson=Economy.PayLesson(p,result.Percent,result.passed);LastLinhThach.total+=LastLinhThach.lesson;}
            LogCorrect(result);
            StudyAward award=null;
            if(!p.quizRewarded){p.quizRewarded=true;award=StudyRewards.FirstQuizAttempt(Cultivation,Catalog,session.Course,result.Percent);}
            Save();if(award!=null)Awarded?.Invoke(award);return award;
        }

        // ---------------------------------------------------------------- spaced review
        public List<KeyValuePair<CourseData,LessonData>> DueReviews()
        {
            var now=Now;var list=new List<KeyValuePair<CourseData,LessonData>>();
            foreach(var c in Catalog.courses)if(c!=null)foreach(var l in c.lessons)
                if(l!=null && (DevMode.Active || ReviewScheduler.IsDue(Progress.Lesson(l.id),now)))list.Add(new KeyValuePair<CourseData,LessonData>(c,l));
            return list;
        }
        public int DueReviewCount=>DueReviews().Count;
        // Display-only query: opening the Hub must not mutate profile timestamps or
        // create empty lesson entries. Study actions keep using the committed clock.
        public int DueReviewCountReadOnly
        {
            get
            {
                var now=clock.UtcNow;var last=Parse(Progress.lastSeenUtc);
                if(last.HasValue&&last.Value>now)now=last.Value;
                int count=0;
                foreach(var course in Catalog.courses)foreach(var lesson in course.lessons)
                    if(lesson!=null&&ReviewScheduler.IsDue(Progress.lessons.Find(p=>p.id==lesson.id),now))count++;
                return count;
            }
        }
        public QuizSession StartReview(CourseData c,LessonData l)
        {
            if(!DevMode.Active && !ReviewScheduler.IsDue(Progress.Lesson(l.id),Now))throw new InvalidOperationException("This lesson is not due for review.");
            var pool=l.questionBank.questions.Where(q=>q!=null && q.lessonId==l.id).ToList();
            active=new QuizSession(QuizKind.Review,c,l,pool,Math.Min(ReviewScheduler.QuestionCount,pool.Count),ReviewScheduler.PassPercent,null,random,GraderRegistry.Default);Timed(active);
            return active;
        }
        // A passed review raises the badge and pays 10% of the lesson's share.
        public StudyAward SubmitReview(QuizSession session)
        {
            if(session==null || session!=active || session.Committed || session.Kind!=QuizKind.Review)throw new InvalidOperationException("Review expired or already submitted.");
            var result=session.Result();session.Committed=true;active=null;
            var p=Progress.Lesson(session.Lesson.id);PayAnswers(result);LogCorrect(result);
            bool passed=ReviewScheduler.Record(p,result.passed,Now);
            if(passed)Daily.Review();
            if(passed && Economy!=null){LastLinhThach.review=Economy.PayReview();LastLinhThach.total+=LastLinhThach.review;}
            StudyAward award=passed?StudyRewards.OnTimeReview(Cultivation,Catalog,session.Course):null;
            Save();if(award!=null)Awarded?.Invoke(award);return award;
        }

        // ---------------------------------------------------------------- quick practice
        void LogCorrect(QuizResult result)
        {
            CampusRift.Progression.LocalTelemetry.Quiz(result);
            RecordStudyAnswers(result);
            var now=Now;var cutoff=now.AddHours(-48);
            Progress.correctLog.RemoveAll(e=>{var t=Parse(e.utc);return !t.HasValue || t.Value<cutoff;});
            foreach(var a in result.answers)if(a.correct){Progress.correctLog.RemoveAll(e=>e.question==a.questionId);Progress.correctLog.Add(new CorrectAnswer{question=a.questionId,utc=Stamp(now)});}
        }
        // Questions of lessons whose reading is finished, minus those answered correctly in the last 24 hours.
        public List<QuestionData> PracticePool()
        {
            var now=Now;var recent=new HashSet<string>(Progress.correctLog.Where(e=>{var t=Parse(e.utc);return t.HasValue && t.Value>now.AddHours(-24);}).Select(e=>e.question));
            var list=new List<QuestionData>();var seen=new HashSet<string>();
            foreach(var c in Catalog.courses)if(c!=null)foreach(var l in c.lessons)
            {
                if(l==null || l.questionBank==null || !DevMode.Active && !Progress.Lesson(l.id).completed)continue;
                foreach(var q in l.questionBank.questions)
                    if(q!=null && q.lessonId==l.id && GraderRegistry.Default.Supports(q) && (DevMode.Active || !recent.Contains(q.id)) && seen.Add(q.id))list.Add(q);
            }
            return list;
        }
        public float PracticeTuViToday{get{string today=Now.ToString("yyyy-MM-dd");return Progress.practiceDate==today?Progress.practiceTuVi:0;}}
        public float PracticeTuViLeft=>Math.Max(0,PracticeDailyCap-PracticeTuViToday);
        public QuizSession StartPractice()
        {
            var pool=PracticePool();if(pool.Count==0)return null;
            active=new QuizSession(QuizKind.Practice,null,null,pool,Math.Min(PracticeQuestions,pool.Count),0,null,random,GraderRegistry.Default);Timed(active);
            return active;
        }
        // 2 Tu Vi per correct answer, at most 150 a day.
        public StudyAward SubmitPractice(QuizSession session)
        {
            if(session==null || session!=active || session.Committed || session.Kind!=QuizKind.Practice)throw new InvalidOperationException("Practice expired or already submitted.");
            var result=session.Result();session.Committed=true;active=null;
            string today=Now.ToString("yyyy-MM-dd");if(Progress.practiceDate!=today){Progress.practiceDate=today;Progress.practiceTuVi=0;}
            float amount=Math.Max(0,Math.Min(result.correct*PracticeTuViPerAnswer,PracticeDailyCap-Progress.practiceTuVi));
            Progress.practiceTuVi+=amount;
            PayAnswers(result);LogCorrect(result);
            var award=StudyRewards.Grant(Cultivation,amount,StudyRewards.SourcePractice);
            Save();if(award!=null)Awarded?.Invoke(award);return award;
        }

        // ---------------------------------------------------------------- breakthrough exam (plan §8.3)
        public int ChapterRealm(CourseData c)=>c.chapterIndex>0?c.chapterIndex-1:Math.Max(0,Catalog.courses.IndexOf(c));
        public DateTime? ExamLockedUntil(CourseData c){if(DevMode.Active)return null;var t=Parse(Progress.Exam(c.id).nextAttemptUtc);return t.HasValue && t.Value>Now?t:null;}
        public bool CanTakeExam(CourseData c,out string reason)
        {
            reason=null;
            if (DevMode.Active)
            {
                if (c == null || !CourseAvailable(c)) { reason = "Chapter unavailable."; return false; }
                if (BreakthroughExam.Pool(c).Count < c.examSize) { reason = "Not enough exam questions."; return false; }
                return true;
            }
            if(Cultivation==null){reason="No cultivation.";return false;}
            if(Cultivation.RealmIndex!=ChapterRealm(c)){reason="This exam belongs to another realm.";return false;}
            if(!Cultivation.CanAttemptBreakthrough){reason="Fill tier 5 first.";return false;}
            var lockedUntil=ExamLockedUntil(c);
            if(lockedUntil.HasValue){reason="Retry in "+(lockedUntil.Value-Now).ToString(@"mm\:ss");return false;}
            if(BreakthroughExam.Pool(c).Count<c.examSize){reason="Not enough exam questions.";return false;}
            return true;
        }
        public BreakthroughExam StartExam(CourseData c,Func<float> unscaledNow)
        {
            if(!CanTakeExam(c,out var reason))throw new InvalidOperationException(reason);
            var session=new QuizSession(QuizKind.Exam,c,null,BreakthroughExam.Pool(c),c.examSize,c.examPassPercent,null,random,GraderRegistry.Default);Timed(session);
            // Marked in progress at once: leaving or restarting the game mid-exam counts as a failed attempt.
            var entry=Progress.Exam(c.id);entry.inProgress=true;Save();
            return new BreakthroughExam(c,session,unscaledNow);
        }
        public ExamOutcome SubmitExam(BreakthroughExam exam)
        {
            if(exam==null || exam.Committed)throw new InvalidOperationException("Exam already submitted.");
            bool timedOut=!exam.Session.Complete;
            if(timedOut)exam.Session.FinishUnanswered();
            var result=exam.Session.Result();exam.Committed=true;
            var entry=Progress.Exam(exam.Course.id);entry.inProgress=false;entry.attempts++;entry.best=Math.Max(entry.best,result.Percent);
            PayAnswers(result);LogCorrect(result);
            var outcome=new ExamOutcome{passed=result.passed,timedOut=timedOut,percent=result.Percent,correct=result.correct,total=result.total,result=result};
            if(result.passed)
            {
                entry.passed=true;entry.nextAttemptUtc=null;
                outcome.breakthrough=Cultivation!=null && Cultivation.CompleteBreakthrough();
                outcome.linhThach=Economy!=null?Economy.PayExam():BreakthroughExam.RewardLinhThach;if(Economy==null)LinhThachSink?.Invoke(outcome.linhThach);LastLinhThach.exam=outcome.linhThach;LastLinhThach.total+=outcome.linhThach;
            }
            else {var retry=Now.AddMinutes(BreakthroughExam.RetryMinutes);entry.nextAttemptUtc=Stamp(retry);outcome.retryAt=retry;}
            Save();return outcome;
        }
        // Leaving the exam early is a failure without a reward.
        public ExamOutcome AbandonExam(BreakthroughExam exam)
        {
            if(exam==null || exam.Committed)return null;
            exam.Committed=true;
            var entry=Progress.Exam(exam.Course.id);entry.inProgress=false;entry.attempts++;
            var retry=Now.AddMinutes(BreakthroughExam.RetryMinutes);entry.nextAttemptUtc=Stamp(retry);
            Save();return new ExamOutcome{abandoned=true,retryAt=retry,total=exam.Session.Questions.Count};
        }
        void ExpireInterruptedExams()
        {
            bool any=false;
            foreach(var e in Progress.exams)if(e.inProgress){e.inProgress=false;e.attempts++;e.nextAttemptUtc=Stamp(Now.AddMinutes(BreakthroughExam.RetryMinutes));any=true;}
            if(any)try{store.Save(Progress);}catch(Exception ex) when(ex is System.IO.IOException || ex is UnauthorizedAccessException){}
        }

        public void Save()
        {
            try{store.Save(Progress);SaveError=null;}
            catch(Exception e) when(e is System.IO.IOException || e is UnauthorizedAccessException){SaveError=e.Message;}
            Changed?.Invoke();
        }
#if UNITY_EDITOR
        public void DebugReset(){active=null;Progress=new LearningProgress();Save();}
        public void DebugPassNext(CourseData course)
        {
            var lesson=course.lessons.Find(l=>!Progress.Lesson(l.id).rewarded && Available(course,l));
            if(lesson==null)return;
            for(int i=0;i<lesson.pages.Count;i++)ReadPage(course,lesson,i);
            var quiz=StartQuiz(course,lesson);
            while(!quiz.Complete)quiz.Answer(quiz.Questions[quiz.Answered].Data.correctOptionIds);
            Submit(quiz);
        }
#endif
    }
}
