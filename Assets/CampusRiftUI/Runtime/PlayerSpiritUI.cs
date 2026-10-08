using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Combat;

namespace CampusRift.UI
{
    // Linh Lực bar: a clone of the energy row (label, value, track, fill) placed under it.
    public sealed class PlayerSpiritUI : MonoBehaviour
    {
        public Image Fill;
        public TMP_Text Value, Label;
        SpiritPower spirit;
        static readonly Color Jade = new Color(.25f, .95f, .72f), Empty = new Color(.5f, .55f, .65f);

        void Start()
        {
            spirit = FindAnyObjectByType<SpiritPower>();
            if (spirit != null) spirit.Changed += Refresh;
            Refresh();
        }
        void OnDestroy() { if (spirit != null) spirit.Changed -= Refresh; }
        void LateUpdate() { if (spirit != null && Fill != null && Mathf.Abs(Fill.fillAmount - spirit.Fraction) > .002f) Refresh(); }
        void Refresh()
        {
            if (spirit == null || Fill == null) return;
            Fill.fillAmount = spirit.Fraction;
            Fill.color = spirit.Fraction > .05f ? Jade : Empty;
            if (Value != null) { string text = Mathf.CeilToInt(spirit.Current) + " / " + Mathf.CeilToInt(spirit.Max); if (Value.text != text) Value.text = text; }
        }
    }
}
