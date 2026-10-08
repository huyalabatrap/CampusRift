using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class HeavenThunderRuntime:Set2SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1,.8f,.08f);protected override float AimRadius=>5;
        int danger;public int Bolts {get;private set;}
        protected override void OnCast(){Bolts=0;danger=DangerZoneRegistry.Register(point,5,2.4f);vfx.Spawn(SkillVfxKind.Ring,point,Accent,1.8f,5,vfx.config.lightningCast);var portal=vfx.Spawn(SkillVfxKind.Portal,point+Vector3.up*7,Accent,1.8f,2);GetComponent<SkillCastPose>()?.Play(.6f,-8,70);}
        protected override void TickCast(float dt)
        {
            int maximum=Mastered?8:5;
            while(Bolts<maximum&&elapsed>=1.2f+Bolts*.10f)
            {
                int index=Bolts++;float angle=index*Mathf.PI*2/maximum;Vector3 p=point+(index==0?Vector3.zero:new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*1.8f);
                var bolt=vfx.Spawn(SkillVfxKind.ChainBolt,p+Vector3.up*14,Accent,.42f,.85f,vfx.config.lightningHit);if(bolt!=null)bolt.End=p+Vector3.up*.2f;
                vfx.Spawn(SkillVfxKind.Shockwave,p,Accent,.6f,2.5f);int n=Snapshot();bool hit=false;for(int i=0;i<n;i++)if(InArea(targets[i],p,2.5f)&&Hit(targets[i],2.2f)>0)hit=true;
                if(hit)Impact(p,1.4f);else vfx.Spawn(SkillVfxKind.Scorch,p,SkillVfxPool.Dark(Accent),2.5f,1);
            }
            if(elapsed>=2.3f)Finish();
        }
        protected override void Cleanup(){DangerZoneRegistry.Remove(danger);}
    }
}
