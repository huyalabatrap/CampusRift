using TMPro;
using UnityEngine;

namespace CampusRift.UI
{
    public static class ComicSkillLayout
    {
        public static void Apply(SkillSlotUI slot)
        {
            if(slot==null)return;
            if(slot.KeyLabel!=null)
            {
                ComicTheme.Text(slot.KeyLabel);
                var r=slot.KeyLabel.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,1);
                r.anchoredPosition=new Vector2(0,-6);r.sizeDelta=new Vector2(116,52);
                slot.KeyLabel.fontStyle=FontStyles.Bold;slot.KeyLabel.fontSize=26;slot.KeyLabel.fontSizeMax=26;slot.KeyLabel.fontSizeMin=22;
                slot.KeyLabel.enableAutoSizing=true;slot.KeyLabel.alignment=TextAlignmentOptions.Center;slot.KeyLabel.color=ComicTheme.Paper;
                ComicTheme.ReadabilityPlate(slot.KeyLabel);
            }
            if(slot.CooldownText!=null){ComicTheme.Text(slot.CooldownText);slot.CooldownText.fontStyle=FontStyles.Bold;slot.CooldownText.enableAutoSizing=true;}
            if(slot.UpgradeLabel!=null)
            {
                ComicTheme.Text(slot.UpgradeLabel);var r=slot.UpgradeLabel.rectTransform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one;r.anchoredPosition=new Vector2(-14,-14);r.sizeDelta=new Vector2(32,38);slot.UpgradeLabel.margin=Vector4.zero;
                slot.UpgradeLabel.fontStyle=FontStyles.Bold;slot.UpgradeLabel.fontSize=18;slot.UpgradeLabel.fontSizeMax=18;slot.UpgradeLabel.fontSizeMin=16;slot.UpgradeLabel.enableAutoSizing=true;slot.UpgradeLabel.alignment=TextAlignmentOptions.Center;slot.UpgradeLabel.color=ComicTheme.Paper;slot.UpgradeLabel.transform.SetAsLastSibling();ComicTheme.ReadabilityPlate(slot.UpgradeLabel);
            }
            var charges=slot.transform.Find("VoidWall Charges");
            if(charges!=null)
            {
                var text=charges.GetComponent<TMP_Text>();var r=(RectTransform)charges;
                r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(0,28);r.sizeDelta=new Vector2(96,44);
                if(text!=null){ComicTheme.Text(text);text.fontStyle=FontStyles.Bold;text.fontSize=16;text.fontSizeMax=16;text.fontSizeMin=14;text.enableAutoSizing=true;text.alignment=TextAlignmentOptions.Center;}
            }
            if(slot.Tooltip!=null){ComicTheme.Text(slot.Tooltip);slot.Tooltip.fontStyle=FontStyles.Normal;slot.Tooltip.textWrappingMode=TextWrappingModes.Normal;slot.Tooltip.enableAutoSizing=true;slot.Tooltip.fontSizeMin=14;}
        }
    }
}
