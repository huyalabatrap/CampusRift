using System;
using System.Linq;
using CampusRift.Progression;
namespace CampusRift.Learning
{
    public sealed partial class LearningEngine
    {
        // AR only needs single-choice runes. Stable source IDs/options remain untouched.
        public QuizSession StartARKnowledge(int seed)
        {
            var pool=Catalog.courses.Where(c=>c!=null).SelectMany(c=>c.lessons).Where(l=>l!=null&&l.questionBank!=null)
                .SelectMany(l=>l.questionBank.questions).Where(q=>q!=null&&q.type=="single-choice"&&q.options.Count>=3&&q.options.Count<=4&&GraderRegistry.Default.Supports(q))
                .GroupBy(q=>q.id).Select(g=>g.First()).ToList();
            return pool.Count==0?null:new QuizSession(QuizKind.Shrine,null,null,pool,1,100,null,new Random(seed),GraderRegistry.Default);
        }
        public int SubmitARKnowledge(QuizSession session,ProfileService owner,bool permitted)
        {
            if(session==null||!session.Complete||session.Committed)return 0;
            session.Committed=true;
            if(!permitted||owner==null||owner.Transient||DevMode.Active)return 0;
            var result=session.Result();var now=Now;
            var award=Economy.PayAnswers(Progress,result,now,AR.ARProgression.Available(owner.Data,now),amount=>AR.ARProgression.Claim(owner,amount,now));
            foreach(var answer in result.answers)RecordNotebookAnswer(answer);
            // No lesson/EXP/daily-study bonus: every AR payout goes through the shared AR cap.
            var cutoff=now.AddHours(-48);
            Progress.correctLog.RemoveAll(e=>{var t=Parse(e.utc);return !t.HasValue||t.Value<cutoff;});
            foreach(var answer in result.answers)if(answer.correct)
            {Progress.correctLog.RemoveAll(e=>e.question==answer.questionId);Progress.correctLog.Add(new CorrectAnswer{question=answer.questionId,utc=Stamp(now)});}
            Save();return award.total;
        }
    }
}
