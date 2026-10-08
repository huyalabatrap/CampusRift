using System;
using System.Linq;
using UnityEngine;
using CampusRift.Levels;
using CampusRift.Learning;
namespace CampusRift.Progression
{
    public sealed class AchievementDefinition
    {
        public string id, nameVN, nameEN, conditionVN, conditionEN, group;
        public Func<ProfileData, bool> met;
        public AchievementDefinition(string id,string vn,string en,string group,string cv,string ce,Func<ProfileData,bool> met)
        {this.id=id;nameVN=vn;nameEN=en;this.group=group;conditionVN=cv;conditionEN=ce;this.met=met;}
    }
    public static class EndgameService
    {
        public const int DailyCap = 100;
        public static int LastReward {get;private set;}
        public static event Action<AchievementDefinition> Unlocked;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){Unlocked=null;LastReward=0;}
        public static EndgameData State(ProfileData p)
        {
            if(p.endgame==null)p.endgame=new EndgameData();
            var e=p.endgame;
            if(e.nightmare==null)e.nightmare=new System.Collections.Generic.List<LevelEntry>();
            if(e.achievements==null)e.achievements=new System.Collections.Generic.List<string>();
            return e;
        }
        public static bool TowerOpen(ProfileData p)=>DevMode.Active || (p.Level(10,false)?.cleared??false);
        public static bool NightmareOpen(ProfileData p,int level)=>level>=1&&level<=10&&(DevMode.Active || (p.Level(level,false)?.stars??0)==7);
        // The high-water date prevents a clock rollback from reopening a paid day.
        public static int Available(ProfileData p,DateTime utc)
        {
            var e=State(p);string day=utc.ToUniversalTime().ToString("yyyy-MM-dd");
            return string.CompareOrdinal(day,e.rewardDay)>0?DailyCap:Math.Max(0,DailyCap-e.rewardToday);
        }
        public static int Claim(ProfileService owner,int requested,DateTime utc)
        {
            if (DevMode.Active) return 0;
            var e=State(owner.Data);string day=utc.ToUniversalTime().ToString("yyyy-MM-dd");
            if(string.CompareOrdinal(day,e.rewardDay)>0){e.rewardDay=day;e.rewardToday=0;}
            int paid=Math.Min(Math.Max(0,requested),Math.Max(0,DailyCap-e.rewardToday));
            e.rewardToday+=paid;if(paid>0)owner.Wallet.Earn(paid,"endgame");owner.MarkDirty();return paid;
        }
        public static void Record(LevelResult result,StarRun run)
        {
            var owner=ProfileService.Instance;if(DevMode.Active || owner==null||!result.won){LastReward=0;return;}
            var p=owner.Data;var e=State(p);var d=result.level;LastReward=0;
            if(d.runMode==EndgameMode.Tower)
            {e.highestFloor=Math.Max(e.highestFloor,d.towerFloor);LastReward=Claim(owner,10,DateTime.UtcNow);}
            else if(d.runMode==EndgameMode.Nightmare)
            {
                var n=e.Nightmare(d.index,true);n.cleared=true;n.attempts++;n.stars|=result.starMask|1;
                if(result.seconds>0&&(n.bestTime<=0||result.seconds<n.bestTime))n.bestTime=result.seconds;
                LastReward=Claim(owner,20,DateTime.UtcNow);
            }
            if(d.runMode!=EndgameMode.Tower&&d.index>=8&&d.index<=10&&run.outdoorFireHits==0)e.fireSafeMask|=1<<(d.index-8);
            Evaluate(owner);owner.MarkDirty();
        }
        static int Clears(ProfileData p)=>p.levels.Count(x=>x.cleared&&x.level>=1&&x.level<=10);
        static int Perfect(ProfileData p)=>p.levels.Count(x=>x.level>=1&&x.level<=10&&x.stars==7);
        public static string[] LessonIds()
        {
            var catalog=Resources.Load<LearningCatalog>("LearningCatalog");
            return catalog==null?Array.Empty<string>():catalog.courses.Where(c=>c!=null).SelectMany(c=>c.lessons).Where(l=>l!=null).Select(l=>l.id).Distinct().ToArray();
        }
        static bool AllGold(ProfileData p){var ids=LessonIds();return ids.Length>0&&ids.All(id=>p.learning.lessons.Any(l=>l.id==id&&l.mastery>=3));}
        public static readonly AchievementDefinition[] Achievements = {
            new AchievementDefinition("first-read","Bước Đầu","First Steps","study","Đọc trọn 1 bài","Read one full lesson",p=>p.learning.lessons.Any(l=>l.readRewarded)),
            new AchievementDefinition("five-lessons","Thư Khách","Library Visitor","study","Hoàn thành 5 bài","Complete five lessons",p=>p.learning.lessons.Count(l=>l.completed)>=5),
            new AchievementDefinition("gold","Ánh Vàng","Golden Light","study","Có 1 huy hiệu Vàng","Earn one Gold badge",p=>p.learning.lessons.Any(l=>l.mastery>=3)),
            new AchievementDefinition("scholar","Bác Học","Scholar","study","Vàng mọi bài trong thư viện","Gold on every library lesson",AllGold),
            new AchievementDefinition("streak7","Bền Chí","Steadfast","study","Chuỗi học 7 ngày","Seven-day study streak",p=>p.learning.studyDaily.streak>=7),
            new AchievementDefinition("streak30","Kiên Trì","Perseverance","study","Chuỗi học 30 ngày","Thirty-day study streak",p=>p.learning.studyDaily.streak>=30),
            new AchievementDefinition("clear1","Hộ Viện","Campus Defender","combat","Qua màn 1","Clear level one",p=>p.Level(1,false)?.cleared??false),
            new AchievementDefinition("clear5","Săn Hồn","Soul Hunter","combat","Qua 5 màn","Clear five levels",p=>Clears(p)>=5),
            new AchievementDefinition("rift","Phá Rift","Rift Breaker","combat","Qua màn 10","Clear level ten",TowerOpen),
            new AchievementDefinition("perfect","Kiếm Tiên","Sword Immortal","combat","Ba sao cả 10 màn thường","Three stars in all ten normal levels",p=>Perfect(p)==10),
            new AchievementDefinition("elements","Ngũ Hành Viên Mãn","Elemental Master","combat","Kích hoạt đủ 9 phản ứng","Trigger all nine reactions",p=>p.seenReactions.Distinct().Count()>=9),
            new AchievementDefinition("kills100","Trăm Trận","Hundred Battles","combat","Hạ 100 quái tính điểm","Defeat 100 counted enemies",p=>State(p).kills>=100),
            new AchievementDefinition("elites10","Phá Giáp","Elite Breaker","combat","Hạ 10 tinh anh","Defeat ten elites",p=>State(p).eliteKills>=10),
            new AchievementDefinition("boss","Trấn Hồn","Soul Warden","combat","Hạ 1 Shaban boss","Defeat one Shaban boss",p=>State(p).bossKills>=1),
            new AchievementDefinition("tower5","Thử Lửa","Trial by Fire","explore","Qua tầng Tháp 5","Clear tower floor five",p=>State(p).highestFloor>=5),
            new AchievementDefinition("tower10","Lăng Vân","Cloud Walker","explore","Qua tầng Tháp 10","Clear tower floor ten",p=>State(p).highestFloor>=10),
            new AchievementDefinition("tower30","Đăng Thiên","Sky Ascendant","explore","Qua tầng Tháp 30","Clear tower floor thirty",p=>State(p).highestFloor>=30),
            new AchievementDefinition("night1","Phá Mộng","Dream Breaker","explore","Qua 1 màn Ác Mộng","Clear one Nightmare level",p=>State(p).nightmare.Any(n=>n.cleared)),
            new AchievementDefinition("night10","Chủ Mộng","Dream Keeper","explore","Qua cả 10 màn Ác Mộng","Clear all ten Nightmare levels",p=>State(p).nightmare.Count(n=>n.cleared&&n.level>=1&&n.level<=10)==10),
            new AchievementDefinition("skyguard","Người Gác Trời","Sky Guardian","explore","Qua 8–10, mỗi màn không trúng Thiên Hỏa ngoài trời","Clear 8–10 with no outdoor fire hits in each run",p=>State(p).fireSafeMask==7)
        };
        public static int Evaluate(ProfileService owner)
        {
            if (DevMode.Active) return 0;
            int count=0;var e=State(owner.Data);
            foreach(var a in Achievements)if(!e.achievements.Contains(a.id)&&a.met(owner.Data))
            {e.achievements.Add(a.id);count++;Unlocked?.Invoke(a);}
            if(count>0)owner.MarkDirty();return count;
        }
        public static bool EquipTitle(ProfileService p,string id)
        {if(!DevMode.Active && !string.IsNullOrEmpty(id)&&!State(p.Data).achievements.Contains(id))return false;State(p.Data).title=id;p.MarkDirty();return true;}
        public static bool TitleOpen(ProfileData p, string id) => DevMode.Active || State(p).achievements.Contains(id);
        public static AchievementDefinition Title(ProfileData p)=>Achievements.FirstOrDefault(a=>a.id==State(p).title);
    }
}
