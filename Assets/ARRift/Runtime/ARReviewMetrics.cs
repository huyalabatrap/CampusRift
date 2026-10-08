using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace CampusRift.AR
{
    // Scalars only. Bounded histograms retain no image, landmark or room coordinates.
    public sealed class ARMetricHistogram
    {
        readonly int[] bins=new int[2001];public int Count {get;private set;}
        public void Add(double ms){if(!double.IsFinite(ms)||ms<0)return;bins[Math.Min(2000,(int)Math.Ceiling(ms))]++;Count++;}
        public void Remove(double ms){int i=Math.Min(2000,(int)Math.Ceiling(ms));if(i>=0&&bins[i]>0){bins[i]--;Count--;}}
        public double? Quantile(double q){if(Count==0)return null;int rank=Math.Max(1,(int)Math.Ceiling(q*Count)),sum=0;for(int i=0;i<bins.Length;i++){sum+=bins[i];if(sum>=rank)return i;}return 2000;}
        public object Summary()=>new object[]{Count,Quantile(.5),Quantile(.95)};
    }
    public sealed class ARReviewMetrics
    {
        public sealed class Cell
        {
            public int g,d,o,n,skip,timeout,correctUnique,multi,late,framing;
            public readonly int[] first=new int[5],intents=new int[5];public readonly ARMetricHistogram latency=new ARMetricHistogram();
            public object Tuple(){var row=new List<object>{g,d,o,n,skip};foreach(int v in first)row.Add(v);row.Add(timeout);foreach(int v in intents)row.Add(v);row.Add(correctUnique);row.Add(multi);row.Add(late);row.Add(framing);row.Add(latency.Count);row.Add(latency.Quantile(.5));row.Add(latency.Quantile(.95));return row;}
        }
        public readonly Cell[] cells=new Cell[30];public readonly int[] correctIntent=new int[5],negativeIntents=new int[5],allIntents=new int[5];
        public readonly Dictionary<string,int> reasons=new Dictionary<string,int>(),drops=new Dictionary<string,int>();
        public readonly ARMetricHistogram firstValidCast=new ARMetricHistogram(),triggerVfx=new ARMetricHistogram(),frames=new ARMetricHistogram(),acquireConvert=new ARMetricHistogram(),inferResult=new ARMetricHistogram(),acquireConsume=new ARMetricHistogram(),submitInfer=new ARMetricHistogram(),resultConsume=new ARMetricHistogram();public double? clockError;public readonly ARMetricHistogram firstValidIntent=new ARMetricHistogram(),captureSpan=new ARMetricHistogram();
        public readonly ARMetricHistogram[] skillCast={new ARMetricHistogram(),new ARMetricHistogram(),new ARMetricHistogram(),new ARMetricHistogram(),new ARMetricHistogram()},skillVfx={new ARMetricHistogram(),new ARMetricHistogram(),new ARMetricHistogram(),new ARMetricHistogram(),new ARMetricHistogram()};
        public double active,noneExposure,poseExposure;public int unique,holdDuplicates,rejectRetries,tierChanges,scheduledVfx;public int? thermalMax;public double below27,slowestRun;
        public readonly ARMetricHistogram rollingFrames=new ARMetricHistogram();
        readonly Queue<(double at,double ms)> rolling=new Queue<(double,double)>();
        public void RenderFrame(double dt){frames.Add(dt*1000);rollingFrames.Add(dt*1000);rolling.Enqueue((active,dt*1000));while(rolling.Count>0&&(rolling.Peek().at<active-10||rolling.Count>12000))rollingFrames.Remove(rolling.Dequeue().ms);}
        public readonly List<object> placementTimes=new List<object>();public int placementTimeouts,expectedRejects,wrongConfirm,placementSkips;public double? cold;
        public ARReviewMetrics(){int index=0;foreach(int d in new[]{25,40,55})for(int o=0;o<2;o++)for(int g=0;g<5;g++)cells[index++]=new Cell{g=g,d=d,o=o};}
        public Cell Find(int g,int d,int o)=>Array.Find(cells,c=>c.g==g&&c.d==d&&c.o==o);
        public void Reason(string r){if(!reasons.ContainsKey(r))reasons[r]=0;reasons[r]++;}
        public static double? Precision(int correct,int total)=>total==0?(double?)null:(double)correct/total;
        public static string Json(object value)
        {
            if(value==null)return "null";if(value is string s){var b=new StringBuilder("\"");foreach(char c in s){if(c=='"'||c=='\\')b.Append('\\').Append(c);else if(c<32||c>126)b.Append("\\u").Append(((int)c).ToString("x4"));else b.Append(c);}return b.Append('"').ToString();}
            if(value is bool boolean)return boolean?"true":"false";
            if(value is IDictionary dict){var parts=new List<string>();foreach(DictionaryEntry e in dict)parts.Add(Json(e.Key.ToString())+":"+Json(e.Value));return "{"+string.Join(",",parts)+"}";}
            if(value is IEnumerable array){var parts=new List<string>();foreach(var v in array)parts.Add(Json(v));return "["+string.Join(",",parts)+"]";}
            if(value is double number&&!double.IsFinite(number)||value is float f&&!float.IsFinite(f))return "null";
            return Convert.ToString(value,CultureInfo.InvariantCulture);
        }
        public string Export(string build,string device,string delegateName,string scores,string hand,string light,bool complete,int[] input,string sid)
        {
            var tuples=new List<object>();foreach(var c in cells)tuples.Add(c.Tuple());
            var data=new Dictionary<string,object>{["v"]=2,["build"]=build,["model"]="97952348cf6a6a4915c2ea1496b4b37ebabc50cbbf80571435643c455f2b0482",["sid"]=sid,["device"]=device,["delegate"]=delegateName,["scores"]=scores,["input"]=input,["hand"]=hand,["light"]=light,["complete"]=complete,["mode"]="practice",["active_s"]=Math.Round(active,1),["cells"]=tuples,["correctIntent"]=correctIntent,["allIntent"]=allIntents,
                ["negative"]=new Dictionary<string,object>{["none_s"]=Math.Round(noneExposure,1),["poses_s"]=Math.Round(poseExposure,1),["intents"]=negativeIntents,["hold_duplicates"]=holdDuplicates,["reject_retries"]=rejectRetries},
                ["placement"]=new Dictionary<string,object>{["warm_ms"]=placementTimes,["timeouts"]=placementTimeouts,["expected_rejects"]=expectedRejects,["wrong_confirm"]=wrongConfirm,["cold_ms"]=cold,["skip"]=placementSkips},
                ["latency"]=new Dictionary<string,object>{["firstValidCast"]=firstValidCast.Summary(),["firstValidIntent"]=firstValidIntent.Summary(),["captureSpan"]=captureSpan.Summary(),["triggerResultVfx"]=triggerVfx.Summary(),["physical"]=null,["clockErrorMs"]=clockError,["acquireConvert"]=acquireConvert.Summary(),["inferResult"]=inferResult.Summary(),["acquireConsume"]=acquireConsume.Summary(),["submitInfer"]=submitInfer.Summary(),["resultConsume"]=resultConsume.Summary(),["skillCast"]=Array.ConvertAll(skillCast,h=>h.Summary()),["skillVfx"]=Array.ConvertAll(skillVfx,h=>h.Summary())},
                ["perf"]=new Dictionary<string,object>{["frameP50"]=frames.Quantile(.5),["frameP95"]=frames.Quantile(.95),["rolling10"]=rollingFrames.Summary(),["vfxScheduled"]=scheduledVfx,["uniqueHz"]=active>0?Math.Round(unique/active,1):(object)null,["thermalMax"]=thermalMax,["tierChanges"]=tierChanges,["below27_s"]=Math.Round(slowestRun,1)},["drops"]=drops,["reasons"]=reasons};
            string json=Json(data);if(Encoding.UTF8.GetByteCount("[ARCheck] "+json)>3500)throw new InvalidOperationException("ARCheck vượt 3500 byte; giữ đủ cells, không cắt kết quả.");return json;
        }
    }
}
