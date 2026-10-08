using System;

namespace CampusRift.Progression
{
    // Linh Thạch of the player (plan §9). It only ever grows from studying and playing; there is no way to buy it.
    // The balance lives in the profile, so this reads the current profile on every call (tests swap profiles).
    public sealed class Wallet
    {
        readonly ProfileService owner;
        public Wallet(ProfileService owner) { this.owner = owner; }
        WalletData Data => owner.Data.wallet;
        public int Balance => DevMode.Active ? int.MaxValue : Data.linhThach;
        public string BalanceText => DevMode.Quantity(Balance);
        public event Action<int, string> Earned;     // amount, source
        public event Action Changed;

        public int Earn(int amount, string source)
        {
            if (DevMode.Active || amount <= 0) return 0;
            Data.linhThach += amount; Data.lifetimeEarned += amount;
            owner.MarkDirty(); Earned?.Invoke(amount, source); Changed?.Invoke();
            return amount;
        }
        public bool CanAfford(int amount) => amount >= 0 && (DevMode.Active || Data.linhThach >= amount);
        public bool TrySpend(int amount)
        {
            if (amount < 0) return false;
            if (DevMode.Active) return true;
            if (Data.linhThach < amount) return false;
            Data.linhThach -= amount; Data.lifetimeSpent += amount;
            owner.MarkDirty(); Changed?.Invoke();
            return true;
        }
    }
}
