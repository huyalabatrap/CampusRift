using UnityEngine;

namespace CampusRift.Monsters
{
    [CreateAssetMenu(menuName = "Campus Rift/Shaban AI Config")]
    public sealed class MonsterAIConfig : ScriptableObject
    {
        [Header("Movement (metres / second)")]
        [Min(0.1f)] public float PatrolSpeed = 2.5f;
        [Min(0.1f)] public float InvestigateSpeed = 3.8f;
        [Min(0.1f)] public float ChaseSpeed = 5.5f;
        [Header("Escalation (active gameplay time)")]
        [Tooltip("V2 levels end by clearing waves, so the old hunt-pressure ramp is off by default.")]
        public bool enableTimeEscalation = false;
        [Min(1)] public float EscalationSeconds = 180;
        [Min(0.1f)] public float SpeedIncreaseInterval = 5f;
        [Min(0)] public float ChaseSpeedPerStep = 0.1f;
        [Min(0.1f)] public float MaxChaseSpeed = 20f;
        [Min(1)] public float MaxAttackRateMultiplier = 2f;
        [Header("Evidence")]
        [Min(1)] public float VisionDistance = 65;
        [Range(1, 180)] public float VisionAngle = 140;
        [Min(1)] public float TrackingDistance = 90;
        [Range(1, 360)] public float TrackingAngle = 220;
        [Min(0)] public float TrackingRetention = 10;
        [Min(1)] public float HearingRange = 75;
        [Min(1)] public float HearingFalloffDistance = 18;
        [Range(0.01f, 1)] public float HearingThreshold = 0.1f;
        [Min(1)] public float MemoryDuration = 40;
        [Range(5,10)] public int ObservationCapacity = 8;
        [Range(8,20)] public int TrailCapacity = 16;
        [Min(1)] public float VelocitySmoothing = 8;
        [Min(0.2f)] public float TrailContinuityGap = 0.4f;
        [Min(10)] public float MaxObservedSpeed = 80;
        [Header("Pursuit")]
        [Range(0.5f, 1.5f)] public float PredictionTime = 0.8f;
        [Min(1)] public float MaxPredictionDistance = 18;
        [Min(0.1f)] public float MinPredictionTime = 0.25f;
        [Min(0.1f)] public float MaxPredictionTime = 1.5f;
        [Min(0)] public float ReactionDelay = 0.18f;
        [Range(15,120)] public float ReactionTurnAngle = 45;
        [Min(0)] public float LostSightGraceTime = 1.5f;
        [Min(1)] public float SearchDuration = 90;
        [Min(1)] public float SearchRadius = 12;
        [Min(0.1f)] public float SearchSpeed = 3.1f;
        [Range(0,1)] public float VisitedPenalty = 0.15f;
        [Range(16,80)] public int MaxSearchCandidates = 48;
        [Min(0.1f)] public float SearchLookSeconds = 0.8f;
        [Min(1)] public float NegativeEvidenceDuration = 24;
        [Header("Strategic planning")]
        public bool EnableInterception = true;
        [Min(0.5f)] public float StrategicRefreshRate = 0.75f;
        [Min(5)] public float InterceptionRange = 35;
        [Min(0)] public float InterceptionSafetyMargin = 0.45f;
        [Range(0,1)] public float MinimumInterceptConfidence = 0.55f;
        [Range(2,8)] public int DestinationCandidateCount = 6;
        [Header("Belief: where could the player be now?")]
        [Tooltip("Hypotheses of the player's location. Only this monster's own perceptions update them.")]
        [Range(64, 1024)] public int BeliefParticles = 320;
        [Range(0.1f, 0.5f)] public float BeliefRefreshRate = 0.2f;
        [Tooltip("Sight checks per belief tick used to rule out places the monster can currently see.")]
        [Range(8, 96)] public int BeliefRaysPerTick = 40;
        [Tooltip("Remaining weight of a hypothesis standing in plain view without being seen.")]
        [Range(0.001f, 0.5f)] public float BeliefMissLikelihood = 0.04f;
        public Vector2 BeliefFleeSpeed = new Vector2(5f, 9.5f);
        public Vector2 BeliefWalkSpeed = new Vector2(1.2f, 3.4f);
        public Vector2 BeliefFleeSeconds = new Vector2(3f, 14f);
        [Tooltip("Chance to stop and hide on reaching a corridor/stair node when not fleeing.")]
        [Range(0, 1)] public float BeliefHoldChance = 0.06f;
        [Tooltip("Chance to stop and hide on reaching the inside of a room when not fleeing.")]
        [Range(0, 1)] public float BeliefRoomHoldChance = 0.35f;
        public Vector2 BeliefHoldSeconds = new Vector2(4f, 25f);
        [Tooltip("Chance that a hypothesis reaching a lift lobby takes the car.")]
        [Range(0, 1)] public float BeliefLiftChance = 0.3f;
        [Tooltip("Weight of staircase edges when no vertical motion was observed.")]
        [Range(0, 2)] public float BeliefStairWeight = 0.6f;
        [Header("Hunt (after losing sight)")]
        [Tooltip("After the hunt window ends, keep roaming likely areas this long before the fixed patrol.")]
        [Min(0)] public float RoamDuration = 60;
        [Tooltip("While the lead is this fresh (seconds), travel to distant search goals at chase speed.")]
        [Min(0)] public float HuntFreshSeconds = 35;
        [Min(1)] public float SearchGoalRadius = 7;
        [Tooltip("Seconds of travel that halve a search goal's value.")]
        [Min(1)] public float SearchTravelHorizon = 12;
        [Min(0.25f)] public float SearchReplanRate = 1f;
        [Header("Elevators (observed like any bystander would)")]
        [Min(2)] public float LiftDisplayReadDistance = 20;
        [Min(2)] public float LiftChimeHearingDistance = 16;
        [Tooltip("Take a lift when its estimated door-to-door time beats the stairs.")]
        public bool RideLifts = true;
        [Tooltip("How many seconds faster the lift must be before the monster prefers it to the stairs.")]
        [Min(0)] public float LiftAdvantageSeconds = 4;
        [Tooltip("Give up waiting for a car after this long and take the stairs.")]
        [Min(5)] public float LiftMaxWait = 35;
        [Tooltip("Walking pace when stepping into and out of a car (m/s).")]
        [Min(0.5f)] public float LiftBoardSpeed = 2.2f;
        [Header("Vision through glazing")]
        [Min(0)] public float GlassVisionDistance = 22;
        [Header("Navigation recovery")]
        [Min(0.5f)] public float StuckSeconds = 1.6f;
        [Header("Attack")]
        [Min(0.1f)] public float AttackDistance = 1.6f;
        [Min(0.2f)] public float AttackCooldown = 1.8f;
        [Min(0)] public float Damage = 25;
        [Min(0)] public float AttackWindup = 0.35f;
        [Header("Tick intervals (seconds)")]
        [Range(0.05f, 0.2f)] public float VisionRefreshRate = 0.1f;
        [Range(0.1f, 0.5f)] public float DecisionRefreshRate = 0.15f;
        [Range(0.1f, 0.5f)] public float PathRefreshRate = 0.15f;
        [Header("Spatial audio")]
        [Min(0.1f)] public float AudioMinDistance = 2;
        [Min(1)] public float AudioMaxDistance = 45;
        public LayerMask EnvironmentMask = ~0;
    }
}
