using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CampusRift.Monsters
{
    public sealed class MonsterDebug : MonoBehaviour
    {
        public bool showDebug;
        [Tooltip("Draw every belief hypothesis (position, weight) in the Scene view.")]
        public bool showBelief = true;
        [Tooltip("Log intent / lift reasoning changes to the Console.")]
        public bool logReasoning;
        [SerializeField] MonsterState currentState;
        [SerializeField] bool canSeePlayer;
        [SerializeField] Vector3 lastSeenPosition, lastHeardPosition, predictedPosition, currentDestination;
        [SerializeField] float memoryConfidence, distanceToPlayer;
        [SerializeField] string currentRoom;
        [SerializeField] string intent;
        [SerializeField] string beliefSummary;
        [SerializeField] string liftSummary;
        [SerializeField] string navigationSummary;
        [SerializeField] MonsterTargetType currentTargetType;
        [SerializeField] float playerConfidence, phantomConfidence;
        [SerializeField] string targetReason;
        MonsterBrain brain; MonsterPerception perception; MonsterMemory memory;
        MonsterNavigation navigation; MonsterPrediction prediction; MonsterSearch search;
        MonsterBelief belief; MonsterElevatorAwareness lifts;
        float nextRefresh;
        string loggedIntent, loggedLift, loggedPlan;
        void Awake()
        {
            brain=GetComponent<MonsterBrain>();perception=GetComponent<MonsterPerception>();memory=GetComponent<MonsterMemory>();
            navigation=GetComponent<MonsterNavigation>();prediction=GetComponent<MonsterPrediction>();search=GetComponent<MonsterSearch>();
        }
        void Start(){belief=GetComponent<MonsterBelief>();lifts=GetComponent<MonsterElevatorAwareness>();}
        void Update()
        {
            if(Keyboard.current!=null && Keyboard.current.f3Key.wasPressedThisFrame)showDebug=!showDebug;
            if(logReasoning)LogChanges();
            if(Time.time<nextRefresh)return;nextRefresh=Time.time+0.5f;
            currentState=brain.CurrentState;canSeePlayer=perception.CanSeePlayer;
            lastSeenPosition=memory.LastSeenPosition;lastHeardPosition=memory.LastHeardPosition;
            predictedPosition=prediction.PredictedPosition;currentDestination=navigation.CurrentDestination;
            memoryConfidence=memory.MemoryConfidence;distanceToPlayer=perception.DistanceToPlayer;
            var location=search.NearestLandmark(transform.position);currentRoom=location!=null?location.RoomID:"Campus / Outdoor";
            intent=brain.Intent;
            if(brain.Assessment!=null)
            {
                currentTargetType=brain.Assessment.CurrentTarget;
                playerConfidence=brain.Assessment.PlayerConfidence;
                phantomConfidence=brain.Assessment.PhantomConfidence;
                targetReason=brain.Assessment.Reason;
            }
            if(!showDebug)return;
            beliefSummary=belief!=null?belief.Summary():"no belief";
            liftSummary=lifts!=null&&lifts.LastEvent.Length>0?$"{lifts.LastEvent} ({Time.time-lifts.LastEventTime:F0}s ago)":"no lift events";
            navigationSummary=$"{navigation.Status}, reachable={navigation.DestinationReachable}, recoveries={navigation.Recoveries}, links={navigation.LinksTraversed}, rides={navigation.LiftRides}"+
                (navigation.UsingLift?"\n  "+navigation.LiftPlan:navigation.LastLiftDecision.Length>0?"\n  last: "+navigation.LastLiftDecision:"");
        }
        void LogChanges()
        {
            if(brain.Intent!=loggedIntent){loggedIntent=brain.Intent;Debug.Log($"[Shaban] {brain.CurrentState}: {loggedIntent}",this);}
            if(lifts!=null && lifts.LastEvent!=loggedLift){loggedLift=lifts.LastEvent;if(loggedLift.Length>0)Debug.Log("[Shaban] "+loggedLift,this);}
            string plan=navigation.UsingLift?navigation.LiftPlan:navigation.LastLiftDecision;
            if(plan!=loggedPlan){loggedPlan=plan;if(plan.Length>0)Debug.Log("[Shaban] "+plan,this);}
        }
        void OnGUI()
        {
            if(!showDebug || brain==null)return;
            GUI.Box(new Rect(12,48,560,245),$"SHABAN  {currentState}\n{intent}\nTarget: {currentTargetType}  Real: {playerConfidence:F2}  Phantom: {phantomConfidence:F2}\n{targetReason}\nVision: {canSeePlayer}  Distance: {(distanceToPlayer<0?"unknown":distanceToPlayer.ToString("F1"))}  Memory: {memoryConfidence:F2}\n"+
                $"{beliefSummary}\n{liftSummary}\nNav: {navigationSummary}\nRoom: {currentRoom}\nDestination: {currentDestination:F1}\nF3: hide debug");
        }
        void OnDrawGizmos()
        {
            if(!showDebug)return;
            var b=GetComponent<MonsterBrain>();if(b==null || b.config==null)return;var c=b.config;
            var eye=transform.position+Vector3.up*1.5f;
            Gizmos.color=Color.yellow;Vector3 previous=eye+Quaternion.Euler(0,-c.VisionAngle/2,0)*transform.forward*c.VisionDistance;
            Gizmos.DrawLine(eye,previous);
            for(int i=1;i<=20;i++)
            {Vector3 next=eye+Quaternion.Euler(0,-c.VisionAngle/2+c.VisionAngle*i/20,0)*transform.forward*c.VisionDistance;Gizmos.DrawLine(previous,next);previous=next;}
            Gizmos.DrawLine(eye,previous);Gizmos.color=new Color(0,0.7f,1,0.35f);Gizmos.DrawWireSphere(transform.position,c.HearingRange);
            if(!Application.isPlaying)return;
            if(memory.HasSeen){Gizmos.color=Color.yellow;Gizmos.DrawSphere(lastSeenPosition,0.2f);}
            if(memory.HasHeard){Gizmos.color=Color.cyan;Gizmos.DrawSphere(lastHeardPosition,0.2f);}
            if(perception.CanSeePhantom){Gizmos.color=new Color(.62f,.3f,1);Gizmos.DrawWireSphere(perception.PhantomPosition+Vector3.up,.5f);}
            Gizmos.color=Color.magenta;Gizmos.DrawWireSphere(predictedPosition,0.4f);
            Gizmos.color=Color.green;Gizmos.DrawLine(transform.position,currentDestination);Gizmos.DrawWireSphere(currentDestination,0.3f);
            var route=navigation.Route;
            Gizmos.color=new Color(0.2f,1f,0.4f,0.8f);
            for(int i=Mathf.Max(1,navigation.RouteIndex);i<route.Count;i++)Gizmos.DrawLine(route[i-1]+Vector3.up*0.3f,route[i]+Vector3.up*0.3f);
            if(currentState==MonsterState.Search){Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(search.Target,0.6f);}
            Gizmos.color=Color.red;Gizmos.DrawWireSphere(transform.position,c.AttackDistance);
            if(showBelief && belief!=null && belief.Active && belief.Graph!=null)
            {
                var particles=belief.Particles;float scale=particles.Length;
                for(int i=0;i<particles.Length;i++)
                {
                    float w=Mathf.Clamp01(particles[i].Weight*scale*0.5f);
                    Gizmos.color=particles[i].Mode==BeliefMode.InLift?new Color(1f,0.5f,0f,0.35f+0.6f*w):
                        particles[i].Mode==BeliefMode.Holding?new Color(0.6f,0.3f,1f,0.35f+0.6f*w):new Color(1f,0.15f,0.15f,0.35f+0.6f*w);
                    Gizmos.DrawCube(belief.PositionOf(particles[i])+Vector3.up*(0.2f+0.05f*(i%7)),Vector3.one*(0.12f+0.25f*w));
                }
            }
#if UNITY_EDITOR
            Handles.Label(eye+Vector3.up*0.6f,currentState+"\n"+intent+"\n"+currentRoom);
#endif
        }
    }
}
