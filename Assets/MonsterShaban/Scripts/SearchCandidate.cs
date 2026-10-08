using System;
using UnityEngine;
namespace CampusRift.Monsters
{
    [Serializable] public sealed class SearchCandidate
    {
        public Vector3 Position;
        public float Probability;
        public bool Visited;
        public string Room, Reason;
        public int ExpansionLevel;
    }
}
