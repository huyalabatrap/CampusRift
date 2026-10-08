using System;
using System.Collections.Generic;
namespace CampusRift.Progression
{
    [Serializable] public sealed class ARProgressData
    {
        public List<ARStageRecord> stages=new List<ARStageRecord>();
        public List<ARScoreRecord> scores=new List<ARScoreRecord>();
        public List<string> cosmetics=new List<string>();
        public string selectedSeal="", rewardDay="", lastSeenUtc=""; public int rewardToday;
    }
    [Serializable] public sealed class ARStageRecord { public int stage,stars,bestScore,clears; public float bestSeconds; }
    [Serializable] public sealed class ARScoreRecord { public string modeId,utcDay; public int score,seed,stage,stars; public float seconds; }
}
