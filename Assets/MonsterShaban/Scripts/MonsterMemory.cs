using UnityEngine;
using System.Collections.Generic;

namespace CampusRift.Monsters
{
    public sealed class MonsterMemory : MonoBehaviour
    {
        public MonsterAIConfig config;
        [SerializeField] Vector3 lastSeenPosition, lastHeardPosition, lastKnownVelocity;
        [SerializeField] float lastSeenTime = -10000, lastHeardTime = -10000;
        [SerializeField] float memoryConfidence;
        [SerializeField] string lastKnownRoom = "Unknown", lastKnownBuilding = "Unknown";
        [SerializeField] int lastKnownFloor = -1;
        public Vector3 LastSeenPosition => lastSeenPosition;
        public Vector3 LastHeardPosition => lastHeardPosition;
        public Vector3 LastKnownVelocity => lastKnownVelocity;
        public float LastSeenTime => lastSeenTime;
        public float LastHeardTime => lastHeardTime;
        public float MemoryConfidence => memoryConfidence;
        public string LastKnownRoom => lastKnownRoom;
        public string LastKnownBuilding => lastKnownBuilding;
        public int LastKnownFloor => lastKnownFloor;
        public bool HasEvidence => memoryConfidence > 0;
        public bool HasSeen => lastSeenTime > -1000;
        public bool HasHeard => lastHeardTime > -1000;
        public Vector3 LatestPosition => lastHeardTime > lastSeenTime ? lastHeardPosition : lastSeenPosition;
        public float LatestTime => Mathf.Max(lastHeardTime, lastSeenTime);
        [SerializeField] List<MonsterMemoryRecord> observations = new List<MonsterMemoryRecord>(10);
        public IReadOnlyList<MonsterMemoryRecord> RecentObservations => observations;
        public Vector3 SoundMovementDirection => soundVelocity.normalized;
        public Vector3 LatestVelocity => lastHeardTime > lastSeenTime ? soundVelocity : lastKnownVelocity;
        public int EvidenceRevision { get; private set; }
        public float LastSoundConfidence => soundConfidence;
        public MonsterEvidenceSource LastSeenSource { get; private set; }
        Vector3 soundVelocity;
        float sightConfidence, soundConfidence;
        MonsterSearch places;
        void Awake() { places = GetComponent<MonsterSearch>(); }

