using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.SkyBeast;
using CampusRift.Levels;
namespace CampusRift.Enemies
{
    [DisallowMultipleComponent]
    public sealed class ExpandedEnemyRuntime:MonoBehaviour
    {
        public bool Busy{get;private set;}
        public int SummonCount{get;private set;}
        public int Teleports{get;private set;}
        public int Dives{get;private set;}
        public int Fireballs{get;private set;}
        public float WarningAt{get;private set;}
        public float ImpactAt{get;private set;}
        public Vector3 WarnedPoint{get;private set;}
        public IReadOnlyList<EnemyInstance> Owned=>owned;
        public static event Action<EnemyInstance,int> Summoned;
        readonly List<EnemyInstance> owned=new List<EnemyInstance>(4);
        EnemyInstance owner;FlyingMotor flying;EnemyTelegraph warning,healRing;Coroutine action;
        float nextSpecial,nextSummon,nextHeal,nextShield,nextTrail;Vector3 priorTrail;
        BurningGround trailGround;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetEvents(){Summoned=null;}
        void Awake(){owner=GetComponent<EnemyInstance>();if(owner.ARSession)return;flying=GetComponent<FlyingMotor>();owner.Died+=Died;}
        public void ResetLife()
        {
            Cancel();ReleaseOwned();SummonCount=Teleports=Dives=Fireballs=0;nextSpecial=Time.time+3;
            nextSummon=Time.time+12;nextHeal=Time.time+1;nextShield=Time.time+4;nextTrail=Time.time+1;priorTrail=transform.position;
        }
        void Update()
        {
            if(owner==null||!owner.Alive)return;
            if(Busy&& (owner.Motor.Held||DomainRuntime.Suppresses(transform.position)))Cancel();
            if(Time.timeScale<=0||LevelDirector.Instance!=null&&LevelDirector.Instance.CinematicPaused)return;
            if(owner.archetype.id=="trieu-hon-su")
            {
                if(owner.scaling.level>=8&&Time.time>=nextHeal){nextHeal=Time.time+1;HealAllies(1);}
                if(owner.scaling.level>=10&&Time.time>=nextShield&&!DomainRuntime.Suppresses(transform.position)){nextShield=Time.time+12;ShieldAllies();}
                if(owner.scaling.level>=8&&(healRing==null||!healRing.Live))healRing=EnemyTelegraph.Show(transform.position,transform.forward,6,1,color:new Color(.15f,1.5f,.85f));
            }
            if(owner.archetype.id=="hoa-linh"&&Time.time>=nextTrail&&(transform.position-priorTrail).sqrMagnitude>1.44f)
            {
                nextTrail=Time.time+1.2f;priorTrail=transform.position;
                if(trailGround==null){var g=new GameObject("P19 Fire Spirit trails");g.transform.SetParent(EnemyPool.Ensure().transform);trailGround=g.AddComponent<BurningGround>();}
                trailGround.SpawnSmallAt(transform.position,FireBreathProfile.Load(Mathf.Clamp(owner.scaling.level,8,10)));
            }
        }
        // Called from Chase before ordinary attacks; support casters retain ranged back-line movement.
        public bool Think(Transform target)
        {
            if(Busy)return true;
            if(target==null||owner.Motor.Held||DomainRuntime.Suppresses(transform.position))return owner.archetype.id=="duc-yeu";
            if(owner.archetype.id=="trieu-hon-su"&&Time.time>=nextSummon)
            {if(EnemyDirector.Ensure().TryAcquire(owner)){nextSummon=Time.time+12;Force("summon",target);return true;}}
            if(Time.time>=nextSpecial&&EnemyDirector.Ensure().TryAcquire(owner))
            {
                if(owner.archetype.id=="anh-yeu"&&Vector3.Distance(transform.position,target.position)<25){nextSpecial=Time.time+8;Force("teleport",target);return true;}
                if(owner.archetype.id=="duc-yeu"&&ShelterDetector.AtFeet(target.position)==Shelter.Outdoor){nextSpecial=Time.time+6;Force(owner.scaling.level>=9&&Fireballs<=Dives?"fireball":"dive",target);return true;}
                EnemyDirector.Instance.Release(owner);
            }
            return owner.archetype.id=="duc-yeu";
        }
        public void Force(string id,Transform target){Cancel();if(owner.Alive&&!DomainRuntime.Suppresses(transform.position))action=StartCoroutine(Execute(id,target));}
        IEnumerator Execute(string id,Transform target)
        {
            Busy=true;owner.Motor.Stop();if(flying!=null)flying.Manual=true;
            Vector3 destination=target!=null?target.position:transform.position;
            if(id=="teleport")
            {
                Vector3 want=destination-(target!=null?target.forward:transform.forward)*2.4f;
                if(!NavMesh.SamplePosition(want,out var h,1.2f,NavMesh.AllAreas)||Mathf.Abs(h.position.y-destination.y)>1.5f||
                    !CombatLine.Clear(destination+Vector3.up,h.position+Vector3.up,transform)){Finish();yield break;}destination=h.position;
            }
            else if(id=="summon")destination=transform.position;
            else if(!FlyingMotor.OutdoorPoint(destination,out destination)){Finish();yield break;}
            WarnedPoint=destination;WarningAt=Time.time;float delay=id=="teleport"?.5f:id=="dive"?.6f:.8f;
            warning=EnemyTelegraph.Show(destination,transform.forward,id=="summon"?2.8f:2,delay+(id=="dive"||id=="fireball"?.35f:0),color:id=="teleport"?new Color(1.6f,.25f,2):id=="summon"?new Color(.25f,1.6f,.8f):new Color(2.5f,.4f,.1f));
            if(id=="teleport")P19EnemyFeedback.Smoke(destination,new Color(.48f,.07f,.8f),1.8f);
            if(id=="summon")P19EnemyFeedback.Smoke(destination,new Color(.08f,.55f,.35f),2);
            owner.GetComponent<EnemyConcealment>()?.RevealFor(delay+1);
            if(owner.Animation!=null)
            {
                string pose=id=="teleport"?"Teleport_Vanish":id=="summon"?"Summon_Cast":id=="dive"?"Dive":"Fireball_Cast";
                bool bespoke=owner.Animation.HasState(pose);
                var clip=bespoke?owner.Animation.profile.Clip(pose):null;
                owner.Animation.BeginAttack(delay,bespoke?pose:owner.Animation.profile.special,clip!=null?clip.length*.5f:-1);
            }
            while(Time.time<WarningAt+delay){if(!owner.Alive||owner.Motor.Held){Finish();yield break;}yield return null;}
            if(id=="teleport")
            {
                P19EnemyFeedback.Smoke(transform.position,new Color(.2f,.02f,.35f));owner.Motor.Place(destination);Teleports++;
                // Arrival does not deal unavoidable damage: ordinary attack must wind up afterwards.
            }
            else if(id=="summon")
            {
                owned.RemoveAll(e=>e==null||!e.Alive);var template=Resources.Load<EnemyArchetype>("P12/TieuYeu");int spawned=0;
                int cap=LevelDirector.Instance?.ConcurrentCap??(Controls.CampusInput.Mobile?9:14);
                for(int i=0;i<2&&owned.Count<4&&EnemyDirector.Instance.Active.Count<cap;i++)
                {
                    var e=EnemyPool.Ensure().Spawn(template,destination+transform.right*(i==0?-2.2f:2.2f),owner.scaling,false);
                    if(e!=null){owned.Add(e);e.Died+=ChildDied;spawned++;LevelDirector.Instance?.TrackSummoned(e);}
                }
                if(spawned>0){SummonCount++;Summoned?.Invoke(owner,SummonCount);}
            }
            else
            {
                Vector3 start=transform.position;float t=0;bool reached=true;
                var fire=P19EnemyFeedback.Vfx?.Spawn(SkillVfxKind.FireBloom,start,new Color(2,.45f,.06f),.45f,.7f);
                while(t<.35f)
                {
                    if(!owner.Alive||owner.Motor.Held){Finish();yield break;}t+=Time.deltaTime;
                    Vector3 p=Vector3.Lerp(start,destination+Vector3.up*.5f,Mathf.Clamp01(t/.35f));
                    if(id=="dive"){if(!flying.MoveSafely(p)){reached=false;break;}}
                    else if(fire!=null)fire.Position=p;
                    yield return null;
                }
                if(reached)
                {
                    P19EnemyFeedback.HitPlayer(owner,destination,2,owner.Damage,id,element:id=="fireball"?Element.Hoa:Element.Moc);
                    EnemyAbilityRunner.Feedback(destination,2);
                    if(id=="dive")Dives++;else{Fireballs++;FireBreathCycle.Instance?.Ground.SpawnSmallAt(destination,FireBreathProfile.Load(Mathf.Clamp(owner.scaling.level,8,10)));}
                }
                if(id=="dive"&&flying!=null)flying.MoveSafely(new Vector3(transform.position.x,flying.GroundPoint.y+4.5f,transform.position.z));
            }
            ImpactAt=Time.time;warning?.Hide();warning=null;yield return new WaitForSeconds(.4f);Finish();
        }
        public void HealAllies(float seconds)
        {
            if(owner.scaling.level<8||owner.Motor.Held||DomainRuntime.Suppresses(transform.position))return;
            bool healed=false;foreach(var e in EnemyDirector.Ensure().Active)if(e!=null&&e!=owner&&e.Alive&&EnemyAbilityRunner.InRange(e.transform.position,transform.position,6)&&CombatLine.Clear(transform.position+Vector3.up,e.transform.position+Vector3.up,transform)){healed|=e.Vitality.Health<e.Vitality.maxHealth;e.Vitality.Heal(e.Vitality.maxHealth*.03f*seconds);}
            var v=P19EnemyFeedback.Vfx;if(healed&&v!=null)v.Spawn(SkillVfxKind.Ring,transform.position,new Color(.2f,1,.6f),.5f,.8f,v.config.bell);
            if(healed)SupportPose("Heal_Channel");
        }
        public int ShieldAllies()
        {
            if(owner.scaling.level<10)return 0;int n=0;
            foreach(var e in EnemyDirector.Ensure().Active.Where(e=>e!=null&&e!=owner&&e.Alive&&EnemyAbilityRunner.InRange(e.transform.position,transform.position,6)).OrderBy(e=>(e.transform.position-transform.position).sqrMagnitude).Take(3))
            {var ward=e.GetComponent<EnemyWard>()??e.gameObject.AddComponent<EnemyWard>();ward.Grant(e.Vitality.maxHealth*.2f);n++;}var v=P19EnemyFeedback.Vfx;if(n>0&&v!=null)v.Spawn(SkillVfxKind.Ring,transform.position,new Color(.2f,.7f,1),.5f,.8f,v.config.iceCast);if(n>0)SupportPose("Shield_Cast");return n;
        }
        // Support feedback never takes priority over spawn, control, a strike or a special warning.
        void SupportPose(string state)
        {
            if(Busy||owner.Motor.Held||owner.Brain.State!=MinionState.Chase||owner.Animation==null||owner.Animation.Dead||!owner.Animation.HasState(state))return;
            var clip=owner.Animation.profile.Clip(state);owner.Animation.Play(state,clip!=null?clip.length/.8f:1,.8f);
        }
        public static EnemyInstance RetreatTarget(EnemyInstance e)
        {
            if(e==null||e.scaling.aiTier<2||e.archetype.id=="trieu-hon-su"||e.Vitality.Health>e.Vitality.maxHealth*.25f)return null;
            EnemyInstance best=null;float d=1600;
            foreach(var other in EnemyDirector.Ensure().Active)if(other!=null&&other.Alive&&other.archetype.id=="trieu-hon-su"&&Mathf.Abs(other.transform.position.y-e.transform.position.y)<2)
            {float s=(other.transform.position-e.transform.position).sqrMagnitude;if(s<d){var path=new NavMeshPath();if(NavMesh.CalculatePath(e.transform.position,other.transform.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete){best=other;d=s;}}}return best;
        }
        void ChildDied(EnemyInstance child){child.Died-=ChildDied;owned.Remove(child);}
        void Died(EnemyInstance e){Cancel();foreach(var child in owned.ToArray())if(child!=null){child.Died-=ChildDied;if(child.Alive)StartCoroutine(DissolveOwned(child));}owned.Clear();healRing?.Hide();}
        IEnumerator DissolveOwned(EnemyInstance e)
        {
            e.Brain.enabled=false;e.GetComponent<EnemyAbilityRunner>()?.Cancel();e.Motor.Stop();foreach(var c in e.GetComponentsInChildren<Collider>())c.enabled=false;
            float t=0;while(t<.65f&&e.gameObject.activeSelf){t+=Time.deltaTime;e.Animation?.Dissolve(t/.65f);yield return null;}
            LevelDirector.Instance?.ForgetSummoned(e);EnemyPool.Instance?.Release(e);
        }
        void ReleaseOwned(){foreach(var e in owned.ToArray())if(e!=null){e.Died-=ChildDied;if(e.gameObject.activeSelf){LevelDirector.Instance?.ForgetSummoned(e);EnemyPool.Instance?.Release(e);}}owned.Clear();}
        void Finish(){Busy=false;action=null;warning?.Hide();warning=null;if(flying!=null)flying.Manual=false;EnemyDirector.Instance?.Release(owner);}
        public void Cancel(){if(action!=null)StopCoroutine(action);Finish();}
        void OnDisable(){Cancel();ReleaseOwned();healRing?.Hide();}
        void OnDestroy(){if(owner!=null)owner.Died-=Died;if(trailGround!=null)Destroy(trailGround.gameObject);}
    }
}
