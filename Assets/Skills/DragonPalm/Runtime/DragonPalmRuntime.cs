using UnityEngine;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public sealed class DragonPalmRuntime:Set2SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1,.72f,.22f);protected override float Range=>15;
        Vector3 origin;float previous;int count,danger;readonly MonsterVitality[] struck=new MonsterVitality[128];
        readonly SkillSet2VisualBatch.Slot[] dragons=new SkillSet2VisualBatch.Slot[2];
        protected override void OnCast()
        {
            origin=transform.position;previous=0;count=0;danger=DangerZoneRegistry.Register(origin,1,.95f,DangerShape.Capsule,direction,15);
            vfx.Spawn(SkillVfxKind.Ring,origin,Accent,.35f,1.3f,vfx.config.bell);GetComponent<SkillCastPose>()?.Play(.5f,-12,60);
            for(int i=0;i<(Mastered?2:1);i++){var dir=Quaternion.Euler(0,Mastered?(i==0?-18:18):0,0)*direction;dragons[i]=vfx.Shapes.Spawn(SkillShape.Dragon,origin+Vector3.up,Accent,1.2f,Vector3.one*1.4f,Quaternion.LookRotation(dir));}
        }
        protected override void TickCast(float dt)
        {
            float along=Mathf.Clamp01((elapsed-.2f)/.65f)*15;
            for(int beam=0;beam<(Mastered?2:1);beam++)
            {
                Vector3 dir=Quaternion.Euler(0,Mastered?(beam==0?-18:18):0,0)*direction;
                if(dragons[beam]!=null)dragons[beam].Position=origin+dir*along+Vector3.up*1.2f;
                if(elapsed<.2f)continue;int n=Snapshot();for(int i=0;i<n;i++)
                {var m=targets[i];var d=m.transform.position-origin;float projection=Vector3.Dot(d,dir);if(projection<previous-1||projection>along+1||Mathf.Abs(d.y)>2||Vector3.ProjectOnPlane(d-dir*projection,Vector3.up).sqrMagnitude>1.5f*1.5f)continue;bool seen=false;for(int j=0;j<count;j++)if(struck[j]==m)seen=true;if(seen||count>=struck.Length)continue;if(!CampusRift.Combat.CombatLine.Clear(origin+Vector3.up,m.transform.position+Vector3.up,transform))continue;struck[count++]=m;if(Hit(m,3.5f)>0){Impact(m.transform.position,1.4f);Knockback(m,dir,3);}}
            }
            previous=along;if(elapsed>=.9f)Finish();
        }
        protected override void Cleanup(){DangerZoneRegistry.Remove(danger);for(int i=0;i<2;i++){dragons[i]?.Fade(.4f);dragons[i]=null;}}
    }
}
