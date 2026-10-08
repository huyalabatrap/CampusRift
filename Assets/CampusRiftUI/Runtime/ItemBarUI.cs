using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CampusRift.Controls;
using CampusRift.Progression;

namespace CampusRift.UI
{
    // The item slots and the running buffs in the gameplay HUD (P08-T03, T08). Built in code under the gameplay HUD, so no
    // scene has to change. A slot shows the icon, the number left, the key (1/2/3) and a cooldown overlay; tapping or
    // clicking it uses the item as well (this is how phones use items). Using an item flashes a ring in its colour.
    public sealed class ItemBarUI : MonoBehaviour
    {
        const float SlotSize = 92f, Gap = 10f;
        PlayerItems items;
        BuffSystem buffs;
        TMP_Text fontSource;
        RectTransform bar, buffRow;
        CanvasGroup touchGroup;
        readonly List<Slot> slots = new List<Slot>();
        readonly List<BuffChip> chips = new List<BuffChip>();
        int builtSlots = -1;
        bool mobile;

        sealed class Slot { public RectTransform rect; public Image icon, cooldown; public Graphic back, ring; public MobileArc arc; public TMP_Text count, key, tag, seconds; public int index; }
        sealed class BuffChip { public RectTransform rect; public Image icon, fill; public TMP_Text time; }

        // Attaches a bar to the gameplay HUD for this player, once.
        public static ItemBarUI Attach(PlayerItems player)
        {
            var hud = Object.FindAnyObjectByType<GameplayHUD>(FindObjectsInactive.Include);
            if (hud == null || player == null) return null;
            var existing = hud.GetComponentInChildren<ItemBarUI>(true);
            if (existing != null) { existing.Bind(player); return existing; }
            var go = new GameObject("Item Bar", typeof(RectTransform));
            go.transform.SetParent(hud.transform, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var ui = go.AddComponent<ItemBarUI>(); ui.Bind(player); return ui;
        }

        void Bind(PlayerItems player)
        {
            if (items != null) { items.Changed -= Rebuild; items.ItemUsed -= OnUsed; }
            items = player; buffs = player.GetComponent<BuffSystem>();
            fontSource = GetComponentInParent<GameplayHUD>() != null ? GetComponentInParent<GameplayHUD>().GetComponentInChildren<TMP_Text>(true) : null;
            items.Changed += Rebuild; items.ItemUsed += OnUsed;
            Rebuild();
        }
        void OnDestroy() { if (items != null) { items.Changed -= Rebuild; items.ItemUsed -= OnUsed; } }

        void Rebuild()
        {
            if (items == null) return;
            EnsureRoots();
            int count = items.Bag != null ? items.Bag.Slots.Count : 0;
            mobile = CampusInput.Mobile;
            if (count != builtSlots || (slots.Count > 0 && bar.anchoredPosition.y != SlotY))
            {
                foreach (var s in slots) if (s.rect != null) Destroy(s.rect.gameObject);
                slots.Clear(); builtSlots = count;
                for (int i = 0; i < count; i++) slots.Add(BuildSlot(i));
            }
            bar.gameObject.SetActive(!mobile && count > 0);
            float y = SlotY; bar.anchoredPosition = new Vector2(24, y);
            Refresh();
        }
        float SlotY => mobile ? 300f : 190f;

        void EnsureRoots()
        {
            if (bar != null) return;
            // The authored GameplayHUD group blocks raycasts. Its mobile interactive child
            // opts out, and is enabled only while gameplay accepts input.
            touchGroup=gameObject.AddComponent<CanvasGroup>();
            bar = NewRect("Slots", transform, Vector2.zero, Vector2.zero, new Vector2(0, 0));
            buffRow = NewRect("Buffs", transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            buffRow.anchoredPosition = new Vector2(24, -150);
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.pivot = pivot; r.sizeDelta = Vector2.zero;
            return r;
        }
        Image NewImage(string name, RectTransform parent, Vector2 size, Color color, Sprite sprite = null)
        {
            var r = NewRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f)); r.sizeDelta = size;
            var img = r.gameObject.AddComponent<Image>(); img.color = color; img.sprite = sprite; img.raycastTarget = false; return img;
        }
        TMP_Text NewText(string name, RectTransform parent, string text, float size, Vector2 anchor, Vector2 offset, TextAlignmentOptions alignment)
        {
            var r = NewRect(name, parent, anchor, anchor, anchor); r.sizeDelta = new Vector2(80, 32); r.anchoredPosition = offset;
            var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.alignment = alignment; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.color = Color.white;
            if (fontSource != null) { t.font = fontSource.font; t.fontSharedMaterial = fontSource.fontSharedMaterial; }
            ComicTheme.Text(t, name == "Key");
            t.margin=Vector4.zero;t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=12;
            r.sizeDelta=new Vector2(68,38);
            return t;
        }

