using UnityEngine;
namespace CampusRift.Enemies
{
    [CreateAssetMenu(menuName="Campus Rift/Elite Affix")]
    public sealed class EliteAffixDefinition:ScriptableObject
    {
        public EliteAffixKind kind;public string nameVN,nameEN;public Color color=Color.yellow;public Sprite icon;public int minLevel=6;
    }
}
