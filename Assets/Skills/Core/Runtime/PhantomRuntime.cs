using System;
using UnityEngine;
using CampusRift.Controls;

namespace CampusRift.Skills
{
    // Ảnh Phân Thân — wraps PhantomDecoySkill.
    [RequireComponent(typeof(PhantomDecoySkill))]
    public sealed class PhantomRuntime : SkillRuntime, ILegacySkill
    {
        PhantomDecoySkill skill;
        PhantomDecoySkill Skill => skill != null ? skill : skill = GetComponent<PhantomDecoySkill>();
        public Behaviour Ability => Skill;
        public override CampusAction? IdentityAction => CampusAction.Phantom;
        public override float CooldownRemaining => Skill.CooldownRemaining;
        public override void AdvanceCooldown(float seconds)=>Skill.AdvanceCooldown(seconds);
        public override float CooldownDuration => Skill.EffectiveCooldown;
        public override bool IsAiming => Skill.IsPreviewing;
        public override bool BeginAim() => Skill.BeginPreview();
        public override bool Confirm() => Skill.Confirm();
        public override void Cancel() => Skill.CancelPreview();
        public override bool QuickCast() => Skill.Cast(transform.forward);
        public override Type GlyphType => typeof(PhantomGlyph);
        public override void ReadyOnRestEquip() => Skill.ReadyOnRestEquip();
        public override SkillState GetState() =>
            !IsUnlocked ? SkillState.Locked : Skill.IsPreviewing ? SkillState.Aiming : Skill.CooldownRemaining > 0 ? SkillState.Cooldown :
            Skill.LiveDecoys>0 ? SkillState.Unavailable : ReadyOrNoSpirit();
        void OnEnable() { Skill.Changed += RaiseChanged; }
        void OnDisable() { if (skill != null) skill.Changed -= RaiseChanged; }
    }
}
