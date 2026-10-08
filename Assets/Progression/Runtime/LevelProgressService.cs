using System;
using UnityEngine;
using CampusRift.Levels;

namespace CampusRift.Progression
{
    // Which levels are open and what the player achieved in each (P06-T06). A level opens when the cultivation
    // reaches its requirement and the previous level has been cleared.
    public sealed class LevelProgressService
    {
        public const int StarCompleted = 1, StarTime = 2, StarChallenge = 4;
        readonly ProfileService owner;
        public LevelProgressService(ProfileService owner) { this.owner = owner; }
        ProfileData Data => owner.Data;

        // Convenience for code that has no reference to the service.
        public static LevelProgressService Current => ProfileService.Instance != null ? ProfileService.Instance.Levels : null;

        public static bool CanPlay(int index, out string reason)
        {
            var service = Current;
            if (service == null) { reason = null; return true; }
            return service.IsUnlocked(index, out reason);
        }

        // Linh Thạch paid by the latest RecordClear (for the result screen).
        public int LastLinhThach { get; private set; }

        public LevelEntry Entry(int index) => Data.Level(index, false);
        public bool IsCleared(int index) { var e = Entry(index); return e != null && e.cleared; }
        public int Stars(int index) => Entry(index)?.stars ?? 0;
        public int StarCount(int index) { int m = Stars(index), n = 0; for (int i = 0; i < 3; i++) if ((m & (1 << i)) != 0) n++; return n; }
        public float BestTime(int index) => Entry(index)?.bestTime ?? 0;

        public int HighestCleared
        {
            get { int best = 0; foreach (var l in Data.levels) if (l.cleared && l.level > best) best = l.level; return best; }
        }

        public bool IsUnlocked(int index, out string reason)
        {
            reason = null;
            var catalog = LevelCatalog.Instance; var level = catalog != null ? catalog.Get(index) : null;
            if (level == null) { reason = LevelHudText(false, "Level not found", "Không có màn này"); return false; }
            return IsUnlocked(level, out reason);
        }

        public bool IsUnlocked(LevelDefinition level, out string reason)
        {
            reason = null;
            if (level == null) return false;
            if (DevMode.Active) return true;
            var cultivation = owner.Cultivation;
            if (cultivation != null && !cultivation.AtLeast(level.requiredRealm, level.requiredTier))
            {
                var need = cultivation.Table.TierName((Realm)Mathf.Clamp(level.requiredRealm, 0, cultivation.Table.RealmCount - 1), level.requiredTier, false);
                var needVN = cultivation.Table.TierName((Realm)Mathf.Clamp(level.requiredRealm, 0, cultivation.Table.RealmCount - 1), level.requiredTier, true);
                reason = LevelHudText(false, "Requires " + need, "Cần " + needVN);
                return false;
            }
            if (level.index > 1 && !IsCleared(level.index - 1))
            {
                reason = LevelHudText(false, "Clear level " + (level.index - 1) + " first", "Hãy qua màn " + (level.index - 1) + " trước");
                return false;
            }
            return true;
        }

        // Records a finished level. Returns true when it was the first clear (which also pays a little Tu Vi).
        public bool RecordClear(LevelDefinition level, float seconds, int starMask, out StudyAward award)
        {
            award = null;
            if (DevMode.Active) { LastLinhThach = 0; return false; }
            var entry = Data.Level(level.index, true);
            bool first = !entry.cleared;
            int newStars = 0; for (int i = 0; i < 3; i++) { int bit = 1 << i; if (((starMask | StarCompleted) & bit) != 0 && (entry.stars & bit) == 0) newStars++; }
            entry.attempts++; entry.cleared = true;
            entry.stars |= starMask | StarCompleted;
            // Linh Thạch (plan §9.1): 40 × level number for the first clear, 20 for every star reached for the first time.
            var config = EconomyConfig.Instance;
            LastLinhThach = (first ? config.levelClearPerIndex * level.index : 0) + newStars * config.perStar;
            if (LastLinhThach > 0 && owner.Wallet != null) owner.Wallet.Earn(LastLinhThach, "level");
            if (entry.bestTime <= 0 || seconds < entry.bestTime) entry.bestTime = seconds;
            if (first) award = StudyRewards.FirstLevelClear(owner.Cultivation);
            owner.MarkDirty();
            return first;
        }

        public void RecordAttempt(int index) { if (DevMode.Active) return; Data.Level(index, true).attempts++; owner.MarkDirty(); }

        // First open level that has not been cleared; when everything open is cleared, the last open one.
        public int NextPlayable()
        {
            int last = 1, total = LevelSession.LastIndex;
            for (int i = 1; i <= total; i++)
            {
                if (!IsUnlocked(i, out _)) break;
                last = i;
                if (!IsCleared(i)) return i;
            }
            return last;
        }

        public void ClearAll() { Data.levels.Clear(); owner.MarkDirty(); }

        static string LevelHudText(bool _, string en, string vn) => UI.LevelHUD.Vietnamese ? vn : en;
    }
}
