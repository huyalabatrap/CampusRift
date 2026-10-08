using System;
using System.Linq;
using CampusRift.Progression;
namespace CampusRift.AR
{
    public static class ARProgression
    {
        public const int DailyCap=60, ScoreLimit=20;
        public const string GoldSeal="ar-seal-gold", AzureSeal="ar-seal-azure";
        public static ARProgressData Read(ProfileData p)=>p?.ar??new ARProgressData();
        static ARProgressData Mutable(ProfileData p)
        {
            if(p.ar==null)p.ar=new ARProgressData();var s=p.ar;
            if(s.stages==null)s.stages=new System.Collections.Generic.List<ARStageRecord>();
            if(s.scores==null)s.scores=new System.Collections.Generic.List<ARScoreRecord>();
            if(s.cosmetics==null)s.cosmetics=new System.Collections.Generic.List<string>();return s;
        }
        public static int Stars(ProfileData p,int stage)=>Read(p).stages?.FirstOrDefault(s=>s.stage==stage)?.stars??0;
        public static int TotalStars(ProfileData p)=>Read(p).stages?.Sum(s=>s.stars)??0;
        public static bool StageOpen(ProfileData p,int stage)=>stage>=1&&stage<=10&&(stage==1||Stars(p,stage-1)>0);
        public static int ModeStars(ProfileData p,string mode)=>Read(p).scores?.Where(s=>s.modeId==mode).Select(s=>s.stars).DefaultIfEmpty(0).Max()??0;
        public static int Available(ProfileData p,DateTime utc)
        {var s=Read(p);string day=utc.ToUniversalTime().ToString("yyyy-MM-dd");return string.CompareOrdinal(day,s.rewardDay)>0?DailyCap:Math.Max(0,DailyCap-s.rewardToday);}
        // Also used by Job5 knowledge seals, keeping every AR reward under the same cap.
        public static int Claim(ProfileService owner,int requested,DateTime utc)
        {
            if(owner==null||DevMode.Active||owner.Transient)return 0;
            var s=Mutable(owner.Data);string day=utc.ToUniversalTime().ToString("yyyy-MM-dd");
            if(string.CompareOrdinal(day,s.rewardDay)>0){s.rewardDay=day;s.rewardToday=0;}
            int paid=Math.Min(Math.Max(0,requested),Math.Max(0,DailyCap-s.rewardToday));
            s.rewardToday+=paid;if(paid>0)owner.Wallet.Earn(paid,"AR Rift");owner.MarkDirty();return paid;
        }
        public static void Record(ProfileService owner,ARBattleSummary run,int requested,DateTime utc)
        {
            if(owner==null||owner.Transient||DevMode.Active||run.saved)return;
            var s=Mutable(owner.Data);run.saved=true;
            if(run.won&&run.stage>=1&&run.stage<=10)
            {
                var record=s.stages.Find(r=>r.stage==run.stage);
                if(record==null){record=new ARStageRecord{stage=run.stage};s.stages.Add(record);}
                record.stars=Math.Max(record.stars,run.stars);record.bestScore=Math.Max(record.bestScore,run.score);record.clears++;
                if(record.bestSeconds<=0||run.seconds<record.bestSeconds)record.bestSeconds=run.seconds;
            }
            // Top twenty per mode, all on-device; no pose or camera data.
            s.scores.Add(new ARScoreRecord{modeId=run.modeId,score=run.score,seed=run.seed,stage=run.stage,stars=run.stars,seconds=run.seconds,utcDay=utc.ToUniversalTime().ToString("yyyy-MM-dd")});
            var ordered=s.scores.Where(r=>r.modeId==run.modeId).OrderByDescending(r=>r.score).ThenBy(r=>r.seconds).ToList();
            foreach(var excess in ordered.Skip(ScoreLimit))s.scores.Remove(excess);
            int total=TotalStars(owner.Data);
            if(total>=3&&!s.cosmetics.Contains(GoldSeal)){s.cosmetics.Add(GoldSeal);if(string.IsNullOrEmpty(s.selectedSeal))s.selectedSeal=GoldSeal;}
            if(total>=15&&!s.cosmetics.Contains(AzureSeal))s.cosmetics.Add(AzureSeal);
            string observed=utc.ToUniversalTime().ToString("o");if(string.CompareOrdinal(observed,s.lastSeenUtc)>0)s.lastSeenUtc=observed;
            run.reward=run.won?Claim(owner,requested,utc):0;owner.MarkDirty();
        }
        public static void SelectSeal(ProfileService owner,string id)
        {if(owner==null||owner.Transient||DevMode.Active)return;var s=Mutable(owner.Data);if(id!=""&&!s.cosmetics.Contains(id))return;s.selectedSeal=id;owner.MarkDirty();}
    }
}
