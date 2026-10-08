var catalog=Resources.Load<CampusRift.Learning.LearningCatalog>("LearningCatalog");
var results=new System.Collections.Generic.List<string>();
CampusRift.BuildTools.ReleaseContentGate.Check(catalog);results.Add("valid catalog PASS");
var bad=UnityEngine.Object.Instantiate(catalog);bad.courses=new System.Collections.Generic.List<CampusRift.Learning.CourseData>(catalog.courses);
var chapter=UnityEngine.Object.Instantiate(catalog.courses[0]);bad.courses[0]=chapter;chapter.isPlaceholder=true;
try{CampusRift.BuildTools.ReleaseContentGate.Check(bad);results.Add("FAIL placeholder accepted");}catch(UnityEditor.Build.BuildFailedException e){results.Add("placeholder BLOCKED: "+e.Message);}
chapter.isPlaceholder=false;chapter.lessons=new System.Collections.Generic.List<CampusRift.Learning.LessonData>(catalog.courses[0].lessons);
var lesson=UnityEngine.Object.Instantiate(chapter.lessons[0]);chapter.lessons[0]=lesson;lesson.isPlaceholder=true;
try{CampusRift.BuildTools.ReleaseContentGate.Check(bad);results.Add("FAIL placeholder lesson accepted");}catch(UnityEditor.Build.BuildFailedException e){results.Add("lesson BLOCKED: "+e.Message);}
lesson.isPlaceholder=false;lesson.id="";
try{CampusRift.BuildTools.ReleaseContentGate.Check(bad);results.Add("FAIL invalid ID accepted");}catch(UnityEditor.Build.BuildFailedException e){results.Add("invalid content BLOCKED: "+e.Message);}
UnityEngine.Object.DestroyImmediate(lesson);UnityEngine.Object.DestroyImmediate(chapter);UnityEngine.Object.DestroyImmediate(bad);
System.IO.File.WriteAllText("task/p17/release-gate.txt",string.Join("\n\n",results));return results;