        Slot BuildSlot(int index)
        {
            var s = new Slot { index = index };
            s.rect = NewRect("Slot " + (index + 1), bar, Vector2.zero, Vector2.zero, Vector2.zero);
            s.rect.sizeDelta = new Vector2(SlotSize, SlotSize); s.rect.anchoredPosition = new Vector2(index * (SlotSize + Gap), 0);
            if(mobile)
            {
                var back=s.rect.gameObject.AddComponent<RiftGraphic>();back.Form=RiftGraphic.Shape.Disc;
                back.color=new Color(.035f,.045f,.085f,.87f);back.raycastTarget=true;s.back=back;
                s.rect.gameObject.AddComponent<CircularItemHitFilter>();
                var inkRect=NewRect("Ink circle",s.rect,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,.5f));inkRect.sizeDelta=Vector2.one*SlotSize;
                var ink=inkRect.gameObject.AddComponent<RiftGraphic>();ink.Form=RiftGraphic.Shape.Ring;ink.Thickness=5;ink.color=ComicTheme.Ink;ink.raycastTarget=false;
                var edgeRect=NewRect("Item edge",s.rect,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,.5f));edgeRect.sizeDelta=Vector2.one*(SlotSize-8);
                var edge=edgeRect.gameObject.AddComponent<RiftGraphic>();edge.Form=RiftGraphic.Shape.Ring;edge.Thickness=2;edge.color=ComicTheme.Gold;edge.raycastTarget=false;
            }
            else s.back = ComicTheme.Frame(s.rect.gameObject);
            var button = s.rect.gameObject.AddComponent<Button>(); button.targetGraphic = s.back;
            int captured = index; button.onClick.AddListener(() => { if (items != null) items.Use(captured); });
            var nav = new Navigation { mode = Navigation.Mode.None }; button.navigation = nav;
            s.icon = NewImage("Icon", s.rect, new Vector2(SlotSize - 20, SlotSize - 20), Color.white);
            if(mobile)
            {
                var arcRect=NewRect("Cooldown",s.rect,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,.5f));arcRect.sizeDelta=Vector2.one*(SlotSize-12);
                s.arc=arcRect.gameObject.AddComponent<MobileArc>();s.arc.color=ComicTheme.Gold;s.arc.raycastTarget=false;
                s.seconds=NewText("Cooldown seconds",s.rect,"",25,new Vector2(.5f,.5f),Vector2.zero,TextAlignmentOptions.Center);
                s.seconds.rectTransform.pivot=new Vector2(.5f,.5f);
            }
            else
            {
                s.cooldown = NewImage("Cooldown", s.rect, new Vector2(SlotSize, SlotSize), new Color(0, 0, 0, .6f));
                s.cooldown.type = Image.Type.Filled; s.cooldown.fillMethod = Image.FillMethod.Vertical; s.cooldown.fillOrigin = 0; s.cooldown.fillAmount = 0;
            }
            var ringRect = NewRect("Ring", s.rect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f)); ringRect.sizeDelta = new Vector2(SlotSize + 10, SlotSize + 10);
            var ring = ringRect.gameObject.AddComponent<RiftGraphic>(); ring.Form = RiftGraphic.Shape.Ring; ring.Thickness = 6; ring.raycastTarget = false; ring.color = new Color(1, 1, 1, 0);
            s.ring = ring;
            s.count = NewText("Count", s.rect, "", 22, new Vector2(1, 0), new Vector2(-18, 18), TextAlignmentOptions.BottomRight);
            s.key = NewText("Key", s.rect, mobile ? "" : (index + 1).ToString(), 18, new Vector2(0, 1), new Vector2(16, -14), TextAlignmentOptions.TopLeft);
            s.key.color = new Color(1f, .85f, .5f);
            s.tag = NewText("Tag", s.rect, "", 13, new Vector2(1, 1), new Vector2(-18, -14), TextAlignmentOptions.TopRight); s.tag.color = new Color(.7f, 1f, .8f);
            if(mobile)
            {
                s.count.rectTransform.anchorMin=s.count.rectTransform.anchorMax=new Vector2(.5f,0);
                s.count.rectTransform.pivot=new Vector2(.5f,.5f);s.count.rectTransform.anchoredPosition=new Vector2(0,-22);s.count.alignment=TextAlignmentOptions.Center;
                s.tag.rectTransform.anchorMin=s.tag.rectTransform.anchorMax=new Vector2(.5f,1);
                s.tag.rectTransform.pivot=new Vector2(.5f,.5f);s.tag.rectTransform.anchoredPosition=new Vector2(0,22);s.tag.alignment=TextAlignmentOptions.Center;
                ComicTheme.ReadabilityPlate(s.count);ComicTheme.ReadabilityPlate(s.tag);
            }
            return s;
        }

        void Update()
        {
            if (items == null) return;
            if (builtSlots < 0 || mobile != CampusInput.Mobile) Rebuild();
            Refresh();
        }

        void Refresh()
        {
            if (items == null || items.Bag == null) return;
            if(touchGroup!=null)
            {
                touchGroup.ignoreParentGroups=mobile;
                bool allowed=mobile&&(UIStateManager.Instance==null||UIStateManager.Instance.GameplayInputEnabled);
                touchGroup.interactable=touchGroup.blocksRaycasts=allowed;
            }
            bool vn = LevelHUD.Vietnamese;
            for (int i = 0; i < slots.Count && i < items.Bag.Slots.Count; i++)
            {
                var s = slots[i]; var data = items.Bag.Slots[i];
                s.icon.sprite = ItemIcons.Get(data.item);
                bool empty = data.remaining <= 0;
                s.icon.color = empty ? new Color(1, 1, 1, .25f) : Color.white;
                s.count.text = Progression.DevMode.Quantity(data.remaining); s.tag.text = data.item.IsPassive ? (vn ? "TỰ ĐỘNG" : "AUTO") : "";
                s.key.text=mobile?"":(i+1).ToString();
                if(s.cooldown!=null)s.cooldown.fillAmount = data.item.IsPassive ? 0 : items.CooldownFraction;
                if(s.arc!=null)
                {
                    s.arc.color=data.item.tint;s.arc.SetFill(data.item.IsPassive?0:items.CooldownFraction);
                    bool cooling=!data.item.IsPassive&&items.CooldownLeft>0;
                    s.seconds.text=cooling?Mathf.CeilToInt(items.CooldownLeft)+"s":"";s.icon.gameObject.SetActive(!cooling);
                }
                if (s.ring.color.a > 0) { var c = s.ring.color; c.a = Mathf.Max(0, c.a - Time.unscaledDeltaTime * 2.2f); s.ring.color = c; s.ring.rectTransform.localScale = Vector3.one * (1f + (1f - c.a) * .25f); }
            }
            RefreshBuffs();
        }

        void RefreshBuffs()
        {
            if (buffs == null) return;
            var active = buffs.Active;
            while (chips.Count < active.Count) chips.Add(BuildChip(chips.Count));
            for (int i = 0; i < chips.Count; i++)
            {
                bool on = i < active.Count; chips[i].rect.gameObject.SetActive(on);
                if (!on) continue;
                var b = active[i];
                chips[i].icon.sprite = ItemIcons.Get(b.item); chips[i].icon.color = ItemIcons.IsGenerated(b.item) ? b.item.tint : Color.white;
                chips[i].fill.fillAmount = b.Fraction;
                chips[i].time.text = Mathf.CeilToInt(b.remaining) + "s";
            }
        }
        BuffChip BuildChip(int index)
        {
            var c = new BuffChip { rect = NewRect("Buff " + index, buffRow, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1)) };
            c.rect.sizeDelta = new Vector2(64, 64); c.rect.anchoredPosition = new Vector2(index * 74, 0);
            var back = ComicTheme.Frame(c.rect.gameObject);
            c.icon = NewImage("Icon", c.rect, new Vector2(50, 50), Color.white);
            c.fill = NewImage("Timer", c.rect, new Vector2(64, 64), new Color(0, 0, 0, .55f));
            c.fill.type = Image.Type.Filled; c.fill.fillMethod = Image.FillMethod.Vertical; c.fill.fillOrigin = 1;
            c.time = NewText("Time", c.rect, "", 20, new Vector2(.5f, 0), new Vector2(0, -22), TextAlignmentOptions.Center);
            return c;
        }

        void OnUsed(ItemDefinition item, ItemUseResult result)
        {
            if (items?.Bag == null) return;
            for (int i = 0; i < items.Bag.Slots.Count && i < slots.Count; i++)
                if (items.Bag.Slots[i].item == item)
                {
                    var color = result == ItemUseResult.Used ? item.tint : new Color(1f, .35f, .35f);
                    color.a = result == ItemUseResult.Used ? .9f : .6f; slots[i].ring.color = color; break;
                }
        }
    }

    // Only mobile slots acquire this filter; Button still owns the normal press/click lifecycle.
    public sealed class CircularItemHitFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 screen, Camera eventCamera)
        {
            var r=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(r,screen,eventCamera,out var p))return false;
            float radius=Mathf.Min(r.rect.width,r.rect.height)*.5f;
            return (p-r.rect.center).sqrMagnitude<=radius*radius;
        }
    }
}
