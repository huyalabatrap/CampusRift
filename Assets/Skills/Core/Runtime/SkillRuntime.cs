using System;
using UnityEngine;
using CampusRift.Controls;

namespace CampusRift.Skills
{
    public enum SkillState { Ready = 0, Aiming = 1, Casting = 2, Cooldown = 3, Locked = 4, NoCharge = 5, Unavailable = 6, NoSpirit = 7 }

    // Behaviour side of a skill. The loadout routes the slot's key/touch button to these calls;
    // legacy skills (Giant Hand, Void Wall, Phantom) keep their own input logic behind an identity action.
    public abstract class SkillRuntime : MonoBehaviour
    {
        public SkillDefinition definition;
        [Range(1, 5)] public int rank = 1;
        public SkillDefinition Definition => definition;
        public bool Mastered=>rank>=4;
        public float CastDamageMultiplier { get; private set; } = 1;
        public void CommitCast()
        {
            if(ARContext==null)Progression.LocalTelemetry.Skill(Id);
            if(Combat.ReactionResolver.AreaSkill(Id))CampusRift.Enemies.EnemyDirector.Instance?.RecordAreaCast();
            if(ARContext==null)UI.TutorialDirector.SkillUsed(Id);
            GetComponent<CampusRift.Monsters.PlayerSoundEmitter>()?.Combat();
            var tracker=GetComponent<Combat.GenerationChainTracker>();
            CastDamageMultiplier=tracker!=null?tracker.Commit(this):1;
            var domain=GetComponent<DomainRuntime>();if(domain!=null&&domain.Contains(transform.position)){CastDamageMultiplier*=1.2f;Combat.ReactionResolver.PublishExtended(Combat.ReactionType.DomainResonance,null,gameObject,transform.position);}
        }
        public float EffectMultiplier=>1+.12f*(Mathf.Clamp(rank,1,5)-1);
        public float CooldownMultiplier=>1-.05f*(Mathf.Clamp(rank,1,5)-1);
        public void ApplyRank(int value){value=Mathf.Clamp(value,1,5);if(rank==value)return;rank=value;RaiseChanged();}
        public string Id => definition != null ? definition.id : name;
        public event Action Changed;
        protected void RaiseChanged() => Changed?.Invoke();

        // Legacy skills read CampusInput.Pressed(Wall/Hand/Phantom); CampusInput resolves that to the slot key.
        // New skills return null and are driven by the loadout directly.
        public virtual CampusAction? IdentityAction => null;
        public abstract float CooldownRemaining { get; }
        public abstract float CooldownDuration { get; }
        // Extra elapsed cooldown time, used by the domain while the caster stands inside it.
        public virtual void AdvanceCooldown(float seconds) { }
        public AR.ARCombatContext ARContext => GetComponent<AR.ARCombatContext>();
        public float WorldScale => ARContext!=null?ARContext.scale:1;
        public bool SessionPaused => ARContext!=null&&ARContext.Paused;
        protected float SessionNow => ARContext!=null?ARContext.Now:Time.time;
        public virtual bool IsUnlocked => ARContext!=null&&ARContext.allUnlocked&&System.Array.IndexOf(AR.GestureSkillMapper.Ids,Id)>=0 || SkillUnlockService.IsUnlocked(this);
        public virtual float SpiritCost => definition != null ? definition.spiritCost : 0;
        public virtual bool HasSpirit { get { var spirit = GetComponent<Combat.SpiritPower>(); return spirit == null || spirit.CanAfford(SpiritCost); } }
        // Ready, or NoSpirit when the only thing missing is Linh Lực.
        protected SkillState ReadyOrNoSpirit() => HasSpirit ? SkillState.Ready : SkillState.NoSpirit;
        public virtual bool IsReady => IsUnlocked && CooldownRemaining <= 0 && HasSpirit;
        public abstract bool IsAiming { get; }
        public abstract bool BeginAim();
        public abstract bool Confirm();
        public abstract void Cancel();
        // Called only when a previously unequipped skill enters a between-wave loadout.
        public virtual void ReadyOnRestEquip() { }
        public abstract bool QuickCast();
        public abstract SkillState GetState();
        // Extra line under the button/slot, e.g. "3 / 3" charges. Empty for none.
        public virtual string StatusText => "";
        // Procedural glyph for the mobile button when the definition has no icon.
        public virtual Type GlyphType => null;
        public virtual Color Accent => CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(.64f, .37f, 1f);
        public string ShortName
        {
            get
            {
                bool vn=UI.LevelHUD.Vietnamese;
                switch(Id){case "tich-lich-nhat-thiem":return vn?"NHẤT THIỂM":"FLASH";case "phat-no-hoa-lien":return vn?"HỎA LIÊN":"FIRE LOTUS";case "han-bang-phong-an":return vn?"HÀN BĂNG":"ICE SEAL";case "than-kiem-ngu-loi":return vn?"NGỰ LÔI":"LIGHTNING";case "kim-chung-trao":return vn?"KIM CHUNG":"GOLD BELL";case "hac-dong-than-la":return vn?"HẮC ĐỘNG":"BLACK HOLE";case "van-kiem-quyet":return vn?"VẠN KIẾM":"SWORD RAIN";}
                return definition!=null&&!string.IsNullOrEmpty(definition.shortName)?definition.shortName:Id.ToUpperInvariant();
            }
        }
    }
}
