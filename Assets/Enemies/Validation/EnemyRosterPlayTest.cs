#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using CampusRift.Levels;
namespace CampusRift.Validation
{
    public sealed class EnemyRosterPlayTest : P12PlayTest
    {
        protected override IEnumerator Run(){Begin();int[] expected={3,4,4,5,5,9,9,11,11,11};
            for(int level=1;level<=10;level++){
                var d=LevelCatalog.Instance.Get(level);var t=d.spawnTable;bool valid=t!=null&&t.roster.Count==expected[level-1];bool seed=true,limits=true,final=true;
                for(int wave=0;wave<d.waves.Count;wave++)for(int s=0;s<1;s++){
                    var a=t.Generate(d.waves[wave].TotalCount,wave,1200+s,wave==d.waves.Count-1);var b=t.Generate(d.waves[wave].TotalCount,wave,1200+s,wave==d.waves.Count-1);
                    seed&=string.Join(",",a.Select(x=>x.archetype.id+":"+x.count))==string.Join(",",b.Select(x=>x.archetype.id+":"+x.count));
                    limits&=a.Sum(x=>x.count)==d.waves[wave].TotalCount&&a.All(x=>t.roster.Any(r=>r.archetype==x.archetype));
                    limits&=a.Where(x=>x.archetype.id=="bao-thi").Sum(x=>x.count)<=Mathf.Max(1,Mathf.FloorToInt(d.waves[wave].TotalCount*t.maxBomberFraction));
                    if(wave==d.waves.Count-1)final&=a.Select(x=>x.archetype.id).Distinct().Count()==expected[level-1];
                }
                Check(valid&&seed&&limits&&final,"level "+level+" roster, seed, fixed final, bomber limits (one smoke seed/wave)");
                if(level>=8)Check(new[]{"tieu-yeu","doc-nhan","liem-hon","thiet-giap-nguu","bao-thi","quang-ma","hoa-trung"}.All(id=>t.roster.Any(r=>r.archetype.id==id)),"level "+level+" retains all seven user-provided types");
                int[] totals={6,10,14,18,23,26,30,32,36,45};
                Check(d.TotalMonsters+(level==3?1:0)+d.bosses.Count==totals[level-1],"level "+level+" exact planned total including elite / boss (before summons)");
                foreach(var r in t.roster){var e=Enemies.EnemyPool.Ensure().Spawn(r.archetype,world.origin+new Vector3(8,0,4),d.Scaling);Check(e!=null&&e.archetype.id==r.archetype.id,"level "+level+" actual pool spawn "+r.archetype.id);if(e!=null)Enemies.EnemyPool.Instance.Release(e);yield return null;}
                Measure("L"+level+" "+string.Join(", ",t.roster.Select(x=>x.archetype.id)));
            }
        }
    }
}
#endif
