using UnityEngine;

namespace CampusRift.Monsters
{
    [CreateAssetMenu(menuName = "Campus Rift/Shaban Room Graph")]
    public sealed class RoomGraph : ScriptableObject
    {
        public RoomNode[] Nodes = new RoomNode[0];
        public int Nearest(Vector3 point,float verticalTolerance=1.6f)
        {
            int nearest=-1;float distance=float.PositiveInfinity;
            for(int i=0;i<Nodes.Length;i++)
            {
                var delta=Nodes[i].WorldPosition-point;
                if(Mathf.Abs(delta.y)>verticalTolerance || delta.sqrMagnitude>=distance)continue;
                nearest=i;distance=delta.sqrMagnitude;
            }
            return nearest;
        }
    }
}
