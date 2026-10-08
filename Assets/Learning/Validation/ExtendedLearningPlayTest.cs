#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CampusRift.UI;
using CampusRift.Progression;
using CampusRift.Localization;
using CampusRift.Controls;
using CampusRift.Levels;
using CampusRift.SkyBeast;
using CampusRift.Combat;
using CampusRift.Enemies;
namespace CampusRift.Learning
{
    public sealed class ExtendedLearningPlayTest:MonoBehaviour
    {
        [Serializable] public sealed class Report{public List<string> passed=new List<string>(),failed=new List<string>();}
        readonly Report report=new Report();ProfileService profile;LearningEngine engine,originalEngine;GameSettings settings;FakeLearningClock clock;
        LearningUI view;CampusExplorer player;Vector3 playerPosition;Quaternion cameraRotation;Vector3 cameraPosition;Camera camera;
        const string Output="task/p20/";
        void Check(bool ok,string label){(ok?report.passed:report.failed).Add(label);File.WriteAllText(Output+"smoke.json",JsonUtility.ToJson(report,true));Debug.Log("P20 "+(ok?"PASS ":"FAIL ")+label);}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Output+"screens");profile=ProfileService.Instance;originalEngine=LearningService.Instance.Engine;settings=SettingsManager.Instance.Current.Copy();profile.UseTransient(new ProfileData{tutorial=new TutorialProgress{skipHub=true,skipCombat=true,skipFire=true}});
            clock=new FakeLearningClock(new DateTime(2026,10,5,8,0,0,DateTimeKind.Utc));engine=new LearningEngine(Resources.Load<LearningCatalog>("LearningCatalog"),new ProfileLearningStore(profile),20,profile.Cultivation,clock){Economy=new StudyEconomy(profile.Wallet)};LearningService.Instance.EditorUseEngine(engine);
            player=FindAnyObjectByType<CampusExplorer>();playerPosition=player.transform.position;camera=player.followCamera;cameraPosition=camera.transform.position;cameraRotation=camera.transform.rotation;
            var run=Run();while(true){bool more=false;object current=null;try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception e){Check(false,e.ToString());break;}if(!more)break;yield return current;}
            UIStateManager.Instance.EnterScene(true);player.enabled=true;camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=playerPosition;cc.enabled=true;
            profile.EndTransient();LearningService.Instance.EditorUseEngine(originalEngine);SettingsManager.Instance.Apply(settings,false);File.WriteAllText(Output+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
        void Configure(bool mobile,bool vi)
        {var s=settings.Copy();s.ControlMode=mobile?ControlMode.Mobile:ControlMode.PC;s.Language=vi?GameLanguage.Vietnamese:GameLanguage.English;SettingsManager.Instance.Apply(s,false);UIValidation.SetResolution(1920,1080);}
        void Open(string screen)
        {LearningUI.PendingScreen=screen;UIStateManager.Instance.EnterScene(true);UIStateManager.Instance.EditorForceCourse();view=FindAnyObjectByType<LearningUI>();}
        IEnumerator Audit(string name,bool photo)
        {
            yield return new WaitForSecondsRealtime(.12f);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            var a=ComicTextAudit.Scan(name);ComicTextAudit.Save(a,Output+"screens/"+name+"-audit.json");Check(a.issues.Count==0,"ComicTextAudit "+name+"0issues");
            if(photo){var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"screens/"+name+".png",frame.EncodeToPNG());Destroy(frame);}
        }
        void Click(string starts){var b=view.content.GetComponentsInChildren<RiftButton>().First(x=>x.isActiveAndEnabled&&x.Label.text.StartsWith(starts));b.onClick.Invoke();}
        void Approach(LearningShrine shrine)
        {
            for(int i=0;i<16;i++)
            {
                var from=shrine.transform.position+Quaternion.Euler(0,i*22.5f,0)*Vector3.back*1.8f;
                if(!UnityEngine.AI.NavMesh.SamplePosition(from,out var hit,.5f,UnityEngine.AI.NavMesh.AllAreas)||Mathf.Abs(hit.position.y-from.y)>1||!CombatLine.Clear(hit.position+Vector3.up,shrine.transform.position+Vector3.up,player.transform))continue;
                var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=hit.position;cc.enabled=true;Physics.SyncTransforms();return;
            }
            throw new Exception("No clear approach to "+shrine.name);
        }
        static string[] Wrong(QuestionData q)
        {if(q.type=="matching")return q.correctOptionIds.Select(p=>p.Split(':')[0]+":"+(p.EndsWith(":1")?"2":"1")).ToArray();if(q.type=="ordering")return q.correctOptionIds.Reverse<string>().ToArray();if(q.type=="multi-choice")return q.correctOptionIds.Take(1).ToArray();return new[]{q.options.First(o=>!q.correctOptionIds.Contains(o.id)).id};}
        IEnumerator Run()
        {
            Configure(false,true);Open("Notebook");player.GetComponent<CampusRift.Monsters.PlayerMonsterHealth>().Revive(1,0);yield return null;var catalog=engine.Catalog;var c=catalog.courses[0];var lesson=c.lessons[0];
            var all=catalog.courses.SelectMany(x=>x.lessons).SelectMany(l=>l.questionBank.questions).ToList();Check(all.Select(q=>q.type).Distinct().Count()==6,"All6 question types installed");
            var newQs=new[]{"multi-choice","matching","fill-blank"}.Select(t=>all.First(q=>q.type==t)).ToList();
            foreach(var q in newQs){var g=GraderRegistry.Default;Check(g.Grade(q,q.correctOptionIds),q.type+" exact answer");Check(!g.Grade(q,Wrong(q)),q.type+" incomplete/wrong pair");Check(!g.Grade(q,q.correctOptionIds.Concat(new[]{q.correctOptionIds[0]}).ToArray()),q.type+" duplicate/extra fails");}
            var m=newQs[0];Check(!GraderRegistry.Default.Grade(m,m.correctOptionIds.Concat(m.options.Where(o=>!m.correctOptionIds.Contains(o.id)).Select(o=>o.id)).ToArray()),"Multi all-options oversupply fails");
            for(int p=0;p<lesson.pages.Count;p++)engine.ReadPage(c,lesson,p);
            var cards=engine.Cards(lesson);float tu=profile.Cultivation.Data.totalEarned;engine.ReviewCard(cards[0],false);string soon=engine.CardProgress(cards[0].id).nextUtc;engine.ReviewCard(cards[1],true);Check(DateTime.Parse(soon)<DateTime.Parse(engine.CardProgress(cards[1].id).nextUtc),"Unremembered card scheduled sooner");Check(profile.Cultivation.Data.totalEarned==tu,"Cards grant no TuVi");
            var saved=JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(profile.Data));Check(saved.learning.flashcards.Count(p=>p.reviews==1)==2&&saved.learning.flashcards.Find(p=>p.id==cards[1].id).remembered,"Card progress persists JSON");
            var examOnly=c.examBank.questions.First();engine.Progress.notebook.Add(new WrongQuestionProgress{id=examOnly.id,lastWrongUtc=engine.Now.ToString("o")});Check(engine.NotebookPool().Any(q=>q.id==examOnly.id),"Exam-only mistakes are reviewable");engine.Progress.notebook.Clear();
            var s=engine.StartShrine();var wrong=s.Questions[0].Data;s.Answer(Wrong(wrong));Check(engine.Progress.notebook.Any(n=>n.id==wrong.id),"Wrong answer saved before session submission");engine.SubmitExtra(s);
            engine.Progress.notebook.RemoveAll(n=>n.id!=wrong.id);s=engine.StartNotebook();s.Answer(s.Questions[0].Data.correctOptionIds);engine.SubmitExtra(s);Check(engine.Progress.notebook.Single().consecutiveCorrect==1,"One correct answer stays in notebook");
            int cash=profile.Wallet.Balance;clock.Advance(TimeSpan.FromHours(25));s=engine.StartNotebook();s.Answer(s.Questions[0].Data.correctOptionIds);engine.SubmitExtra(s);Check(engine.Progress.notebook.Count==0&&profile.Wallet.Balance==cash+2,"Second correct removes notebook;24h retry+2");
            var dailyData=new StudyDailyProgress();DateTime date=new DateTime(2026,10,5,8,0,0,DateTimeKind.Utc);int earned=0;var daily=new StudyDaily(dailyData,()=>date,n=>earned+=n);
            for(int day=0;day<7;day++){daily.Lesson();daily.Correct(20);daily.Review();daily.Lesson();daily.Correct(20);daily.Review();date=date.AddDays(1);}Check(earned==780&&dailyData.streak==7,"Seven fake-clock study days90/day+150 once");
            date=date.AddDays(1);daily.Lesson();Check(dailyData.streak==8&&daily.RestUsedThisWeek,"One missed day preserves streak using rest day");date=date.AddDays(2);daily.Lesson();Check(dailyData.streak==1,"Second rest day same week restarts streak");
            date=date.AddDays(3);daily.Lesson();Check(dailyData.streak==1,"Missing two days resets only streak");var before=engine.Now;clock.Now=clock.Now.AddDays(-2);Check(engine.Now==before,"Device clock rollback cannot reset rewards");
            P20EconomyChecks.Run(player.gameObject,profile,Check);
            for(int lang=0;lang<2;lang++)for(int mobile=0;mobile<2;mobile++)
            {
                Configure(mobile==1,lang==0);Open("Notebook");yield return null;
                foreach(var q in newQs)
                {
                    engine.Progress.notebook.Clear();engine.Progress.notebook.Add(new WrongQuestionProgress{id=q.id,lastWrongUtc=engine.Now.ToString("o")});view.StartNotebook();yield return null;
                    string name=q.type+(mobile==1?"-mobile":"-pc")+(lang==1?"-en":"");yield return Audit(name,lang==0);
                    Check(view.content.GetComponentsInChildren<RiftButton>().Where(b=>b.isActiveAndEnabled).All(b=>b.GetComponent<RectTransform>().rect.height>=68),name+" targets>=68");
                    if(q.type=="matching")
                    {
                        var correct=q.correctOptionIds[0].Split(':');var from=view.content.GetComponentsInChildren<LearningMatchDrag>().First(d=>d.id==correct[0]);var to=view.content.GetComponentsInChildren<LearningMatchDrag>().First(d=>d.id==correct[1]);
                        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,to.GetComponent<RectTransform>().position)};
                        from.OnEndDrag(data);Check(view.content.GetComponentsInChildren<RiftButton>().Any(b=>b.Label.text.Contains(correct[0]+".")&&b.Label.text.Contains("→ "+correct[1])),name+" drag raycast linked first pair");foreach(var pair in q.correctOptionIds){var p=pair.Split(':');view.SelectMatch(p[0]);view.SelectMatch(p[1]);}
                        Click(lang==0?"XÁC NHẬN CẶP":"CONFIRM PAIRS");
                    }
                    else if(q.type=="multi-choice")
                    {foreach(var id in q.correctOptionIds){var option=q.options.Find(o=>o.id==id);var button=view.content.GetComponentsInChildren<RiftButton>().First(b=>b.Label.text.Contains(option.text));button.onClick.Invoke();}Click(lang==0?"XÁC NHẬN ĐÁP ÁN":"CONFIRM ANSWERS");}
                    else {var option=q.options.Find(o=>o.id==q.correctOptionIds[0]);Click(option.text);}
                    yield return new WaitForEndOfFrame();Check(view.Screen=="Explanation"&&view.heading.text==(lang==0?"ĐÚNG":"CORRECT"),name+" UI answer graded correctly · "+view.heading.text);view.NextQuestion();
                }
                view.ShowCards();yield return Audit("cards-list"+(lang==1?"-en":"")+(mobile==1?"-mobile":""),false);view.OpenCards(lesson);Click(lang==0?"Chạm":"Tap");yield return Audit("flashcards"+(lang==1?"-en":"")+(mobile==1?"-mobile":""),lang==0&&mobile==0);
                view.ShowNotebook();yield return Audit("notebook"+(lang==1?"-en":"")+(mobile==1?"-mobile":""),lang==0&&mobile==0);
                view.ShowDaily();yield return Audit("daily"+(lang==1?"-en":"")+(mobile==1?"-mobile":""),lang==0&&mobile==0);
            }
            Configure(false,true);view.Close();UIStateManager.Instance.EnterScene(true);yield return null;
            for(int level=1;level<=10;level++){var positions=LearningShrines.Positions(LevelCatalog.Instance.Get(level));Check(positions.Count>=1&&positions.Count<=2&&positions.All(p=>ShelterDetector.AtFeet(p)==Shelter.Indoor),"Level"+level+"1–2 indoor NavMesh stele positions");}
            var director=LevelDirector.Ensure();director.Begin(LevelCatalog.Instance.Get(8));director.enabled=false;yield return null;var shrines=FindObjectsByType<LearningShrine>();Check(shrines.Length>=1,"Real level8 stele spawned");
            var shrine=shrines.First();Approach(shrine);player.enabled=false;
            camera.transform.position=player.transform.position+new Vector3(2,2.1f,-3);camera.transform.LookAt(shrine.transform.position+Vector3.up*.9f);yield return Audit("linh-bia-world",true);
            var intent=director.GetComponent<SwordIntent>();bool fullBefore=intent.Full;int defeated=intent.DefeatedCount;shrine.Interact(player);yield return null;Check(Time.timeScale==0&&UIStateManager.Instance.State==UIState.Course,"Stele pauses combat");Check(shrine.Session!=null&&shrine.Session.Questions.All(q=>engine.Progress.Lesson(q.Data.lessonId).completed),"Stele asks only learned content");
            view=FindAnyObjectByType<LearningUI>();view.Answer(shrine.Session.Questions[0].Data.correctOptionIds);view.NextQuestion();yield return Audit("linh-bia-choice",true);Check(shrine.Choices.Length==3&&shrine.Choices.Distinct().Count()==3,"Three distinct fortunes drawn from six");
            shrine.Choose(shrine.Choices[0]);view.Close();yield return null;Check(Time.timeScale==1&&intent.Full==fullBefore&&intent.DefeatedCount==defeated,"Fortune resumes and leaves SwordIntent unchanged");
            var second=shrines.Last();if(second==shrine){var go=new GameObject("P20 wrong-answer stele");go.transform.position=shrine.transform.position+Vector3.right*2;second=go.AddComponent<LearningShrine>();}
            Configure(true,true);Approach(second);second.Interact(player);yield return null;Check(Time.timeScale==0&&second.Session!=null,"Mobile stele interaction pauses combat · safe="+second.Safe);var question=second.Session.Questions[0].Data;cash=profile.Wallet.Balance;view.Answer(Wrong(question));view.NextQuestion();yield return Audit("linh-bia-mobile",false);Check(second.Used&&second.Choices==null&&profile.Wallet.Balance==cash&&engine.Progress.notebook.Any(n=>n.id==question.id),"Wrong stele shuts down, no penalty, notebook records");view.Close();
            var stats=player.GetComponent<PlayerStats>();var buffs=player.GetComponent<BuffSystem>();var items=player.GetComponent<PlayerItems>();var hp=player.GetComponent<CampusRift.Monsters.PlayerMonsterHealth>();var control=PlayerEnemyControl.Ensure(player.gameObject);
            foreach(ShrineFortune fortune in Enum.GetValues(typeof(ShrineFortune)))
            {
                items.BeginLevel();control.Clear();hp.Revive(.2f,0);var probe=new GameObject("P20 fortune probe").AddComponent<LearningShrine>();probe.transform.position=shrine.transform.position;Approach(probe);probe.Interact(player);yield return null;
                for(int draw=0;draw<100;draw++){probe.Resolve(true);if(probe.Choices.Contains(fortune))break;}
                float damage=stats.DamageDealt,cooldown=stats.CooldownReduction,spirit=stats.MaxSpirit,speed=stats.MoveSpeedBonus,health=hp.CurrentHealth;bool applied=probe.Choose(fortune);view.Close();
                bool correct=fortune==ShrineFortune.Damage?Mathf.Abs(stats.DamageDealt-damage-.2f)<.001f:fortune==ShrineFortune.Cooldown?Mathf.Abs(stats.CooldownReduction-cooldown-.2f)<.001f:fortune==ShrineFortune.Spirit?Mathf.Abs(stats.MaxSpirit-spirit-30)<.001f:fortune==ShrineFortune.Speed?Mathf.Abs(stats.MoveSpeedBonus-speed-.15f)<.001f:fortune==ShrineFortune.Heal?Mathf.Abs(hp.CurrentHealth-health-hp.maxHealth*.3f)<.01f:true;
                if(fortune==ShrineFortune.ControlGuard){control.Stun(3);correct=!control.Stunned;control.Stun(3);correct&=control.Stunned;control.Clear();}
                Check(applied&&correct,"Actual fortune effect "+fortune);Check(!probe.Choose(fortune),"Fortune can be chosen only once "+fortune);
                buffs.Tick(91);if(fortune==ShrineFortune.Damage||fortune==ShrineFortune.Cooldown||fortune==ShrineFortune.Speed)Check(Mathf.Abs(stats.DamageDealt-damage)<.001f&&Mathf.Abs(stats.CooldownReduction-cooldown)<.001f&&Mathf.Abs(stats.MoveSpeedBonus-speed)<.001f,fortune+" expires90seconds");
                if(fortune==ShrineFortune.Spirit){Check(Mathf.Abs(stats.MaxSpirit-spirit-30)<.001f,"Spirit fortune persists past90seconds");items.BeginLevel();Check(Mathf.Abs(stats.MaxSpirit-spirit)<.001f,"Spirit fortune resets next level");}
                Destroy(probe.gameObject);
            }
            items.BeginLevel();Check(intent.Full==fullBefore&&intent.DefeatedCount==defeated,"All six fortunes preserve SwordIntent");
            Check(all.All(q=>GraderRegistry.Default.Supports(q)&&!string.IsNullOrEmpty(q.source)),"Runtime question schema/source validation");director.enabled=true;
        }
    }
}
#endif
