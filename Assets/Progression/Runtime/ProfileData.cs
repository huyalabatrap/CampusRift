using System;
using System.Collections.Generic;
using CampusRift.Learning;

namespace CampusRift.Progression
{
    // The whole V2 save (plan §14.6). JsonUtility has no dictionaries, so keyed data is stored as lists of pairs.
    // Exam results live in `learning.exams`. Sections that later phases fill (wallet/inventory P08, skills/loadouts P09–P10, daily P16) exist
    // from the start so an old save never needs a new version just to gain an empty section.
    [Serializable] public sealed class ProfileData
    {
        public const int CurrentVersion = 2;
        public int version = CurrentVersion;
        public CultivationData cultivation = new CultivationData();
        public WalletData wallet = new WalletData();
        // Reading and quiz progress per lesson. Same shape as the old learning save; the survival and stat-bonus fields are unused.
        public LearningProgress learning = new LearningProgress();
        public SkillsData skills = new SkillsData();
        public List<LoadoutEntry> loadouts = new List<LoadoutEntry>();
        public List<KeyCount> inventory = new List<KeyCount>();
        // What the player chose to take into the next level (item id → count), at most one entry per item.
        public List<KeyCount> carry = new List<KeyCount>();
        public List<KeyCount> artifacts = new List<KeyCount>();
        public List<LevelEntry> levels = new List<LevelEntry>();
        public DailyData daily = new DailyData();
        public MigrationData migration = new MigrationData();
        public CampusRift.UI.TutorialProgress tutorial = new CampusRift.UI.TutorialProgress();
        public EndgameData endgame = new EndgameData();
        public ARProgressData ar = new ARProgressData();
        public bool heavenSwordSeen;
        public bool longVuongRevealSeen;
        public bool riftBreakerUnlocked;
        public List<string> seenReactions = new List<string>();

        public LevelEntry Level(int index, bool create)
        {
            var entry = levels.Find(l => l.level == index);
            if (entry == null && create) { entry = new LevelEntry { level = index }; levels.Add(entry); }
            return entry;
        }
    }

    // tuVi is progress inside the current tier (0 … Tu Vi of a tier). Full tier 5 = bottleneck.
    [Serializable] public sealed class CultivationData
    {
        public int realm;               // Realm
        public int tier = 1;            // 1..5
        public float tuVi;
        public float totalEarned;       // lifetime, for statistics only
    }

    [Serializable] public sealed class WalletData { public int linhThach; public int lifetimeEarned, lifetimeSpent; }

    [Serializable] public sealed class SkillsData
    {
        public List<string> unlocked = new List<string>();
        public List<KeyCount> ranks = new List<KeyCount>();
    }

    [Serializable] public sealed class LoadoutEntry { public List<string> ids = new List<string>(); }

    [Serializable] public sealed class KeyCount { public string key; public int count; }

    [Serializable] public sealed class LevelEntry
    {
        public int level;
        public bool cleared;
        public int stars;               // bit mask: 1 = completed, 2 = time, 4 = special challenge
        public float bestTime;
        public int attempts;
    }

    [Serializable] public sealed class DailyData
    {
        public string date;
        public List<bool> quests = new List<bool>();
        public int streak;
        public bool restDayUsed;
    }

    [Serializable] public sealed class MigrationData
    {
        // Set once when an old learning-v1 save was archived; the menu shows the notice until it is dismissed.
        public bool v1Archived;
        public bool noticePending;
    }
}
