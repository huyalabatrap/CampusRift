using UnityEngine;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    [DefaultExecutionOrder(180)]
    public abstract class Set1SkillRuntime : SkillRuntime
    {
        protected CampusExplorer explorer;
        protected CampusInput input;
        protected PlayerStats stats;
        protected SpiritPower spirit;
        protected PlayerMonsterHealth health;
        protected SkillVfxPool vfx;
        protected GroundAimIndicator aim;
        protected SkillImpact impact;
        protected TargetLock targetLock;
        protected SkillLoadout loadout;
        Camera AimCamera=>explorer!=null&&explorer.followCamera!=null?explorer.followCamera:Camera.main;
        protected float elapsed, readyAt, power=1;
        protected Vector3 point,direction;
        protected bool casting,aiming,arUltimate;
        // Seal energy pays for this AR-only empowered cast; ordinary skill costs/CD remain intact.
        public bool CanARUltimate=>ARContext!=null&&Allowed&&!casting&&definition!=null;
        public bool CastARUltimate(Vector3 destination,float multiplier=3)
        {if(!CanARUltimate||!Finite(destination))return false;Cancel();point=ARContext.Constrain(destination);direction=ARContext.battlefield.Root.forward;arUltimate=false;CommitCast();power=EffectMultiplier*multiplier;elapsed=0;casting=true;arUltimate=true;LastHitCount=0;LastDamage=0;CastCount++;OnCast();RaiseChanged();return true;}
        protected bool ARTarget(MonsterVitality m)=>m!=null&&!m.Defeated&&m.isActiveAndEnabled&&m.GetComponent<AR.ARCombatContext>()?.battlefield==ARContext?.battlefield&&!(ARContext?.battlefield?.GetComponent<AR.ARDepthCollision>()?.Hidden(m)??false);
        protected readonly System.Random rng=new System.Random(10010);
        public int CastCount {get;private set;}
        public int LastHitCount {get;protected set;}
        public float LastDamage {get;protected set;}
        public override float CooldownRemaining=>CampusRift.Progression.DevMode.NoCooldown ? 0 : Mathf.Max(0,readyAt-SessionNow);
        public override void AdvanceCooldown(float seconds){readyAt=Mathf.Max(SessionNow,readyAt-Mathf.Max(0,seconds));}
        public override float CooldownDuration=>stats!=null?stats.ScaleCooldown(definition.cooldown*CooldownMultiplier):definition!=null?definition.cooldown*CooldownMultiplier:1;
        public override bool IsAiming=>aiming;
        public bool IsCasting=>casting;
        protected virtual float Range=>18;
        protected virtual float AimRadius=>1;
        protected virtual bool Cone=>false;
        protected virtual bool PreserveCooldownOnRecast=>false;
        protected bool Allowed=>isActiveAndEnabled&&!SessionPaused&&IsUnlocked&&health!=null&&!health.IsDead&&(ARContext!=null||input==null||input.Allowed);
        protected virtual void Awake()
        {
            explorer=GetComponent<CampusExplorer>();input=GetComponent<CampusInput>();stats=GetComponent<PlayerStats>();spirit=GetComponent<SpiritPower>();health=GetComponent<PlayerMonsterHealth>();
            vfx=GetComponent<SkillVfxPool>();aim=GetComponent<GroundAimIndicator>();impact=GetComponent<SkillImpact>();targetLock=GetComponent<TargetLock>();loadout=GetComponent<SkillLoadout>();
        }
        public override bool BeginAim()
        {
            if(!Allowed||!IsReady||casting)return false;
            if(loadout!=null)loadout.CancelAll();aiming=true;
            if(aim!=null){aim.Show(Cone,AimRadius,Accent);aim.Evaluate(AimCamera,input,Range,Cone);}
            RaiseChanged();return true;
        }
        public override bool Confirm()
        {
            if(!aiming)return false;
            // Instant self/chain skills need no valid ground target on touch release.
            if(definition!=null&&definition.castType==CastType.Instant)return QuickCast();
            if(aim!=null){aim.Evaluate(AimCamera,input,Range,Cone);if(!aim.Valid)return false;return CastAt(aim.Point);}
            return CastAt(transform.position+transform.forward*8);
        }
        public override bool QuickCast()
        {
            if(aim!=null){aim.Evaluate(AimCamera,input,Range,Cone);if(!aim.Valid&&definition.castType==CastType.Aimed)return false;return CastAt(aim.Point);}
            return CastAt(transform.position+transform.forward*8);
        }
        public bool CastAt(Vector3 destination)
        {
            if(!Allowed||!IsReady||casting||definition==null||!Finite(destination))return false;
            if(definition.castType==CastType.Aimed && ((destination-transform.position).magnitude>Range+.2f*WorldScale || !CombatLine.Clear(transform.position+Vector3.up*WorldScale,destination+Vector3.up*(.3f*WorldScale),transform)))return false;
            float previousReadyAt=readyAt;bool recast=CooldownRemaining>0;
            if(spirit!=null&&!spirit.TrySpend(SpiritCost))return false;
            Cancel();point=destination;direction=Vector3.ProjectOnPlane(point-transform.position,Vector3.up).normalized;if(direction.sqrMagnitude<.01f)direction=transform.forward;
            arUltimate=false;CommitCast();power=EffectMultiplier;elapsed=0;casting=true;readyAt=recast&&PreserveCooldownOnRecast?previousReadyAt:SessionNow+CooldownDuration;LastHitCount=0;LastDamage=0;CastCount++;OnCast();RaiseChanged();return true;
        }
        public override void Cancel(){if(!aiming)return;aiming=false;if(aim!=null)aim.Hide();RaiseChanged();}
        public override SkillState GetState()=>!IsUnlocked?SkillState.Locked:casting?SkillState.Casting:aiming?SkillState.Aiming:CooldownRemaining>0?SkillState.Cooldown:ReadyOrNoSpirit();
        protected virtual void Update()
        {
            if(SessionPaused)return;
            if(health!=null&&health.IsDead){Cancel();if(casting){casting=false;Cleanup();}return;}
            if(casting){float dt=ARContext!=null?ARContext.battlefield.CombatDelta:Time.deltaTime;elapsed+=dt;TickCast(dt);}
            if(!Allowed){Cancel();return;}
            if(aiming&&aim!=null)aim.Evaluate(AimCamera,input,Range,Cone);
            if(input==null||CampusInput.Mobile||loadout==null)return;
            int slot=loadout.IndexOf(this);if(slot<0)return;
            if(input.Pressed(SkillLoadout.SlotAction(slot)))
            {if(definition.castType==CastType.Instant)QuickCast();else if(aiming)Confirm();else BeginAim();}
            if(aiming&&input.Pressed(CampusAction.Confirm))Confirm();
            if(aiming&&input.Pressed(CampusAction.Cancel))Cancel();
        }
        protected abstract void OnCast();
        protected abstract void TickCast(float dt);
        protected virtual void Cleanup(){}
        protected void Finish(){casting=false;Cleanup();arUltimate=false;RaiseChanged();}
        protected bool Hit(MonsterVitality victim,float percent,DamageSource source=DamageSource.Skill,bool heavy=false)
        {
            if(victim==null||!victim.isActiveAndEnabled||victim.Defeated||(ARContext?.battlefield?.GetComponent<AR.ARDepthCollision>()?.Hidden(victim)??false))return false;
            var info=DamageCalculator.Compute(stats!=null?stats.Attack*stats.DamageDealt:20,percent*power*CastDamageMultiplier,definition.element,victim,stats!=null?stats.EffectiveCritChance:0,stats!=null?stats.CritDamage:1.5f,rng,source);
            info.attacker=gameObject;info.skillId=Id;info.direction=direction;info.point=victim.transform.position+Vector3.up*WorldScale;
            info.isArea=ReactionResolver.AreaSkill(Id);
            info.isHeavy=heavy;
            if(!victim.ApplyDamage(info))return false;LastHitCount++;LastDamage+=info.amount;
            if(impact!=null&&source==DamageSource.Skill)impact.HoldVictim(victim,.065f);
            return true;
        }
        protected bool InArea(MonsterVitality victim,Vector3 center,float radius,bool visible=true)
        {
            if(victim==null||!victim.isActiveAndEnabled||victim.Defeated||(ARContext?.battlefield?.GetComponent<AR.ARDepthCollision>()?.Hidden(victim)??false))return false;Vector3 delta=victim.transform.position-center;radius*=WorldScale;
            return Mathf.Abs(delta.y)<3*WorldScale && Vector3.ProjectOnPlane(delta,Vector3.up).sqrMagnitude<=radius*radius && (!visible||CombatLine.Clear(center+Vector3.up*(.8f*WorldScale),victim.transform.position+Vector3.up*WorldScale,transform));
        }
        public static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.z);
        // QA resets only the cooldown; real resource/unlock/aim validation still runs.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void ResetCooldownForValidation(){readyAt=0;}
#endif
        public override void ReadyOnRestEquip(){Cancel();if(casting){casting=false;Cleanup();}readyAt=0;RaiseChanged();}
        protected virtual void OnDisable(){Cancel();casting=false;Cleanup();}
    }
}
