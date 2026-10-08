using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public sealed class SwordRainRuntime:Set1SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1,.78f,.22f);
        protected override float Range=>18*WorldScale;
        protected override float AimRadius=>8;
        int danger;SkillVfxPool.Node ultimateSword;bool ultimateHit;
        float RainRadius=>ARContext!=null&&ARContext.battlefield!=null?Mathf.Min(8*WorldScale,ARContext.battlefield.placement.Radius*.9f):8*WorldScale;
        readonly SkillVfxPool.Node[] portals=new SkillVfxPool.Node[5];
        public int SwordsLaunched {get;private set;}
        public int SwordImpacts {get;private set;}
        public int CreatedSwords=>vfx!=null?vfx.RainSwordCount:0;
        protected override void OnCast()
        {
            if(arUltimate){ultimateHit=false;point=ARContext.battlefield.Root.position;float r=ARContext.battlefield.placement.Radius;ultimateSword=vfx.Spawn(SkillVfxKind.Sword,point+Vector3.up*r*1.6f,Accent,1.1f,r/WorldScale*1.8f,vfx.config.sword);vfx.Spawn(SkillVfxKind.Ring,point,Accent,1.1f,r/WorldScale);return;}
            if(ARContext!=null)point=ARContext.ConstrainVisual(point,RainRadius);
            SwordsLaunched=SwordImpacts=0;danger=DangerZoneRegistry.Register(point,RainRadius,3.6f);
            vfx.Spawn(SkillVfxKind.Ring,point,Accent,3.65f,RainRadius/WorldScale,vfx.config.sword);
            for(int i=0;i<5;i++)
            {
                float orbit=ARContext!=null?Mathf.Min(4*WorldScale,ARContext.battlefield.placement.Radius*.55f):4*WorldScale;
                Vector3 center=ARContext!=null?ARContext.ConstrainVisual(point,orbit+1.2f*WorldScale):point;
                Vector3 p=center+Quaternion.Euler(0,i*72,0)*Vector3.forward*orbit+Vector3.up*((ARContext!=null?2.4f:3.7f)*WorldScale);
                portals[i]=vfx.Spawn(SkillVfxKind.Portal,p,Accent,3.7f,1.1f);
            }
            GetComponent<SkillCastPose>()?.Play(.6f,-10,70);
        }
        protected override void TickCast(float dt)
        {
            if(arUltimate){if(ultimateSword!=null)ultimateSword.Position=point+ARContext.battlefield.Root.forward*Mathf.Lerp(-ARContext.battlefield.placement.Radius,ARContext.battlefield.placement.Radius,Mathf.Clamp01(elapsed/.8f))+Vector3.up*WorldScale;if(!ultimateHit&&elapsed>=.5f){ultimateHit=true;var actors=ARContext.battlefield.GetComponent<AR.ARMonsterDirector>().Actors;foreach(var e in actors)if(e!=null&&ARTarget(e.Vitality))Hit(e.Vitality,3,DamageSource.Skill,true);vfx.Burst(point,Accent,ARContext.battlefield.placement.Radius/WorldScale,vfx.config.sword);}if(elapsed>=1.1f)Finish();return;}
            for(int i=0;i<portals.Length;i++)if(portals[i]!=null&&portals[i].Live)portals[i].Progress=1+Mathf.Clamp01(elapsed-2)*2;
            int total=Mastered?50:30;
            while(SwordsLaunched<total&&elapsed>=.2f+2.8f*Mathf.Sqrt((SwordsLaunched+1)/(float)total))
            {
                float a=(float)rng.NextDouble()*Mathf.PI*2;float r=Mathf.Sqrt((float)rng.NextDouble())*RainRadius;
                // The AR disc can be wide relative to one impact. Seed its aimed center
                // once so a Black Hole combo can hit before Pulled expires.
                Vector3 destination=ARContext!=null&&SwordsLaunched==0?point:point+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);if(ARContext!=null)destination=ARContext.Constrain(destination);RaycastHit floor;
                if(Physics.Raycast(destination+Vector3.up*(2*WorldScale),Vector3.down,out floor,4*WorldScale,CombatLine.SolidMask,QueryTriggerInteraction.Ignore))destination=floor.point;
                if(vfx.LaunchRain(this,destination))SwordsLaunched++;else break;
            }
            if(elapsed>=4.2f)Finish();
        }
        public void SwordImpact(Vector3 destination)
        {
            SwordImpacts++;var mark=vfx.Spawn(SkillVfxKind.CopperMark,destination,new Color(.7f,.43f,.06f),1.8f,.6f);
            bool hit=false;for(int i=0;i<MonsterVitality.Active.Count;i++){var m=MonsterVitality.Active[i];if(InArea(m,destination,.85f)&&Hit(m,.4f))hit=true;}
            var spark=vfx.Spawn(SkillVfxKind.SwordImpact,destination+Vector3.up*(.2f*WorldScale),Accent,.5f,.85f,vfx.config.sword);if(spark!=null)vfx.Emit(spark,SkillVfxPool.MobileQuality?6:14);
            if(hit)impact.Pulse(.5f,.065f);
        }
        protected override void Cleanup(){DangerZoneRegistry.Remove(danger);}
        public void SwordDissolve(Vector3 destination){var dust=vfx.Spawn(SkillVfxKind.SwordDust,destination+Vector3.up*(.4f*WorldScale),Accent,.6f);if(dust!=null)vfx.Emit(dust,SkillVfxPool.MobileQuality?3:6);}
    }
}
