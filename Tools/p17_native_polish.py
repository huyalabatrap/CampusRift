"""Extend the new dev-only native smoke before creating native players."""
from pathlib import Path
p=Path('Assets/CampusRiftUI/Validation/P17PlayerSmoke.cs')
s=p.read_text(encoding='utf-8-sig').replace('using System.IO;','using System.IO;\nusing System.Linq;\nusing CampusRift.Learning;')
s=s.replace('if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p17-smoke")<0)return;',
 'if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p17-smoke")<0&&Array.IndexOf(Environment.GetCommandLineArgs(),"-p17-persistence")<0)return;')
old='            ProfileService.Instance.UseTransient(new ProfileData());UIStateManager.Instance.OpenHub();HubUI.Instance.Select(HubUI.Tab.Map);yield return new WaitForSecondsRealtime(2);'
new='''            var profile=ProfileService.Instance;string qaSave=Path.Combine(folder,"player-profile.json");profile.Initialize(new JsonProfileStore(qaSave),null);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p17-persistence")>=0)
            {
                bool retained=profile.Data.learning.lessons.Any(x=>x.rewarded)&&profile.Inventory.Count("hoi-khi-dan")==1;
                File.WriteAllText(Path.Combine(folder,"PERSISTENCE.txt"),(retained?"PASS":"FAIL")+" reopened player; mastered lesson="+profile.Data.learning.lessons.Any(x=>x.rewarded)+"; Recovery Pill="+profile.Inventory.Count("hoi-khi-dan"));Application.Quit();yield break;
            }
            // All automated answers are confined to this build-folder QA save, never the user's profile.
            var engine=new LearningEngine(Resources.Load<LearningCatalog>("LearningCatalog"),new ProfileLearningStore(profile),17,profile.Cultivation);engine.Economy=new StudyEconomy(profile.Wallet);
            typeof(LearningService).GetProperty("Engine").SetValue(LearningService.Instance,engine,null);
            UIStateManager.Instance.OpenHub();HubUI.Instance.Select(HubUI.Tab.Library);yield return new WaitForSecondsRealtime(.5f);
            HubUI.Instance.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Chapter 0").onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);
            var course=engine.Catalog.courses[0];var lesson=course.lessons[0];for(int i=0;i<lesson.pages.Count;i++)engine.ReadPage(course,lesson,i);
            var quiz=engine.StartQuiz(course,lesson);while(!quiz.Complete)quiz.Answer(quiz.Questions[quiz.Answered].Data.correctOptionIds);engine.Submit(quiz);
            UIStateManager.Instance.Back();HubUI.Instance.Select(HubUI.Tab.Shop);yield return new WaitForSecondsRealtime(.5f);
            HubUI.Instance.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Button TRAO ĐỔI").onClick.Invoke();profile.Flush();
            bool studyBuy=profile.Data.learning.Lesson(lesson.id).rewarded&&profile.Inventory.Count("hoi-khi-dan")==1;
            File.WriteAllText(Path.Combine(folder,"study-buy.txt"),(studyBuy?"PASS":"FAIL")+" automated study/quiz/buy through runtime rules; no human usability evidence; QA save="+qaSave+"; stones="+profile.Wallet.Balance);
            HubUI.Instance.Select(HubUI.Tab.Map);yield return new WaitForSecondsRealtime(2);'''
assert old in s;s=s.replace(old,new).replace('ProfileService.Instance.EndTransient();Application.Quit();','profile.Flush();Application.Quit();')
p.write_text(s,encoding='utf-8')
print(p)
