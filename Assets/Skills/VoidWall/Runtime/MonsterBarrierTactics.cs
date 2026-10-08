using UnityEngine;
using UnityEngine.AI;
using CampusRift.Monsters;

namespace CampusRift.Skills
{
    [DisallowMultipleComponent]
    public sealed class MonsterBarrierTactics : MonoBehaviour
    {
        public string Decision {get;private set;}="";
        public int Detours {get;private set;}
        public int Breaches {get;private set;}
        public VoidWall Target {get;private set;}
        MonsterNavigation nav;MonsterCombat combat;MonsterPerception sight;
        NavMeshPath path;
        readonly Vector3[] corners=new Vector3[128];
        float nextChoice,commitUntil;
        bool detour;
        Vector3 approach,previousGoal;
        int revision=-1;
        void Awake(){nav=GetComponent<MonsterNavigation>();combat=GetComponent<MonsterCombat>();sight=GetComponent<MonsterPerception>();path=new NavMeshPath();}

        // Only the brain's observed/heard goal is supplied. No hidden player-position queries.
        public bool Tick(Vector3 knownGoal)
        {
            if(!nav.Ready || nav.Riding || nav.Agent.isOnOffMeshLink || VoidWall.Active.Count==0){Target=null;return false;}
            var candidate=VoidWall.Blocking(transform.position,knownGoal,nav.Agent.radius*0.65f);
            if(candidate==null && Mathf.Abs(knownGoal.y-transform.position.y)>1.5f)
                candidate=VoidWall.Blocking(transform.position,new Vector3(knownGoal.x,transform.position.y,knownGoal.z),nav.Agent.radius*0.65f);
            if(candidate==null)
            {
                // A bent route can encounter a wall that the straight goal segment misses.
                int n=nav.Agent.hasPath?nav.Agent.path.GetCornersNonAlloc(corners):0;
                Vector3 a=transform.position;
                for(int i=0;i<n && i<5;i++){candidate=VoidWall.Blocking(a,corners[i],nav.Agent.radius*0.65f);if(candidate!=null)break;a=corners[i];}
            }
            if(candidate==null || Vector3.Distance(transform.position,candidate.StrikePoint(transform.position))>8 ||
                Mathf.Abs(transform.position.y-candidate.transform.position.y)>1.4f ||
                !sight.ClearLine(sight.Eye,candidate.StrikePoint(transform.position),candidate.transform))
            {Target=null;return false;}
            if(Target!=candidate || revision!=VoidWall.Revision || (knownGoal-previousGoal).sqrMagnitude>9)
            {Target=candidate;nextChoice=0;commitUntil=0;revision=VoidWall.Revision;}
            if(Time.time>=nextChoice && Time.time>=commitUntil)
            {
                previousGoal=knownGoal;nextChoice=Time.time+0.6f;
                Vector3 local=candidate.transform.InverseTransformPoint(transform.position);
                Vector3 closest=candidate.transform.InverseTransformPoint(candidate.StrikePoint(transform.position));
                closest.y=0;closest.z=Mathf.Sign(Mathf.Abs(local.z)>0.01f?local.z:1)*(candidate.config.thickness/2+nav.Agent.radius+0.6f);
                approach=candidate.transform.TransformPoint(closest);
                if(NavMesh.SamplePosition(approach,out var hit,0.65f,nav.Agent.areaMask) && Mathf.Abs(hit.position.y-approach.y)<0.5f)approach=hit.position;
                float speed=Mathf.Max(1,nav.ChaseSpeed);
                float breakSeconds=Vector3.Distance(transform.position,approach)/speed + combat.WindupSeconds +
                    Mathf.Max(0,Mathf.Ceil(candidate.Health/Mathf.Max(1,combat.config.Damage))-1)*combat.CooldownSeconds;
                bool complete=nav.Agent.CalculatePath(knownGoal,path) && path.status==NavMeshPathStatus.PathComplete;
                int n=complete?path.GetCornersNonAlloc(corners):0;
                float length=0;Vector3 from=transform.position;
                for(int i=0;i<n;i++)
                {
                    if(VoidWall.Blocking(from,corners[i],nav.Agent.radius*0.5f)!=null)complete=false;
                    length+=Vector3.Distance(from,corners[i]);from=corners[i];
                }
                detour=complete && n<corners.Length && length/speed<Mathf.Min(candidate.Remaining,breakSeconds)+0.8f;
                if(detour){Detours++;Decision="Void Wall: taking a faster detour";commitUntil=Time.time+1.1f;}
                else{Breaches++;Decision="Void Wall: breaking the blocked route";commitUntil=Time.time+0.8f;}
            }
            if(detour)
            {nav.SetSpeed(nav.config.ChaseSpeed);nav.MoveTo(knownGoal);return true;}
            Vector3 point=candidate.StrikePoint(transform.position);
            if(Vector3.Distance(sight.Eye,point)<=combat.config.AttackDistance+0.15f)
            {
                nav.Stop();Vector3 face=Vector3.ProjectOnPlane(candidate.Center-transform.position,Vector3.up);
                if(face.sqrMagnitude>0.01f)transform.rotation=Quaternion.LookRotation(face);
                combat.TryAttackBarrier(candidate);return true;
            }
            nav.SetSpeed(nav.config.ChaseSpeed);
            if(!nav.MoveTo(approach) && nav.LastMoveFailed)
            {Target=null;nextChoice=Time.time+0.6f;return false;}
            return true;
        }
    }
}
