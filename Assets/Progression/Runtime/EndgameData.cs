using System;
using System.Collections.Generic;
namespace CampusRift.Progression
{
    [Serializable] public sealed class EndgameData
    {
        public int highestFloor, kills, eliteKills, bossKills, fireSafeMask;
        public List<LevelEntry> nightmare = new List<LevelEntry>();
        public List<string> achievements = new List<string>();
        public string title, costume = "default", rewardDay;
        public int rewardToday;
        public LevelEntry Nightmare(int index, bool create = false)
        {
            var e = nightmare.Find(x => x.level == index);
            if(e == null && create) { e = new LevelEntry { level = index }; nightmare.Add(e); }
            return e;
        }
    }
}
