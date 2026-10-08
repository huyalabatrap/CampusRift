using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CampusRift.Learning;
using UnityEditor;
using UnityEngine;

// P07-T07: turns the content CSV files into Course/Lesson/QuestionBank assets. Row numbers in messages are the line
// numbers of the CSV file (the header is line 1). Nothing is written when any error is found.
public static class CourseCsvImporter
{
    public const string DataFolder = "Assets/Learning/Data/TTHCM";
    public sealed class Report
    {
        public readonly List<string> errors = new List<string>(), warnings = new List<string>();
        public int chapters, lessons, pages, questions, examQuestions;
        public override string ToString() => $"chapters {chapters}, lessons {lessons}, pages {pages}, questions {questions} (+{examQuestions} exam-only); {errors.Count} errors, {warnings.Count} warnings";
    }

    // ---------------------------------------------------------------- CSV reading (RFC 4180: quotes, commas and newlines inside quotes)
    public static List<string[]> ReadCsv(string text, out List<int> lineNumbers)
    {
        var rows = new List<string[]>(); lineNumbers = new List<int>();
        if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);
        var field = new StringBuilder(); var row = new List<string>(); bool quoted = false; int line = 1, rowStart = 1;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (quoted)
            {
                if (c == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; } else quoted = false; }
                else { if (c == '\n') line++; field.Append(c); }
            }
            else if (c == '"' && field.Length == 0) quoted = true;
            else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (c == '\r') { }
            else if (c == '\n')
            {
                row.Add(field.ToString()); field.Clear();
                if (row.Count > 1 || row[0].Trim().Length > 0) { rows.Add(row.ToArray()); lineNumbers.Add(rowStart); }
                row = new List<string>(); line++; rowStart = line;
            }
            else field.Append(c);
        }
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); if (row.Count > 1 || row[0].Trim().Length > 0) { rows.Add(row.ToArray()); lineNumbers.Add(rowStart); } }
        return rows;
    }

    sealed class Table
    {
        public readonly List<string[]> rows; public readonly List<int> lines; readonly Dictionary<string, int> columns = new Dictionary<string, int>();
        public Table(string path, Report report, params string[] required)
        {
            var all = ReadCsv(File.ReadAllText(path, Encoding.UTF8), out var numbers);
            if (all.Count == 0) { report.errors.Add(Path.GetFileName(path) + ": file is empty"); rows = new List<string[]>(); lines = new List<int>(); return; }
            for (int i = 0; i < all[0].Length; i++) columns[all[0][i].Trim().ToLowerInvariant()] = i;
            foreach (var name in required) if (!columns.ContainsKey(name)) report.errors.Add(Path.GetFileName(path) + ": missing column \"" + name + "\"");
            rows = all.Skip(1).ToList(); lines = numbers.Skip(1).ToList();
        }
        public bool Has(string column) => columns.ContainsKey(column.ToLowerInvariant());
        public string Get(string[] row, string column) => columns.TryGetValue(column.ToLowerInvariant(), out var i) && i < row.Length ? row[i].Trim() : "";
        // Text fields keep inner line breaks but are trimmed at both ends.
    }

    sealed class PageRow { public int page; public string title, text, takeaway; public int line; }
    sealed class LessonRow { public string id, title; public int chapter; public List<PageRow> pages = new List<PageRow>(); }
    sealed class ChapterRow { public int number; public string title; }

    static readonly Dictionary<string, string> Types = new Dictionary<string, string>
    { { "mot-dap-an", "single-choice" }, { "dung-sai", "true-false" }, { "sap-xep", "ordering" }, { "nhieu-dap-an", "multi-choice" }, { "noi-cap", "matching" }, { "dien-khuyet", "fill-blank" } };
    static readonly string[] Letters = { "A", "B", "C", "D", "E", "F" };

    // ---------------------------------------------------------------- import
    [MenuItem("Campus Rift/V2/Import Course CSV")]
    public static void ImportMenu()
    {
        var report = Import("Content", true);
        Debug.Log("Course CSV import: " + report);
        foreach (var w in report.warnings.Take(20)) Debug.LogWarning(w);
        if (report.errors.Count > 0) Debug.LogError("Course CSV import failed:\n" + string.Join("\n", report.errors.Take(60)));
    }

    public static Report Import(string folder, bool write, string outputFolder = DataFolder, bool updateCatalog = true)
    {
        var report = new Report();
        string lessonsPath = Path.Combine(folder, "lessons.csv"), questionsPath = Path.Combine(folder, "questions.csv"), chaptersPath = Path.Combine(folder, "chapters.csv");
        foreach (var p in new[] { lessonsPath, questionsPath }) if (!File.Exists(p)) report.errors.Add("Missing file: " + p);
        if (report.errors.Count > 0) return report;

        // chapters (optional file)
        var chapters = new Dictionary<int, ChapterRow>();
        if (File.Exists(chaptersPath))
        {
            var t = new Table(chaptersPath, report, "chuong", "ten_chuong");
            for (int i = 0; i < t.rows.Count; i++)
            {
                var r = t.rows[i]; int line = t.lines[i];
                if (!int.TryParse(t.Get(r, "chuong"), out int n) || n < 1) { report.errors.Add($"chapters.csv line {line}: chuong must be a number from 1"); continue; }
                if (chapters.ContainsKey(n)) { report.errors.Add($"chapters.csv line {line}: duplicate chuong {n}"); continue; }
                string name = t.Get(r, "ten_chuong"); if (name.Length == 0) report.errors.Add($"chapters.csv line {line}: missing ten_chuong");
                chapters[n] = new ChapterRow { number = n, title = name };
            }
        }

        // lessons and pages
        var lessons = new Dictionary<string, LessonRow>(); var lessonOrder = new List<LessonRow>();
        var lt = new Table(lessonsPath, report, "chuong", "bai_id", "bai_ten", "trang_so", "noi_dung_trang");
        for (int i = 0; i < lt.rows.Count; i++)
        {
            var r = lt.rows[i]; int line = lt.lines[i];
            string id = lt.Get(r, "bai_id"), title = lt.Get(r, "bai_ten"), content = lt.Get(r, "noi_dung_trang");
            if (!int.TryParse(lt.Get(r, "chuong"), out int chapter) || chapter < 1) { report.errors.Add($"lessons.csv line {line}: chuong must be a number from 1"); continue; }
            if (id.Length == 0) { report.errors.Add($"lessons.csv line {line}: missing bai_id"); continue; }
            if (System.Text.RegularExpressions.Regex.IsMatch(id, "^ch[0-9]+$")) { report.errors.Add($"lessons.csv line {line}: bai_id \"{id}\" is reserved for chapter exam questions"); continue; }
            if (title.Length == 0) report.errors.Add($"lessons.csv line {line}: missing bai_ten");
            if (content.Length == 0) report.errors.Add($"lessons.csv line {line}: missing noi_dung_trang");
            if (!int.TryParse(lt.Get(r, "trang_so"), out int pageNumber) || pageNumber < 1) { report.errors.Add($"lessons.csv line {line}: trang_so must be a number from 1"); continue; }
            if (!lessons.TryGetValue(id, out var lesson))
            {
                lesson = new LessonRow { id = id, title = title, chapter = chapter }; lessons[id] = lesson; lessonOrder.Add(lesson);
                if (!chapters.ContainsKey(chapter)) chapters[chapter] = new ChapterRow { number = chapter, title = "Chương " + chapter };
            }
            else if (lesson.chapter != chapter) report.errors.Add($"lessons.csv line {line}: lesson \"{id}\" appears under two chapters");
            if (lesson.pages.Any(p => p.page == pageNumber)) report.errors.Add($"lessons.csv line {line}: duplicate page {pageNumber} in lesson \"{id}\"");
            string pageTitle = lt.Has("tieu_de_trang") ? lt.Get(r, "tieu_de_trang") : "";
            lesson.pages.Add(new PageRow { page = pageNumber, title = pageTitle.Length > 0 ? pageTitle : title, text = content, takeaway = lt.Get(r, "ghi_nho"), line = line });
        }
        foreach (var l in lessonOrder) l.pages.Sort((a, b) => a.page.CompareTo(b.page));

        // questions
        var questions = new List<QuestionData>(); var ids = new HashSet<string>();
        var qt = new Table(questionsPath, report, "cau_id", "bai_id", "loai", "do_kho", "cau_hoi", "dap_an", "giai_thich", "nguon");
        var chapterExam = new Dictionary<int, List<QuestionData>>();
        for (int i = 0; i < qt.rows.Count; i++)
        {
            var r = qt.rows[i]; int line = qt.lines[i];
            string id = qt.Get(r, "cau_id"), owner = qt.Get(r, "bai_id"), kind = qt.Get(r, "loai").ToLowerInvariant(), prompt = qt.Get(r, "cau_hoi"),
                answer = qt.Get(r, "dap_an"), explain = qt.Get(r, "giai_thich"), source = qt.Get(r, "nguon");
            if (id.Length == 0) { report.errors.Add($"questions.csv line {line}: missing cau_id"); continue; }
            if (!ids.Add(id)) { report.errors.Add($"questions.csv line {line}: duplicate cau_id \"{id}\""); continue; }
            var q = new QuestionData { id = id, prompt = prompt, promptVN = prompt, explanation = explain, explanationVN = explain, source = source };
            int chapterOfExam = 0; bool examOnly = false;
            var examMatch = System.Text.RegularExpressions.Regex.Match(owner, "^ch([0-9]+)$");
            if (examMatch.Success) { examOnly = true; chapterOfExam = int.Parse(examMatch.Groups[1].Value); }
            LessonRow lesson = null;
            if (owner.Length == 0) report.errors.Add($"questions.csv line {line}: missing bai_id");
            else if (examOnly) { if (!chapters.ContainsKey(chapterOfExam)) report.errors.Add($"questions.csv line {line}: exam questions for chapter {chapterOfExam}, which has no lessons"); q.chapterId = "tthcm-ch" + chapterOfExam; }
            else if (!lessons.TryGetValue(owner, out lesson)) report.errors.Add($"questions.csv line {line}: unknown bai_id \"{owner}\"");
            else { q.lessonId = owner; q.topic = lesson.title; q.chapterId = "tthcm-ch" + lesson.chapter; }
            if (prompt.Length == 0) report.errors.Add($"questions.csv line {line}: missing cau_hoi");
            if (explain.Length == 0) report.errors.Add($"questions.csv line {line}: missing giai_thich");
            if (source.Length == 0) report.errors.Add($"questions.csv line {line}: missing nguon");
            if (!int.TryParse(qt.Get(r, "do_kho"), out int difficulty) || difficulty < 1 || difficulty > 3) report.errors.Add($"questions.csv line {line}: do_kho must be 1, 2 or 3");
            else q.difficulty = difficulty;
            if (!Types.TryGetValue(kind, out string type)) { report.errors.Add($"questions.csv line {line}: unknown loai \"{kind}\" (mot-dap-an, dung-sai, sap-xep)"); continue; }
            q.type = type;
            var letterOptions = new List<AnswerOption>();
            foreach (var letter in Letters) { string text = qt.Get(r, letter); if (text.Length > 0) letterOptions.Add(new AnswerOption { id = letter, text = text, textVN = text }); }
            if (type == "true-false")
            {
                q.options.Add(new AnswerOption { id = "true", text = "Đúng", textVN = "Đúng" }); q.options.Add(new AnswerOption { id = "false", text = "Sai", textVN = "Sai" });
                string a = answer.ToLowerInvariant();
                if (a == "dung" || a == "đúng" || a == "true") q.correctOptionIds.Add("true");
                else if (a == "sai" || a == "false") q.correctOptionIds.Add("false");
                else report.errors.Add($"questions.csv line {line}: dap_an of a dung-sai question must be dung or sai");
            }
            else if(type=="matching")
            {
                // Two pairs in the compact authoring format: A/B = left, C/D = right; answers a:1,b:2.
                string[] keys={"a","b","1","2"};
                for(int k=0;k<4;k++)q.options.Add(new AnswerOption{id=keys[k],side=k<2?"left":"right",text=qt.Get(r,Letters[k]),textVN=qt.Get(r,Letters[k])});
                q.correctOptionIds=answer.Split(new[]{',',';',' '},StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                q.options = letterOptions;
                if (letterOptions.Count < 2) report.errors.Add($"questions.csv line {line}: needs at least two options");
                var picked = answer.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpperInvariant()).ToList();
                foreach (var letter in picked) if (!letterOptions.Any(o => o.id == letter)) report.errors.Add($"questions.csv line {line}: dap_an \"{letter}\" is not one of the filled options");
                if (type == "single-choice" || type == "fill-blank")
                {
                    if (picked.Count != 1) report.errors.Add($"questions.csv line {line}: a mot-dap-an question needs exactly one dap_an");
                    else q.correctOptionIds.Add(picked[0]);
                }
                else if(type=="multi-choice")q.correctOptionIds.AddRange(picked);
                else
                {
                    if (picked.Count != letterOptions.Count || picked.Distinct().Count() != picked.Count) report.errors.Add($"questions.csv line {line}: dap_an of a sap-xep question must list every option once, in the right order");
                    else q.correctOptionIds.AddRange(picked);
                }
            }
            if(!GraderRegistry.Default.Supports(q) || q.options.Any(o=>string.IsNullOrWhiteSpace(o.text)))report.errors.Add($"questions.csv line {line}: malformed {type} question (options/answer/blank/pairs)");
            if (examOnly) { if (!chapterExam.TryGetValue(chapterOfExam, out var list)) chapterExam[chapterOfExam] = list = new List<QuestionData>(); list.Add(q); report.examQuestions++; }
            else { questions.Add(q); report.questions++; }
        }

        // per-lesson checks (counts are warnings here; the validator makes them errors for a release)
        foreach (var lesson in lessonOrder)
        {
            report.lessons++; report.pages += lesson.pages.Count;
            int n = questions.Count(q => q.lessonId == lesson.id);
            if (n < 15) report.warnings.Add($"lesson \"{lesson.id}\" has {n} questions (15 wanted)");
            for (int p = 0; p < lesson.pages.Count; p++) if (lesson.pages[p].page != p + 1) { report.errors.Add($"lessons.csv line {lesson.pages[p].line}: pages of \"{lesson.id}\" must be numbered 1, 2, 3 without gaps"); break; }
        }
        report.chapters = chapters.Count;
        foreach (var c in chapters.Values)
        {
            int total = questions.Count(q => lessons.TryGetValue(q.lessonId ?? "", out var l) && l.chapter == c.number) + (chapterExam.TryGetValue(c.number, out var e) ? e.Count : 0);
            if (total < 40) report.warnings.Add($"chapter {c.number} has {total} questions for the exam (40 wanted)");
        }
        if (report.errors.Count > 0 || !write) return report;

        // ---------------------------------------------------------------- write assets
        Directory.CreateDirectory(outputFolder);
        var courses = new List<CourseData>();
        foreach (var chapter in chapters.Values.OrderBy(c => c.number))
        {
            var course = Asset<CourseData>(outputFolder + "/ch" + chapter.number + ".asset");
            course.id = "tthcm-ch" + chapter.number; course.title = chapter.title; course.titleVN = chapter.title; course.subject = "Tư tưởng Hồ Chí Minh";
            course.chapterIndex = chapter.number; course.isPlaceholder = false; course.lessons = new List<LessonData>();
            var exam = Asset<QuestionBankData>(outputFolder + "/ch" + chapter.number + "-exam-bank.asset");
            exam.questions = chapterExam.TryGetValue(chapter.number, out var extra) ? extra : new List<QuestionData>(); EditorUtility.SetDirty(exam);
            course.examBank = exam;
            LessonData previous = null;
            foreach (var row in lessonOrder.Where(l => l.chapter == chapter.number))
            {
                var bank = Asset<QuestionBankData>(outputFolder + "/" + row.id + "-bank.asset");
                bank.questions = questions.Where(q => q.lessonId == row.id).ToList(); EditorUtility.SetDirty(bank);
                var lesson = Asset<LessonData>(outputFolder + "/" + row.id + ".asset");
                lesson.id = row.id; lesson.title = row.title; lesson.titleVN = row.title; lesson.topic = row.title; lesson.isPlaceholder = false;
                lesson.pages = row.pages.Select(p => new LessonPage { title = p.title, titleVN = p.title, content = p.text, contentVN = p.text, takeaway = p.takeaway, takeawayVN = p.takeaway }).ToList();
                lesson.questionBank = bank; lesson.quizSize = 10; lesson.passPercent = 80; lesson.requiredSurvivalSeconds = 0;
                lesson.prerequisiteLessonIds = previous != null ? new List<string> { previous.id } : new List<string>();
                EditorUtility.SetDirty(lesson); course.lessons.Add(lesson); previous = lesson;
            }
            EditorUtility.SetDirty(course); courses.Add(course);
        }
        if (updateCatalog)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LearningCatalog>("Assets/Learning/Resources/LearningCatalog.asset");
            catalog.courses = courses; EditorUtility.SetDirty(catalog);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        return report;
    }

    static T Asset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
    }
}
