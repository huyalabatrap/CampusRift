var e=CampusRift.Learning.LearningService.Instance.Engine;var c=e.Catalog.courses[0];
e.DebugReset();for(int i=0;i<4;i++)e.DebugPassNext(c);
CampusRift.UI.UIStateManager.Instance.OpenCourse();
var v=UnityEngine.Object.FindAnyObjectByType<CampusRift.Learning.LearningUI>();
v.ShowLessons(c);v.OpenLesson(c.lessons[4]);e.ReadPage(c,c.lessons[4],0);v.StartQuiz();
for(int i=0;i<5;i++){var q=(CampusRift.Learning.QuizSession)typeof(CampusRift.Learning.LearningUI).GetField("quiz",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(v);v.Answer(q.Questions[q.Answered].Data.correctOptionIds[0]);v.NextQuestion();}
v.ShowBreakthrough();return "Major breakthrough open for visual inspection";
