using System.Collections.Generic;
using System.Linq;
namespace CampusRift.Learning
{
    public sealed class MultiChoiceGrader : IQuestionGrader
    {
        internal static bool OptionsValid(QuestionData q) => q != null && q.options != null && q.options.Count >= 2 &&
            q.options.All(o=>o!=null && !string.IsNullOrWhiteSpace(o.id)) && q.options.Select(o=>o.id).Distinct().Count()==q.options.Count;
        public bool Supports(QuestionData q) => q.type=="multi-choice" && OptionsValid(q) && q.correctOptionIds!=null &&
            q.correctOptionIds.Count>=2 && q.correctOptionIds.Distinct().Count()==q.correctOptionIds.Count && q.correctOptionIds.All(id=>q.options.Any(o=>o.id==id));
        public bool Grade(QuestionData q,IReadOnlyList<string> selected) => selected!=null && selected.Count==q.correctOptionIds.Count &&
            selected.Distinct().Count()==selected.Count && new HashSet<string>(selected).SetEquals(q.correctOptionIds);
    }
    public sealed class FillBlankGrader : IQuestionGrader
    {
        public bool Supports(QuestionData q) => q.type=="fill-blank" && MultiChoiceGrader.OptionsValid(q) && q.prompt!=null && q.prompt.Contains("___") &&
            q.correctOptionIds!=null && q.correctOptionIds.Count==1 && q.options.Any(o=>o.id==q.correctOptionIds[0]);
        public bool Grade(QuestionData q,IReadOnlyList<string> selected) => selected!=null && selected.Count==1 && selected[0]==q.correctOptionIds[0];
    }
    public sealed class MatchingGrader : IQuestionGrader
    {
        public static bool PairValid(QuestionData q,string value)
        {
            if(value==null)return false;var p=value.Split(':');return p.Length==2 && q.options.Any(o=>o.side=="left"&&o.id==p[0]) && q.options.Any(o=>o.side=="right"&&o.id==p[1]);
        }
        public bool Supports(QuestionData q)
        {
            if(q.type!="matching" || !MultiChoiceGrader.OptionsValid(q) || q.options.Any(o=>o.side!="left"&&o.side!="right") || q.correctOptionIds==null)return false;
            int n=q.options.Count(o=>o.side=="left");
            return n>=2 && q.options.Count(o=>o.side=="right")==n && q.correctOptionIds.Count==n && q.correctOptionIds.All(p=>PairValid(q,p)) &&
                q.correctOptionIds.Select(p=>p.Split(':')[0]).Distinct().Count()==n && q.correctOptionIds.Select(p=>p.Split(':')[1]).Distinct().Count()==n;
        }
        public bool Grade(QuestionData q,IReadOnlyList<string> selected) => selected!=null && selected.Count==q.correctOptionIds.Count &&
            selected.All(p=>PairValid(q,p)) && selected.Distinct().Count()==selected.Count && new HashSet<string>(selected).SetEquals(q.correctOptionIds);
    }
    public static class QuestionAnswerText
    {
        public static string Format(QuestionData q,IEnumerable<string> ids)
        {
            if(q.type=="matching")return string.Join("; ",ids.Select(p=>{var a=p.Split(':');return q.options.Find(o=>o.id==a[0]).text+" ↔ "+q.options.Find(o=>o.id==a[1]).text;}));
            return string.Join(q.type=="ordering"?" → ":", ",ids.Select(id=>q.options.Find(o=>o.id==id).text));
        }
    }
}
