using System;
using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.SkyBeast
{
    // A segment is one sword strike, never a numeric HP pool.
    [DisallowMultipleComponent]
    public sealed class SkyBeastVitality:MonoBehaviour,IDamageable
    {
        public int TotalSegments{get;private set;}
        public int RemainingSegments{get;private set;}
        public bool IsDead=>RemainingSegments<=0;
        public Element Element=>Element.Hoa;
        public Transform Anchor=>transform;
        public event Action<SkyBeastVitality> Changed,Defeated;
        public Func<bool> CanReceiveSword;
        public void Initialize(int segments){TotalSegments=RemainingSegments=Mathf.Max(1,segments);Changed?.Invoke(this);}
        public bool ApplyDamage(DamageInfo hit)=>hit.source==DamageSource.Skill&&hit.skillId=="thien-kiem"&&hit.amount>0&&ApplySkySwordHit();
        public bool ApplySkySwordHit()
        {
            if(IsDead||(CanReceiveSword!=null&&!CanReceiveSword()))return false;
            RemainingSegments--;Changed?.Invoke(this);if(IsDead)Defeated?.Invoke(this);return true;
        }
    }
}
