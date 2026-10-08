using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class ImmortalSwordArrayRuntime:Set2SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1,.72f,.22f);protected override float AimRadius=>8;
        public int Ticks {get;private set;}public bool PlayerBuff {get;private set;}public float Duration=>Mastered?15:10;
        int danger;SkillSet2VisualBatch.Slot wheel;SkillVfxPool.Node portal;
        public bool Contains(Vector3 p){Vector3 d=p-point;return casting&&Mathf.Abs(d.y)<3&&Vector3.ProjectOnPlane(d,Vector3.up).sqrMagnitude<=64;}
        protected override void OnCast()
        {
            Ticks=0;PlayerBuff=false;danger=DangerZoneRegistry.Register(point,8,Duration+.1f);
            wheel=vfx.Shapes.Spawn(SkillShape.SwordWheel,point,Accent,Duration+.6f,Vector3.one*8,Quaternion.identity);
            if(wheel!=null)wheel.Opacity=.55f;
            if(Mastered){var inner=vfx.Shapes.Spawn(SkillShape.SwordWheel,point,new Color(1.4f,.9f,.4f),Duration+.6f,Vector3.one*3,Quaternion.Euler(0,30,0));if(inner!=null)inner.Opacity=.4f;}
            portal=vfx.Spawn(SkillVfxKind.Ring,point,Accent,Duration+.6f,8,vfx.config.sword,vfx.config.bell);
            vfx.Spawn(SkillVfxKind.Shockwave,point,Accent,.7f,8);GetComponent<SkillCastPose>()?.Play(.7f,-9,75);
        }
        protected override void TickCast(float dt)
        {
            bool inside=Contains(transform.position);if(inside!=PlayerBuff){PlayerBuff=inside;stats.SetModifier(StatSource.Buff,Id,StatType.CritChance,inside?.2f:0,0);}
            while(Ticks<(Mastered?30:20)&&elapsed>=(Ticks+1)*.5f)
            {Ticks++;int n=Snapshot();int effects=0;for(int i=0;i<n;i++){var m=targets[i];if(!InArea(m,point,8))continue;if(Hit(m,.6f)>0&&effects++<1){var slash=vfx.Spawn(SkillVfxKind.Slash,m.transform.position+Vector3.up*.8f-Vector3.right,Accent,.4f,.4f,vfx.config.sword);if(slash!=null){slash.End=m.transform.position+Vector3.up*1.1f+Vector3.right;slash.Opacity=.5f;}}}if(Ticks%4==1&&effects>0)impact.Pulse(.35f,.065f);}
            if(elapsed>=Duration+.1f)Finish();
        }
        protected override void Cleanup(){DangerZoneRegistry.Remove(danger);stats?.RemoveSource(StatSource.Buff,Id);PlayerBuff=false;wheel?.Fade(.5f);wheel=null;portal?.StopLoop();portal?.Fade(.5f);portal=null;vfx.Spawn(SkillVfxKind.CopperMark,point,Accent,2,8);}
    }
}
