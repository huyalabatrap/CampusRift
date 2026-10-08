using System.Collections.Generic;
using UnityEngine;
namespace CampusRift.Combat
{
    public interface IHealingAlly { Transform Anchor {get;}bool Alive {get;}float MaxHealth {get;}void Heal(float amount); }
    public static class HealingAllies
    {
        public static readonly List<IHealingAlly> Active=new List<IHealingAlly>(5);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){Active.Clear();}
    }
}
