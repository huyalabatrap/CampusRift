using UnityEngine;
using UnityEngine.AI;
using CampusRift.SkyBeast;
namespace CampusRift.Enemies
{
    [DisallowMultipleComponent]
    public sealed class FlyingMotor:MonoBehaviour
    {
        public Vector3 Velocity {get;private set;}
        public Vector3 GroundPoint {get;private set;}
        public float Height=>transform.position.y-GroundPoint.y;
        public int ObstacleAvoidances {get;private set;}
        public bool Manual {get;set;}
        EnemyInstance owner;Vector3 goal;float nextPlan,nextFlap;Vector3 last;AudioSource wings;AudioClip flap;
        void Awake(){owner=GetComponent<EnemyInstance>();flap=Resources.Load<AudioClip>("P14/wing-flap");wings=gameObject.AddComponent<AudioSource>();wings.playOnAwake=false;wings.spatialBlend=1;wings.rolloffMode=AudioRolloffMode.Linear;wings.minDistance=2;wings.maxDistance=24;wings.volume=.22f;var mixer=CampusRift.UI.SettingsManager.Instance?.Mixer;if(mixer!=null){var groups=mixer.FindMatchingGroups("Monster");if(groups.Length>0)wings.outputAudioMixerGroup=groups[0];}}
        public static bool OutdoorPoint(Vector3 want,out Vector3 point)
        {
            for(int i=0;i<33;i++)
            {
                float a=i*2.39996f,r=i==0?0:2+Mathf.Sqrt(i)*2;var p=want+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
                if(NavMesh.SamplePosition(p,out var h,2.5f,NavMesh.AllAreas)&&ShelterDetector.AtFeet(h.position)==Shelter.Outdoor){point=h.position;return true;}
            }
            point=default;return false;
        }
        public bool ResetLife()
        {
            Manual=false;Velocity=Vector3.zero;ObstacleAvoidances=0;nextFlap=Time.time+.5f;
            if(!OutdoorPoint(transform.position,out var ground))return false;
            owner.Motor.Stop();owner.Motor.Agent.enabled=false;GroundPoint=ground;transform.position=ground+Vector3.up*4.5f;goal=ground;last=transform.position;nextPlan=0;return true;
        }
        public bool MoveSafely(Vector3 want)
        {
            Vector3 delta=want-transform.position;float distance=delta.magnitude;if(distance<.001f)return true;
            if(Physics.SphereCast(transform.position+Vector3.up*.4f,.5f,delta.normalized,out var hit,distance,ShelterDetector.EnvironmentMask,QueryTriggerInteraction.Ignore)){ObstacleAvoidances++;return false;}
            // Check shelter at the surface below every flight step, not just at the destination.
            Vector3 ground=want;ground.y=GroundPoint.y;
            if(!NavMesh.SamplePosition(ground,out var nav,2,NavMesh.AllAreas)||ShelterDetector.AtFeet(nav.position)!=Shelter.Outdoor){ObstacleAvoidances++;return false;}
            GroundPoint=nav.position;transform.position=want;return true;
        }
        void Update()
        {
            if(owner==null||!owner.Alive)return;
            if(Time.time>=nextFlap){nextFlap=Time.time+1.6f;if(flap!=null)wings.PlayOneShot(flap);}
            if(!Manual&&!owner.Motor.Held)
            {
                var p=EnemyDirector.Instance?.FindPlayer();
                if(p!=null&&Time.time>=nextPlan){nextPlan=Time.time+.5f;Vector3 away=Vector3.ProjectOnPlane(transform.position-p.position,Vector3.up).normalized;if(away.sqrMagnitude<.01f)away=transform.right;
                    Vector3 want=EnemyDirector.Instance.Squad!=null&&EnemyDirector.Instance.Squad.TryDestination(owner,out var squadPoint)?squadPoint:p.position+away*6;
                    if(OutdoorPoint(want,out var point))goal=point;}
                Vector3 target=goal+Vector3.up*(4.5f+Mathf.Sin(Time.time*1.2f)*.65f),delta=target-transform.position;
                Vector3 step=Vector3.ClampMagnitude(delta,owner.Speed*Time.deltaTime);
                if(!MoveSafely(transform.position+step))
                    for(int i=0;i<4;i++){var side=Quaternion.Euler(0,90+i*90,0)*Vector3.ProjectOnPlane(step,Vector3.up);side.y=step.y;if(MoveSafely(transform.position+side))break;}
                owner.Motor.FaceTowards(target);
            }
            Velocity=(transform.position-last)/Mathf.Max(Time.deltaTime,.001f);last=transform.position;
        }
        void OnDisable(){Manual=false;Velocity=Vector3.zero;if(wings!=null)wings.Stop();}
    }
}
