using System;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Monsters;

namespace CampusRift.Progression
{
    public enum ItemUseResult { Used, EmptySlot, Cooldown, Passive, NothingToDo, Blocked }

    // The items the player carries into a level and their use (P08-T04, T05, T08). Keys 1/2/3 (or the touch buttons) use the
    // slots; the Hộ Mệnh Phù is never used by hand: it saves the player once per level when a hit would kill.
    [DisallowMultipleComponent, DefaultExecutionOrder(70)]
    public sealed class PlayerItems : MonoBehaviour
    {
        public const float SharedCooldown = 1f, ReviveHealth = 0.4f, ReviveInvulnerability = 2f;
        PlayerStats stats;
        PlayerMonsterHealth health;
        SpiritPower spirit;
        BuffSystem buffs;
        CampusInput input;
        ProfileService profile;
        float readyAt;
        bool reviveSpent,fullHealSpent,elementSpent;
        // Time source for the shared cooldown; tests replace it.
        public Func<float> Clock = () => Time.time;
        public LevelBag Bag { get; private set; }
        public float CooldownLeft => DevMode.NoCooldown ? 0 : Mathf.Max(0, readyAt - Clock());
        public float CooldownFraction => Mathf.Clamp01(CooldownLeft / SharedCooldown);
        public bool ReviveUsed => reviveSpent;
        public event Action Changed;
        // item, result — the HUD flashes a ring in the item's colour on Used.
        public event Action<ItemDefinition, ItemUseResult> ItemUsed;
        public event Action<ItemDefinition> Revived;

        void Awake()
        {
            stats = GetComponent<PlayerStats>(); health = GetComponent<PlayerMonsterHealth>(); spirit = GetComponent<SpiritPower>();
            buffs = GetComponent<BuffSystem>(); if (buffs == null) buffs = gameObject.AddComponent<BuffSystem>();
            input = GetComponent<CampusInput>();
        }

        void Start()
        {
            profile = ProfileService.Instance;
            if (health != null) health.BeforeDefeat += OnBeforeDefeat;
            if (profile != null) { profile.Artifacts.Changed += ApplyArtifacts; profile.Changed += ApplyArtifacts; }
            ApplyArtifacts();
            BeginLevel();
            UI.ItemBarUI.Attach(this);
        }
        void OnDestroy()
        {
            if (health != null) health.BeforeDefeat -= OnBeforeDefeat;
            if (profile != null && profile.Artifacts != null) { profile.Artifacts.Changed -= ApplyArtifacts; profile.Changed -= ApplyArtifacts; }
        }

        void ApplyArtifacts() { if (profile != null && stats != null) profile.Artifacts.ApplyTo(stats); }

        // A fresh bag for a new attempt: buffs of an earlier attempt do not carry over.
        public void BeginLevel()
        {
            reviveSpent = fullHealSpent = elementSpent = false; readyAt = 0;
            buffs.ClearAll();
            Bag = profile != null ? profile.Inventory.BeginLevel() : null;
            Changed?.Invoke();
        }

        void Update()
        {
            if (input == null || Bag == null) return;
            if (input.Pressed(CampusAction.Item1)) Use(0);
            if (input.Pressed(CampusAction.Item2)) Use(1);
            if (input.Pressed(CampusAction.Item3)) Use(2);
        }

        // Read-only availability for both item UIs; Use remains the only consuming path.
        public ItemUseResult Availability(int slot)
        {
            if (Bag == null || slot < 0 || slot >= Bag.Slots.Count || Bag.Slots[slot].remaining <= 0) return ItemUseResult.EmptySlot;
            var item = Bag.Slots[slot].item;
            if (item == null) return ItemUseResult.EmptySlot;
            if(!DevMode.Active && ((item.id=="cuu-chuyen-hoan-hon-dan"&&fullHealSpent)||(item.IsElementTalisman&&elementSpent)))return ItemUseResult.Blocked;
            if (item.IsPassive) return ItemUseResult.Passive;
            if (health != null && health.IsDead) return ItemUseResult.Blocked;
            if (CooldownLeft > 0) return ItemUseResult.Cooldown;
            // A heal at full health or a spirit refill at full spirit would only waste the item.
            if (item.effects.Count > 0 && OnlyWasted(item)) return ItemUseResult.NothingToDo;
            return ItemUseResult.Used;
        }
        public ItemUseResult Use(int slot)
        {
            var available = Availability(slot);
            if (available == ItemUseResult.EmptySlot) return available;
            var item = Bag.Slots[slot].item;
            if (available != ItemUseResult.Used) return Report(item, available);
            if (!Bag.Consume(slot)) return ItemUseResult.EmptySlot;
            if(item.id=="cuu-chuyen-hoan-hon-dan")fullHealSpent=true;if(item.IsElementTalisman)elementSpent=true;
            Effect(item);
            readyAt = Clock() + SharedCooldown;
            Changed?.Invoke();
            return Report(item, ItemUseResult.Used);
        }
        ItemUseResult Report(ItemDefinition item, ItemUseResult result) { if (result == ItemUseResult.Used && item != null) { LocalTelemetry.Item(item.id); UI.TutorialDirector.ItemUsed(); } ItemUsed?.Invoke(item, result); return result; }

        bool OnlyWasted(ItemDefinition item)
        {
            bool healFull = health != null && health.CurrentHealth >= health.maxHealth - 0.01f;
            bool spiritFull = spirit == null || spirit.Current >= spirit.Max - 0.01f;
            foreach (var e in item.effects)
            {
                switch (e.type)
                {
                    case ItemEffectType.HealInstant: case ItemEffectType.HealOverTime: if (!healFull) return false; break;
                    case ItemEffectType.RestoreSpirit: if (!spiritFull) return false; break;
                    default: return false;
                }
            }
            return true;
        }

        void Effect(ItemDefinition item)
        {
            foreach (var e in item.effects)
            {
                switch (e.type)
                {
                    case ItemEffectType.RestoreEnergy: GetComponent<CampusExplorer>()?.RefillEnergy();break;
                    case ItemEffectType.ClearNegative: GetComponent<Enemies.PlayerEnemyControl>()?.Clear();GetComponent<StatusEffectHost>()?.Clear();break;
                    case ItemEffectType.HealInstant: if (health != null) health.Heal(health.maxHealth * e.value); break;
                    case ItemEffectType.RestoreSpirit: if (spirit != null) spirit.Restore(spirit.Max * e.value); break;
                }
            }
            buffs.Apply(item);   // stat buffs, flags and heal over time
        }

        // Hộ Mệnh Phù: cancels one death per level.
        bool OnBeforeDefeat(DamageInfo info)
        {
            if (Bag == null || (!DevMode.Active && reviveSpent)) return false;
            int slot = Bag.IndexOf(i => i.IsPassive);
            if (slot < 0) return false;
            var item = Bag.Slots[slot].item;
            if (!Bag.Consume(slot)) return false;
            reviveSpent = true;
            float fraction = ReviveHealth, seconds = ReviveInvulnerability;
            foreach (var e in item.effects) if (e.type == ItemEffectType.Revive) { fraction = e.value; seconds = e.duration; }
            health.Revive(fraction, seconds);
            Changed?.Invoke(); Revived?.Invoke(item); LocalTelemetry.Item(item.id); ItemUsed?.Invoke(item, ItemUseResult.Used);
            return true;
        }
    }
}
