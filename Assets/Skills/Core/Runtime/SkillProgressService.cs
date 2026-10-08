using UnityEngine;
using CampusRift.Progression;
namespace CampusRift.Skills
{
    // Reads the current profile on every call, including transient validation profiles.
    public sealed class SkillProgressService
    {
        readonly ProfileService owner;
        static readonly int[] Prices={0,150,400,900,1600};
        public SkillProgressService(ProfileService profile){owner=profile;}
        public int GetRank(string id)
        {if(DevMode.Active)return 5;foreach(var entry in owner.Data.skills.ranks)if(entry.key==id)return Mathf.Clamp(entry.count,1,5);return 1;}
        public int NextCost(SkillDefinition d)=>d!=null&&GetRank(d.id)<5?Prices[GetRank(d.id)]:0;
        public int RequiredRealm(SkillDefinition d)=>Mathf.Min(6,d.unlockRealm+GetRank(d.id));
        public bool IsUnlocked(SkillDefinition d)=>d!=null&&(d.starter||owner.Cultivation.AtLeast(d.unlockRealm,d.unlockTier));
        public bool CanUpgrade(SkillDefinition d)=>IsUnlocked(d)&&GetRank(d.id)<5&&owner.Cultivation.AtLeast(RequiredRealm(d),1)&&owner.Wallet.CanAfford(NextCost(d));
        public bool TryUpgrade(SkillDefinition d)
        {
            if(!CanUpgrade(d))return false;
            int next=GetRank(d.id)+1;
            if(!owner.Wallet.TrySpend(Prices[next-1]))return false;
            KeyCount found=null;foreach(var entry in owner.Data.skills.ranks)if(entry.key==d.id){found=entry;break;}
            if(found==null){found=new KeyCount{key=d.id};owner.Data.skills.ranks.Add(found);}found.count=next;
            owner.MarkDirty();return true;
        }
    }
}
