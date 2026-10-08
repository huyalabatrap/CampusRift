using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace CampusRift.Learning
{
    [Serializable] public sealed class AnswerRecord
    {
        public string questionId, prompt, selected, expected, explanation, type, source;
        public bool correct;
        // Seconds between the question appearing and the answer (Linh Thạch anti-farming, plan §9.2).
        public float seconds;
    }
    [Serializable] public sealed class QuizResult
    {
        public int correct, total;
        public bool passed;
        public float Percent => total > 0 ? 100f * correct / total : 0;
        public List<AnswerRecord> answers = new List<AnswerRecord>();
    }
    [Serializable] public sealed class LessonProgress
    {
        public string id;
        public int pagesRead, attempts;
        public bool completed, rewarded;
        // Tu Vi for the first full reading and the first quiz attempt is paid once per lesson (plan §8.2).
        public bool readRewarded, quizRewarded;
        // Spaced review (plan §8.2): 0 none, 1 Đồng, 2 Bạc, 3 Vàng; the next review falls due at nextReviewUtc (ISO 8601).
        public int mastery;
        public string nextReviewUtc;
        public int reviewsDone;
        // Linh Thạch already paid for finishing the lesson (0, 30 or 50), so a better result only pays the difference.
        public int linhPaid;
        public float bestPercent;
        public string lastQuestionOrder;
        public QuizResult lastResult;
    }
    [Serializable] public sealed class ExamProgress
    {
        public string course;
        public bool passed, inProgress;
        public float best;
        public int attempts;
        public string nextAttemptUtc;
    }
    [Serializable] public sealed class CorrectAnswer { public string question, utc; }
    [Serializable] public sealed class LearningProgress
    {
        public int version = 1, breakthroughs;
        public float survivalSeconds, healthBonus, movementBonus, energyBonus;
        public List<LessonProgress> lessons = new List<LessonProgress>();
        public List<string> unlockedSkills = new List<string>();
        // V2: exams per chapter, the latest time the game has seen (a clock set back never helps), answers given correctly
        // recently (practice only takes questions not answered correctly in the last 24 hours) and the practice Tu Vi of the day.
        public List<ExamProgress> exams = new List<ExamProgress>();
        public string lastSeenUtc;
        public List<CorrectAnswer> correctLog = new List<CorrectAnswer>();
        public string practiceDate;
        public float practiceTuVi;
        public List<FlashcardProgress> flashcards = new List<FlashcardProgress>();
        public List<WrongQuestionProgress> notebook = new List<WrongQuestionProgress>();
        public StudyDailyProgress studyDaily = new StudyDailyProgress();
        // Linh Thạch (P08): questions ever answered correctly, retry income of the current UTC day, and the pause after too-fast answering.
        public List<string> solved = new List<string>();
        public string linhDate;
        public int linhRetryToday;
        public string linhPausedUntilUtc;
        public ExamProgress Exam(string course)
        {
            var value = exams.Find(x => x.course == course);
            if(value == null) { value = new ExamProgress { course = course }; exams.Add(value); }
            return value;
        }
        public LessonProgress Lesson(string id)
        {
            var value = lessons.Find(x => x.id == id);
            if(value == null) { value = new LessonProgress { id = id }; lessons.Add(value); }
            return value;
        }
    }
    public interface ILearningStore
    {
        LearningProgress Load();
        void Save(LearningProgress data);
    }
    public sealed class JsonLearningStore : ILearningStore
    {
        public readonly string Path;
        public string RecoveryMessage { get; private set; }
        bool writeBlocked;
        public JsonLearningStore(string path) { Path = path; }
        public LearningProgress Load()
        {
            foreach(var candidate in new[]{Path, Path+".bak"})
            {
                if(!File.Exists(candidate))continue;
                try
                {
                    var data = JsonUtility.FromJson<LearningProgress>(File.ReadAllText(candidate));
                    if(data == null || data.version != 1 || data.lessons == null || data.unlockedSkills == null || data.breakthroughs < 0)
                        throw new InvalidDataException("Unsupported or damaged learning save.");
                    if(candidate != Path) { RecoveryMessage="Learning save recovered from backup."; File.Copy(candidate,Path,true); }
                    return data;
                }
                catch(Exception e) when(e is IOException || e is ArgumentException || e is UnauthorizedAccessException || e is InvalidDataException)
                { RecoveryMessage=e.Message; }
            }
            // Never silently overwrite an unreadable save, including a newer schema.
            writeBlocked=File.Exists(Path) || File.Exists(Path+".bak");
            return new LearningProgress();
        }
        public void Save(LearningProgress data)
        {
            if(writeBlocked)throw new IOException("Existing learning save needs recovery. Original files have been preserved.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temp=Path+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(data,true));
            if(File.Exists(Path)) File.Replace(temp,Path,Path+".bak");
            else File.Move(temp,Path);
        }
    }
}
