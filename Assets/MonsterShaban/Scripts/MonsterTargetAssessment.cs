using CampusRift.Skills;
using UnityEngine;

namespace CampusRift.Monsters
{
    public enum MonsterTargetType { None, Player, Phantom }

    // Scores observations; it never receives a command from the skill and never reads a
    // hidden player's position. The Phantom label is debug provenance, not a forced target.
    [DisallowMultipleComponent]
    public sealed class MonsterTargetAssessment : MonoBehaviour
    {
        public MonsterTargetType VisibleTarget { get; private set; }
        public MonsterTargetType CurrentTarget { get; private set; }
        public float PlayerConfidence { get; private set; }
        public float PhantomConfidence { get; private set; }
        public string Reason { get; private set; } = "No evidence";
        public bool RejectedNow { get; private set; }
        public float RejectedSpawnTime { get; private set; }
        public Vector3 RejectedPosition { get; private set; }
        public Vector3 ChosenPosition { get; private set; }
        public Vector3 ChosenVelocity { get; private set; }
        public float ChosenTime { get; private set; }

        float phantomSeenFor, lastEvaluation = -1, rejectedSpawnAt = -1;
        float lastPhantomSoundAt = -10000;
        PhantomDecoy watched;

        public void Evaluate(MonsterPerception sight, MonsterHearing hearing)
        {
            RejectedNow = false;
            float now = Time.time;
            var phantom = PhantomDecoy.Active;
            if (phantom != watched) { watched = phantom; phantomSeenFor = 0; lastPhantomSoundAt = -10000; }
            float dt = lastEvaluation < 0 ? 0 : Mathf.Clamp(now - lastEvaluation, 0, .3f);
            lastEvaluation = now;
            bool seePlayer = sight.CanSeePlayer;
            bool seePhantom = sight.CanSeePhantom && phantom != null && phantom.SpawnedAt != rejectedSpawnAt;
            if (seePhantom) phantomSeenFor += dt; else phantomSeenFor = 0;

            if (seePhantom && ((sight.DistanceToPhantom < 3.5f && phantomSeenFor > .65f) ||
                (sight.DistanceToPhantom < 8f && phantomSeenFor > 2.1f)))
            {
                rejectedSpawnAt = phantom.SpawnedAt;
                RejectedSpawnTime = phantom.SpawnedAt;
                RejectedPosition = sight.PhantomPosition;
                RejectedNow = true; seePhantom = false;
                Reason = "Close observation exposed dimensional shimmer";
                CurrentTarget = MonsterTargetType.None;
                phantom.Dissolve(true);
            }
            float playerScore = seePlayer ? .72f + .25f *
                (1 - sight.DistanceToPlayer / sight.PlayerVisionDistance) +
                (sight.DistanceToPlayer < 7f ? .08f : 0) : 0;
            float phantomScore = seePhantom ? .58f + .15f *
                (1 - sight.DistanceToPhantom / sight.PlayerVisionDistance) : 0;
            if (seePhantom && hearing != null)
            {
                float match = hearing.RecentSoundMatch(sight.PhantomPosition, 3.5f, 1.2f);
                bool ambiguous = seePlayer && Vector3.Distance(sight.PhantomPosition,
                    sight.ObservedPosition) < 2.5f;
                if (!ambiguous && match >= .55f) lastPhantomSoundAt = now;
                // Hold a corroborated track between footsteps so a sprinting player's
                // intervening sound does not switch the target back every scan.
                float corroboration = ambiguous ? match * .3f :
                    Mathf.Max(match, now - lastPhantomSoundAt < .75f ? 1f : 0f);
                phantomScore += .38f * corroboration;
            }
            if (seePhantom && CurrentTarget == MonsterTargetType.Phantom) phantomScore += .045f;
            if (seePlayer && CurrentTarget == MonsterTargetType.Player) playerScore += .025f;
            // Compare before clamping: a strong visual + sound match must beat a
            // nearby player even when both displayed confidence values reach 1.
            PhantomConfidence = Mathf.Clamp01(phantomScore);
            PlayerConfidence = Mathf.Clamp01(playerScore);

            MonsterTargetType selected = !seePlayer && !seePhantom ? MonsterTargetType.None :
                seePlayer && (!seePhantom ||
                    (!sight.SightThroughGlass && sight.DistanceToPlayer <= sight.config.AttackDistance + .4f) ||
                    playerScore >= phantomScore)
                    ? MonsterTargetType.Player : MonsterTargetType.Phantom;
            VisibleTarget = selected;
            if (selected == MonsterTargetType.None) return;
            if (selected != CurrentTarget)
                Reason = selected == MonsterTargetType.Phantom
                    ? "Visual candidate + moving sound outweigh older track"
                    : "Clearer human observation outweighs phantom evidence";
            CurrentTarget = selected;
            ChosenPosition = selected == MonsterTargetType.Player ? sight.ObservedPosition : sight.PhantomPosition;
            ChosenVelocity = selected == MonsterTargetType.Player ? sight.ObservedVelocity : sight.PhantomVelocity;
            ChosenTime = selected == MonsterTargetType.Player ? sight.ObservationTime : sight.PhantomObservationTime;
        }
    }
}
