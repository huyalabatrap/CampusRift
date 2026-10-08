using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CampusRift.Enemies;
namespace CampusRift.Levels
{
    [Serializable] public sealed class SpawnWeight
    {
        public EnemyArchetype archetype;public float weight=1;public int minPerWave=0,maxPerWave=99;
    }
    [CreateAssetMenu(menuName="Campus Rift/Level Spawn Table")]
    public sealed class LevelSpawnTable : ScriptableObject
    {
        public List<SpawnWeight> roster=new List<SpawnWeight>();
        public int maxHeavyConcurrent=2,maxBombersConcurrent=2;
        public float maxBomberFraction=.2f,minRangedFraction=.15f,maxRangedFraction=.45f;
        public List<SpawnEntry> finalWave=new List<SpawnEntry>();
        public int[] elitesPerWave=new int[0];
        List<SpawnEntry> AssignElites(List<SpawnEntry> entries,int wave,int seed){int n=wave<elitesPerWave.Length?elitesPerWave[wave]:0;var random=new System.Random(seed+wave*113);var remaining=entries.Sum(e=>e.count);while(n>0&&remaining>0){int i=random.Next(entries.Count);var e=entries[i];if(e.eliteCount>=e.count)continue;e.eliteCount++;entries[i]=e;n--;remaining--;}return entries;}
        public List<SpawnEntry> Generate(int count,int wave,int seed,bool final)
        {
            if(final)return AssignElites(finalWave.Select(e=>new SpawnEntry{archetype=e.archetype,count=e.count}).ToList(),wave,seed);
            var random=new System.Random(seed+wave*7919);var counts=new int[roster.Count];int used=0;
            for(int i=0;i<roster.Count;i++){counts[i]=Mathf.Min(roster[i].minPerWave,count-used);used+=counts[i];}
            while(used<count){var candidates=new List<int>();float total=0;
                for(int i=0;i<roster.Count;i++){
                    var r=roster[i];if(r.archetype==null||counts[i]>=r.maxPerWave)continue;
                    if(r.archetype.id=="bao-thi"&&counts[i]>=Mathf.Max(1,Mathf.FloorToInt(count*maxBomberFraction)))continue;
                    int ranged=0;for(int j=0;j<roster.Count;j++)if(roster[j].archetype.ranged)ranged+=counts[j];
                    if(r.archetype.ranged&&ranged>=Mathf.CeilToInt(count*maxRangedFraction))continue;
                    if(!r.archetype.ranged&&roster.Any(x=>x.archetype.ranged)&&count-used<=Mathf.CeilToInt(count*minRangedFraction)-ranged)continue;
                    candidates.Add(i);total+=Mathf.Max(.01f,r.weight);
                }
                if(candidates.Count==0)throw new InvalidOperationException("Unsatisfiable roster constraints: "+name);
                float pick=(float)random.NextDouble()*total;int chosen=candidates[candidates.Count-1];
                foreach(int i in candidates){pick-=Mathf.Max(.01f,roster[i].weight);if(pick<=0){chosen=i;break;}}
                counts[chosen]++;used++;
            }
            var entries=new List<SpawnEntry>();for(int i=0;i<counts.Length;i++)if(counts[i]>0)entries.Add(new SpawnEntry{archetype=roster[i].archetype,count=counts[i]});return AssignElites(entries,wave,seed);
        }
        public bool CanSpawn(EnemyArchetype next,IEnumerable<EnemyInstance> alive)
        {
            if(next.id=="thiet-giap-nguu"&&alive.Count(e=>e.archetype.id==next.id)>=maxHeavyConcurrent)return false;
            if(next.id=="bao-thi"&&alive.Count(e=>e.archetype.id==next.id)>=maxBombersConcurrent)return false;
            return true;
        }
    }
}
