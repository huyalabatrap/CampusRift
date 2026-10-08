#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using CampusRift.Levels;
using CampusRift.Skills;
namespace CampusRift.SkyBeast
{
    public sealed class P14DevSmoke:MonoBehaviour
    {
        [Serializable]sealed class Entry{public int level,beasts,spawned,breathSources;public bool menu,attack;}
        [Serializable]sealed class Report{public List<Entry> entries=new List<Entry>();public bool pass;public string method="One short DEV menu run per level8/9/10, actual LevelDirector spawn; no balance trial.";}
        IEnumerator Start()
        {
            var world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();var report=new Report{pass=true};yield return null;
            foreach(int level in new[]{8,9,10})
            {
                var entry=new Entry{level=level,menu=EditorApplication.ExecuteMenuItem("Campus Rift/DEV/P14/Level "+level)};
                var director=LevelDirector.Instance;director.enabled=true;world.player.GetComponent<Monsters.PlayerMonsterHealth>().SetProgressionMaxHealth(10000);
                yield return new WaitForSecondsRealtime(3.3f);entry.spawned=director.SpawnedTotal;
                var cycle=FireBreathCycle.Instance;cycle.AutoAdvance=false;cycle.Advance(cycle.Remaining);foreach(var b in SkyBeastScheduler.Instance.Beasts)if(b.PoseActive)entry.breathSources++;
                entry.beasts=SkyBeastScheduler.Instance.Beasts.Count;cycle.Advance(4);cycle.Ground.Clear();
                if(level==9)entry.attack=FindAnyObjectByType<FeatherBarrage>().TryDrop();
                else if(level==10){var scheduler=SkyBeastScheduler.Instance;scheduler.ApplySkySwordHit();scheduler.ApplySkySwordHit();entry.attack=scheduler.Phase==3&&FindAnyObjectByType<MeteorShower>().TryDrop();}
                else entry.attack=SkyBeastScheduler.Instance.ApplySkySwordHit();
                report.pass&=entry.menu&&entry.spawned>0&&entry.beasts==(level==10?2:1)&&entry.breathSources==1&&entry.attack;report.entries.Add(entry);director.End();yield return null;
            }
            world.End();File.WriteAllText("task/p14/dev-smoke.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/p14/dev-DONE.txt",report.pass?"DEV8/9/10 PASS":"DEV FAIL");Destroy(gameObject);
        }
    }
}
#endif
