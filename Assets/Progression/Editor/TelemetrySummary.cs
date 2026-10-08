using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace CampusRift.Progression
{
    public static class TelemetrySummary
    {
        [MenuItem("Campus Rift/V2/Telemetry/Summarize local files")]
        public static void Summarize()
        {
            var folder=EditorUtility.OpenFolderPanel("Local telemetry folder",LocalTelemetry.Folder,"");
            if(string.IsNullOrEmpty(folder))return;
            string output=Path.Combine("Artifacts/V2","Telemetry-Summary.csv"); Directory.CreateDirectory("Artifacts/V2");
            File.WriteAllText(output,Build(folder),new UTF8Encoding(true)); Debug.Log("Local telemetry summary: "+Path.GetFullPath(output));
        }
        public static string Build(string folder)
        {
            var rows=new List<LocalTelemetry.Row>();int malformed=0;
            if(Directory.Exists(folder))foreach(var file in Directory.GetFiles(folder,"*.jsonl"))foreach(var line in File.ReadLines(file))
            {
                if(string.IsNullOrWhiteSpace(line))continue;
                try{var r=JsonUtility.FromJson<LocalTelemetry.Row>(line);if(r!=null&&r.schema==1)rows.Add(r);else malformed++;}catch(System.ArgumentException){malformed++;}
            }
            var b=new StringBuilder("kind,id,samples,successes,mean_seconds,deaths,fire_hits\n");
            foreach(var g in rows.Where(r=>r.kind=="run").GroupBy(r=>r.level).OrderBy(g=>g.Key))
                b.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"level,{0},{1},{2},{3:0.00},{4},{5}",g.Key,g.Count(),g.Count(r=>r.outcome=="won"),g.Average(r=>r.seconds),g.Sum(r=>r.deaths),g.Sum(r=>r.fireHits)));
            foreach(var g in rows.Where(r=>r.kind=="quiz").SelectMany(r=>r.answers).GroupBy(a=>a.id).OrderBy(g=>g.Key))
                b.AppendLine("question,"+g.Key+","+g.Count()+","+g.Count(a=>a.correct)+",,,");
            b.AppendLine("malformed,rows,"+malformed+",,,,");return b.ToString();
        }
    }
}
