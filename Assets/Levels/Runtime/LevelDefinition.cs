using System;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.UI;

namespace CampusRift.Levels
{
    public enum StarKind { None, KillsWithSkill, MaxProjectileHits, MaxFireHitsOutdoors, ComboReactions, BossTimeLimit, BeatParTime, NoHealPotion, SwordWithin, SummonerBeforeSecond }

    // The special (third-star) challenge of one level. Evaluated by StarEvaluator (P12); P05 only stores it.
    [Serializable] public struct StarCondition
    {
        public StarKind kind;
        public float value;
        public string descriptionEN, descriptionVN;
    }

    [Serializable] public struct SpawnEntry
    {
        public EnemyArchetype archetype;
        [Min(1)] public int count;
        // Elites are applied by P12 (affixes); until then they are spawned as ordinary monsters.
        [Min(0)] public int eliteCount;
    }

    [Serializable] public sealed class WaveDefinition
    {
        public List<SpawnEntry> entries = new List<SpawnEntry>();
        // Ids of RiftZoneData in the owning level that this wave may spawn from.
        public string[] riftZones = new string[0];
        public int TotalCount { get { int n = 0; foreach (var e in entries) n += e.count; return n; } }
    }

    // A spot in the campus where a rift opens (plan §2.2). Points are checked against the NavMesh by LevelValidation.
    [Serializable] public struct RiftZoneData
    {
        public EndgameMode runMode;
        public int towerFloor, scalingLevelOverride;
        public TowerRule towerRule;
        public string id;
        public Vector3 position;
        [Min(0.5f)] public float radius;
    }

    // One of the ten levels (plan §14.3). Purely data; LevelDirector plays it.
    [CreateAssetMenu(menuName = "Campus Rift/Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public EndgameMode runMode;
        public int towerFloor, scalingLevelOverride;
        public TowerRule towerRule;
        public string id;
        [Range(1, 10)] public int index = 1;
        public string displayName, displayNameVN;
        [TextArea] public string briefEN, briefVN;
        // Required cultivation: realm 0..6 (Luyện Khí … Độ Kiếp) and tier 1..9. Realms arrive with P06.
        public int requiredRealm, requiredTier = 1;
        public SkyPreset sky = SkyPreset.Dusk;
        public Vector3 spawnPoint = new Vector3(0, 0.13f, -10);
        public float spawnYaw;
        public List<RiftZoneData> zones = new List<RiftZoneData>();
        public List<WaveDefinition> waves = new List<WaveDefinition>();
        public LevelSpawnTable spawnTable;
        [Range(0, 4)] public int aiTier;
        [Min(0.1f)] public float healthMultiplier = 1, damageMultiplier = 1, speedMultiplier = 1;
        // Boss and sky-beast phases are played by P12 and P14–P15.
        public List<EnemyArchetype> bosses = new List<EnemyArchetype>();
        public StarCondition[] stars = new StarCondition[3];
        [Min(30)] public float parTimeSeconds = 240;
        [Min(1)] public float restSeconds = 15;

        public int TotalMonsters { get { int n = 0; foreach (var w in waves) n += w.TotalCount; return n; } }
        public EnemyScaling Scaling => new EnemyScaling { health = healthMultiplier, damage = damageMultiplier, speed = speedMultiplier, aiTier = aiTier, level = scalingLevelOverride>0?scalingLevelOverride:index };
        public string LocalizedName(bool vietnamese) => vietnamese && !string.IsNullOrEmpty(displayNameVN) ? displayNameVN : displayName;
        public bool TryGetZone(string zoneId, out RiftZoneData zone)
        {
            foreach (var z in zones) if (z.id == zoneId) { zone = z; return true; }
            zone = default; return false;
        }
    }
}
