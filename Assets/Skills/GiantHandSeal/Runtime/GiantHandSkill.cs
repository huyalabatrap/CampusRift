using UnityEngine;
using UnityEngine.InputSystem;
using CampusRift.Monsters;
using CampusRift.UI;

namespace CampusRift.Skills
{
    [DefaultExecutionOrder(150), DisallowMultipleComponent, RequireComponent(typeof(CampusExplorer))]
    public sealed class GiantHandSkill : MonoBehaviour
    {
        public GiantHandConfig config;
        public bool IsPreviewing {get;private set;}
        public bool IsCasting {get;private set;}
        public HandSealTarget Target {get;private set;}
        public float CooldownRemaining => CampusRift.Progression.DevMode.NoCooldown ? 0 : Mathf.Max(0,readyAt-SessionNow);
        public void AdvanceCooldown(float seconds){readyAt=Mathf.Max(SessionNow,readyAt-Mathf.Max(0,seconds));}
        public void ReadyOnRestEquip(){CancelAll();readyAt=0;Changed?.Invoke();}
        public string Feedback {get;private set;}="";
        public float FeedbackUntil {get;private set;}
        public int ImpactCount {get;private set;}
        public int LastHitCount {get;private set;}
        public float LastDamage {get;private set;}
        public MonsterVitality LastVictim {get;private set;}
        public GiantHandVisual Visual {get;private set;}
        public GiantHandTargeting Targeting {get;private set;}
        public MonsterVitality LockedMonster {get;private set;}
        public MonsterVitality LastAutoTarget {get;private set;}
        public event System.Action Changed;
        readonly Collider[] colliders=new Collider[128];
        readonly MonsterVitality[] victims=new MonsterVitality[64];
        AR.ARCombatContext ar;float Scale=>ar!=null?ar.scale:1;float SessionNow=>ar!=null?ar.Now:Time.time;
        CampusExplorer explorer;
        Controls.CampusInput controls;
        PlayerMonsterHealth health;
        VoidWallSkill wall;
        GiantHandCameraImpulse impulse;
        float readyAt,castAt,nextFeedback;
        GiantHandRuntime runtime;
        public float EffectiveCooldown=>config.cooldown*(runtime!=null?runtime.CooldownMultiplier:1);
        GiantHandVisual twinVisual;bool arTwin;float absorptionPower=1;
        bool impacted,arAbsorption;int gathered;readonly MonsterVitality[] absorbed=new MonsterVitality[6];
        public bool CanARAbsorption=>ar!=null&&InputAllowed&&!IsCasting;
        bool DepthHidden(MonsterVitality victim)=>ar?.battlefield?.GetComponent<AR.ARDepthCollision>()?.Hidden(victim)??false;
        public bool CastARAbsorption(float multiplier=1)
        {if(!CanARAbsorption)return false;CancelPreview();gathered=0;arTwin=false;absorptionPower=multiplier;arAbsorption=true;committed=new HandSealTarget{valid=true,point=ar.battlefield.Root.position,normal=Vector3.up,height=2.5f*Scale,visualScale=Mathf.Min(ar.battlefield.placement.Radius*.8f/2.2f,Scale*2.5f)};castAt=SessionNow;IsCasting=true;impacted=false;foreach(var e in ar.battlefield.GetComponent<AR.ARMonsterDirector>().Actors){if(e==null||!e.Alive||DepthHidden(e.Vitality)||gathered==absorbed.Length)continue;absorbed[gathered++]=e.Vitality;e.Status?.Apply(Combat.StatusType.Pulled,config.summonTime+config.descentTime+.1f,0,gameObject);}runtime?.CommitCast();Visual.Begin(committed,Quaternion.LookRotation(ar.battlefield.Root.forward));Changed?.Invoke();return true;}
        public bool CastARTwin(Vector3 point)
        {
            if(ar==null||!CastAt(point))return false;arTwin=true;
            if(twinVisual==null){var go=new GameObject("AR twin hand reused");go.transform.SetParent(ar.battlefield.Root,true);twinVisual=go.AddComponent<GiantHandVisual>();twinVisual.Initialize(config,transform);}
            var second=committed;second.point=ar.ConstrainVisual(point+ar.battlefield.Root.right*ar.battlefield.placement.Radius*.23f);twinVisual.Begin(second,Quaternion.LookRotation(ar.battlefield.Root.forward));
            var first=committed;first.point=ar.ConstrainVisual(point-ar.battlefield.Root.right*ar.battlefield.placement.Radius*.23f);Visual.Begin(first,Quaternion.LookRotation(ar.battlefield.Root.forward));return true;
        }
        HandSealTarget committed;
        public bool IsUnlocked => ar!=null?runtime!=null&&runtime.IsUnlocked:Learning.LearningSkillGate.Allows(this);
        bool InputAllowed => IsUnlocked && health!=null && health.CurrentHealth>0 && Time.timeScale>0 &&
            (ar!=null?!ar.Paused:(UIStateManager.Instance==null || UIStateManager.Instance.GameplayInputEnabled));

