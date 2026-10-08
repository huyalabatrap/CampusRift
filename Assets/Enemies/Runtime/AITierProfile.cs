using System;
using UnityEngine;

namespace CampusRift.Enemies
{
    // How clever monsters are at each AI tier T0–T4 (plan §3.3). One asset holds all five rows.
    [CreateAssetMenu(menuName = "Campus Rift/AI Tier Profiles")]
    public sealed class AITierProfile : ScriptableObject
    {
        [Serializable] public struct Tier
        {
            public string name;
            // Seconds between the red warning and the strike.
            [Min(0.1f)] public float windup;
            // How many monsters may be in a wind-up/strike at once; the rest wait their turn.
            [Min(1)] public int meleeTokens;
            [Min(1)] public int rangedTokens;
            // Spread around the player instead of piling up (T1+).
            public bool surround;
            // Chance to sidestep a marked danger zone, 0..1 (T2+; used from P12).
            [Range(0, 1)] public float dodgeChance;
            public bool ambush, adapt;
            public bool squad;
            [Range(.5f, 1f)] public float squadRefresh;
            [Range(1, 2)] public int chasers;
            [Range(2, 4)] public float predictionSeconds;
            [Range(2, 12)] public float flankRadius;
            [Range(30, 90)] public float escapeDegrees;
            [Range(0, 12)] public float routePenalty;
            [Range(1, 4)] public int pathsPerFrame;
            public bool interception, herding;
            [Min(5)] public float stationarySeconds;
            public bool phasedEncirclement;
            [Range(180, 320)] public float closeCoverage;
            [Range(1, 2)] public float formationSpeed;
        }
        public Tier[] tiers = new Tier[5];

        public Tier Get(int tier) => tiers[Mathf.Clamp(tier, 0, tiers.Length - 1)];
    }
}
