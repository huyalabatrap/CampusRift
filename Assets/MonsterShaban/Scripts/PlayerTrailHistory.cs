using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Monsters
{
    public enum MovementTrend { Unknown, Stopped, Straight, Turning, ZigZag, Reversing, ChangingFloor }
    [Serializable] public struct PlayerTrailSample { public Vector3 Position; public float Timestamp; }

    // Owned by the monster, not a tracker attached to the player. Only successful vision
    // feeds Observe; loss of sight freezes the trail and reacquisition starts a new segment.
    [Serializable]
    public sealed class PlayerTrailHistory
    {
        [SerializeField] List<PlayerTrailSample> samples = new List<PlayerTrailSample>(20);
        public IReadOnlyList<PlayerTrailSample> Samples => samples;
        public Vector3 SmoothedVelocity { get; private set; }
        public Vector3 Acceleration { get; private set; }
        public float PlayerSpeed => Vector3.ProjectOnPlane(SmoothedVelocity,Vector3.up).magnitude;
        public Vector3 PlayerDirection => Vector3.ProjectOnPlane(SmoothedVelocity,Vector3.up).normalized;
        public float TurnRate { get; private set; }
        public float SignedTurnRate { get; private set; }
        public Vector3 AverageDirection { get; private set; }
        public float AverageSpeed { get; private set; }
        public MovementTrend Trend { get; private set; }
        public float Coherence { get; private set; }
        public int SegmentSamples => samples.Count;
        float lastSignedTurn;
        int alternatingTurns;

        public void Observe(Vector3 position, float timestamp, MonsterAIConfig config)
        {
            if(samples.Count>0)
            {
                var previous=samples[samples.Count-1];float dt=timestamp-previous.Timestamp;
                if(dt<0.08f)return; // At most 12.5 Hz even if combat requests an extra sight check.
                Vector3 difference=position-previous.Position;
                if(dt>config.TrailContinuityGap || difference.magnitude/dt>config.MaxObservedSpeed) Clear();
                else
                {
                    Vector3 measured=difference/dt,old=SmoothedVelocity;
                    float blend=1-Mathf.Exp(-config.VelocitySmoothing*dt);
                    SmoothedVelocity=samples.Count==1?measured:Vector3.Lerp(old,measured,blend);
                    Acceleration=Vector3.ClampMagnitude((SmoothedVelocity-old)/dt,80);
                    var before=Vector3.ProjectOnPlane(old,Vector3.up);var after=Vector3.ProjectOnPlane(measured,Vector3.up);
                    float angle=before.sqrMagnitude>.25f && after.sqrMagnitude>.25f?Vector3.SignedAngle(before,after,Vector3.up):0;
                    SignedTurnRate=Mathf.Lerp(SignedTurnRate,angle/dt,blend);
                    TurnRate=Mathf.Lerp(TurnRate,Mathf.Abs(angle/dt),blend);
                    if(Mathf.Abs(angle)>18 && lastSignedTurn*angle<0)alternatingTurns++;
                    if(Mathf.Abs(angle)>10)lastSignedTurn=angle;
                    if(TurnRate<15)alternatingTurns=Mathf.Max(0,alternatingTurns-1);
                }
            }
            while(samples.Count>=Mathf.Clamp(config.TrailCapacity,8,20))samples.RemoveAt(0);
            samples.Add(new PlayerTrailSample{Position=position,Timestamp=timestamp});
            RecomputeTrend();
        }
        void RecomputeTrend()
        {
            if(samples.Count<2){Trend=MovementTrend.Unknown;return;}
            Vector3 total=Vector3.zero;float length=0;
            for(int i=1;i<samples.Count;i++){var delta=samples[i].Position-samples[i-1].Position;total+=delta;length+=delta.magnitude;}
            float elapsed=samples[samples.Count-1].Timestamp-samples[0].Timestamp;
            AverageDirection=total.normalized;AverageSpeed=length/Mathf.Max(.01f,elapsed);
            Coherence=length>.1f?Mathf.Clamp01(total.magnitude/length):1;
            if(PlayerSpeed<.35f)Trend=MovementTrend.Stopped;
            else if(Mathf.Abs(total.y)>1.2f && Mathf.Abs(SmoothedVelocity.y)>.5f)Trend=MovementTrend.ChangingFloor;
            else if(alternatingTurns>=2)Trend=MovementTrend.ZigZag;
            else if(Vector3.Dot(AverageDirection,PlayerDirection)<-.25f)Trend=MovementTrend.Reversing;
            else if(TurnRate>30)Trend=MovementTrend.Turning;
            else Trend=MovementTrend.Straight;
        }
        public void Clear()
        {
            samples.Clear();SmoothedVelocity=Acceleration=AverageDirection=Vector3.zero;
            TurnRate=SignedTurnRate=AverageSpeed=Coherence=lastSignedTurn=0;
            alternatingTurns=0;Trend=MovementTrend.Unknown;
        }
    }
}
