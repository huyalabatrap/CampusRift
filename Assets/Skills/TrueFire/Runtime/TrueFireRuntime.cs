using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class TrueFireRuntime:Set2SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1,.3f,.035f);protected override float Range=>8;protected override float AimRadius=>8;protected override bool Cone=>true;
        public int Ticks {get;private set;}
        int danger;float nextBloom;SkillVfxPool.Node fan,loop;
        protected override void OnCast()
        {
            Ticks=0;nextBloom=.15f;explorer.SkillMoveMultiplier=.35f;
            danger=DangerZoneRegistry.Register(transform.position,8,3.2f,DangerShape.Cone,direction,90);
            fan=vfx.Spawn(SkillVfxKind.Cone,transform.position,Accent,3.7f,8,vfx.config.fireCast);if(fan!=null)fan.End=transform.position+direction;
            loop=vfx.Spawn(SkillVfxKind.FireRibbon,transform.position+Vector3.up,Accent,3.5f,1,null,vfx.config.fireCast);
            GetComponent<SkillCastPose>()?.Play(3.2f,-9,60);
        }
        protected override void TickCast(float dt)
        {
            if(fan!=null){fan.Position=transform.position;fan.End=transform.position+direction;}if(loop!=null)loop.Position=transform.position+direction*.5f+Vector3.up;
            if(elapsed>=nextBloom&&elapsed<3.15f){nextBloom+=.36f;for(int i=0;i<3;i++){Vector3 p=transform.position+direction*(1.7f+i*2.1f);vfx.Spawn(SkillVfxKind.FireBloom,p+Vector3.up*.65f,Accent,.65f,.7f+i*.4f);}DangerZoneRegistry.Remove(danger);danger=DangerZoneRegistry.Register(transform.position,8,.5f,DangerShape.Cone,direction,90);}
            while(Ticks<12&&elapsed>=.15f+(Ticks+1)*.25f)
            {
                Ticks++;int n=Snapshot();bool hit=false;for(int i=0;i<n;i++){var m=targets[i];if(!InArea(m,transform.position,8)||Vector3.Angle(direction,Vector3.ProjectOnPlane(m.transform.position-transform.position,Vector3.up))>45)continue;if(Hit(m,.9f)>0){hit=true;m.GetComponent<StatusEffectHost>()?.Apply(StatusType.Burn,3,(stats!=null?stats.Attack*stats.DamageDealt:20)*.1f*power*CastDamageMultiplier,gameObject);}}
                if(hit&&Ticks%4==1)impact.Pulse(.35f,.065f);
            }
            if(elapsed>=3.2f)Finish();
        }
        protected override void Cleanup()
        {
            if(Mastered&&Ticks==12)GetComponent<SkillMasteryFields>()?.FireWall(transform.position+direction*7,direction,(stats!=null?stats.Attack*stats.DamageDealt:20)*power*CastDamageMultiplier);
            explorer.SkillMoveMultiplier=1;DangerZoneRegistry.Remove(danger);fan?.Fade(.5f);loop?.StopLoop();loop?.Fade(.5f);
            vfx.Spawn(SkillVfxKind.Scorch,transform.position+direction*4,Accent,2.5f,3);
        }
    }
}
