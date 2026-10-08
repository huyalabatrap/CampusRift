using UnityEngine;
using CampusRift.Combat;

namespace CampusRift.Progression
{
    // Pushes the stats of the player's realm and tier into PlayerStats (P06-T04). Modifiers are keyed, so applying
    // twice (a scene reload, a tier change) replaces the previous values instead of adding to them.
    [DefaultExecutionOrder(50), DisallowMultipleComponent]
    public sealed class CultivationPlayerBridge : MonoBehaviour
    {
        public const string Key = "cultivation";
        PlayerStats stats; CultivationService cultivation;

        void Start()
        {
            // The economy components (P08) hang on the same player object; added here so no scene or prefab has to change.
            if (GetComponent<BuffSystem>() == null) gameObject.AddComponent<BuffSystem>();
            if (GetComponent<PlayerItems>() == null) gameObject.AddComponent<PlayerItems>();
            stats = GetComponent<PlayerStats>();
            cultivation = ProfileService.CultivationOrNull;
            if (stats == null || cultivation == null) return;
            cultivation.Changed += Apply; Apply();
        }
        void OnDestroy() { if (cultivation != null) cultivation.Changed -= Apply; }

        public void Apply()
        {
            if (stats == null || cultivation == null) return;
            stats.SetModifier(StatSource.Cultivation, Key, StatType.MaxHealth, cultivation.MaxHealth - stats.BaseHealth, 0);
            stats.SetModifier(StatSource.Cultivation, Key, StatType.Attack, cultivation.Attack - stats.baseAttack, 0);
            stats.SetModifier(StatSource.Cultivation, Key, StatType.MaxSpirit, cultivation.MaxSpirit - stats.baseSpirit, 0);
            stats.SetModifier(StatSource.Cultivation, Key, StatType.Defense, cultivation.Defense - stats.baseDefense, 0);
            stats.SetModifier(StatSource.Cultivation, Key, StatType.MoveSpeed, 0, cultivation.RunBonus);
        }
    }
}
