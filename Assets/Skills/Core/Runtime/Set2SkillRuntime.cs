using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    // Uses the same cast/aim/resource contract as Set1; a separate type keeps its QA scope intact.
    [DefaultExecutionOrder(180)]
    public abstract class Set2SkillRuntime : SkillRuntime
    {
        protected CampusExplorer explorer;protected CampusInput input;protected PlayerStats stats;protected SpiritPower spirit;
        protected PlayerMonsterHealth health;protected SkillVfxPool vfx;protected GroundAimIndicator aim;protected SkillImpact impact;protected SkillLoadout loadout;
        protected float elapsed,readyAt,power;protected bool casting,aiming;protected Vector3 point,direction;
        protected readonly System.Random rng=new System.Random(18018);
        protected readonly MonsterVitality[] targets=new MonsterVitality[128];
        public int LastHitCount {get;protected set;}public float LastDamage {get;protected set;}public bool IsCasting=>casting;
        public override float CooldownRemaining=>CampusRift.Progression.DevMode.NoCooldown ? 0 : Mathf.Max(0,readyAt-Time.time);
        public override void AdvanceCooldown(float seconds){readyAt=Mathf.Max(Time.time,readyAt-Mathf.Max(0,seconds));}
        public override float CooldownDuration=>stats!=null?stats.ScaleCooldown(definition.cooldown*CooldownMultiplier):definition.cooldown*CooldownMultiplier;
        public override bool IsAiming=>aiming;
        protected virtual float Range=>18;protected virtual float AimRadius=>1;protected virtual bool Cone=>false;
        Camera View=>explorer!=null&&explorer.followCamera!=null?explorer.followCamera:Camera.main;
        protected bool Allowed=>isActiveAndEnabled&&IsUnlocked&&health!=null&&!health.IsDead&&(input==null||input.Allowed)&&!(GetComponent<Enemies.PlayerEnemyControl>()?.Stunned??false);
        protected virtual void Awake(){explorer=GetComponent<CampusExplorer>();input=GetComponent<CampusInput>();stats=GetComponent<PlayerStats>();spirit=GetComponent<SpiritPower>();health=GetComponent<PlayerMonsterHealth>();vfx=GetComponent<SkillVfxPool>();aim=GetComponent<GroundAimIndicator>();impact=GetComponent<SkillImpact>();loadout=GetComponent<SkillLoadout>();}
        public override bool BeginAim(){if(!Allowed||!IsReady||casting)return false;loadout?.CancelAll();aiming=true;if(aim!=null){aim.Show(Cone,AimRadius,Accent);aim.Evaluate(View,input,Range,Cone);}RaiseChanged();return true;}
        public override bool Confirm(){if(!aiming)return false;if(aim!=null){aim.Evaluate(View,input,Range,Cone);if(!aim.Valid)return false;return CastAt(aim.Point);}return CastAt(transform.position+transform.forward*8);}
        public override bool QuickCast(){if(aim!=null){aim.Evaluate(View,input,Range,Cone);if(!aim.Valid&&definition.castType==CastType.Aimed)return false;return CastAt(aim.Point);}return CastAt(transform.position+transform.forward*8);}
        public bool CastAt(Vector3 destination)
        {
            if(!Allowed||!IsReady||casting||definition==null||!Set1SkillRuntime.Finite(destination))return false;
            if(definition.castType==CastType.Aimed&&((destination-transform.position).magnitude>Range+.2f||!CombatLine.Clear(transform.position+Vector3.up,destination+Vector3.up*.3f,transform)))return false;
            if(spirit!=null&&!spirit.TrySpend(SpiritCost))return false;Cancel();point=destination;direction=Vector3.ProjectOnPlane(point-transform.position,Vector3.up).normalized;if(direction.sqrMagnitude<.01f)direction=transform.forward;
            CommitCast();power=EffectMultiplier;elapsed=0;casting=true;readyAt=Time.time+CooldownDuration;LastHitCount=0;LastDamage=0;OnCast();RaiseChanged();return true;
        }
        public override void Cancel(){if(!aiming)return;aiming=false;aim?.Hide();RaiseChanged();}
        public override SkillState GetState()=>!IsUnlocked?SkillState.Locked:casting?SkillState.Casting:aiming?SkillState.Aiming:CooldownRemaining>0?SkillState.Cooldown:ReadyOrNoSpirit();
        protected virtual void Update()
        {
            if(health!=null&&health.IsDead){Cancel();if(casting)Finish();return;}
            if(casting){elapsed+=Time.deltaTime;TickCast(Time.deltaTime);}if(!Allowed){Cancel();return;}
            if(aiming&&aim!=null)aim.Evaluate(View,input,Range,Cone);
            if(input==null||CampusInput.Mobile||loadout==null)return;int slot=loadout.IndexOf(this);if(slot<0)return;
            if(input.Pressed(SkillLoadout.SlotAction(slot))){if(definition.castType!=CastType.Aimed)QuickCast();else if(aiming)Confirm();else BeginAim();}
            if(aiming&&input.Pressed(CampusAction.Confirm))Confirm();if(aiming&&input.Pressed(CampusAction.Cancel))Cancel();
        }
        protected abstract void OnCast();protected abstract void TickCast(float dt);protected virtual void Cleanup(){}
        protected void Finish(){casting=false;Cleanup();RaiseChanged();}
        protected int Snapshot(){int n=0;for(int i=0;i<MonsterVitality.Active.Count&&n<targets.Length;i++){var m=MonsterVitality.Active[i];if(m!=null&&!m.Defeated&&m.isActiveAndEnabled)targets[n++]=m;}return n;}
        protected bool InArea(MonsterVitality m,Vector3 center,float radius,bool visible=true){if(m==null||m.Defeated||!m.isActiveAndEnabled)return false;var d=m.transform.position-center;return Mathf.Abs(d.y)<3&&Vector3.ProjectOnPlane(d,Vector3.up).sqrMagnitude<=radius*radius&&(!visible||CombatLine.Clear(center+Vector3.up*.8f,m.transform.position+Vector3.up,transform));}
        protected float Hit(MonsterVitality m,float percent,bool area=true)
        {
            if(m==null||m.Defeated||!m.isActiveAndEnabled)return 0;float before=m.Health;
            var hit=DamageCalculator.Compute(stats!=null?stats.Attack*stats.DamageDealt:20,percent*power*CastDamageMultiplier,definition.element,m,stats!=null?stats.EffectiveCritChance:0,stats!=null?stats.CritDamage:1.5f,rng,DamageSource.Skill);
            hit.attacker=gameObject;hit.skillId=Id;hit.direction=direction;hit.point=m.transform.position+Vector3.up;hit.isArea=area;
            if(!m.ApplyDamage(hit))return 0;float dealt=Mathf.Min(before,Mathf.Max(0,before-m.Health));LastHitCount++;LastDamage+=dealt;impact?.HoldVictim(m,.065f);return dealt;
        }
        protected void Impact(Vector3 p,float size=.8f){vfx.Burst(p+Vector3.up,Accent,size,null,false);impact?.Pulse(.5f,.065f);vfx.Spawn(SkillVfxKind.Scorch,p,SkillVfxPool.Dark(Accent),2,Mathf.Max(.6f,size));}
        public static bool Knockback(MonsterVitality m,Vector3 direction,float distance)
        {
            if(m==null||m.resistHardControl||m.Defeated)return false;var agent=m.GetComponent<NavMeshAgent>();if(agent==null||!agent.enabled||!agent.isOnNavMesh||agent.isOnOffMeshLink)return false;
            var from=m.transform.position;NavMeshHit end,edge;if(!NavMesh.SamplePosition(from+Vector3.ProjectOnPlane(direction,Vector3.up).normalized*distance,out end,1,agent.areaMask)||Mathf.Abs(end.position.y-from.y)>1)return false;
            if(NavMesh.Raycast(from,end.position,out edge,agent.areaMask))end.position=edge.position;return agent.Warp(end.position);
        }
        public override void ReadyOnRestEquip(){Cancel();if(casting)Finish();readyAt=0;}
        protected virtual void OnDisable(){Cancel();if(casting)Finish();}
#if UNITY_EDITOR
        public void ResetCooldownForValidation(){readyAt=0;}
#endif
    }
}
