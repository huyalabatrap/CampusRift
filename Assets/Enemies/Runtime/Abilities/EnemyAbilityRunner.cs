using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Skills;
namespace CampusRift.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAbilityRunner : MonoBehaviour
    {
        public bool Busy {get;private set;}
        public string LastAbility {get;private set;}
        public float WarningAt {get;private set;}
        public float ImpactAt {get;private set;}
        public int Executions {get;private set;}
        public int Explosions {get;private set;}
        public event Action<string> TelegraphStarted, Impact;
        EnemyInstance owner;EnemyTelegraph warning;Coroutine running;bool exploded;float nextAbility;
        string winHold;
        static readonly RaycastHit[] hits=new RaycastHit[24];
        void Awake(){owner=GetComponent<EnemyInstance>();if(owner.ARSession)return;owner.Vitality.DefeatedOnce+=OnDeath;}
        void OnDestroy(){if(owner!=null&&owner.Vitality!=null)owner.Vitality.DefeatedOnce-=OnDeath;}
        public void ResetLife(){Cancel();exploded=false;Executions=Explosions=0;nextAbility=Time.time+3.5f;}
        public bool Unlocked(string id)
        {foreach(var u in owner.archetype.abilities)if(u.ability!=null&&u.ability.id==id&&owner.scaling.level>=u.minLevel)return true;return false;}
        public bool TryUse(Transform target)
        {
            if(Busy||!owner.Alive||owner.Motor.Held||DomainRuntime.Suppresses(transform.position)||Time.time<nextAbility||target==null)return false;
            float distance=Vector3.Distance(transform.position,target.position);
            EnemyAbility selected=null;
            foreach(var u in owner.archetype.abilities)if(u.ability!=null&&owner.scaling.level>=u.minLevel&&!(u.ability is ExplodeAbility)&&distance<=u.ability.range&&distance>=2.3f){selected=u.ability;break;}
            if(owner.archetype.id=="bao-thi" && owner.scaling.level>=7 && distance<=2){if(!EnemyDirector.Ensure().TryAcquire(owner))return false;running=StartCoroutine(Fuse(.8f,target));return true;}
            if(selected==null||!EnemyDirector.Ensure().TryAcquire(owner))return false;
            nextAbility=Time.time+selected.cooldown*(owner.Elite!=null?owner.Elite.AttackIntervalMultiplier:1)*(1-.4f*Mathf.Clamp01((owner.scaling.level-1)/9f));
            running=StartCoroutine(Execute(selected,target));return true;
        }
        public void Force(EnemyAbility ability,Transform target){Cancel();if(!DomainRuntime.Suppresses(transform.position))running=StartCoroutine(Execute(ability,target));}
        void Update(){if(Busy&&owner.Alive&&DomainRuntime.Suppresses(transform.position))Cancel();}
        IEnumerator Execute(EnemyAbility ability,Transform target)
        {
            Busy=true;LastAbility=ability.id;owner.Motor.Stop();Vector3 origin=transform.position;
            Vector3 end=target!=null?target.position:origin+transform.forward*ability.range;
            Vector3 direction=Vector3.ProjectOnPlane(end-origin,Vector3.up).normalized;
            if(direction.sqrMagnitude<.01f)direction=transform.forward;transform.rotation=Quaternion.LookRotation(direction);
            bool charge=ability is ChargeAbility,leap=ability is LeapAbility;
            end=origin+direction*Mathf.Min(ability.range,Vector3.ProjectOnPlane(end-origin,Vector3.up).magnitude);
            WarningAt=Time.time;float impactTime=WarningAt+ability.telegraphSeconds;
            bool fan=ability is SpreadShotAbility;
            warning=EnemyTelegraph.Show(leap?end:origin,direction,charge?1.3f:leap?1.5f:fan?ability.range:ability.radius,ability.telegraphSeconds,
                charge?DangerShape.Capsule:fan?DangerShape.Cone:DangerShape.Circle,charge?ability.range:fan?36:120);
            owner.Animation?.BeginAttack(ability.telegraphSeconds,ability.clip,ability.impactSeconds);
            TelegraphStarted?.Invoke(ability.id);
            while(Time.time<impactTime){if(!owner.Alive||owner.Motor.Held){Finish();yield break;}yield return null;}
            warning?.Hide();warning=null;Executions++;
            if(!leap){ImpactAt=Time.time;Impact?.Invoke(ability.id);}
            if(charge){yield return MoveStrike(direction,ability.range,14,false);}
            else if(leap){yield return MoveStrike(direction,Vector3.Distance(origin,end),10,true);if(!owner.Alive||owner.Motor.Held){Finish();yield break;}var landing=transform.position;ImpactAt=Time.time;Impact?.Invoke(ability.id);HitPlayer(landing,1.5f,owner.Damage,DamageSource.Melee);Feedback(landing,1.5f);}
            else if(ability is SpreadShotAbility){for(int i=-1;i<=1;i++)EnemyProjectilePool.Ensure().Fire(origin+Vector3.up+direction*.5f,Quaternion.Euler(0,i*18,0)*direction,owner.archetype.projectileSpeed,owner.Damage,owner.archetype.element,gameObject);}
            yield return new WaitForSeconds(.5f);Finish();
        }
        public IEnumerator MoveStrike(Vector3 direction,float distance,float speed,bool jump)
        {
            float travelled=0;bool struck=false;var animation=owner.Animation;
            while(travelled<distance && owner.Alive && !owner.Motor.Held)
            {
                float step=Mathf.Min(speed*Time.deltaTime,distance-travelled);Vector3 from=transform.position;
                var barrier=VoidWall.Blocking(from,from+direction*step,.6f);
                if(barrier!=null){if(owner.archetype.id=="thiet-giap-nguu"&&owner.scaling.level>=6)barrier.Damage(barrier.Health,barrier.StrikePoint(from));break;}
                int n=Physics.SphereCastNonAlloc(from+Vector3.up*.65f,.35f,direction,hits,step,~((1<<7)|(1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore);
                bool blocked=false;for(int i=0;i<n;i++)if(hits[i].collider.GetComponentInParent<PlayerMonsterHealth>()==null && !hits[i].collider.transform.IsChildOf(transform)){blocked=true;break;}
                if(blocked || !owner.Motor.OnMesh || NavMesh.Raycast(from,from+direction*step,out var navHit,NavMesh.AllAreas))break;
                owner.Motor.Agent.Move(direction*step);travelled+=step;
                if(!jump&&!struck){struck=HitPlayer(transform.position,1.3f,owner.Damage,DamageSource.Melee);if(struck){PlayerEnemyControl.Ensure(EnemyDirector.Instance.Player.gameObject).Stun(.5f);Feedback(transform.position,1.3f);}}
                if(jump&&animation!=null)animation.AirborneHeight=Mathf.Sin(Mathf.PI*travelled/Mathf.Max(.1f,distance))*1.1f;
                yield return null;
            }
            if(jump&&animation!=null)animation.AirborneHeight=0;
        }
        IEnumerator Fuse(float seconds,Transform target)
        {
            Busy=true;LastAbility="explode";WarningAt=Time.time;owner.Motor.Stop();
            warning=EnemyTelegraph.Show(transform.position,transform.forward,3.5f,seconds);
            owner.Animation?.Play("Sniff",1,seconds);owner.Vitality.SetTelegraph(true);TelegraphStarted?.Invoke("explode");
            yield return new WaitForSeconds(seconds);
            if(owner.Alive){Explode();owner.Vitality.ApplyDamage(DamageInfo.Create(owner.Vitality.Health+1,Element.None,DamageSource.Reaction,transform.position,Vector3.up));}
            owner.Vitality.SetTelegraph(false);Finish();
        }
        void OnDeath(){if(owner.archetype!=null&&owner.archetype.id=="bao-thi"&&!exploded){Cancel();running=StartCoroutine(DeathExplosion());}}
        IEnumerator DeathExplosion(){Busy=true;LastAbility="explode";winHold="bomber-"+GetEntityId();CampusRift.Levels.LevelDirector.Instance?.HoldWin(winHold);WarningAt=Time.time;warning=EnemyTelegraph.Show(transform.position,transform.forward,3.5f,.4f);TelegraphStarted?.Invoke("explode");yield return new WaitForSeconds(.4f);Explode();Finish();}
        void Explode()
        {
            if(exploded)return;exploded=true;Explosions++;ImpactAt=Time.time;warning?.Hide();Impact?.Invoke("explode");
            HitPlayer(transform.position,3.5f,30*owner.scaling.damage,DamageSource.Reaction);
            var targets=MonsterVitality.Active.ToArray();foreach(var victim in targets)if(victim!=owner.Vitality&&!victim.Defeated&&InRange(victim.transform.position,transform.position,3.5f)&&CombatLine.Clear(transform.position+Vector3.up,victim.transform.position+Vector3.up,transform,true))
            {var info=DamageInfo.Create(15*owner.scaling.damage,owner.archetype.element,DamageSource.Reaction,victim.transform.position,(victim.transform.position-transform.position).normalized,gameObject);info.skillId="bao-thi-explode";victim.ApplyDamage(info);}
            Feedback(transform.position,3.5f);
        }
        public static bool InRange(Vector3 a,Vector3 b,float radius)=>Mathf.Abs(a.y-b.y)<2.5f&&Vector3.ProjectOnPlane(a-b,Vector3.up).sqrMagnitude<=radius*radius;
        public bool HitPlayer(Vector3 center,float radius,float damage,DamageSource source)
        {
            var p=EnemyDirector.Ensure().Player;if(p==null||!InRange(p.transform.position,center,radius)||!CombatLine.Clear(center+Vector3.up,p.transform.position+Vector3.up,transform,true))return false;
            var info=DamageInfo.Create(damage,owner.archetype.element,source,p.transform.position+Vector3.up,(p.transform.position-center).normalized,gameObject);info.skillId=LastAbility;return p.ApplyDamage(info);
        }
        public static void Feedback(Vector3 point,float radius)
        {
            var p=EnemyDirector.Ensure().Player;var vfx=p!=null?p.GetComponent<SkillVfxPool>():null;
            if(vfx!=null){vfx.Burst(point+Vector3.up*.3f,new Color(2.7f,.2f,.08f),radius*.45f,vfx.config.fireHit);vfx.Spawn(SkillVfxKind.Scorch,point,new Color(.7f,.05f,.03f),2.3f,radius);}
            if(p!=null && Vector3.Distance(p.transform.position,point)<10)p.GetComponent<GiantHandCameraImpulse>()?.Pulse(.2f);
            if(p!=null && Vector3.Distance(p.transform.position,point)<3){p.GetComponent<SkillImpact>()?.Pulse(.35f,.065f);PlayerEnemyControl.Ensure(p.gameObject).ImpactHold(.065f);}
        }
        void Finish(){Busy=false;if(owner.Animation!=null)owner.Animation.AirborneHeight=0;warning?.Hide();warning=null;if(winHold!=null){CampusRift.Levels.LevelDirector.Instance?.ReleaseWin(winHold);winHold=null;}owner.Vitality.SetTelegraph(false);EnemyDirector.Instance?.Release(owner);running=null;}
        public void Cancel(){if(running!=null)StopCoroutine(running);Finish();}
        void OnDisable(){Cancel();}
    }
}
