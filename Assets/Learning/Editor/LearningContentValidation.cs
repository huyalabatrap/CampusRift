using System;
using System.Collections.Generic;
using System.Linq;
using CampusRift.Learning;
using UnityEditor;
using UnityEngine;

// Content checks for the learning catalog. V2 rules (P07-T07): every lesson has at least 15 questions and every chapter at
// least 40 for its exam, every question has a source and Vietnamese text, and stand-in content is reported as a warning.
public static class LearningContentValidation
{
    public const int MinQuestionsPerLesson = 15, MinQuestionsPerChapter = 40;
    public sealed class Result { public readonly List<string> errors = new List<string>(), warnings = new List<string>(); public int courses, lessons, questions; }

    [MenuItem("Campus Rift/Learning/Validate Content")]
    public static void Validate()
    {
        var result = Run(Resources.Load<LearningCatalog>("LearningCatalog"));
        foreach (var w in result.warnings.Take(30)) Debug.LogWarning("Learning content: " + w);
        if (result.errors.Count > 0) throw new InvalidOperationException(string.Join("\n", result.errors));
        Debug.Log($"Learning content valid: {result.courses} chapters / {result.lessons} lessons / {result.questions} questions, {result.warnings.Count} warnings.");
    }

    public static Result Run(LearningCatalog catalog, bool v2 = true)
    {
        var r = new Result();
        if (catalog == null) { r.errors.Add("LearningCatalog is missing from Resources."); return r; }
        var courseIds = new HashSet<string>(); var lessonIds = new HashSet<string>(); var questionIds = new HashSet<string>();
        int chapterNumber = 0;
        foreach (var c in catalog.courses)
        {
            chapterNumber++;
            if (c == null) { r.errors.Add("Missing course reference"); continue; }
            r.courses++;
            if (string.IsNullOrWhiteSpace(c.id) || !courseIds.Add(c.id)) r.errors.Add("Duplicate/empty course ID: " + c.name);
            if (c.isPlaceholder) r.warnings.Add(c.name + ": chapter still uses placeholder content");
            if (v2 && c.chapterIndex != chapterNumber) r.errors.Add(c.name + ": chapterIndex " + c.chapterIndex + " does not match its position " + chapterNumber + " in the catalog");
            if (c.examSize < 1 || c.examPassPercent < 1 || c.examPassPercent > 100 || c.examMinutes < 1) r.errors.Add(c.name + ": invalid exam settings");
            var chapterPool = new HashSet<string>();
            if (c.examBank != null) foreach (var q in c.examBank.questions) if (q != null && !string.IsNullOrEmpty(q.id)) chapterPool.Add(q.id);
            if (c.examBank != null) CheckQuestions(r, c.name + " exam bank", c.examBank.questions, null, questionIds, v2);
            foreach (var l in c.lessons)
            {
                if (l == null) { r.errors.Add(c.name + ": missing lesson reference"); continue; }
                r.lessons++;
                if (string.IsNullOrWhiteSpace(l.id) || !lessonIds.Add(l.id)) r.errors.Add("Duplicate/empty lesson ID: " + l.name);
                if (l.isPlaceholder) r.warnings.Add(l.name + ": lesson still uses placeholder content");
                if (l.pages.Count == 0 || l.pages.Any(p => p == null || string.IsNullOrWhiteSpace(p.content))) r.errors.Add(l.name + ": empty reading pages");
                if(v2)foreach(var page in l.pages)if(page!=null&&!string.IsNullOrWhiteSpace(page.takeaway)&&!System.Text.RegularExpressions.Regex.IsMatch(page.content??"",@"\((?:Giáo trình|GT)\s*2021,\s*tr\.[^)]+\)"))r.errors.Add(l.name+": flashcard has no source page");
                if (l.passPercent < 1 || l.passPercent > 100) r.errors.Add(l.name + ": invalid pass percentage");
                if (l.questionBank == null) { r.errors.Add(l.name + ": missing bank"); continue; }
                var pool = l.questionBank.questions.Where(q => q != null && q.lessonId == l.id).ToList();
                if (pool.Count < l.quizSize || l.quizSize < 1) r.errors.Add(l.name + ": insufficient questions for the quiz size");
                if (v2 && pool.Count < MinQuestionsPerLesson) r.errors.Add(l.name + ": " + pool.Count + " questions, at least " + MinQuestionsPerLesson + " needed");
                CheckQuestions(r, l.name, pool, l, questionIds, v2);
                foreach (var q in pool) chapterPool.Add(q.id);
                if (l.prerequisiteLessonIds.Contains(l.id)) r.errors.Add(l.name + ": self prerequisite");
            }
            if (v2 && chapterPool.Count < MinQuestionsPerChapter) r.errors.Add(c.name + ": " + chapterPool.Count + " distinct exam questions, at least " + MinQuestionsPerChapter + " needed");
            if (chapterPool.Count < c.examSize) r.errors.Add(c.name + ": fewer questions than the exam size");
            if (c.skillTiers.Select(t => t.tier).Distinct().Count() != c.skillTiers.Count) r.errors.Add(c.name + ": duplicate reward tiers");
            if (c.skillTiers.Any(t => t.skill == null || string.IsNullOrWhiteSpace(t.skill.id))) r.errors.Add(c.name + ": missing skill reward/ID");
        }
        foreach (var c in catalog.courses.Where(c => c != null))
            foreach (var l in c.lessons.Where(l => l != null))
                if (l.prerequisiteLessonIds.Any(id => !lessonIds.Contains(id))) r.errors.Add(l.name + ": unknown prerequisite");
        return r;
    }

    static void CheckQuestions(Result r, string owner, IEnumerable<QuestionData> questions, LessonData lesson, HashSet<string> seen, bool v2)
    {
        var grader = GraderRegistry.Default;
        foreach (var q in questions)
        {
            if (q == null) { r.errors.Add(owner + ": null question"); continue; }
            r.questions++;
            if (string.IsNullOrWhiteSpace(q.id)) { r.errors.Add(owner + ": question without an ID"); continue; }
            if (!seen.Add(q.id)) r.errors.Add(owner + ": duplicate question ID " + q.id);
            if (!grader.Supports(q)) r.errors.Add(owner + ": question " + q.id + " has invalid type or options");
            if (string.IsNullOrWhiteSpace(q.explanation)) r.errors.Add(owner + ": question " + q.id + " has no explanation");
            if (string.IsNullOrWhiteSpace(q.prompt)) r.errors.Add(owner + ": question " + q.id + " has no text");
            if (v2)
            {
                if (string.IsNullOrWhiteSpace(q.source)) r.errors.Add(owner + ": question " + q.id + " has no source page");
                if (q.options.Any(o => o == null || string.IsNullOrWhiteSpace(o.text))) r.errors.Add(owner + ": question " + q.id + " has an empty option");
            }
        }
    }
}
