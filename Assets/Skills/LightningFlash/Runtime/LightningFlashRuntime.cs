using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public sealed class LightningFlashRuntime : Set1SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1,.78f,.08f);
        protected override float Range=>10;
        float extraUntil;bool secondCast;
        public bool ExtraReady=>Mastered&&Time.time<extraUntil;
        public override bool IsReady=>base.IsReady||ExtraReady&&IsUnlocked&&HasSpirit;
        public override float SpiritCost=>ExtraReady?0:base.SpiritCost;
        public override string StatusText=>ExtraReady?"1 / 2":"";
        protected override bool PreserveCooldownOnRecast=>Mastered;
        public override SkillState GetState()=>ExtraReady&&!casting?SkillState.Ready:base.GetState();
        public override void ReadyOnRestEquip(){base.ReadyOnRestEquip();extraUntil=0;}
        Vector3 start,previous,pathEnd;
        readonly Collider[] overlaps=new Collider[128];
        readonly MonsterVitality[] pierced=new MonsterVitality[64];
        readonly Collider[] ignored=new Collider[128];
        readonly bool[] previouslyIgnored=new bool[128];
        int ignoredCount;
        Collider body;
        int count,ghosts;bool dashed,resolved;
        readonly bool[] struck=new bool[64];
        float stoppedAt=-1,slashAt=-1;
        bool pulsed;
        SkillVfxPool.Node slash;
        public float SlashStartedAt=>slashAt;
        public float DashStoppedAt=>stoppedAt;
        int danger;
        SkillVfxPool.Node trail;
        SpeedForceVFX speed;
        protected override void Awake(){base.Awake();speed=GetComponent<SpeedForceVFX>();body=GetComponent<CharacterController>();}
        protected override void OnCast()
        {
            secondCast=ExtraReady;extraUntil=0;if(Mastered)vfx.Spawn(SkillVfxKind.Ring,transform.position,new Color(.7f,.1f,1),.8f,1.4f);
            dashed=false;resolved=false;pulsed=false;stoppedAt=slashAt=-1;count=ghosts=0;start=previous=transform.position;System.Array.Clear(struck,0,struck.Length);
            if(targetLock!=null&&targetLock.Current!=null&&!targetLock.Current.Defeated)direction=Vector3.ProjectOnPlane(targetLock.Current.transform.position-start,Vector3.up).normalized;
            if(direction.sqrMagnitude<.01f)direction=transform.forward;
            danger=DangerZoneRegistry.Register(start,.8f,.7f,DangerShape.Capsule,direction,10);
            var charge=vfx.Spawn(SkillVfxKind.Bolt,start+Vector3.up*.15f,Accent,.22f,.6f,vfx.config.lightningCast);if(charge!=null)charge.End=start+direction*.7f+Vector3.up*.15f;
            GetComponent<SkillCastPose>()?.Play(.32f,-14,24);
        }
        protected override void TickCast(float dt)
        {
            if(!dashed&&elapsed>=.1f)
            {
                dashed=true;previous=transform.position;explorer.Dash(direction,10,.2f);health.GrantInvulnerability(.2f);if(speed!=null)speed.Burst(.25f);
                IgnoreMonsterBodies();
                trail=vfx.Spawn(SkillVfxKind.Bolt,start+Vector3.up*.3f,Accent,.6f,1);if(trail!=null)trail.End=start+Vector3.up*.3f;
            }
            if(dashed&&!resolved)
            {
                Vector3 now=transform.position;
                int hits=Physics.OverlapCapsuleNonAlloc(previous+Vector3.up,now+Vector3.up,.8f,overlaps,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<hits;i++)
                {
                    var victim=overlaps[i].GetComponentInParent<MonsterVitality>();if(victim==null||victim.Defeated||count>=pierced.Length)continue;
                    bool seen=false;for(int j=0;j<count;j++)if(pierced[j]==victim){seen=true;break;}if(seen)continue;
                    if(!CombatLine.Clear(previous+Vector3.up,victim.transform.position+Vector3.up,transform))continue;
                    pierced[count++]=victim;var spark=vfx.Spawn(SkillVfxKind.Bolt,victim.transform.position+Vector3.up*.1f,Accent,.15f,.4f);if(spark!=null)spark.End=victim.transform.position+Vector3.up*1.7f;
                }
                previous=now;if(trail!=null)trail.End=now+Vector3.up*.3f;
                const int wanted=2;
                if(ghosts<wanted&&elapsed>=.16f+ghosts*.075f){vfx.Spawn(SkillVfxKind.Ghost,now,Accent,.6f);ghosts++;}
                if(stoppedAt<0&&(elapsed>=.34f || elapsed>.13f&&!explorer.IsDashing))
                {stoppedAt=elapsed;pathEnd=transform.position;RestoreCollision();explorer.CancelDash();}
                if(stoppedAt>=0&&elapsed>=stoppedAt+.2f)BeginSlash();
            }
            if(resolved)
            {
                float grow=Mathf.Clamp01((elapsed-slashAt)/.08f);if(slash!=null)slash.Progress=grow;
                Vector3 path=pathEnd-start;float length=path.magnitude;
                for(int i=0;i<count;i++)
                {
                    var victim=pierced[i];if(struck[i]||victim==null)continue;
                    float along=length>.01f?Mathf.Clamp01(Vector3.Dot(victim.transform.position-start,path.normalized)/length):0;
                    if(grow<along)continue;struck[i]=true;
                    if(Hit(victim,1.8f))
                    {
                        victim.GetComponent<StatusEffectHost>()?.Apply(StatusType.Shock,.5f,0,gameObject);
                        var renderer=victim.GetComponentInChildren<Renderer>();float size=renderer!=null?Mathf.Max(.85f,renderer.bounds.size.y*.9f):1.5f;
                        vfx.Burst(victim.transform.position+Vector3.up*.85f,Accent,size,null,false);
                        for(int branch=0;branch<2;branch++)
                        {
                            var spark=vfx.Spawn(SkillVfxKind.Bolt,victim.transform.position+Vector3.up*.85f,Accent,.34f,.5f);
                            if(spark!=null)spark.End=spark.Position+Vector3.up*.65f+Vector3.Cross(direction,Vector3.up)*(branch==0?-.7f:.7f);
                        }
                        if(!pulsed&&LastHitCount>=Mathf.Min(3,count)){pulsed=true;impact.Pulse(.75f,.065f);}
                    }
                }
                if(elapsed>=slashAt+.85f)Finish();
            }
        }
        void BeginSlash()
        {
            resolved=true;slashAt=elapsed;
            slash=vfx.Spawn(SkillVfxKind.Slash,start+Vector3.up*.85f,Accent,.68f,1,vfx.config.lightningHit);if(slash!=null){slash.End=pathEnd+Vector3.up*.85f;slash.Progress=0;}
            for(int i=0;i<3;i++)vfx.Spawn(SkillVfxKind.Scorch,Vector3.Lerp(start,pathEnd,(i+.5f)/3),new Color(.16f,.025f,.22f),2.7f,1);
            DangerZoneRegistry.Remove(danger);
        }
        void IgnoreMonsterBodies()
        {
            if(body==null)return;
            int n=Physics.OverlapCapsuleNonAlloc(start+Vector3.up,start+direction*10+Vector3.up,1.8f,overlaps,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n&&ignoredCount<ignored.Length;i++)if(overlaps[i].GetComponentInParent<MonsterVitality>()!=null)
            {var c=overlaps[i];ignored[ignoredCount]=c;previouslyIgnored[ignoredCount]=Physics.GetIgnoreCollision(body,c);Physics.IgnoreCollision(body,c,true);ignoredCount++;}
        }
        void RestoreCollision(){for(int i=0;i<ignoredCount;i++){if(body!=null&&ignored[i]!=null)Physics.IgnoreCollision(body,ignored[i],previouslyIgnored[i]);ignored[i]=null;}ignoredCount=0;}
        protected override void Cleanup(){DangerZoneRegistry.Remove(danger);RestoreCollision();if(explorer!=null)explorer.CancelDash();if(Mastered&&!secondCast&&resolved&&elapsed>=slashAt+.8f&&isActiveAndEnabled&&health!=null&&!health.IsDead)extraUntil=Time.time+2;}
    }
}
