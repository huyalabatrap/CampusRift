using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.UI;

namespace CampusRift.Skills
{
    public sealed class VoidWallHUD : MonoBehaviour
    {
        public SkillSlotUI slot;
        public TMP_Text charges, instruction;
        public Graphic[] crystals;
        public Graphic glyph;
        VoidWallSkill skill;
        Controls.CampusInput input;
        string Key => input!=null?input.KeyLabel(Controls.CampusAction.Wall):"Q";
        int shown=-1;
        bool wasUnlocked;
        string lastText;
        string lastReason;
        int lastMode=-1;
        int lastDistance=-1;
        void Start()
        {
            skill=FindAnyObjectByType<VoidWallSkill>(); if(skill!=null){skill.Changed+=Refresh;input=skill.GetComponent<Controls.CampusInput>();}
            if(slot!=null)slot.Tooltip.text="HU KHONG BICH / VOID WALL\nTap: instant wall • Hold: aim, release to deploy • Wheel: near/far • RMB: cancel\nFits doorways, stops at walls, turns to face a monster ahead.\n3 charges, one returns every 12 s • Walk through your wall. Monsters must detour or break it.";
            Refresh();
        }
        void OnDestroy(){if(skill!=null)skill.Changed-=Refresh;}
        void Update()
        {
            if(skill==null)return;
            if(Key==""){if(instruction.text!=""){instruction.text="";lastText="";}return;}
            if(wasUnlocked!=skill.IsUnlocked){wasUnlocked=skill.IsUnlocked;Refresh();}
            if(!skill.IsUnlocked){instruction.text="LEARNING REWARD / LOCKED";return;}
            int mode=skill.IsPreviewing?(skill.Placement.valid?1:2):Time.unscaledTime<skill.FeedbackUntil?3:0;
            string reason=mode==1||mode==2?skill.Placement.reason:mode==3?skill.Feedback:"";
            int distance=skill.IsPreviewing?Mathf.RoundToInt(skill.PreviewDistance*10):-1;
            if(mode==lastMode && reason==lastReason && distance==lastDistance)return;
            lastMode=mode;lastReason=reason;lastDistance=distance;
            string message=skill.IsPreviewing ? (skill.Placement.valid?$"RELEASE {Key}  DEPLOY  •  WHEEL NEAR/FAR {skill.PreviewDistance:0.0}m  •  RMB CANCEL"+(skill.Placement.assisted?"  •  LOCKED ON THREAT":""):"CANNOT PLACE: "+skill.Placement.reason) :
                Time.unscaledTime<skill.FeedbackUntil?skill.Feedback:Key+"  TAP: INSTANT WALL  •  HOLD: AIM";
            if(lastText!=message){lastText=message;instruction.text=message;instruction.color=skill.IsPreviewing&&!skill.Placement.valid?new Color(1,0.34f,0.4f):new Color(0.6f,0.9f,1);}
        }
        void Refresh()
        {
            if(skill==null||slot==null)return;
            slot.SetLocked(!skill.IsUnlocked);
            if(!skill.IsUnlocked){slot.KeyLabel.text="LOCKED";return;}
            if(shown==skill.Charges)return;
            shown=skill.Charges;charges.text=shown+" / "+skill.MaxCharges;
            int lit=shown==0?0:Mathf.CeilToInt(shown*crystals.Length/(float)skill.MaxCharges);
            for(int i=0;i<crystals.Length;i++)crystals[i].color=i<lit?new Color(0.18f,0.92f,1):new Color(0.19f,0.2f,0.29f);
            glyph.color=shown>0?Color.white:new Color(0.36f,0.36f,0.44f);
            slot.KeyLabel.text=shown>0?Key:Key+"  EMPTY";
            charges.color=shown>0?new Color(0.9f,0.93f,1):new Color(1,0.34f,0.4f);
        }
    }
}
