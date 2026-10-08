#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.Skills;
namespace CampusRift.UI
{
    // Each original P18 base smoke once, with its own world/cleanup; FPS and photos explicitly skipped.
    public sealed class P23BaseSkillRegression:MonoBehaviour
    {
        [Serializable]sealed class Report{public List<string> passed=new List<string>(),failed=new List<string>();public List<P18SkillSmoke.Result> results=new List<P18SkillSmoke.Result>();}
        IEnumerator Start()
        {
            var report=new Report();var folder="Artifacts/V2/P23-base-skills/";Directory.CreateDirectory(folder);
            var ids=FindAnyObjectByType<CampusExplorer>().GetComponents<Set2SkillRuntime>().Select(s=>s.Id).ToArray();
            foreach(var id in ids)
            {
                var stamp=DateTime.UtcNow;var h=new GameObject("P23 original base smoke "+id).AddComponent<P18SkillSmoke>();h.skillId=id;h.testRank=1;h.SkipPerformance=true;h.SkipPhoto=true;
                float deadline=Time.realtimeSinceStartup+40;while(h!=null&&Time.realtimeSinceStartup<deadline)yield return null;
                var path="Artifacts/Skills/Set2/"+id+"-smoke.json";
                if(h!=null||!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){report.failed.Add(id+" fresh completion timeout");if(h!=null)Destroy(h.gameObject);break;}
                var r=JsonUtility.FromJson<P18SkillSmoke.Result>(File.ReadAllText(path));report.results.Add(r);File.Copy(path,folder+id+".json",true);
                (r.rank==1&&r.cast&&r.finished&&r.finite&&r.poolExhaustion==0?report.passed:report.failed).Add(id+" rank1 cast / finished / finite / no exhaustion");
            }
            if(ids.Length!=11)report.failed.Add("Expected eleven base runtimes");
            File.WriteAllText(folder+"result.json",JsonUtility.ToJson(report,true));File.WriteAllText(folder+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
    }
}
#endif
