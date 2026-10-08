using UnityEngine;
using TMPro;

namespace CampusRift.UI
{
    [CreateAssetMenu(menuName = "Campus Rift/UI Theme")]
    public sealed class CampusRiftUITheme : ScriptableObject
    {
        public TMP_FontAsset Font;
        public Color PrimaryBackground = new Color(.025f,.035f,.075f,1);
        public Color SecondaryBackground = new Color(.05f,.07f,.13f,1);
        public Color PanelBackground = new Color(.05f,.065f,.12f,.9f);
        public Color PrimaryAccent = new Color(.57f,.39f,1,1);
        public Color BlueAccent = new Color(.27f,.68f,1,1);
        public Color SecondaryAccent = new Color(1,.77f,.39f,1);
        public Color DangerColor = new Color(1,.28f,.38f,1);
        public Color SuccessColor = new Color(.25f,.9f,.8f,1);
        public Color TextPrimary = new Color(.93f,.95f,1,1);
        public Color TextSecondary = new Color(.61f,.68f,.8f,1);
        public Color DisabledColor = new Color(.34f,.38f,.47f,.55f);
        public float BodySize = 24, LabelSize = 18, HeadingSize = 48, Spacing = 16;
        public float TransitionDuration = .22f;
    }
}
