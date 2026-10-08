using System;
using UnityEngine;

namespace CampusRift.Progression
{
    // Realm, tier and Tu Vi (plan §8.1). Pure logic on top of CultivationData: no scene objects, no disk.
    public sealed class CultivationService
    {
        public CultivationData Data { get; private set; }
        public CultivationTable Table { get; }
        public event Action Changed;
        public event Action<Realm, int> TierChanged;
        public event Action<Realm> RealmChanged;
        public event Action<float, string> TuViAdded;      // amount actually gained, source

        public CultivationService(CultivationData data, CultivationTable table = null)
        { Data = data; Table = table != null ? table : CultivationTable.Instance; Clamp(); }

        public void Bind(CultivationData data) { Data = data; Clamp(); Changed?.Invoke(); }

        public Realm Realm => DevMode.Active ? (Realm)(Table.RealmCount - 1) : (Realm)Mathf.Clamp(Data.realm, 0, Table.RealmCount - 1);
        public int Tier => DevMode.Active ? CultivationTable.TiersPerRealm : Mathf.Clamp(Data.tier, 1, CultivationTable.TiersPerRealm);
        public float TuVi => Data.tuVi;
        public int TuViPerTier => Table.TuViPerTier(Realm);
        public float Fraction => DevMode.Active ? 1 : Mathf.Clamp01(Data.tuVi / Mathf.Max(1, TuViPerTier));
        public string TuViText => DevMode.Active ? "∞" : Mathf.FloorToInt(TuVi).ToString();
        public bool IsLastRealm => (int)Realm >= Table.RealmCount - 1;
        // Tier 5 full: further Tu Vi is lost until the breakthrough exam is passed (plan §8.1).
        public bool IsBottleneck => Tier >= CultivationTable.TiersPerRealm && Data.tuVi >= TuViPerTier - 0.0001f;
        public bool CanAttemptBreakthrough => IsBottleneck && !IsLastRealm;
        public int RealmIndex => (int)Realm;
        public string Name(bool vietnamese) => Table.TierName(Realm, Tier, vietnamese);

        // Tu Vi required to go from the very start to (realm, tier); used for ordering and tests.
        public bool AtLeast(int realm, int tier) => RealmIndex > realm || (RealmIndex == realm && Tier >= tier);

        // Adds Tu Vi, rising through tiers as they fill. Returns the amount that counted (excess at the bottleneck is dropped).
        public float AddTuVi(float amount, string source)
        {
            if (DevMode.Active || amount <= 0 || float.IsNaN(amount)) return 0;
            float gained = 0, remaining = amount; int tiersRaised = 0;
            var realmBefore = Realm; int tierBefore = Tier;
            while (remaining > 0.00001f && !IsBottleneck)
            {
                float room = TuViPerTier - Data.tuVi;
                if (remaining >= room - 0.00001f)
                {
                    gained += room; remaining -= room; Data.tuVi = TuViPerTier;
                    if (Tier < CultivationTable.TiersPerRealm) { Data.tier = Tier + 1; Data.tuVi = 0; tiersRaised++; }
                }
                else { Data.tuVi += remaining; gained += remaining; remaining = 0; }
            }
            if (gained <= 0) return 0;
            Data.totalEarned += gained;
            TuViAdded?.Invoke(gained, source);
            if (tiersRaised > 0) TierChanged?.Invoke(Realm, Tier);
            Changed?.Invoke();
            return gained;
        }

        // Passing the breakthrough exam: next realm, tier 1, empty bar.
        public bool CompleteBreakthrough()
        {
            if (DevMode.Active || !CanAttemptBreakthrough) return false;
            Data.realm = RealmIndex + 1; Data.tier = 1; Data.tuVi = 0;
            RealmChanged?.Invoke(Realm); TierChanged?.Invoke(Realm, 1); Changed?.Invoke();
            return true;
        }

        // Test and debug helper.
        public void SetState(Realm realm, int tier, float tuVi)
        {
            Data.realm = (int)realm; Data.tier = tier; Data.tuVi = tuVi; Clamp(); Changed?.Invoke();
        }

        void Clamp()
        {
            Data.realm = Mathf.Clamp(Data.realm, 0, Table.RealmCount - 1);
            Data.tier = Mathf.Clamp(Data.tier, 1, CultivationTable.TiersPerRealm);
            Data.tuVi = Mathf.Clamp(Data.tuVi, 0, Table.TuViPerTier((Realm)Data.realm));
        }

        // Stats of the current position (plan §8.4).
        public float MaxHealth => Table.Health(Realm, Tier);
        public float Attack => Table.Attack(Realm, Tier);
        public float MaxSpirit => Table.Spirit(Realm, Tier);
        public float Defense => Table.Defense(Realm);
        public float RunBonus => Table.RunBonus(Realm);
    }
}
