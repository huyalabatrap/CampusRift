using System;
using System.Collections.Generic;
using System.Linq;
namespace CampusRift.Learning
{
    public interface IQuestionGrader
    {
        bool Supports(QuestionData question);
        bool Grade(QuestionData question, IReadOnlyList<string> selected);
    }
    public sealed class SingleChoiceGrader : IQuestionGrader
    {
        public bool Supports(QuestionData q) => q.type=="single-choice" && q.options!=null && q.options.Count>=2 &&
            q.options.All(o=>o!=null && !string.IsNullOrWhiteSpace(o.id)) && q.options.Select(o=>o.id).Distinct().Count()==q.options.Count &&
            q.correctOptionIds!=null && q.correctOptionIds.Count==1 && q.options.Any(o=>o.id==q.correctOptionIds[0]);
        public bool Grade(QuestionData q,IReadOnlyList<string> selected) => selected.Count==1 && selected[0]==q.correctOptionIds[0];
    }
    public sealed class QuizQuestion
    {
        public QuestionData Data {get;}
        public IReadOnlyList<AnswerOption> Options {get;}
        public QuizQuestion(QuestionData data,List<AnswerOption> options){Data=data;Options=options.AsReadOnly();}
    }
    public enum QuizKind { Lesson, Exam, Review, Practice, Notebook, Shrine }
    public sealed class QuizSession
    {
        public readonly CourseData Course;
        public readonly LessonData Lesson;
        public QuizKind Kind {get;}
        public int PassPercent {get;}
        public IReadOnlyList<QuizQuestion> Questions {get;}
        public int Answered => answers.Count;
        public bool Complete => Answered==Questions.Count;
        internal bool Committed;
        readonly List<AnswerRecord> answers=new List<AnswerRecord>();
        readonly IQuestionGrader grader;
        public event System.Action<AnswerRecord> AnsweredRecord;
        // Time of the last question shown; answers record the seconds since then. Tests replace the source.
        public Func<float> TimeSource = () => UnityEngine.Time.realtimeSinceStartup;
        float lastTick;
        public string Signature => string.Join("|",Questions.Select(q=>q.Data.id));
        // Lesson quiz: questions of the lesson's own bank.
        public QuizSession(CourseData course,LessonData lesson,string previous,Random random,IQuestionGrader grader)
            :this(QuizKind.Lesson,course,lesson,lesson.questionBank.questions.Where(q=>q!=null && q.lessonId==lesson.id).ToList(),lesson.quizSize,lesson.passPercent,previous,random,grader){}
        // Any mix of questions (exam, review, practice).
        public QuizSession(QuizKind kind,CourseData course,LessonData lesson,IList<QuestionData> source,int size,int passPercent,string previous,Random random,IQuestionGrader grader)
        {
            Kind=kind;Course=course;Lesson=lesson;this.grader=grader;PassPercent=passPercent;
            var pool=source.Where(q=>q!=null && grader.Supports(q)).ToList();
            if(size<1 || pool.Count<size || pool.Select(q=>q.id).Distinct().Count()!=pool.Count)
                throw new InvalidOperationException("Question bank needs enough supported questions with unique IDs.");
            Shuffle(pool,random);
            var chosen=pool.Take(size).ToList();
            if(string.Join("|",chosen.Select(q=>q.id))==previous)
            {
                if(pool.Count>chosen.Count) chosen[chosen.Count-1]=pool[chosen.Count];
                else if(chosen.Count>1) {var first=chosen[0];chosen.RemoveAt(0);chosen.Add(first);}
            }
            Questions=chosen.Select(q=>new QuizQuestion(q,Arrange(q,random))).ToList().AsReadOnly();
            lastTick=TimeSource();
        }
        // True/false keeps its fixed order; other types are shuffled (an ordering question never starts solved).
        static List<AnswerOption> Arrange(QuestionData q,Random random)
        {
            var options=q.options.ToList();
            if(q.type=="true-false"){options.Sort((a,b)=>a.id=="true"?-1:b.id=="true"?1:0);return options;}
            for(int attempt=0;attempt<8;attempt++)
            {
                Shuffle(options,random);
                if(q.type!="ordering" || !options.Select(o=>o.id).SequenceEqual(q.correctOptionIds))break;
            }
            return options;
        }
        static void Shuffle<T>(List<T> values,Random random)
        { for(int i=values.Count-1;i>0;i--){int j=random.Next(i+1);T value=values[i];values[i]=values[j];values[j]=value;} }
        public void ResetTimer(){lastTick=TimeSource();}
        public AnswerRecord Answer(string optionId)=>Answer(new[]{optionId});
        public AnswerRecord Answer(IReadOnlyList<string> selected)
        {
            if(Complete)throw new InvalidOperationException("Quiz is already complete.");
            var q=Questions[Answered].Data;
            if(selected==null || selected.Count==0 || selected.Any(id=>q.type=="matching"?!MatchingGrader.PairValid(q,id):q.options.Find(x=>x.id==id)==null))throw new ArgumentException("Unknown answer ID.");
            bool ordering=q.type=="ordering";
            string Text(string id)=>q.options.Find(x=>x.id==id).text;
            string separator=ordering?" → ":", ";
            var record=new AnswerRecord {questionId=q.id,prompt=q.prompt,type=q.type,source=q.source,
                selected=QuestionAnswerText.Format(q,selected),
                expected=QuestionAnswerText.Format(q,q.correctOptionIds),
                correct=grader.Grade(q,selected),explanation=q.explanation};
            float now=TimeSource();record.seconds=Math.Max(0,now-lastTick);lastTick=now;
            answers.Add(record);AnsweredRecord?.Invoke(record);return record;
        }
        // Time ran out: every unanswered question counts as wrong.
        public void FinishUnanswered()
        {
            while(!Complete)
            {
                var q=Questions[Answered].Data;
                bool ordering=q.type=="ordering";
                var missing=new AnswerRecord{questionId=q.id,prompt=q.prompt,type=q.type,source=q.source,selected="",
                    expected=QuestionAnswerText.Format(q,q.correctOptionIds),correct=false,explanation=q.explanation,seconds=999f};
                answers.Add(missing);AnsweredRecord?.Invoke(missing);
            }
        }
        public QuizResult Result()
        {
            if(!Complete)throw new InvalidOperationException("Answer all questions first.");
            int correct=answers.Count(a=>a.correct);
            return new QuizResult {correct=correct,total=Questions.Count,passed=correct*100>=Questions.Count*PassPercent,answers=new List<AnswerRecord>(answers)};
        }
    }
}
