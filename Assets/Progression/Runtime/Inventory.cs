using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Progression
{
    // Items the player owns and what they take into the next level (P08-T05). Both live in the profile.
    // Nothing is taken out of the inventory when a level starts: an item leaves it only when it is used, so unused items
    // are always still there afterwards, whether the level was won, lost or the game was closed.
    public sealed class Inventory
    {
        readonly ProfileService owner;
        public Inventory(ProfileService owner) { this.owner = owner; }
        ProfileData Data => owner.Data;
        public event Action Changed;
        void Touch() { owner.MarkDirty(); Changed?.Invoke(); }

        public string CountText(string id) => DevMode.Quantity(Count(id));
        public int Count(string id) { if (DevMode.Active && ItemCatalog.Instance.Item(id) != null) return int.MaxValue; var e = Data.inventory.Find(x => x.key == id); return e != null ? e.count : 0; }
        public void Add(string id, int amount)
        {
            if (DevMode.Active || amount <= 0) return;
            var e = Data.inventory.Find(x => x.key == id);
            if (e == null) { e = new KeyCount { key = id }; Data.inventory.Add(e); }
            e.count += amount; Touch();
        }
        public bool TryRemove(string id, int amount)
        {
            if (DevMode.Active) return amount > 0 && ItemCatalog.Instance.Item(id) != null;
            var e = Data.inventory.Find(x => x.key == id);
            if (amount <= 0 || e == null || e.count < amount) return false;
            e.count -= amount; if (e.count == 0) Data.inventory.Remove(e);
            // A carried stack cannot outgrow what is left.
            var c = Data.carry.Find(x => x.key == id);
            if (c != null) { c.count = Math.Min(c.count, Count(id)); if (c.count <= 0) Data.carry.Remove(c); }
            Touch(); return true;
        }

        public int SlotCount => owner.Artifacts != null ? owner.Artifacts.ItemSlots() : ArtifactService.BaseItemSlots;
        public int CarryCount(string id) { var e = Data.carry.Find(x => x.key == id); return e != null ? e.count : 0; }
        public int CarriedKinds => Data.carry.Count;
        public IReadOnlyList<KeyCount> Carry => Data.carry;

        // Sets how many of an item to take. The limit is the per-level maximum of the item and what the player owns; a new kind
        // needs a free slot. Returns the number actually set.
        public int SetCarry(ItemDefinition item, int wanted)
        {
            if (item == null) return 0;
            if(item.IsElementTalisman&&wanted>0&&Data.carry.Exists(c=>c.key!=item.id&&c.key.StartsWith("ngu-hanh-phu")))return 0;
            int count = Mathf.Clamp(wanted, 0, Mathf.Min(item.maxPerLevel, Count(item.id)));
            var e = Data.carry.Find(x => x.key == item.id);
            if (count == 0) { if (e != null) Data.carry.Remove(e); Touch(); return 0; }
            if (e == null)
            {
                if (Data.carry.Count >= SlotCount) return 0;
                e = new KeyCount { key = item.id }; Data.carry.Add(e);
            }
            e.count = count; Touch(); return count;
        }
        public void ClearCarry() { Data.carry.Clear(); Touch(); }

        // The bag of one level attempt: the carried items, in the order they were chosen.
        public LevelBag BeginLevel(ItemCatalog catalog = null)
        {
            catalog = catalog != null ? catalog : ItemCatalog.Instance;
            var bag = new LevelBag(this);
            if (DevMode.Active && Data.carry.Count == 0)
                foreach (var item in catalog.items) { if (item == null || item.IsPassive) continue; SetCarry(item, item.maxPerLevel); if (Data.carry.Count >= SlotCount) break; }
            foreach (var c in new List<KeyCount>(Data.carry))
            {
                var def = catalog.Item(c.key); if (def == null) continue;
                int count = Mathf.Min(c.count, def.maxPerLevel, Count(c.key));
                if (count > 0 && bag.Slots.Count < SlotCount) bag.Slots.Add(new LevelBag.Slot { item = def, remaining = count });
            }
            return bag;
        }
    }

    // What is left of the carried items during a level.
    public sealed class LevelBag
    {
        public sealed class Slot { public ItemDefinition item; public int remaining; }
        public readonly List<Slot> Slots = new List<Slot>();
        readonly Inventory inventory;
        public LevelBag(Inventory inventory) { this.inventory = inventory; }

        // Uses one item: it leaves the inventory for good.
        public bool Consume(int index)
        {
            if (index < 0 || index >= Slots.Count) return false;
            var slot = Slots[index];
            if (slot.remaining <= 0 || !inventory.TryRemove(slot.item.id, 1)) return false;
            if (!DevMode.Active) slot.remaining--; return true;
        }
        public int IndexOf(Func<ItemDefinition, bool> match)
        {
            for (int i = 0; i < Slots.Count; i++) if (Slots[i].remaining > 0 && match(Slots[i].item)) return i;
            return -1;
        }
    }
}
