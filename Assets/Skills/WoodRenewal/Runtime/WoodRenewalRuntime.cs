using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class WoodRenewalRuntime:Set2SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(.15f,1,.42f);
        int healTicks,fieldTicks;SkillSet2VisualBatch.Slot tree;SkillVfxPool.Node ring;
        public float HealingApplied {get;private set;}public int AllyHeals {get;private set;}
        protected override void OnCast()
        {
            point=transform.position;healTicks=fieldTicks=0;HealingApplied=0;AllyHeals=0;
            if(Mastered){GetComponent<Enemies.PlayerEnemyControl>()?.Clear();GetComponent<StatusEffectHost>()?.Clear();vfx.Spawn(SkillVfxKind.Shockwave,point,Color.white,1,5);}
            tree=vfx.Shapes.Spawn(SkillShape.Tree,point+transform.right*1.6f,Accent,8.5f,Vector3.one*.8f,Quaternion.identity);if(tree!=null)tree.Opacity=.4f;
            ring=vfx.Spawn(SkillVfxKind.Ring,point,Accent,8.5f,5,vfx.config.iceCast,vfx.config.voidCast);
            var motes=vfx.Spawn(SkillVfxKind.SwordDust,point+Vector3.up*.5f,Accent,5,2);if(motes!=null)vfx.Emit(motes,SkillVfxPool.MobileQuality?12:24);
            GetComponent<SkillCastPose>()?.Play(.5f,-6,70);
        }
        protected override void TickCast(float dt)
        {
            while(healTicks<5&&elapsed>=healTicks+1){healTicks++;float before=health.CurrentHealth;health.Heal(health.maxHealth*.05f*power);HealingApplied+=health.CurrentHealth-before;var r=vfx.Spawn(SkillVfxKind.Ring,transform.position,Accent,.55f,1.5f);if(r!=null)r.Progress=0;}
            while(fieldTicks<8&&elapsed>=fieldTicks+1)
            {fieldTicks++;foreach(var ally in HealingAllies.Active){if(ally==null||!ally.Alive||ally.Anchor==null)continue;Vector3 d=ally.Anchor.position-point;if(Mathf.Abs(d.y)<3&&Vector3.ProjectOnPlane(d,Vector3.up).sqrMagnitude<=25){ally.Heal(ally.MaxHealth*.03125f*power);if(Mastered)ally.Anchor.GetComponent<StatusEffectHost>()?.Clear();AllyHeals++;}}}
            if(elapsed>=8.1f)Finish();
        }
        protected override void Cleanup(){tree?.Fade(.5f);tree=null;ring?.StopLoop();ring?.Fade(.5f);ring=null;vfx.Spawn(SkillVfxKind.Scorch,point,Accent,1.5f,3);}
    }
}
