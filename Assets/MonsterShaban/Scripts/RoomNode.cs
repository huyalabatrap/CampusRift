using System;
using UnityEngine;

namespace CampusRift.Monsters
{
    public enum RoomConnectionType { Corridor, Door, Stair, Outdoor }
    // ElevatorLanding exists only in the runtime CampusNavGraph view. Observation: extra look-out
    // points inside large rooms/halls. Values are appended, so stored values are unchanged.
    public enum RoomNodeKind { Zone, Door, StairLanding, Exit, Junction, Outdoor, ElevatorLanding, Observation }
    [Serializable] public sealed class RoomConnection
    {
        public int Neighbor;
        public float Cost;
        public float Distance;
        public int FloorFrom, FloorTo;
        public RoomConnectionType ConnectionType;
    }
    [Serializable] public sealed class RoomNode
    {
        public string RoomID, BuildingID;
        public int FloorID;
        public Vector3 WorldPosition;
        public RoomNodeKind Kind;
        public bool StairEntry, StairExit;
        public RoomConnection[] Neighbors = new RoomConnection[0];
    }
}
