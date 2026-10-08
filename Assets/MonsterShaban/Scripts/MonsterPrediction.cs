using UnityEngine;
using UnityEngine.AI;

namespace CampusRift.Monsters
{
    public sealed class MonsterPrediction : MonoBehaviour
    {
        public MonsterAIConfig config;
        [SerializeField] Vector3 predictedPosition;
        [SerializeField] bool landingPrediction;
        public Vector3 PredictedPosition => predictedPosition;
        public bool LandingPrediction => landingPrediction;
        public float PredictionConfidence { get; private set; }
        public float CurrentPredictionTime { get; private set; }
        public bool Reacting => pendingReaction && Time.time < reactionUntil;
        Vector3 acceptedDirection;
        bool hasPrediction, pendingReaction;
        float reactionUntil;
        Vector3 lastGroundedEvidence;
        bool hasGroundedEvidence;
        MonsterNavigation navigation;
        float ChaseSpeed => navigation != null ? navigation.ChaseSpeed : config.ChaseSpeed;
        void Awake() { navigation=GetComponent<MonsterNavigation>(); }
        readonly RaycastHit[] hits = new RaycastHit[32];

        public Vector3 PredictCandidate(Vector3 position, Vector3 velocity)
        {
            landingPrediction = false;
            CurrentPredictionTime = Mathf.Clamp(Vector3.Distance(transform.position, position) /
                Mathf.Max(.1f, ChaseSpeed), .12f, .55f);
            PredictionConfidence = .48f;
            Vector3 projected = position + Vector3.ClampMagnitude(
                Vector3.ProjectOnPlane(velocity, Vector3.up) * CurrentPredictionTime,
                config.MaxPredictionDistance);
            NavMeshHit start, end, wall;
            if (!NavMesh.SamplePosition(position, out start, .8f, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(projected, out end, 1.3f, NavMesh.AllAreas) ||
                Mathf.Abs(end.position.y - position.y) > 1.2f)
                return predictedPosition = position;
            if (NavMesh.Raycast(start.position, end.position, out wall, NavMesh.AllAreas))
            { PredictionConfidence *= .6f; return predictedPosition = wall.position; }
            return predictedPosition = end.position;
        }

        public Vector3 Predict(MonsterPerception sight, MonsterMemory memory)
        {
            // Never read a player transform here. All inputs are snapshots from successful vision.
            landingPrediction = false;
            if (!sight.CanSeePlayer) return predictedPosition = memory.LatestPosition;
            Vector3 position = sight.ObservedPosition, velocity = sight.ObservedVelocity;
            if(ShouldDelayTurn(velocity,Time.time))return predictedPosition;
            if (sight.ObservedGrounded)
            {
                lastGroundedEvidence = position; hasGroundedEvidence = true;
                CurrentPredictionTime=PredictionHorizon(Vector3.Distance(transform.position,position),sight.Trail.TurnRate);
                PredictionConfidence=Confidence(sight.Trail);
                Vector3 futureVelocity=Vector3.ProjectOnPlane(velocity, Vector3.up);
                if(sight.Trail.SegmentSamples>=5 && sight.Trail.Trend==MovementTrend.Turning && sight.Trail.Coherence>.65f)
                {
                    float turn=Mathf.Clamp(sight.Trail.SignedTurnRate*CurrentPredictionTime*.3f,-35,35);
                    futureVelocity=Quaternion.Euler(0,turn,0)*futureVelocity;
                    PredictionConfidence*=.75f;
                }
                Vector3 displacement = Vector3.ClampMagnitude(futureVelocity * CurrentPredictionTime, config.MaxPredictionDistance);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(position + displacement, out hit, 1.5f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y-position.y)<1.2f)
                {
                    NavMeshHit start,wall;
                    if(NavMesh.SamplePosition(position,out start,.8f,NavMesh.AllAreas) && NavMesh.Raycast(start.position,hit.position,out wall,NavMesh.AllAreas))
                    {PredictionConfidence*=.65f;return predictedPosition=wall.position;}
                    return predictedPosition = hit.position;
                }
                PredictionConfidence*=.5f;
                return predictedPosition = position;
            }
            if (TryLanding(position,velocity,sight.ObservedGravity,sight.player != null ? sight.player.transform : null,out predictedPosition))
            { landingPrediction = true; return predictedPosition; }
            return predictedPosition = hasGroundedEvidence ? lastGroundedEvidence : memory.LastSeenPosition;
        }

