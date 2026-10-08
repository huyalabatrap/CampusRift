using System.Collections.Generic;
using System.Linq;

namespace CampusRift.Learning
{
    public sealed class TrueFalseGrader : IQuestionGrader
    {
        public bool Supports(QuestionData q) => q.type == "true-false" && q.options != null && q.options.Count == 2 &&
            q.options.Any(o => o != null && o.id == "true") && q.options.Any(o => o != null && o.id == "false") &&
            q.correctOptionIds != null && q.correctOptionIds.Count == 1 && (q.correctOptionIds[0] == "true" || q.correctOptionIds[0] == "false");
        public bool Grade(QuestionData q, IReadOnlyList<string> selected) => selected.Count == 1 && selected[0] == q.correctOptionIds[0];
    }

    // The player arranges every option; correctOptionIds is the right order, so one wrong position makes it wrong.
    public sealed class OrderingGrader : IQuestionGrader
    {
        public bool Supports(QuestionData q) => q.type == "ordering" && q.options != null && q.options.Count >= 2 &&
            q.options.All(o => o != null && !string.IsNullOrWhiteSpace(o.id)) && q.options.Select(o => o.id).Distinct().Count() == q.options.Count &&
            q.correctOptionIds != null && q.correctOptionIds.Count == q.options.Count && q.correctOptionIds.Distinct().Count() == q.options.Count &&
            q.correctOptionIds.All(id => q.options.Any(o => o.id == id));
        public bool Grade(QuestionData q, IReadOnlyList<string> selected) =>
            selected.Count == q.correctOptionIds.Count && selected.Select((id, i) => id == q.correctOptionIds[i]).All(x => x);
    }

    // Chooses the grader by the question's type key; this is the one place that knows which types exist.
    public sealed class GraderRegistry : IQuestionGrader
    {
        public static readonly GraderRegistry Default = new GraderRegistry();
        readonly IQuestionGrader[] graders = { new SingleChoiceGrader(), new TrueFalseGrader(), new OrderingGrader(), new MultiChoiceGrader(), new MatchingGrader(), new FillBlankGrader() };
        public IQuestionGrader For(QuestionData q) { foreach (var g in graders) if (g.Supports(q)) return g; return null; }
        public bool Supports(QuestionData q) => For(q) != null;
        public bool Grade(QuestionData q, IReadOnlyList<string> selected) { var g = For(q); return g != null && g.Grade(q, selected); }
    }
}
