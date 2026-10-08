using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CampusRift.UI
{
    public sealed class RiftButton : Button
    {
        public CampusRiftUITheme Theme;
        public Graphic Edge;
        public TMPro.TMP_Text Label;
        public bool Gold, Danger;
        // Celestial frame: Glow fades in on hover/selection, Gems light up with it.
        public Graphic Glow;
        public Graphic[] Gems;
        float lit;
        protected override void Awake() { base.Awake(); onClick.AddListener(ClickAudio); ComicTheme.Button(this); }
        void ClickAudio() { UIAudioManager.Instance?.Click(); }
        protected override void OnDestroy() { onClick.RemoveListener(ClickAudio); base.OnDestroy(); }
        public override void OnPointerEnter(PointerEventData e) { base.OnPointerEnter(e); if (IsInteractable()) UIAudioManager.Instance?.Hover(); }
        void Update()
        {
            if (Theme == null) return;
            var state = currentSelectionState;
            bool interactable = IsInteractable();
            bool active = state == SelectionState.Highlighted || state == SelectionState.Selected || state == SelectionState.Pressed;
            float ease = 1-Mathf.Exp(-16*Time.unscaledDeltaTime);
            lit = Mathf.Lerp(lit, interactable && active ? 1 : 0, ease);
            float scale = state == SelectionState.Pressed ? .98f : active ? 1.02f : 1;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale, 1-Mathf.Exp(-22*Time.unscaledDeltaTime));
            if (Label != null)
            {
                var hot = Danger ? new Color(1,.64f,.62f,1) : new Color(1,.92f,.74f,1);
                var image = targetGraphic as Image;
                bool primary = image != null && image.sprite == ComicTheme.Sprite("button-green");
                Label.color = primary ? ComicTheme.Ink : interactable ? ComicTheme.Paper : Theme.DisabledColor;
            }
            if (Glow != null)
            {
                var tint = Danger ? new Color(1,.45f,.45f,1) : Color.white;
                tint.a = lit * (state == SelectionState.Pressed ? .75f : 1);
                Glow.color = tint;
            }
            if (Gems != null)
            {
                var idle = new Color(.62f,.56f,.78f,.75f);
                foreach (var gem in Gems) if (gem != null) gem.color = interactable ? Color.Lerp(idle, Color.white, lit) : new Color(.35f,.33f,.42f,.45f);
            }
            var accent = Danger ? Theme.DangerColor : Gold ? Theme.SecondaryAccent : Theme.PrimaryAccent;
            if (Edge != null) Edge.color = !interactable ? Theme.DisabledColor : active ? accent : new Color(accent.r,accent.g,accent.b,.4f);
        }
        protected override void OnDisable() { base.OnDisable(); transform.localScale = Vector3.one; lit = 0; }
    }
}
