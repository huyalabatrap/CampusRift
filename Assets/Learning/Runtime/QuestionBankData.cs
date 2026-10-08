using System;
using System.Collections.Generic;
using UnityEngine;
namespace CampusRift.Learning
{
    [Serializable] public sealed class AnswerOption { public string id, text, textVN; public string side; }
    [Serializable] public sealed class QuestionData
    {
        public string id;
        // Stable type keys allow new graders/renderers without changing saved answers.
        public string type = "single-choice";
        public string lessonId, topic;
        [Range(1,5)] public int difficulty = 1;
        public List<string> tags = new List<string>();
        // Where the answer comes from, e.g. "GT 2021, tr. 45" (V2 content must always have it).
        public string source, chapterId;
        [TextArea] public string prompt, explanation;
        [TextArea] public string promptVN, explanationVN;
        public List<AnswerOption> options = new List<AnswerOption>();
        public List<string> correctOptionIds = new List<string>();
    }
    [CreateAssetMenu(menuName="Campus Rift/Learning/Question Bank")]
    public sealed class QuestionBankData : ScriptableObject
    {
        public List<QuestionData> questions = new List<QuestionData>();
    }
}
