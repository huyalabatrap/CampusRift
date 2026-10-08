using System;
using UnityEngine;

namespace CampusRift.Monsters
{
    public enum MonsterEvidenceType { Visual, Sound, Prediction, SearchResult }
    public enum MonsterEvidenceSource { Unknown, PlayerCandidate, PhantomCandidate, Environment }

    [Serializable]
    public struct MonsterMemoryRecord
    {
        public Vector3 Position, Velocity;
        public float Timestamp, Confidence;
        public MonsterEvidenceType EvidenceType;
        public MonsterEvidenceSource Source;
        public string RoomID, BuildingID;
        public int FloorID;
        public float ConfidenceAt(float now, float duration)
        {
            float age = Mathf.Max(0, now - Timestamp);
            return age > duration ? 0 : Confidence * Mathf.Exp(-3f * age / Mathf.Max(0.1f, duration));
        }
    }
}
