using System;
using UnityEngine;
using CampusRift.Controls;

namespace CampusRift.Skills
{
    // Hư Không Kết Giới — wraps VoidWallSkill (tap Q instant wall, hold to aim, buffered presses).
    [RequireComponent(typeof(VoidWallSkill))]
    public sealed class VoidWallRuntime : SkillRuntime, ILegacySkill
    {
        VoidWallSkill skill;
        VoidWallSkill Skill => skill != null ? skill : skill = GetComponent<VoidWallSkill>();
        public Behaviour Ability => Skill;
        public override CampusAction? IdentityAction => CampusAction.Wall;
        // Shows the per-charge recharge; placement itself has a short deploy cooldown.
        public override float CooldownRemaining => Skill.Charges > 0 ? Skill.CooldownRemaining : Skill.RechargeRemaining;
        public override void AdvanceCooldown(float seconds)=>Skill.AdvanceCooldown(seconds);
        public override float CooldownDuration => Skill.Charges > 0 ? (Skill.config != null ? Skill.DeployCooldown : 1) : Skill.RechargeSeconds;
        public override bool IsAiming => Skill.IsPreviewing;
        public override bool BeginAim() => Skill.BeginPreview();
        public override bool Confirm() => Skill.Confirm();
        public override void Cancel() => Skill.CancelPreview();
        public override void ReadyOnRestEquip() => Skill.ReadyOnRestEquip();
        public override bool QuickCast() => Skill.QuickCast();
        public override string StatusText => Skill.Charges + " / " + Skill.MaxCharges;
        public override Type GlyphType => typeof(VoidWallGlyph);
        public override SkillState GetState() =>
            !IsUnlocked ? SkillState.Locked : Skill.Charges == 0 ? SkillState.NoCharge : Skill.IsPreviewing ? SkillState.Aiming :
            Skill.CooldownRemaining > 0 ? SkillState.Cooldown : ReadyOrNoSpirit();
        void OnEnable() { Skill.Changed += RaiseChanged; }
        void OnDisable() { if (skill != null) skill.Changed -= RaiseChanged; }
    }
}
