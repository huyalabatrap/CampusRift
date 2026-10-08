using System.Collections.Generic;
using UnityEngine;
namespace CampusRift.Learning
{
    [CreateAssetMenu(menuName="Campus Rift/Learning/Course")]
    public sealed class CourseData : ScriptableObject
    {
        public string id, title, subject;
        public string titleVN;
        [TextArea] public string descriptionVN;
        [TextArea] public string description;
        // Kept so old assets still open; V2 no longer gates anything by survival time.
        [Min(0)] public float requiredSurvivalSeconds;
        [Header("Chapter (V2): one CourseData is one chapter of the subject")]
        // 1-based chapter number; chapter k belongs to realm k-1 and its exam lifts the player out of that realm.
        [Min(0)] public int chapterIndex;
        // Extra questions used only by the breakthrough exam (the lessons' own questions are added to the pool).
        public QuestionBankData examBank;
        [Min(1)] public int examSize = 20;
        [Range(1,100)] public int examPassPercent = 80;
        [Min(1)] public int examMinutes = 20;
        // Stand-in content that must be replaced before release.
        public bool isPlaceholder;
        public List<LessonData> lessons = new List<LessonData>();
        // Tier uses global groups of five. The course supplying the fifth pass chooses its reward.
        public List<SkillTier> skillTiers = new List<SkillTier>();
    }
}