        void Awake()
        {
            ar=GetComponent<AR.ARCombatContext>();explorer=GetComponent<CampusExplorer>();health=GetComponent<PlayerMonsterHealth>();wall=GetComponent<VoidWallSkill>();
            controls=GetComponent<Controls.CampusInput>();
            runtime=GetComponent<GiantHandRuntime>();
            Targeting=new GiantHandTargeting();
            impulse=GetComponent<GiantHandCameraImpulse>();
            var go=new GameObject("Giant Hand Seal (reused)");
            if(ar!=null)go.transform.SetParent(ar.battlefield.Root,true);Visual=go.AddComponent<GiantHandVisual>();Visual.Initialize(config,transform);
        }
        void Update()
        {
            if(ar!=null&&ar.Paused)return;
            if(health==null || health.CurrentHealth<=0){CancelAll();return;}
            if(IsCasting)
            {
                float elapsed=SessionNow-castAt;
                if(arAbsorption&&!impacted)for(int i=0;i<gathered;i++){var m=absorbed[i];if(m==null||m.Defeated||m.resistHardControl||DepthHidden(m))continue;var next=ar.Constrain(Vector3.MoveTowards(m.transform.position,committed.point,ar.battlefield.CombatDelta*3*Scale));var agent=m.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null&&agent.enabled&&agent.isOnNavMesh)agent.Warp(next);else m.transform.position=next;}
                Visual.Animate(elapsed,transform.position);if(arTwin&&twinVisual!=null)twinVisual.Animate(elapsed,transform.position);
                if(!impacted && elapsed>=config.summonTime+config.descentTime){impacted=true;Impact();}
                if(elapsed>=config.summonTime+config.descentTime+config.aftermathTime)
                {IsCasting=false;ReleaseAbsorbed();Visual.Finish();if(twinVisual!=null)twinVisual.Finish();arTwin=false;Changed?.Invoke();}
            }
            if(!InputAllowed){CancelPreview();return;}
            if(ar!=null)return;
            if(controls==null)controls=GetComponent<Controls.CampusInput>();if(controls==null)return;
            if(IsPreviewing && (wall!=null && wall.IsPreviewing))CancelPreview();
            if(controls.Pressed(Controls.CampusAction.Hand))
            {CastNearest();}
            else if(IsPreviewing && controls.Pressed(Controls.CampusAction.Confirm))Confirm();
            if(IsPreviewing && controls.Pressed(Controls.CampusAction.Cancel))CancelPreview();
            if(IsPreviewing)RefreshPreview();
        }
        public bool BeginPreview()
        {
            if(!InputAllowed || IsCasting)return false;
            if(CooldownRemaining>0){Notify("SEAL RECHARGING");return false;}
            var target=Targeting.NearestMonster(transform,config,out var monster);
            if(!target.valid){Notify(target.reason);return false;}
            wall?.CancelPreview();GetComponent<PhantomDecoySkill>()?.CancelPreview();
            LockedMonster=monster;Target=target;IsPreviewing=true;Visual.Preview(Target);Changed?.Invoke();return true;
        }
        public bool CastNearest()
        {
            if(!InputAllowed || IsCasting)return false;
            if(CooldownRemaining>0){Notify("SEAL RECHARGING");return false;}
            var target=Targeting.NearestMonster(transform,config,out var monster);
            if(!target.valid){Notify(target.reason);return false;}
            wall?.CancelPreview();GetComponent<PhantomDecoySkill>()?.CancelPreview();
            LastAutoTarget=monster;
            return CastAt(target.point);
        }
        public void RefreshPreview()
        {
            if(LockedMonster==null || !LockedMonster.isActiveAndEnabled || LockedMonster.Defeated)
            {
                Target=Targeting.NearestMonster(transform,config,out var replacement);
                LockedMonster=replacement;
            }
            else Target=Targeting.Evaluate(LockedMonster.transform.position,transform,config);
            Visual.Preview(Target);
        }
        public void CancelPreview()
        {if(!IsPreviewing)return;IsPreviewing=false;LockedMonster=null;Visual.HidePreview();Changed?.Invoke();}
        public bool Confirm()
        {
            if(!IsPreviewing || !InputAllowed || IsCasting || CooldownRemaining>0)return false;
            RefreshPreview();
            if(!Target.valid){Notify(Target.reason);return false;}
            LastAutoTarget=LockedMonster;
            return CastAt(Target.point);
        }
        // Also used by AI-independent integration tests; all casts pass the same validation.
        public bool CastAt(Vector3 point)
        {
            if(!InputAllowed || IsCasting || CooldownRemaining>0)return false;
            var evaluated=Targeting.Evaluate(point,transform,config);
            if(!evaluated.valid){Notify(evaluated.reason);return false;}
            if(!SkillSpirit.TryPay(this,"dai-thu-an")){CancelPreview();Notify("NOT ENOUGH SPIRIT");return false;}
            arAbsorption=false;arTwin=false;absorptionPower=1;committed=evaluated;Target=evaluated;CancelPreview();
            castAt=SessionNow;readyAt=SessionNow+EffectiveCooldown;IsCasting=true;impacted=false;
            LastHitCount=0;LastDamage=0;LastVictim=null;
            // Bind immediately so a fast chase cannot outrun the hand during its windup.
            // This bounded timer also releases targets if the cast is interrupted.
            int bound=CollectVictims();
            for(int i=0;i<bound;i++)
            {
                victims[i].Suppress(config.summonTime+config.descentTime+config.stagger);
                victims[i]=null;
            }
            Visual.Begin(committed,Quaternion.LookRotation(Vector3.ProjectOnPlane(committed.point-transform.position,Vector3.up).sqrMagnitude>0.01f
                ?Vector3.ProjectOnPlane(committed.point-transform.position,Vector3.up):transform.forward));
            GetComponent<PlayerSoundEmitter>()?.Combat();impulse?.Pulse(0.15f);
            Feedback="SEAL INVOKED";FeedbackUntil=SessionNow+config.summonTime;Changed?.Invoke();return true;
        }
        void Impact()
        {
            ImpactCount++;LastHitCount=0;LastDamage=0;
            Visual.Impact();if(arTwin&&twinVisual!=null)twinVisual.Impact();impulse?.Pulse(1);
            if(arAbsorption){for(int i=0;i<gathered;i++){var m=absorbed[i];if(m==null||m.Defeated||DepthHidden(m)||m.GetComponent<AR.ARCombatContext>()?.battlefield!=ar.battlefield)continue;var info=Compute(m);info.amount*=(1+.3f*gathered)*absorptionPower;if(m.ReceiveSeal(info,config.stagger)){LastHitCount++;LastDamage+=info.amount;}m.GetComponent<Combat.StatusEffectHost>()?.Consume(Combat.StatusType.Pulled);}return;}
            // A lift may have departed during the summon. Do not strike a different floor.
            if(!Targeting.Cast(committed.point+Vector3.up*(0.15f*Scale),Vector3.down,0.6f*Scale,transform,out var support) ||
                Mathf.Abs(support.point.y-committed.point.y)>0.25f*Scale)
            {Feedback="SEAL DISPERSED";FeedbackUntil=SessionNow+1.5f;Changed?.Invoke();return;}
            int seen=CollectVictims();
            for(int i=0;i<seen;i++)
            {
                var victim=victims[i];
                Vector3 delta=victim.transform.position-committed.point;
                float distance=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;
                // 300% Công, element Thổ (plan §7.3); no falloff towards the rim.
                var info=Compute(victim);
                if(victim.ReceiveSeal(info,config.stagger))
                {LastHitCount++;LastDamage+=info.amount;LastVictim=victim;}
            }
            if(runtime!=null&&runtime.Mastered)for(int i=0;i<seen;i++)if(victims[i]!=null&&(victims[i].transform.position-committed.point).sqrMagnitude<4){var push=Vector3.ProjectOnPlane(victims[i].transform.position-committed.point,Vector3.up);Set2SkillRuntime.Knockback(victims[i],push.sqrMagnitude>.01f?push:Vector3.right,3);}
            for(int i=0;i<seen;i++)victims[i]=null;
            if(runtime!=null&&runtime.Mastered)GetComponent<SkillMasteryFields>()?.Mountain(committed.point,Quaternion.LookRotation(transform.forward));
            Feedback=LastHitCount>0?(LastVictim.Defeated?"SEAL BREAK • TARGET DEFEATED":"DIRECT IMPACT • TARGET SUPPRESSED"):"SEAL IMPACT";
            FeedbackUntil=SessionNow+2.5f;Changed?.Invoke();
        }
        readonly System.Random rng=new System.Random();
        Combat.PlayerStats stats;
        Combat.DamageInfo Compute(MonsterVitality victim)
        {
            if(stats==null)stats=GetComponent<Combat.PlayerStats>();
            float attack=stats!=null?stats.Attack*stats.DamageDealt:config.damage/Mathf.Max(.01f,config.damagePercent);
            var info=Combat.DamageCalculator.Compute(attack,config.damagePercent*(runtime!=null?runtime.EffectMultiplier*runtime.CastDamageMultiplier:1),config.element,victim,
                stats!=null?stats.EffectiveCritChance:0,stats!=null?stats.CritDamage:1.5f,rng,Combat.DamageSource.Skill);
            info.attacker=gameObject;info.point=victim.transform.position+Vector3.up*Scale;info.direction=Vector3.down;info.skillId="dai-thu-an";
            info.isArea=true;if(arTwin)info.amount*=2;
            return info;
        }
        int CollectVictims()
        {
            int count=Physics.OverlapSphereNonAlloc(committed.point+Vector3.up*(0.7f*Scale),config.radius+Scale,colliders,~0,QueryTriggerInteraction.Ignore);
            int seen=0;
            for(int i=0;i<count;i++)
            {
                var victim=colliders[i].GetComponentInParent<MonsterVitality>();
                if(victim==null || !victim.isActiveAndEnabled || victim.Defeated||(ar?.battlefield?.GetComponent<AR.ARDepthCollision>()?.Hidden(victim)??false))continue;
                bool duplicate=false;for(int n=0;n<seen;n++)if(victims[n]==victim){duplicate=true;break;}
                if(duplicate || seen==victims.Length)continue;
                Vector3 delta=victim.transform.position-committed.point;
                if(Vector3.ProjectOnPlane(delta,Vector3.up).magnitude>config.radius || Mathf.Abs(delta.y)>config.verticalTolerance)continue;
                if(!Targeting.Clear(committed.point+Vector3.up*(0.6f*Scale),victim.transform.position+Vector3.up*(0.9f*Scale),transform))continue;
                victims[seen++]=victim;
            }
            return seen;
        }
        void Notify(string message)
        {
            Feedback=message;FeedbackUntil=SessionNow+1.25f;
            if(Time.unscaledTime>=nextFeedback){Visual.Unavailable();nextFeedback=Time.unscaledTime+0.4f;}Changed?.Invoke();
        }
        void ReleaseAbsorbed(){for(int i=0;i<gathered;i++){if(absorbed[i]!=null)absorbed[i].GetComponent<Combat.StatusEffectHost>()?.Consume(Combat.StatusType.Pulled);absorbed[i]=null;}gathered=0;arAbsorption=false;}
        void CancelAll(){ReleaseAbsorbed();CancelPreview();if(IsCasting){IsCasting=false;ReleaseAbsorbed();Visual.Finish();if(twinVisual!=null)twinVisual.Finish();arTwin=false;Changed?.Invoke();}}
        void OnDisable(){if(Visual!=null)CancelAll();}
        void OnDestroy(){if(Visual!=null)Destroy(Visual.gameObject);if(twinVisual!=null)Destroy(twinVisual.gameObject);}
    }
}
