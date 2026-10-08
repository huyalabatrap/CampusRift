using System;
using System.Linq;
using UnityEngine;
using CampusRift.Enemies;
namespace CampusRift.Levels
{
    public enum EndgameMode { Normal, Tower, Nightmare }
    public enum TowerRule { FireMoon, Lightning, Silence }
    public static class EndgameFactory
    {
        public const float MaxTowerMultiplier=5;
        public static float Multiplier(int floor)=>Mathf.Min(MaxTowerMultiplier,1+.08f*(Mathf.Max(1,floor)-1));
        public static TowerRule Weekly(DateTime utc)
        {
            var epoch=new DateTime(2026,1,5,0,0,0,DateTimeKind.Utc);
            long week=(long)Math.Floor((utc.ToUniversalTime()-epoch).TotalDays/7);
            return (TowerRule)((week%3+3)%3);
        }
        public static LevelDefinition Tower(int floor,DateTime utc)
        {
            floor=Math.Max(1,floor);var source=LevelCatalog.Instance.Get((floor-1)%10+1);
            var d=UnityEngine.Object.Instantiate(source);d.name="Tower "+floor;d.runMode=EndgameMode.Tower;d.towerFloor=floor;d.towerRule=Weekly(utc);
            d.index=1;d.id="tower-"+floor;d.displayName="Trial Tower · Floor "+floor;d.displayNameVN="Tháp Thí Luyện · Tầng "+floor;
            d.requiredRealm=6;d.requiredTier=1;d.aiTier=4;d.scalingLevelOverride=10;
            d.healthMultiplier=Multiplier(floor);d.damageMultiplier=Multiplier(floor);d.speedMultiplier=1;d.bosses.Clear();
            d.waves.Clear();var w=new WaveDefinition {riftZones=source.zones.Select(z=>z.id).ToArray()};
            var roster=LevelCatalog.Instance.Get(10).spawnTable.roster.Where(x=>x.archetype!=null).Select(x=>x.archetype).ToArray();
            int total=Mathf.Min(floor%10==0?31:32,11+(floor-1)/2);for(int i=0;i<roster.Length;i++)w.entries.Add(new SpawnEntry{archetype=roster[i],count=total/roster.Length+(i<total%roster.Length?1:0),eliteCount=floor%5==0&&i==0?1:0});
            d.waves.Add(w);d.spawnTable=ScriptableObject.CreateInstance<LevelSpawnTable>();
            d.spawnTable.roster=roster.Select(a=>new SpawnWeight{archetype=a}).ToList();
            d.spawnTable.finalWave=w.entries.ToList();d.spawnTable.elitesPerWave=new[]{floor%5==0?Mathf.Min(3,1+floor/15):0};
            if(floor%10==0){var boss=LevelCatalog.Instance.Get(7).bosses.FirstOrDefault();if(boss!=null)d.bosses.Add(boss);}
            d.parTimeSeconds=600;d.restSeconds=3;return d;
        }
        public static LevelDefinition Nightmare(int index)
        {
            var d=UnityEngine.Object.Instantiate(LevelCatalog.Instance.Get(Mathf.Clamp(index,1,10)));
            d.runMode=EndgameMode.Nightmare;d.id="nightmare-"+index;
            d.displayName="Nightmare · "+d.displayName;d.displayNameVN="Ác Mộng · "+d.displayNameVN;
            d.healthMultiplier*=1.5f;d.damageMultiplier*=1.5f;d.speedMultiplier*=1.5f;d.aiTier=Mathf.Min(4,d.aiTier+1);
            return d;
        }
        public static void Dispose(LevelDefinition d)
        {if(d==null||d.runMode==EndgameMode.Normal)return;if(d.runMode==EndgameMode.Tower)UnityEngine.Object.Destroy(d.spawnTable);UnityEngine.Object.Destroy(d);}
    }
}
