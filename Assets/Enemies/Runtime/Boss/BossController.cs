using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.Combat;
using CampusRift.Levels;
namespace CampusRift.Enemies
{
    public enum BossAttack { Roar, LeapSlam, ShadowDash }
    [DisallowMultipleComponent]
    public sealed class BossController : MonoBehaviour
    {
        public bool PhaseTwo {get;private set;}
        public int PhaseTransitions {get;private set;}
        public int AddsSpawned {get;private set;}
        public bool Busy {get;private set;}
        public BossAttack LastAttack {get;private set;}
        public float WarningAt {get;private set;}
        public float ImpactAt {get;private set;}
        public int Roars,Slams,Dashes;
        public event Action<BossAttack> Warning,Impact;
        EnemyInstance owner;MonsterBrain brain;MonsterCombat combat;MonsterNavigation nav;
        EnemyTelegraph telegraph;Coroutine action;float nextAction;int sequence;bool phasePending;
        Animator animator;Vector3 modelRest;
        public bool ActiveBoss=>owner!=null&&owner.archetype!=null&&owner.archetype.isBoss;
        void Awake(){owner=GetComponent<EnemyInstance>();brain=GetComponent<MonsterBrain>();nav=GetComponent<MonsterNavigation>();combat=GetComponent<MonsterCombat>();animator=GetComponentInChildren<Animator>();modelRest=animator.transform.localPosition;}
        public void ResetLife(){Cancel();PhaseTwo=false;PhaseTransitions=AddsSpawned=Roars=Slams=Dashes=sequence=0;phasePending=false;nextAction=Time.time+4;
            if(ActiveBoss){UI.BossHealthBarUI.Attach(this);StartMusic(false);}}
        void Update()
        {
            if(!ActiveBoss||!owner.Alive)return;
            if(owner.scaling.level==7&&!PhaseTwo&&owner.Vitality.Health<=owner.MaxHealth*.5f){
                PhaseTwo=true;PhaseTransitions++;phasePending=true;
                var config=GetComponent<ShabanEnemyBridge>().RuntimeConfig;config.ChaseSpeed*=1.3f;
                SpawnAdds();StartMusic(true);
            }
            if(Busy||owner.Vitality.Suppressed||owner.Status.Immobilized||Time.time<nextAction)return;
            var p=EnemyDirector.Ensure().FindPlayer();if(p==null||Vector3.Distance(transform.position,p.position)>24)return;
            var attack=phasePending?BossAttack.Roar:PhaseTwo&&sequence%3==2?BossAttack.ShadowDash:sequence%2==0?BossAttack.Roar:BossAttack.LeapSlam;
            phasePending=false;sequence++;Force(attack);
        }
        void SpawnAdds()
        {
            var arch=Resources.Load<EnemyArchetype>("P19/AnhYeu");if(arch==null)return;
            for(int i=0;i<2;i++){Vector3 p=transform.position+Quaternion.Euler(0,i*180,0)*Vector3.forward*4;
                var e=EnemyPool.Ensure().Spawn(arch,p,owner.scaling,false);if(e!=null){AddsSpawned++;LevelDirector.Instance?.TrackSummoned(e);}}
        }
        public void Force(BossAttack attack){Cancel();action=StartCoroutine(Execute(attack));}
        IEnumerator Execute(BossAttack attack)
        {
            Busy=true;LastAttack=attack;brain.enabled=false;combat.Interrupt();combat.enabled=false;nav.Stop();
            int repeats=attack==BossAttack.ShadowDash?3:1;
            for(int i=0;i<repeats;i++){
                var target=EnemyDirector.Ensure().FindPlayer();if(target==null)break;
                Vector3 start=transform.position;Vector3 to=Vector3.ProjectOnPlane(target.position-start,Vector3.up);
                Vector3 direction=to.sqrMagnitude>.01f?to.normalized:transform.forward;
                float distance=Mathf.Min(attack==BossAttack.ShadowDash?10:12,to.magnitude);Vector3 end=start+direction*distance;
                if(!NavMesh.SamplePosition(end,out var hit,1,NavMesh.AllAreas)||NavMesh.Raycast(start,hit.position,out var wall,NavMesh.AllAreas))end=start;
                else end=hit.position;
                float seconds=attack==BossAttack.Roar?1.2f:attack==BossAttack.LeapSlam?.8f:.4f;
                WarningAt=Time.time;Warning?.Invoke(attack);transform.rotation=Quaternion.LookRotation(direction);
                telegraph=EnemyTelegraph.Show(attack==BossAttack.LeapSlam?end:start,direction,attack==BossAttack.Roar?8:attack==BossAttack.LeapSlam?3:1.8f,seconds,
                    attack==BossAttack.ShadowDash?DangerShape.Capsule:DangerShape.Circle,attack==BossAttack.ShadowDash?distance:120);
                owner.Vitality.SetTelegraph(true);
                animator.SetFloat("AttackSpeed",MonsterCombat.ClipImpactTime/seconds);animator.CrossFadeInFixedTime("Attack",.08f,0,0);
                while(Time.time<WarningAt+seconds){if(!owner.Alive||owner.Vitality.Suppressed||owner.Status.Immobilized){Restore();yield break;}yield return null;}
                telegraph?.Hide();telegraph=null;owner.Vitality.SetTelegraph(false);
                if(attack==BossAttack.Roar){Roars++;Stamp(attack);EnemyAbilityRunner.Feedback(start,8);var p=EnemyDirector.Ensure().Player;
                    if(p!=null&&EnemyAbilityRunner.InRange(p.transform.position,start,8)&&CombatLine.Clear(start+Vector3.up,p.transform.position+Vector3.up,transform,true)){
                        var bell=p.GetComponent<GoldenBellRuntime>();if(bell==null||!bell.ShieldActive)PlayerEnemyControl.Ensure(p.gameObject).Stun(1);
                    }
                }else{
                    bool struck=false;float travel=attack==BossAttack.LeapSlam?.65f:.38f,clock=0;
                    while(clock<travel&&owner.Alive){clock+=Time.deltaTime;float t=Mathf.Clamp01(clock/travel);Vector3 want=Vector3.Lerp(start,end,t);
                        if(VoidWall.Blocking(transform.position,want,.7f)!=null)break;
                        if(nav.Ready)nav.Agent.Move(want-transform.position);
                        if(animator.transform!=transform)animator.transform.localPosition=modelRest+Vector3.up*(attack==BossAttack.LeapSlam?Mathf.Sin(t*Mathf.PI)*2.8f:0);
                        if(attack==BossAttack.ShadowDash){var vfx=EnemyDirector.Ensure().Player?.GetComponent<SkillVfxPool>();if(vfx!=null&&Time.frameCount%4==0)vfx.Spawn(SkillVfxKind.Smoke,transform.position+Vector3.up,new Color(.16f,.04f,.3f),.7f,1.4f);
                            if(!struck)struck=Damage(transform.position,1.8f,owner.Damage,"shaban-shadow");}
                        yield return null;
                    }
                    animator.transform.localPosition=modelRest;Stamp(attack);
                    if(attack==BossAttack.LeapSlam){Slams++;Damage(transform.position,3,owner.Damage,"shaban-slam");EnemyAbilityRunner.Feedback(transform.position,3);
                        var vfx=EnemyDirector.Ensure().Player?.GetComponent<SkillVfxPool>();if(vfx!=null)vfx.Spawn(SkillVfxKind.Shockwave,transform.position,new Color(2,.12f,.05f),.65f,3);}
                    else Dashes++;
                }
                yield return new WaitForSeconds(.35f);
            }
            yield return new WaitForSeconds(.6f);Restore();
        }
        void Stamp(BossAttack attack){ImpactAt=Time.time;Impact?.Invoke(attack);}
        bool Damage(Vector3 center,float radius,float amount,string id)
        {
            var p=EnemyDirector.Ensure().Player;if(p==null||!EnemyAbilityRunner.InRange(p.transform.position,center,radius)||!CombatLine.Clear(center+Vector3.up,p.transform.position+Vector3.up,transform,true))return false;
            var info=DamageInfo.Create(amount,Element.Am,DamageSource.Melee,p.transform.position+Vector3.up,(p.transform.position-center).normalized,gameObject);info.skillId=id;return p.ApplyDamage(info);
        }
        void Restore(){Busy=false;action=null;telegraph?.Hide();telegraph=null;if(animator!=null){animator.transform.localPosition=modelRest;animator.speed=1;}
            if(owner!=null){owner.Vitality.SetTelegraph(false);if(owner.Alive){brain.enabled=true;combat.enabled=true;}}
            nextAction=Time.time+(PhaseTwo?3.5f:5);}
        public void Cancel(){if(action!=null)StopCoroutine(action);Restore();}
        void OnDisable(){Cancel();}
        void StartMusic(bool phase2)
        {
            Audio.LevelMusicDirector.Instance?.PlayBoss(owner.scaling.level,phase2);
        }
    }
}