        public void RememberSight(MonsterPerception sight)
        {
            if (!sight.CanSeePlayer) return;
            ObserveVisual(sight.ObservedPosition, sight.ObservedVelocity, sight.ObservationTime);
        }
        public void ObserveVisual(Vector3 position, Vector3 velocity, float timestamp, float confidence = 1f,
            MonsterEvidenceSource source = MonsterEvidenceSource.Unknown)
        {
            if(timestamp <= lastSeenTime || timestamp > Time.time + 0.1f) return;
            lastSeenPosition = position; lastKnownVelocity = velocity; lastSeenTime = timestamp;
            sightConfidence = Mathf.Clamp01(confidence); memoryConfidence = sightConfidence;
            LastSeenSource = source;
            RememberLocation(position);
            AddRecord(position, velocity, timestamp, MonsterEvidenceType.Visual, sightConfidence, source);
            EvidenceRevision++;
        }
        public void RememberSound(Vector3 position, float confidence, float timestamp)
        {
            if (timestamp <= lastHeardTime || timestamp > Time.time + 0.1f) return;
            float dt = timestamp - lastHeardTime;
            // An audible sequence suggests motion; it never identifies its emitter as the player.
            Vector3 measured = dt > 0.08f && dt < 1.6f ? (position-lastHeardPosition)/dt : Vector3.zero;
            float sinceSight = timestamp - lastSeenTime;
            if (lastSeenTime > lastHeardTime && sinceSight > 0.2f && sinceSight < 3f)
            {
                // First sound right after losing sight: seen there, heard here moments later is a heading.
                Vector3 moved = (position - lastSeenPosition) / sinceSight;
                soundVelocity = moved.sqrMagnitude <= config.MaxObservedSpeed * config.MaxObservedSpeed ? moved : Vector3.zero;
            }
            else soundVelocity = measured.sqrMagnitude <= config.MaxObservedSpeed * config.MaxObservedSpeed ? Vector3.Lerp(soundVelocity, measured, 0.65f) : Vector3.zero;
            lastHeardPosition = position; lastHeardTime = timestamp;
            soundConfidence = Mathf.Clamp(confidence, 0.15f, 0.8f);
            RememberLocation(position);
            AddRecord(position, soundVelocity, timestamp, MonsterEvidenceType.Sound, soundConfidence);
            EvidenceRevision++; Decay();
        }
        void AddRecord(Vector3 position, Vector3 velocity, float timestamp, MonsterEvidenceType type, float confidence,
            MonsterEvidenceSource source = MonsterEvidenceSource.Unknown)
        {
            int capacity = config != null ? Mathf.Clamp(config.ObservationCapacity,5,10) : 8;
            while(observations.Count >= capacity) observations.RemoveAt(0);
            observations.Add(new MonsterMemoryRecord { Position=position,Velocity=velocity,Timestamp=timestamp,
                EvidenceType=type,Source=source,Confidence=confidence,RoomID=lastKnownRoom,FloorID=lastKnownFloor,BuildingID=lastKnownBuilding });
        }
        public void RejectPhantom(Vector3 position, float spawnedAt)
        {
            if (LastSeenSource == MonsterEvidenceSource.PhantomCandidate && lastSeenTime >= spawnedAt)
                sightConfidence = Mathf.Min(sightConfidence, 0.18f);
            if (lastHeardTime >= spawnedAt && Vector3.Distance(lastHeardPosition, position) < 6f)
                soundConfidence = Mathf.Min(soundConfidence, 0.12f);
            for (int i = 0; i < observations.Count; i++)
            {
                var record = observations[i];
                if (record.Timestamp < spawnedAt || Vector3.Distance(record.Position, position) > 6f) continue;
                record.Confidence = Mathf.Min(record.Confidence, 0.18f);
                observations[i] = record;
            }
            Decay(); EvidenceRevision++;
        }
        public void RememberInference(Vector3 position, MonsterEvidenceType type, float confidence)
        {
            if(type!=MonsterEvidenceType.Prediction && type!=MonsterEvidenceType.SearchResult)return;
            // Inference must not renew real sensory evidence or overwrite its last known location.
            AddRecord(position,Vector3.zero,Time.time,type,Mathf.Clamp01(confidence));
        }
        void RememberLocation(Vector3 evidence)
        {
            if(places==null) places=GetComponent<MonsterSearch>();
            if (places == null) return;
            var point = places.NearestLandmark(evidence);
            lastKnownRoom = point == null ? "Outdoor" : point.RoomID;
            lastKnownFloor = point == null ? 0 : point.FloorID;
            lastKnownBuilding = point == null ? "Campus" : point.BuildingID;
        }
        public void Decay()
        {
            float duration = config != null ? config.MemoryDuration : 16;
            var visual=new MonsterMemoryRecord{Timestamp=lastSeenTime,Confidence=sightConfidence};
            var sound=new MonsterMemoryRecord{Timestamp=lastHeardTime,Confidence=soundConfidence};
            memoryConfidence=Mathf.Max(visual.ConfidenceAt(Time.time,duration),sound.ConfidenceAt(Time.time,duration));
        }
        public void Forget()
        {
            memoryConfidence = sightConfidence = soundConfidence = 0; lastSeenTime = lastHeardTime = -10000;
            lastKnownVelocity = Vector3.zero; lastKnownRoom = lastKnownBuilding = "Unknown"; lastKnownFloor = -1;
            soundVelocity=Vector3.zero;observations.Clear();EvidenceRevision++;
            LastSeenSource = MonsterEvidenceSource.Unknown;
        }
    }
}
