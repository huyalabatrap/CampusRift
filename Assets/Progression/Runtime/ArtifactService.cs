using System;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Combat;

namespace CampusRift.Progression
{
    // Levels of the artifacts (in the profile), upgrades and their effect on the player (P08-T07).
    public sealed class ArtifactService
    {
        public const string Key = "artifact";
        public const int BaseItemSlots = 3;
        readonly ProfileService owner;
        public ArtifactService(ProfileService owner) { this.owner = owner; }
        public event Action Changed;

        public int Level(string id) { if (DevMode.Active) return ItemCatalog.Instance.artifacts.Find(a => a != null && a.id == id)?.MaxLevel ?? 0; var e = owner.Data.artifacts.Find(a => a.key == id); return e != null ? e.count : 0; }
        // Artifact levels never exceed the number of realms reached (realm 1 = Luyện Khí), so money cannot outrun study.
        public int LevelCap(CultivationService cultivation) => cultivation == null ? int.MaxValue : cultivation.RealmIndex + 1;
        public int Price(ArtifactDefinition def) { int level = Level(def.id); return level >= def.MaxLevel ? 0 : def.prices[level]; }

        public bool CanUpgrade(ArtifactDefinition def, out string reasonEN, out string reasonVN)
        {
            reasonEN = reasonVN = null;
            int level = Level(def.id);
            if (level >= def.MaxLevel) { reasonEN = "Maximum level"; reasonVN = "Đã đạt cấp tối đa"; return false; }
            int cap = LevelCap(owner.Cultivation);
            if (level >= cap) { reasonEN = "Reach a higher realm to go beyond level " + level; reasonVN = "Cần cảnh giới cao hơn để vượt cấp " + level; return false; }
            int price = def.prices[level];
            if (!owner.Wallet.CanAfford(price)) { int need = price - owner.Wallet.Balance; reasonEN = "Need " + need + " more Linh Thạch"; reasonVN = "Cần thêm " + need + " Linh Thạch"; return false; }
            return true;
        }

        public bool TryUpgrade(ArtifactDefinition def)
        {
            if (!CanUpgrade(def, out _, out _)) return false;
            if (!owner.Wallet.TrySpend(def.prices[Level(def.id)])) return false;
            var e = owner.Data.artifacts.Find(a => a.key == def.id);
            if (e == null) { e = new KeyCount { key = def.id }; owner.Data.artifacts.Add(e); }
            e.count++;
            owner.MarkDirty(); Changed?.Invoke();
            return true;
        }

        public float CounterBonus {get {float value=0;foreach(var a in ItemCatalog.Instance.artifacts)if(a!=null&&a.effect==ArtifactEffect.ElementCounter)value+=a.perLevel*Level(a.id);return value;}}

        public int ItemSlots(ItemCatalog catalog = null)
        {
            catalog = catalog != null ? catalog : ItemCatalog.Instance;
            int slots = BaseItemSlots;
            foreach (var a in catalog.artifacts) if (a != null && a.effect == ArtifactEffect.ItemSlots) slots += Mathf.RoundToInt(a.perLevel * Level(a.id));
            return slots;
        }

        // Pushes the permanent stat bonuses into the player. Keyed, so applying twice replaces instead of adding.
        public void ApplyTo(PlayerStats stats, ItemCatalog catalog = null)
        {
            if (stats == null) return;
            catalog = catalog != null ? catalog : ItemCatalog.Instance;
            foreach (var a in catalog.artifacts)
            {
                if (a == null) continue;
                float value = a.perLevel * Level(a.id);
                switch (a.effect)
                {
                    case ArtifactEffect.Spirit: stats.SetModifier(StatSource.Artifact,Key+":"+a.id,StatType.MaxSpirit,value,0);stats.SetModifier(StatSource.Artifact,Key+":"+a.id,StatType.SpiritRegen,a.regenPerLevel*Level(a.id),0);break;
                    case ArtifactEffect.BasicDamage: stats.SetModifier(StatSource.Artifact, Key + ":" + a.id, StatType.Attack, 0, value); break;
                    case ArtifactEffect.MaxHealth: stats.SetModifier(StatSource.Artifact, Key + ":" + a.id, StatType.MaxHealth, 0, value); break;
                }
            }
        }
    }
}
