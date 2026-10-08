using System;
using UnityEngine;
using CampusRift.Controls;

namespace CampusRift.Skills
{
    // Đại Thủ Ấn — wraps GiantHandSkill. The skill keeps reading CampusAction.Hand, now resolved to its slot key.
    [RequireComponent(typeof(GiantHandSkill))]
    public sealed class GiantHandRuntime : SkillRuntime, ILegacySkill
    {
        GiantHandSkill skill;
        GiantHandSkill Skill => skill != null ? skill : skill = GetComponent<GiantHandSkill>();
        public Behaviour Ability => Skill;
        public override CampusAction? IdentityAction => CampusAction.Hand;
        public override float CooldownRemaining => Skill.CooldownRemaining;
        public override void AdvanceCooldown(float seconds)=>Skill.AdvanceCooldown(seconds);
        public override float CooldownDuration => Skill.config != null ? Skill.EffectiveCooldown : 1;
        public override bool IsAiming => Skill.IsPreviewing;
        public override bool BeginAim() => Skill.BeginPreview();
        public override bool Confirm() => Skill.Confirm();
        public override void Cancel() => Skill.CancelPreview();
        public override void ReadyOnRestEquip() => Skill.ReadyOnRestEquip();
        public override bool QuickCast() => Skill.CastNearest();
        public override Type GlyphType => typeof(GiantHandGlyph);
        public override Color Accent => CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1, .73f, .3f);
        public override SkillState GetState() =>
            !IsUnlocked ? SkillState.Locked : Skill.IsPreviewing ? (Skill.Target.valid ? SkillState.Aiming : SkillState.Unavailable) :
            Skill.IsCasting ? SkillState.Casting : Skill.CooldownRemaining > 0 ? SkillState.Cooldown : ReadyOrNoSpirit();
        void OnEnable() { Skill.Changed += RaiseChanged; }
        void OnDisable() { if (skill != null) skill.Changed -= RaiseChanged; }
    }
}
