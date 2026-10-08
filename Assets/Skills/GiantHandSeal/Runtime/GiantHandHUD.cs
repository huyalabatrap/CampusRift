using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CampusRift.UI;

namespace CampusRift.Skills
{
    public sealed class GiantHandHUD : MonoBehaviour
    {
        public SkillSlotUI slot;
        public Graphic glyph;
        public TMP_Text instruction,hitFeedback;
        GiantHandSkill skill;
        Controls.CampusInput input;
        // Key of the loadout slot holding the seal; empty when it is not equipped.
        string Key => input!=null?input.KeyLabel(Controls.CampusAction.Hand):"F";
        int lastSeconds=-1;
        bool wasUnlocked;
        string lastMessage;
        void Start()
        {
            skill=FindAnyObjectByType<GiantHandSkill>();if(skill!=null){skill.Changed+=Refresh;input=skill.GetComponent<Controls.CampusInput>();}
            if(slot!=null)slot.Tooltip.text="DAI THU AN / GIANT HAND SEAL\nAuto lock and cast on nearest monster\n16m range • 2.8m radius\nWalls and floors block the seal.";
            Refresh();
        }
        void OnDestroy(){if(skill!=null)skill.Changed-=Refresh;}
        void Update()
        {
            if(skill==null)return;
            if(Key==""){if(instruction.text!="")instruction.text="";hitFeedback.enabled=false;return;}
            if(wasUnlocked!=skill.IsUnlocked){wasUnlocked=skill.IsUnlocked;Refresh();}
            if(!skill.IsUnlocked){instruction.text="LEARN / 5 BREAKTHROUGHS TO UNLOCK SEAL";hitFeedback.enabled=false;return;}
            float remaining=skill.CooldownRemaining;
            slot.CooldownOverlay.fillAmount=Mathf.Clamp01(remaining/skill.EffectiveCooldown);
            int seconds=Mathf.CeilToInt(remaining);
            if(seconds!=lastSeconds){lastSeconds=seconds;slot.CooldownText.text=seconds>0?seconds.ToString():"";Refresh();}
            string message=skill.IsPreviewing?(skill.Target.valid?"TARGET LOCKED  •  RELEASE TO SEAL":skill.Target.reason):
                skill.IsCasting?"GIANT HAND SEAL  /  DESCENDING":remaining>0?"":Key+"  AUTO LOCK NEAREST MONSTER";
            if(message!=lastMessage){lastMessage=message;instruction.text=message;instruction.color=skill.IsPreviewing&&!skill.Target.valid?new Color(1,.35f,.35f):new Color(1,.75f,.35f);}
            bool show=Time.time<skill.FeedbackUntil;
            hitFeedback.enabled=show;
        }
        void Refresh()
        {
            if(slot==null)return;
            if(skill==null){slot.SetLocked(true);return;}
            slot.SetLocked(!skill.IsUnlocked);
            if(!skill.IsUnlocked){slot.KeyLabel.text="LOCKED";return;}
            bool ready=skill.CooldownRemaining<=0;
            glyph.color=ready?Color.white:new Color(.42f,.4f,.52f);
            slot.KeyLabel.text=Key;
            instruction.color=skill.IsPreviewing&&!skill.Target.valid?new Color(1,.35f,.35f):new Color(1,.75f,.35f);
            if(skill.LastHitCount>0 && skill.LastVictim!=null)
                hitFeedback.text=$"{skill.Feedback}   −{skill.LastDamage:0}   [{skill.LastVictim.Health:0} / {skill.LastVictim.maxHealth:0}]";
            else hitFeedback.text=skill.Feedback;
        }
    }
}
