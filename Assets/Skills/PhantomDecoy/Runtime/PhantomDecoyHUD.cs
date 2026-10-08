using CampusRift.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusRift.Skills
{
    public sealed class PhantomDecoyHUD : MonoBehaviour
    {
        public SkillSlotUI slot;
        public Graphic glyph;
        public TMP_Text instruction;
        PhantomDecoySkill skill;
        Controls.CampusInput input;
        string Key => input!=null?input.KeyLabel(Controls.CampusAction.Phantom):"E";
        int lastSeconds = -1;
        bool wasUnlocked;
        string lastMessage;
        void Start() { skill=FindAnyObjectByType<PhantomDecoySkill>(); if(skill!=null){skill.Changed+=Refresh;input=skill.GetComponent<Controls.CampusInput>();} Refresh(); }
        void OnDestroy() { if(skill!=null)skill.Changed-=Refresh; }
        void Update()
        {
            if(skill==null || slot==null)return;
            if(Key==""){if(instruction!=null&&instruction.text!="")instruction.text="";lastMessage="";return;}
            if(wasUnlocked!=skill.IsUnlocked){wasUnlocked=skill.IsUnlocked;Refresh();}
            if(!skill.IsUnlocked){instruction.text="LEARNING REWARD / LOCKED";return;}
            float left=skill.CooldownRemaining;
            slot.CooldownOverlay.fillAmount=Mathf.Clamp01(left/Mathf.Max(.1f,skill.EffectiveCooldown));
            int seconds=Mathf.CeilToInt(left);
            if(seconds!=lastSeconds){lastSeconds=seconds;slot.CooldownText.text=seconds>0?seconds.ToString():"";Refresh();}
            string message=skill.IsPreviewing?Key+" / LMB  RELEASE PHANTOM   •   RMB  CANCEL":
                left>0?"":Key+"  PHANTOM DECOY  /  READY";
            if(message!=lastMessage){lastMessage=message;instruction.text=message;}
        }
        void Refresh()
        {
            if(slot==null)return;
            slot.SetLocked(skill==null || !skill.IsUnlocked);
            if(skill==null || !skill.IsUnlocked){slot.KeyLabel.text="LOCKED";return;}
            bool ready=skill.Ready;
            if(glyph!=null)glyph.color=ready?Color.white:new Color(.42f,.4f,.52f);
            slot.KeyLabel.text=Key;
        }
    }
}
