using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace CampusRift.Learning
{
    [Serializable] public sealed class FlashcardProgress { public string id,nextUtc,reviewDate; public bool remembered; public int reviews; }
    [Serializable] public sealed class WrongQuestionProgress { public string id,lastWrongUtc; public int consecutiveCorrect,wrongCount; }
    [Serializable] public sealed class StudyDailyProgress
    {
        public string date,lastStudyDate,restWeek;
        public int lessons,correct,reviews,streak,cycles;
        public bool lessonPaid,correctPaid,reviewPaid;
    }
    public sealed class StudyDaily
    {
        readonly StudyDailyProgress data; readonly Func<DateTime> now; readonly Action<int> pay;
        public StudyDaily(StudyDailyProgress data,Func<DateTime> now,Action<int> pay){this.data=data;this.now=now;this.pay=pay;}
        static string Day(DateTime t)=>t.ToString("yyyy-MM-dd");
        static string Week(DateTime t)=>Day(t.Date.AddDays(-((int)t.DayOfWeek+6)%7));
        public StudyDailyProgress State {get{Refresh();return data;}}
        public bool RestUsedThisWeek=>data.restWeek==Week(now());
        public void Refresh()
        {
            if (Progression.DevMode.Active) return;
            var t=now().Date;string today=Day(t);
            if(data.date==today)return;
            data.date=today;data.lessons=data.correct=data.reviews=0;data.lessonPaid=data.correctPaid=data.reviewPaid=false;
            if(DateTime.TryParse(data.lastStudyDate,out var last) && (t-last.Date).Days>2)data.streak=0;
        }
        void Activity()
        {
            Refresh();var t=now().Date;string today=Day(t);if(data.lastStudyDate==today)return;
            int gap=DateTime.TryParse(data.lastStudyDate,out var last)?(t-last.Date).Days:0;
            if(gap<0)return;
            if(gap==2 && data.restWeek!=Week(t.AddDays(-1)))data.restWeek=Week(t.AddDays(-1));
            else if(gap>1)data.streak=0;
            data.streak++;data.lastStudyDate=today;
            if(data.streak%7==0){data.cycles++;pay?.Invoke(150);}
        }
        public void Lesson(){if(Progression.DevMode.Active)return;Activity();data.lessons++;if(!data.lessonPaid){data.lessonPaid=true;pay?.Invoke(30);}}
        public void Correct(int count){if(Progression.DevMode.Active || count<=0)return;Activity();data.correct+=count;if(data.correct>=20&&!data.correctPaid){data.correctPaid=true;pay?.Invoke(30);}}
        public void Review(){if(Progression.DevMode.Active)return;Activity();data.reviews++;if(!data.reviewPaid){data.reviewPaid=true;pay?.Invoke(30);}}
        public void Cards(){if(Progression.DevMode.Active)return;Activity();}
    }
    public sealed class StudyCard
    {
        public string id,lessonId,front,back,source;
    }
    public sealed partial class LearningEngine
    {
        public StudyDaily Daily => new StudyDaily(Progress.studyDaily??(Progress.studyDaily=new StudyDailyProgress()),()=>Now,amount=>Economy?.PayDaily(amount));
        public List<StudyCard> Cards(LessonData lesson)
        {
            var cards=new List<StudyCard>();if(lesson==null || !Progression.DevMode.Active && !Progress.Lesson(lesson.id).completed)return cards;
            for(int i=0;i<lesson.pages.Count;i++)
            {
                var p=lesson.pages[i];if(string.IsNullOrWhiteSpace(p.takeaway))continue;
                var source=Regex.Match(p.content??"",@"\((?:Giáo trình|GT)\s*2021,\s*tr\.[^)]+\)").Value;
                cards.Add(new StudyCard{id=lesson.id+"/card/"+(i+1),lessonId=lesson.id,front=p.title,back=p.takeaway,source=source});
            }
            return cards.OrderBy(c=>CardProgress(c.id).nextUtc??"").ToList();
        }
        public FlashcardProgress CardProgress(string id)
        {
            if(Progress.flashcards==null)Progress.flashcards=new List<FlashcardProgress>();
            var p=Progress.flashcards.Find(c=>c.id==id);if(p==null){p=new FlashcardProgress{id=id};Progress.flashcards.Add(p);}return p;
        }
        public void ReviewCard(StudyCard card,bool remembered)
        {
            if(!Cards(Catalog.courses.SelectMany(c=>c.lessons).FirstOrDefault(l=>l.id==card.lessonId)).Any(c=>c.id==card.id))throw new InvalidOperationException("Card not available.");
            var p=CardProgress(card.id);p.remembered=remembered;p.reviews++;p.nextUtc=Now.AddMinutes(remembered?1440:5).ToString("o");p.reviewDate=Now.ToString("yyyy-MM-dd");Daily.Cards();
            var lesson=Catalog.courses.SelectMany(c=>c.lessons).First(l=>l.id==card.lessonId);
            var cards=Cards(lesson);if(cards.Count>0&&cards.All(c=>CardProgress(c.id).reviewDate==p.reviewDate))Daily.Lesson();
            Save();
        }
        public List<QuestionData> LearnedPool()=>Catalog.courses.SelectMany(c=>c.lessons).Where(l=>l!=null&&(Progression.DevMode.Active || Progress.Lesson(l.id).completed)&&l.questionBank!=null)
            .SelectMany(l=>l.questionBank.questions.Where(q=>q.lessonId==l.id&&GraderRegistry.Default.Supports(q))).GroupBy(q=>q.id).Select(g=>g.First()).ToList();
        public List<QuestionData> NotebookPool()
        {
            if(Progress.notebook==null)Progress.notebook=new List<WrongQuestionProgress>();
            // Include exam-only questions as well: a mistake already encountered is always reviewable.
            var all=Catalog.courses.SelectMany(c=>c.lessons.Where(l=>l!=null&&l.questionBank!=null).SelectMany(l=>l.questionBank.questions)
                .Concat(c.examBank!=null?c.examBank.questions:Enumerable.Empty<QuestionData>()));
            return all.Where(q=>q!=null&&GraderRegistry.Default.Supports(q)&&Progress.notebook.Any(n=>n.id==q.id)).GroupBy(q=>q.id).Select(g=>g.First()).ToList();
        }
        public QuizSession StartNotebook()
        {
            var pool=NotebookPool();if(pool.Count==0)return null;
            active=Timed(new QuizSession(QuizKind.Notebook,null,null,pool,Math.Min(10,pool.Count),0,null,random,GraderRegistry.Default));return active;
        }
        public QuizSession StartShrine()
        {
            var pool=LearnedPool();if(pool.Count==0)return null;
            active=Timed(new QuizSession(QuizKind.Shrine,null,null,pool,1,100,null,random,GraderRegistry.Default));return active;
        }
        public QuizResult SubmitExtra(QuizSession session)
        {
            if(session==null||session!=active||session.Committed||(session.Kind!=QuizKind.Notebook&&session.Kind!=QuizKind.Shrine))throw new InvalidOperationException("Session expired.");
            var result=session.Result();session.Committed=true;active=null;
            if(session.Kind==QuizKind.Notebook)PayAnswers(result);else LastLinhThach=new LinhThachAward();
            LogCorrect(result);Save();return result;
        }
        void RecordNotebookAnswer(AnswerRecord a)
        {
            if(Progress.notebook==null)Progress.notebook=new List<WrongQuestionProgress>();
                var p=Progress.notebook.Find(n=>n.id==a.questionId);
                if(!a.correct){if(p==null){p=new WrongQuestionProgress{id=a.questionId};Progress.notebook.Add(p);}p.consecutiveCorrect=0;p.wrongCount++;p.lastWrongUtc=Now.ToString("o");}
                else if(p!=null){p.consecutiveCorrect++;if(p.consecutiveCorrect>=2)Progress.notebook.Remove(p);}
            Save();
        }
        void RecordStudyAnswers(QuizResult result)
        {
            Daily.Correct(result.correct);
        }
    }
}
