from pathlib import Path
def edit(p,a,b):
 p=Path(p);s=p.read_text(encoding='utf-8');assert a in s,(p,a);p.write_text(s.replace(a,b),encoding='utf-8')
edit('Assets/Localization/Runtime/LocalizationPlayTest.cs',
 'State.OpenCourse();View.ShowCourses();yield return Frames();Check(View.heading.text==Loc.Translate("COURSES"),"Library uses Vietnamese heading");\n            var course=engine.Catalog.courses[0];var lesson=course.lessons[0];View.ShowLessons(course);View.OpenLesson(lesson);yield return Frames();',
 'LearningUI.PendingScreen="Lessons:0";State.OpenCourse();yield return Frames(); // P09 Hub Library opens a selected chapter directly.\n            var course=engine.Catalog.courses[0];var lesson=course.lessons[0];Check(View!=null&&View.heading.text==course.titleVN,"Hub Library opens the selected chapter in Vietnamese");View.OpenLesson(lesson);yield return Frames();')
edit('Assets/Localization/Runtime/LocalizationPlayTest.cs','bool content=true,options=true;int lessons=0,questions=0;',
 'bool content=true,options=true;int lessons=0,questions=0;var contentDetails=new List<string>();Func<string,string,string,bool> map=(en,vn,label)=>{bool ok=Loc.Translate(en??"")== (vn??"");if(!ok)contentDetails.Add(label+": EN="+en+"; VN="+vn+"; ACTUAL="+Loc.Translate(en??""));return ok;};')
edit('Assets/Localization/Runtime/LocalizationPlayTest.cs','Loc.Translate(l.title)==l.titleVN','map(l.title,l.titleVN,l.id+" title")')
for field in ['content','example','takeaway']:
 edit('Assets/Localization/Runtime/LocalizationPlayTest.cs',f'Loc.Translate(page.{field})==page.{field}VN',f'map(page.{field},page.{field}VN,l.id+" {field}")')
for field in ['prompt','explanation']:
 edit('Assets/Localization/Runtime/LocalizationPlayTest.cs',f'Loc.Translate(q.{field})==q.{field}VN',f'map(q.{field},q.{field}VN,q.id+" {field}")')
edit('Assets/Localization/Runtime/LocalizationPlayTest.cs','Check(content && lessons==21 && questions==440,','File.WriteAllText(Output+"content-details.txt",$"lessons={lessons}; questions={questions}\\n"+string.Join("\\n",contentDetails));\n            Check(content && lessons==21 && questions==440,')
edit('Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs','var rigs=new SkinnedMeshRenderer[7][];',
 'world.player.cameraHeight=world.victims[0].GetComponentInChildren<SkinnedMeshRenderer>().bounds.center.y-world.player.transform.position.y;world.Look(90,0);yield return new WaitForEndOfFrame();\n            var cameraDetails=new System.Text.StringBuilder();\n            var rigs=new SkinnedMeshRenderer[7][];')
edit('Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs','Vector3 cameraPos=world.player.followCamera.transform.position;for(int i=0;',
 'Vector3 cameraPos=world.player.followCamera.transform.position;cameraDetails.AppendLine("camera="+cameraPos+"; player="+world.player.transform.position+"; victim="+rigs[0][0].bounds);for(int i=0;')
edit('Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs','Check(clipSamples>0,','File.WriteAllText("Artifacts/Skills/camera-clipping-details.txt",cameraDetails.ToString());\n            Check(clipSamples>0,')
edit('Assets/Levels/Validation/StabilizePolishCapture.cs','panel.Close();yield return null;yield return Shot("long-skill-name");director.End();Clean();',
 'var listForName=panel.GetComponentsInChildren<ScrollRect>().Single();listForName.verticalNormalizedPosition=1;yield return null;yield return Shot("long-skill-name");panel.Close();yield return null;director.End();Clean();')
edit('Assets/Levels/Validation/StabilizePolishCapture.cs','new Vector3(6,2.6f,-8),new Vector3(6,1,0)','new Vector3(6,3.2f,-10),new Vector3(6,2.6f,0)')
