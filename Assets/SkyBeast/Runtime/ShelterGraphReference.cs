using UnityEngine;
namespace CampusRift.SkyBeast
{
    public sealed class ShelterGraphReference:ScriptableObject
    {
        public Monsters.RoomGraph graph;
        public static Monsters.RoomGraph Graph=>Resources.Load<ShelterGraphReference>("P13/ShelterGraph")?.graph;
    }
}
