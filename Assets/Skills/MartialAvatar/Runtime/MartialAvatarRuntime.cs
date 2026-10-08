using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class MartialAvatarRuntime:Set2SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1.3f,.55f,.12f);
        public bool Active=>casting;public bool ControlImmune=>casting&&Mastered;
        SkillSet2VisualBatch.Slot avatar;float previousCamera,previousHeight,pulse;
        protected override void OnCast()
        {
            stats.SetModifier(StatSource.Buff,Id,StatType.DamageDealt,0,.5f*power);stats.SetModifier(StatSource.Buff,Id,StatType.DamageTaken,0,-.3f);
            previousCamera=explorer.cameraDistance;previousHeight=explorer.cameraHeight;explorer.cameraDistance=Mathf.Max(7.5f,previousCamera);explorer.cameraHeight=2.2f;pulse=.25f;
            avatar=vfx.Shapes.Spawn(SkillShape.Avatar,transform.position,Accent,10.5f,Vector3.one*2.05f,transform.rotation);if(avatar!=null){avatar.Follow=transform;avatar.Opacity=.055f;}
            vfx.Spawn(SkillVfxKind.Shockwave,transform.position,Accent,1,4,vfx.config.bell);GetComponent<SkillCastPose>()?.Play(.65f,-12,75);
            if(Mastered)GetComponent<Enemies.PlayerEnemyControl>()?.Clear();
        }
        public void Sweep(float percent)
        {
            if(!Active)return;direction=transform.forward;int n=Snapshot();var combat=GetComponent<PlayerCombat>();for(int i=0;i<n;i++)if(InArea(targets[i],transform.position,4)){var m=targets[i];float before=m.Health;var hit=DamageCalculator.Compute(stats.Attack*stats.DamageDealt,percent*CastDamageMultiplier,definition.element,m,stats.EffectiveCritChance,stats.CritDamage,rng,DamageSource.Melee);hit.attacker=gameObject;hit.skillId=Id;hit.point=m.transform.position+Vector3.up;hit.direction=direction;hit.isArea=true;if(combat.ResolveAvatarHit(m,hit)){LastHitCount++;LastDamage+=before-m.Health;impact?.HoldVictim(m,.065f);Impact(m.transform.position,.5f);}}
            var slash=vfx.Spawn(SkillVfxKind.Slash,transform.position+Vector3.up*1.8f,Accent,.55f,4,vfx.config.sword);if(slash!=null){slash.End=transform.position+transform.forward*4;slash.Opacity=.65f;}
            impact?.Pulse(.65f,.065f);
        }
        protected override void TickCast(float dt)
        {
            if(avatar!=null)avatar.Rotation=transform.rotation;
            if(elapsed>=pulse){pulse+=1;vfx.Spawn(SkillVfxKind.Ring,transform.position,Accent,.9f,Mastered?2.5f:1.5f);}
            if(elapsed>=10)Finish();
        }
        protected override void Cleanup(){stats?.RemoveSource(StatSource.Buff,Id);if(explorer!=null){explorer.cameraDistance=previousCamera;explorer.cameraHeight=previousHeight;}avatar?.Fade(.6f);avatar=null;}
    }
}
