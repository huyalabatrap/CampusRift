using System.Collections.Generic;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public sealed class DomainRuntime:Set2SkillRuntime
    {
        static readonly List<DomainRuntime> live=new List<DomainRuntime>(4);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){live.Clear();}
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(.6f,.2f,1.25f);
        public bool Active=>casting;
        public Vector3 Center {get;private set;}
        SkillRuntime[] skills;SkillSet2VisualBatch.Slot dome;float pulse;
        public bool Contains(Vector3 p)=>casting&&Mathf.Abs(p.y-Center.y)<6&&Vector3.ProjectOnPlane(p-Center,Vector3.up).sqrMagnitude<=144;
        public static bool Suppresses(Vector3 p){foreach(var d in live)if(d!=null&&d.Mastered&&d.Contains(p))return true;return false;}
        public static bool Slows(Vector3 p){foreach(var d in live)if(d!=null&&d.Contains(p))return true;return false;}
        protected override void OnCast()
        {
            Center=transform.position;skills=GetComponents<SkillRuntime>();if(!live.Contains(this))live.Add(this);pulse=.25f;
            dome=vfx.Shapes.Spawn(SkillShape.Domain,Center,Accent,8.5f,Vector3.one*12,Quaternion.identity);if(dome!=null)dome.Opacity=.5f;
            vfx.Spawn(SkillVfxKind.Shockwave,Center,Accent,1.2f,12,vfx.config.voidCast);
            GetComponent<SkillCastPose>()?.Play(.8f,-20,95);
        }
        protected override void TickCast(float dt)
        {
            if(elapsed>=8){Finish();return;}
            if(Contains(transform.position))foreach(var s in skills)if(s!=null)s.AdvanceCooldown(dt);
            for(int i=0;i<MonsterVitality.Active.Count;i++){var m=MonsterVitality.Active[i];if(m!=null)m.GetComponent<StatusEffectHost>()?.SetDomainSlow(Slows(m.transform.position)?.4f:0);}
            if(elapsed>=pulse){pulse+=2;var ripple=vfx.Spawn(SkillVfxKind.Ring,Center,Accent,.9f,3);if(ripple!=null)ripple.Opacity=.4f;if(Mastered)vfx.Spawn(SkillVfxKind.CopperMark,Center,new Color(.8f,.4f,1.5f),1.2f,2);}
        }
        protected override void Cleanup()
        {
            live.Remove(this);dome?.Fade(.5f);dome=null;
            foreach(var m in MonsterVitality.Active)if(m!=null)m.GetComponent<StatusEffectHost>()?.SetDomainSlow(Slows(m.transform.position)?.4f:0);
        }
    }
}
