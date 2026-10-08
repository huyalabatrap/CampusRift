using UnityEngine;

namespace CampusRift.Progression
{
    // Every Linh Thạch number of plan §9.1 and §9.2 in one place. Missing asset = these defaults.
    [CreateAssetMenu(menuName = "Campus Rift/Economy Config")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Header("Earning (plan §9.1)")]
        public int firstCorrect = 5;
        public int retryCorrect = 2;
        public int lessonPassed = 30, lessonPerfect = 50;
        public int reviewOnTime = 40;
        public int examPassed = 300;
        public int levelClearPerIndex = 40;
        public int perStar = 20;
        [Header("Anti-farming (plan §9.2)")]
        [Tooltip("A correct answer already given within this many hours pays nothing again.")]
        public int retryLockHours = 24;
        public int retryDailyCap = 300;
        [Tooltip("This many answers in a row faster than fastSeconds pause the rewards.")]
        public int fastStreak = 5;
        public float fastSeconds = 1.5f;
        public int fastPauseMinutes = 2;

        static EconomyConfig cached;
        public static EconomyConfig Instance
        {
            get
            {
                if (cached == null) cached = Resources.Load<EconomyConfig>("EconomyConfig");
                if (cached == null) cached = CreateInstance<EconomyConfig>();
                return cached;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cached = null; }
    }
}
