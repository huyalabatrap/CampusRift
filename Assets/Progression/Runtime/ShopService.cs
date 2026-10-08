using System;
using System.Collections.Generic;

namespace CampusRift.Progression
{
    public enum PurchaseResult { Ok, NotFound, Locked, NotEnough, TooMany }

    // Buying items (P08-T06). Linh Thạch is the only currency and it is only earned by studying and playing:
    // there is no purchase path with real money (plan §10).
    public sealed class ShopService
    {
        readonly ProfileService owner;
        readonly ItemCatalog catalog;
        public ShopService(ProfileService owner, ItemCatalog catalog = null) { this.owner = owner; this.catalog = catalog != null ? catalog : ItemCatalog.Instance; }
        public ItemCatalog Catalog => catalog;

        static string RealmOnly(Realm realm, bool vietnamese) => CultivationTable.Instance.RealmName(realm, vietnamese);

        public bool IsUnlocked(ItemDefinition item) => item != null && (DevMode.Active || owner.Cultivation != null && owner.Cultivation.RealmIndex >= (int)item.availableFrom);

        public string LockReason(ItemDefinition item, bool vietnamese) =>
            IsUnlocked(item) ? null : (vietnamese ? "Cần cảnh giới " : "Requires ") + RealmOnly(item.availableFrom, vietnamese);

        // Price of a stack. There is no discount, no bundle and no premium option.
        public int Price(ItemDefinition item, int quantity) => DevMode.Active ? 0 : item.price * Math.Max(0, quantity);

        public const int StackLimit = 99;
        public int MaxPurchasable(ItemDefinition item) => DevMode.Active ? StackLimit : Math.Max(0, StackLimit - owner.Inventory.Count(item.id));

        public PurchaseResult Check(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0) return PurchaseResult.NotFound;
            if (!IsUnlocked(item)) return PurchaseResult.Locked;
            if (quantity > MaxPurchasable(item)) return PurchaseResult.TooMany;
            if (!owner.Wallet.CanAfford(Price(item, quantity))) return PurchaseResult.NotEnough;
            return PurchaseResult.Ok;
        }

        public PurchaseResult Buy(ItemDefinition item, int quantity, CampusRift.Combat.Element chosen = CampusRift.Combat.Element.None)
        {
            if(item!=null&&item.id=="ngu-hanh-phu"){item=catalog.ElementVariant(chosen);if(item==null)return PurchaseResult.NotFound;}
            var check = Check(item, quantity);
            if (check != PurchaseResult.Ok) return check;
            if (!owner.Wallet.TrySpend(Price(item, quantity))) return PurchaseResult.NotEnough;
            owner.Inventory.Add(item.id, quantity);
            return PurchaseResult.Ok;
        }

        public int Missing(ItemDefinition item, int quantity) => Math.Max(0, Price(item, quantity) - owner.Wallet.Balance);
    }
}