        public float PredictionHorizon(float distance,float turnRate)
        {
            float time=Mathf.Clamp(distance/Mathf.Max(.1f,ChaseSpeed)*config.PredictionTime,config.MinPredictionTime,config.MaxPredictionTime);
            return Mathf.Max(config.MinPredictionTime,time/(1+Mathf.Max(0,turnRate)/100));
        }
        public Vector3 PredictLost(MonsterMemory memory)
        {
            // Advance only from the last sensory snapshot, never from a hidden transform.
            float elapsed=Mathf.Clamp(Time.time-memory.LatestTime,0,config.LostSightGraceTime);
            Vector3 offset=Vector3.ClampMagnitude(Vector3.ProjectOnPlane(memory.LatestVelocity,Vector3.up)*elapsed,config.MaxPredictionDistance);
            Vector3 position=memory.LatestPosition;
            if(NavMesh.SamplePosition(position,out var start,1f,NavMesh.AllAreas) &&
                NavMesh.SamplePosition(position+offset,out var end,1.5f,NavMesh.AllAreas) &&
                Mathf.Abs(end.position.y-position.y)<1.2f)
                return NavMesh.Raycast(start.position,end.position,out var wall,NavMesh.AllAreas)?wall.position:end.position;
            return position;
        }
        public float Confidence(PlayerTrailHistory trail)
        {
            if(trail.SegmentSamples<3)return .25f;
            return Mathf.Clamp((.45f+.45f*trail.Coherence)/(1+trail.TurnRate/160),.15f,.9f);
        }
        public bool ShouldDelayTurn(Vector3 velocity,float now)
        {
            var direction=Vector3.ProjectOnPlane(velocity,Vector3.up);
            if(pendingReaction)
            {
                if(now<reactionUntil)return true;
                pendingReaction=false;acceptedDirection=direction;return false;
            }
            if(hasPrediction && direction.sqrMagnitude>1 && acceptedDirection.sqrMagnitude>1 &&
                Vector3.Angle(direction,acceptedDirection)>=config.ReactionTurnAngle)
            {pendingReaction=true;reactionUntil=now+config.ReactionDelay;return config.ReactionDelay>0;}
            acceptedDirection=direction;hasPrediction=true;return false;
        }

        public bool TryLanding(Vector3 position, Vector3 velocity, float gravity, Transform observedActor, out Vector3 landing)
        {
            landing = position;
            Vector3 previous = position + Vector3.up * 0.15f;
            for (float t = 0.15f; t <= 2.5f; t += 0.15f)
            {
                Vector3 point = position + velocity * t + Vector3.up * (0.5f * Mathf.Min(-0.1f,gravity) * t*t + 0.15f);
                if (Vector3.ProjectOnPlane(point-position,Vector3.up).magnitude > config.MaxPredictionDistance) return false;
                Vector3 delta = point-previous;
                int count = Physics.RaycastNonAlloc(previous,delta.normalized,hits,delta.magnitude,config.EnvironmentMask,QueryTriggerInteraction.Ignore);
                if (count == hits.Length) return false;
                int nearest=-1; float best=float.PositiveInfinity;
                for(int i=0;i<count;i++)
                {
                    var tr=hits[i].transform;
                    if(tr.IsChildOf(transform)||(observedActor!=null&&tr.IsChildOf(observedActor)))continue;
                    if(hits[i].distance<best){best=hits[i].distance;nearest=i;}
                }
                if(nearest>=0)
                {
                    if(hits[nearest].normal.y<0.65f || velocity.y+gravity*t>0) return false;
                    NavMeshHit nav;
                    if(!NavMesh.SamplePosition(hits[nearest].point,out nav,1,NavMesh.AllAreas) || Mathf.Abs(nav.position.y-hits[nearest].point.y)>0.5f)return false;
                    landing=nav.position;return true;
                }
                previous=point;
            }
            return false;
        }
    }
}
