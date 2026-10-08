using UnityEngine;
using CampusRift.Skills;
using CampusRift.Combat;
namespace CampusRift.SkyBeast
{
    [CreateAssetMenu(menuName="Campus Rift/Sky Beast/Heaven Sword")]
    public sealed class HeavenSwordConfig : ScriptableObject
    {
        public float hoaThanChannel=2.5f, luyenHuChannel=2, doKiepChannel=1.5f;
        public float firstSeconds=6, repeatSeconds=3, restSeconds=15;
        public float swordLength=150, protectionSeconds=10, protectionFireResistance=.5f;
        public int highSwords=2400, lowSwords=1600, mobileSwords=960;
        public NguKiemConfig blade;
        public GiantHandConfig sounds;
        public Material gold;
        public AudioClip ready, victory;
        public static HeavenSwordConfig Load()=>Resources.Load<HeavenSwordConfig>("P15/HeavenSword");
    }
}
