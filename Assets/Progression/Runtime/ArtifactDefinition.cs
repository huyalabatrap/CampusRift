using UnityEngine;

namespace CampusRift.Progression
{
    public enum ArtifactEffect { BasicDamage = 0, MaxHealth = 1, ItemSlots = 2, Spirit = 3, ElementCounter = 4 }

    // A permanent upgrade of plan §9.4 (MVP: three of the five).
    [CreateAssetMenu(menuName = "Campus Rift/Artifact")]
    public sealed class ArtifactDefinition : ScriptableObject
    {
        public string id;
        public string nameEN, nameVN, descriptionEN, descriptionVN;
        public ArtifactEffect effect;
        [Tooltip("Per level: 0.06 = +6% (BasicDamage, MaxHealth) or 1 = one item slot.")] public float perLevel = 0.05f;
        public float regenPerLevel;
        public int[] prices = { 300, 600, 1000, 1600, 2500 };
        public int MaxLevel => prices != null ? prices.Length : 0;
        public string Name(bool vietnamese) => vietnamese && !string.IsNullOrEmpty(nameVN) ? nameVN : nameEN;
        public string Description(bool vietnamese) => vietnamese && !string.IsNullOrEmpty(descriptionVN) ? descriptionVN : descriptionEN;
        public string EffectText(int level, bool vietnamese)
        {
            switch (effect)
            {
                case ArtifactEffect.Spirit: return "+"+(perLevel*level).ToString("0")+" Linh Lực · +"+(regenPerLevel*level).ToString("0.0")+"/s";
                case ArtifactEffect.ElementCounter: return "+"+Mathf.RoundToInt(perLevel*level*100)+"% "+(vietnamese?"sát thương khắc hệ":"counter-element damage");
                case ArtifactEffect.ItemSlots: return vietnamese ? "+" + Mathf.RoundToInt(perLevel * level) + " ô vật phẩm" : "+" + Mathf.RoundToInt(perLevel * level) + " item slot(s)";
                case ArtifactEffect.MaxHealth: return (vietnamese ? "+" : "+") + Mathf.RoundToInt(perLevel * level * 100) + "% " + (vietnamese ? "máu tối đa" : "max health");
                default: return "+" + Mathf.RoundToInt(perLevel * level * 100) + "% " + (vietnamese ? "sát thương đánh thường" : "basic attack damage");
            }
        }
    }
}
