using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Learning
{
    [CreateAssetMenu(menuName="Campus Rift/Learning/Catalog")]
    public sealed class LearningCatalog : ScriptableObject
    {
        public List<CourseData> courses = new List<CourseData>();
        [Min(1)] public int breakthroughsPerSkill = 5;
        [Range(0,1)] public float movementBonusCap = .1f;
        [Range(0,5)] public float healthBonusCap = 1;
        [Range(0,3)] public float energyBonusCap = .5f;
    }
    [Serializable] public sealed class LessonPage
    {
        public string title;
        public string titleVN;
        [TextArea(4,16)] public string contentVN;
        [TextArea(2,6)] public string exampleVN, takeawayVN;
        [TextArea(4,16)] public string content;
        [TextArea(2,6)] public string example, takeaway;
        public Sprite illustration;
    }
    [Serializable] public sealed class SkillTier
    {
        [Min(1)] public int tier = 1;
        public SkillRewardData skill;
    }
    [Serializable] public sealed class StatReward
    {
        [Min(0)] public float healthFraction = .05f;
        [Min(0)] public float movementFraction = .02f;
        [Min(0)] public float energyFraction;
    }
}
