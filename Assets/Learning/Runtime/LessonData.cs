using System.Collections.Generic;
using UnityEngine;
namespace CampusRift.Learning
{
    [CreateAssetMenu(menuName="Campus Rift/Learning/Lesson")]
    public sealed class LessonData : ScriptableObject
    {
        public string id, title, topic;
        public string titleVN;
        public List<LessonPage> pages = new List<LessonPage>();
        public QuestionBankData questionBank;
        [Min(1)] public int quizSize = 10;
        [Range(1,100)] public int passPercent = 80;
        public List<string> prerequisiteLessonIds = new List<string>();
        [Min(0)] public float requiredSurvivalSeconds;
        public StatReward reward = new StatReward();
        public bool isPlaceholder;
    }
}
