using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    // Prewarmed persistent effects survive the originating cast, then release independently.
    public sealed class SkillMasteryFields:MonoBehaviour
    {
        enum Kind { Fire, Chill, Mountain }
        sealed class Field{public bool active;public Kind kind;public Vector3 p,forward;public float until,next,attack;public SkillSet2VisualBatch.Slot shape;public int obstacle=-1;}
        readonly Field[] fields=new Field[12];readonly GameObject[] obstacles=new GameObject[2];
        readonly MonsterVitality[] targets=new MonsterVitality[128];SkillVfxPool pool;GameObject root;
        public int ActiveCount {get;private set;}
        void Awake()
        {
            pool=GetComponent<SkillVfxPool>();for(int i=0;i<fields.Length;i++)fields[i]=new Field();root=new GameObject("P18 pooled mastery fields");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,gameObject.scene);
            for(int i=0;i<2;i++){var go=new GameObject("Five Finger Mountain obstacle "+i);go.transform.SetParent(root.transform);var body=go.AddComponent<BoxCollider>();body.center=Vector3.up*.9f;body.size=new Vector3(3.2f,1.8f,1.5f);var nav=go.AddComponent<NavMeshObstacle>();nav.shape=NavMeshObstacleShape.Box;nav.center=body.center;nav.size=body.size;nav.carving=true;nav.carveOnlyStationary=true;go.SetActive(false);obstacles[i]=go;}
        }
        Field Begin(Kind kind,Vector3 p,Vector3 forward,float duration,float attack=0)
        {foreach(var f in fields)if(!f.active){f.active=true;f.kind=kind;f.p=p;f.forward=forward;f.until=Time.time+duration;f.next=Time.time+.5f;f.attack=attack;f.shape=null;f.obstacle=-1;return f;}return null;}
        public void FireWall(Vector3 p,Vector3 direction,float attack)
        {
            var f=Begin(Kind.Fire,p,direction,4,attack);if(f==null)return;f.shape=pool.Shapes.Spawn(SkillShape.FireWall,p,new Color(1,.28f,.03f),4.5f,Vector3.one,Quaternion.LookRotation(direction));if(f.shape!=null)f.shape.Opacity=.35f;
            pool.Spawn(SkillVfxKind.FireField,p,new Color(1,.3f,.04f),4.4f,2.5f,pool.config.fireHit,pool.config.fireCast);
        }
        public void ChillZone(Vector3 p)
        {var f=Begin(Kind.Chill,p,Vector3.forward,3);if(f==null)return;pool.Spawn(SkillVfxKind.Ring,p,new Color(.12f,.8f,1),3.5f,2);pool.Spawn(SkillVfxKind.FrostMist,p,new Color(.2f,.85f,1),3.5f,2);pool.Spawn(SkillVfxKind.Scorch,p,new Color(.02f,.1f,.2f),3.5f,2);}
        public void Mountain(Vector3 p,Quaternion q)
        {var f=Begin(Kind.Mountain,p,Vector3.forward,5);if(f==null)return;for(int i=0;i<2;i++)if(!obstacles[i].activeSelf){f.obstacle=i;obstacles[i].transform.SetPositionAndRotation(p,q);obstacles[i].SetActive(true);break;}f.shape=pool.Shapes.Spawn(SkillShape.Mountain,p,new Color(.8f,.38f,.09f),5.5f,Vector3.one,q);pool.Spawn(SkillVfxKind.SwordDust,p+Vector3.up,new Color(.8f,.4f,.1f),1,2);}
        void Update()
        {
            ActiveCount=0;foreach(var f in fields)
            {
                if(!f.active)continue;if(Time.time>=f.until){Release(f);continue;}ActiveCount++;if(f.kind==Kind.Mountain)continue;
                int n=0;foreach(var m in MonsterVitality.Active)if(m!=null&&!m.Defeated&&n<targets.Length)targets[n++]=m;
                bool tick=Time.time>=f.next;if(tick)f.next+=.5f;
                for(int i=0;i<n;i++){var m=targets[i];var d=m.transform.position-f.p;if(Mathf.Abs(d.y)>3)continue;
                    bool inside=f.kind==Kind.Chill?Vector3.ProjectOnPlane(d,Vector3.up).sqrMagnitude<=4:Mathf.Abs(Vector3.Dot(d,f.forward))<=.9f&&Mathf.Abs(Vector3.Dot(d,Vector3.Cross(Vector3.up,f.forward)))<=3;
                    if(!inside||!CombatLine.Clear(f.p+Vector3.up,m.transform.position+Vector3.up,transform))continue;var status=m.GetComponent<StatusEffectHost>();
                    if(f.kind==Kind.Chill)status?.Apply(StatusType.Chill,.18f,.5f,gameObject);
                    else if(tick){var hit=DamageCalculator.Compute(f.attack,.3f,Element.Hoa,m,0,1.5f,null,DamageSource.Skill);hit.attacker=gameObject;hit.point=m.transform.position+Vector3.up;hit.skillId="tam-muoi-chan-hoa-wall";hit.isArea=true;m.ApplyDamage(hit);status?.Apply(StatusType.Burn,3,f.attack*.1f,gameObject);}
                }
            }
        }
        void Release(Field f){f.active=false;f.shape?.Fade(.5f);if(f.obstacle>=0)obstacles[f.obstacle].SetActive(false);f.obstacle=-1;}
        public void Clear(){foreach(var f in fields)if(f.active)Release(f);ActiveCount=0;}
        void OnDisable(){Clear();}void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
