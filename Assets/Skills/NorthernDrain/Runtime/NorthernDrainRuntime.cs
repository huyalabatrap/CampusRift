using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public sealed class NorthernDrainRuntime:Set2SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(.12f,.75f,1);
        public int Ticks {get;private set;}public float HealingApplied {get;private set;}public bool Interrupted {get;private set;}
        public float SpiritDrained {get;private set;}
        readonly MonsterVitality[] nearest=new MonsterVitality[3];readonly float[] distances=new float[3];
        int dodgeAtStart;SkillVfxPool.Node vortex;
        protected override void OnCast()
        {
            Ticks=0;HealingApplied=SpiritDrained=0;Interrupted=false;dodgeAtStart=GetComponent<DodgeAbility>()?.DodgeCount??0;
            vortex=vfx.Spawn(SkillVfxKind.Ring,transform.position+Vector3.up,Accent,3.5f,1.15f,vfx.config.iceCast,vfx.config.voidCast);if(vortex!=null){vortex.Follow=transform;vortex.Offset=Vector3.up;}
            vfx.Spawn(SkillVfxKind.Ring,transform.position,Accent,.45f,1.4f);GetComponent<SkillCastPose>()?.Play(3,-4,65);
        }
        void Acquire()
        {
            for(int i=0;i<3;i++){nearest[i]=null;distances[i]=float.MaxValue;}int n=Snapshot();
            for(int i=0;i<n;i++){var m=targets[i];if(!InArea(m,transform.position,12))continue;float d=(m.transform.position-transform.position).sqrMagnitude;for(int j=0;j<3;j++)if(d<distances[j]){for(int k=2;k>j;k--){nearest[k]=nearest[k-1];distances[k]=distances[k-1];}nearest[j]=m;distances[j]=d;break;}}
        }
        protected override void TickCast(float dt)
        {
            if((GetComponent<DodgeAbility>()?.DodgeCount??0)!=dodgeAtStart||(GetComponent<Enemies.PlayerEnemyControl>()?.Stunned??false)){Interrupted=true;Finish();return;}
            while(Ticks<6&&elapsed>=(Ticks+1)*.5f)
            {
                Ticks++;Acquire();float dealt=0;for(int i=0;i<3;i++)
                {var m=nearest[i];if(m==null)continue;dealt+=Hit(m,.7f,false);m.GetComponent<StatusEffectHost>()?.Apply(StatusType.Wet,3,0,gameObject);var beam=vfx.Spawn(SkillVfxKind.Bolt,m.transform.position+Vector3.up,Accent,.58f,1.1f,vfx.config.iceCast);if(beam!=null){beam.End=transform.position+Vector3.up*1.2f;beam.Progress=-1;}}
                bool guarded=GetComponent<GoldenBellRuntime>()?.ShieldActive??false;float heal=dealt*(guarded?1:.5f);float before=health.CurrentHealth;health.Heal(heal);HealingApplied+=health.CurrentHealth-before;if(guarded&&dealt>0)ReactionResolver.PublishExtended(ReactionType.GuardDrain,null,gameObject,transform.position);
                if(Mastered&&dealt>0){float previous=spirit.Current;spirit.Restore(3);SpiritDrained+=spirit.Current-previous;vfx.Spawn(SkillVfxKind.SwordDust,transform.position+Vector3.up*1.2f,new Color(.3f,1.4f,1.3f),.55f,.6f);}
                var ring=vfx.Spawn(SkillVfxKind.Ring,transform.position,Accent,.45f,1.1f);if(ring!=null)ring.Progress=0;
            }
            if(elapsed>=3.1f)Finish();
        }
        protected override void Cleanup(){vortex?.StopLoop();vortex?.Fade(.5f);vortex=null;vfx.Spawn(SkillVfxKind.FrostMist,transform.position,Accent,.7f,1);}
    }
}
