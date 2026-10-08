using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRift.Monsters
{
    [DisallowMultipleComponent]
    public sealed class InterceptionPlanner : MonoBehaviour
    {
        public MonsterAIConfig config;
        public bool HasIntercept { get; private set; }
        public Vector3 SelectedInterceptPoint { get; private set; }
        public string TargetRoom { get; private set; }
        public float PlayerETA { get; private set; }
        public float MonsterETA { get; private set; }
        public float InterceptScore { get; private set; }
        public float PredictionConfidence { get; private set; }
        public IReadOnlyList<DestinationCandidate> Candidates => candidates;
        readonly List<DestinationCandidate> candidates=new List<DestinationCandidate>(8);
        readonly List<Vector3> route=new List<Vector3>(64);
        readonly Vector3[] corners=new Vector3[256];
        NavMeshPath path;
        MonsterNavigation navigation;
        float nextPlan;
        public int PlanCount { get; private set; }
        void Awake(){navigation=GetComponent<MonsterNavigation>();path=new NavMeshPath();}

        public static bool ArrivesFirst(float playerDistance,float playerSpeed,float monsterDistance,float monsterSpeed,float margin,out float playerETA,out float monsterETA)
        {
            playerETA=playerDistance/Mathf.Max(.5f,playerSpeed);monsterETA=monsterDistance/Mathf.Max(.5f,monsterSpeed);
            return playerDistance>0 && monsterDistance>=0 && monsterETA+margin<playerETA;
        }
        public void Plan(MonsterPerception sight,MonsterMemory memory,MonsterPrediction prediction)
        {
            if(config==null || Time.time<nextPlan || prediction.Reacting)return;
            nextPlan=Time.time+config.StrategicRefreshRate;PlanCount++;
            HasIntercept=false;InterceptScore=0;candidates.Clear();
            if(!config.EnableInterception || !memory.HasEvidence)return;
            Vector3 evidence=sight.CanSeePlayer?sight.ObservedPosition:memory.LatestPosition;
            Vector3 velocity=sight.CanSeePlayer?sight.ObservedVelocity:memory.LatestVelocity;
            PredictionConfidence=(sight.CanSeePlayer?prediction.PredictionConfidence:memory.MemoryConfidence*.6f);
            float speed=Vector3.ProjectOnPlane(velocity,Vector3.up).magnitude;
            if(speed<2 || PredictionConfidence<config.MinimumInterceptConfidence)return;
            // Airborne interceptions are supplied by landing prediction, not a secret ground target.
            if(sight.CanSeePlayer && !sight.ObservedGrounded)return;
            DestinationPredictor.Build(navigation.roomGraph,evidence,velocity,PredictionConfidence,config,sight,candidates);
            foreach(var candidate in candidates)
            {
                float playerDistance,monsterDistance;
                bool playerRoute=RouteDistance(evidence,candidate.Position,out playerDistance);
                bool monsterRoute=RouteDistance(transform.position,candidate.Position,out monsterDistance);
                if(!monsterRoute)continue; // A guessed monster shortcut must never justify interception.
                if(!playerRoute)playerDistance=Vector3.Distance(evidence,candidate.Position); // conservative lower bound
                float peta,meta;
                if(!ArrivesFirst(playerDistance,speed,monsterDistance,navigation.ChaseSpeed,config.InterceptionSafetyMargin,out peta,out meta))continue;
                if(peta>10 || peta<.6f)continue;
                float gain=Mathf.Clamp01((peta-meta-config.InterceptionSafetyMargin)/3);
                float score=gain*.4f+candidate.TopologyScore*.25f+PredictionConfidence*.25f+candidate.DirectionScore*.1f;
                if(!playerRoute)score*=.7f;
                if(score<=InterceptScore)continue;
                HasIntercept=true;InterceptScore=score;SelectedInterceptPoint=candidate.Position;TargetRoom=candidate.Room;
                PlayerETA=peta;MonsterETA=meta;
            }
        }
        public bool RouteDistance(Vector3 from,Vector3 to,out float distance)
        {
            distance=0;NavMeshHit a,b;
            if(!NavMesh.SamplePosition(from,out a,1.2f,NavMesh.AllAreas) || Mathf.Abs(a.position.y-from.y)>1 ||
                !NavMesh.SamplePosition(to,out b,1.2f,NavMesh.AllAreas) || Mathf.Abs(b.position.y-to.y)>1)return false;
            if(path==null)path=new NavMeshPath();
            if(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path) && path.status==NavMeshPathStatus.PathComplete)
            {
                int count=path.GetCornersNonAlloc(corners);if(count==corners.Length)return false;
                for(int i=1;i<count;i++)distance+=Vector3.Distance(corners[i-1],corners[i]);return true;
            }
            return RoomPathfinder.Find(navigation.roomGraph,a.position,b.position,route,out distance);
        }
    }
}
