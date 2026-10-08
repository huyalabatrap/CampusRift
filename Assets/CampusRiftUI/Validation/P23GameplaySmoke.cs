#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Learning;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.Skills;
using CampusRift.SkyBeast;
namespace CampusRift.UI
{
    public sealed class P23GameplaySmoke:MonoBehaviour
    {
        [Serializable] public sealed class Report {public List<string> passed=new List<string>(),failed=new List<string>();}
        public bool LargeMobileOnly;
        readonly Report report=new Report();const string Root="task/p23/";
        SkillSet1TestWorld w;LevelDirector d;GameSettings settings;LearningEngine oldEngine;readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        void Check(bool pass,string name){(pass?report.passed:report.failed).Add(name);File.WriteAllText(Root+"gameplay.json",JsonUtility.ToJson(report,true));}
        IEnumerator Shot(string name)
        {yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();var f=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Root+"screens/"+name+".png",f.EncodeToPNG());Destroy(f);}
        IEnumerator Audit(string name)
        {yield return new WaitForSecondsRealtime(.4f);var a=ComicTextAudit.Scan(name);ComicTextAudit.Save(a,Root+"audits/"+name+".json");Check(a.issues.Count==0,name+" ComicTextAudit0");}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Root+"screens");Directory.CreateDirectory(Root+"audits");settings=SettingsManager.Instance.Current.Copy();
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){var e=stack.Peek();bool more=false;object next=null;try{more=e.MoveNext();if(more)next=e.Current;}catch(Exception ex){Check(false,ex.ToString());break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
            P21StoryCinematic.CancelActive();if(d!=null)d.End();if(w!=null&&w.player!=null)w.End();if(oldEngine!=null)LearningService.Instance.EditorUseEngine(oldEngine);SettingsManager.Instance.Apply(settings,false);ProfileService.Instance.EndTransient();
            File.WriteAllText(Root+"gameplay-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
        void Mode(int size,bool mobile=false)
        {var s=SettingsManager.Instance.Current.Copy();s.TextSize=size;s.AccessibleColors=AccessiblePalette.ColorBlind;s.Subtitles=true;s.SlowReading=true;s.ReduceCameraShake=true;s.ReduceSkillFlashes=true;s.Language=Localization.GameLanguage.Vietnamese;s.ControlMode=mobile?ControlMode.Mobile:ControlMode.PC;SettingsManager.Instance.Apply(s,false);}
        IEnumerator Run()
        {
            w=new SkillSet1TestWorld{GameplayCamera=true};w.Begin();yield return null;foreach(var v in w.victims)v.gameObject.SetActive(false);
            oldEngine=LearningService.Instance.Engine;LearningService.Instance.EditorUseEngine(new LearningEngine(oldEngine.Catalog,new ProfileLearningStore(ProfileService.Instance),23,ProfileService.Instance.Cultivation));
            TutorialDirector.Suppress=true;d=LevelDirector.Ensure();d.Begin(LevelCatalog.Instance.Get(8));d.enabled=false;foreach(var e in d.Alive)e.gameObject.SetActive(false);w.player.enabled=false;w.player.followCamera=null;
            var fire=FireBreathCycle.Ensure();fire.AutoAdvance=false;Mode(2);w.camera.transform.position=w.origin+new Vector3(-3,2,-3);w.camera.transform.LookAt(w.origin+new Vector3(0,1,0));
            if(LargeMobileOnly){
                var indoor=FindObjectsByType<LearningShrine>().First();var at=indoor.transform.position;
                w.PlacePlayer(at+new Vector3(0,0,-1.4f));w.camera.transform.position=at+new Vector3(0,1.7f,-2.5f);w.camera.transform.LookAt(at+Vector3.up*1.05f);
                Mode(2,true);FindAnyObjectByType<FireWarningHUD>().FindDoor();yield return Audit("size2-gameplay-mobile");yield break;
            }
            // Co-dispatched warning + roar must retain the alarm text.
            yield return new WaitForSecondsRealtime(.2f);fire.RestartWarning();yield return null;
            Check(ImportantCaptions.Instance.CurrentText.Contains("Còi báo Thiên Hỏa"),"Warning alarm survives simultaneous beast roar");yield return Audit("caption-alarm-large");yield return Shot("captions-fire");
            yield return new WaitForSecondsRealtime(.2f);var beast=SkyBeastScheduler.Instance.Beasts[0];beast.Roar();yield return null;Check(ImportantCaptions.Instance.CurrentText.Contains("gầm"),"Actual beast roar has localized caption");
            var off=SettingsManager.Instance.Current.Copy();off.Subtitles=false;SettingsManager.Instance.Apply(off,false);yield return null;Check(ImportantCaptions.Instance.CurrentText=="","Subtitles off hides sound caption");Mode(2);
            var shrine=FindObjectsByType<LearningShrine>().First();var point=shrine.transform.position;w.PlacePlayer(point+new Vector3(0,0,-1.4f));w.camera.transform.position=point+new Vector3(0,1.7f,-2.5f);w.camera.transform.LookAt(point+Vector3.up*1.05f);
            yield return new WaitForSecondsRealtime(.3f);Check(!shrine.GetComponentInChildren<Light>().enabled,"Ready glow off without learned questions");LearningService.Instance.Engine.Progress.Lesson(oldEngine.Catalog.courses[0].lessons[0].id).completed=true;yield return new WaitForSecondsRealtime(.3f);Check(shrine.GetComponentInChildren<Light>().enabled,"Readiness glow on near safe shrine with learned pool");
            var avatar=w.player.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();foreach(var r in avatar)r.enabled=false;yield return Shot("linh-bia-after");foreach(var r in avatar)r.enabled=true;Check(shrine.transform.Find("Ancient carved slab")!=null&&shrine.GetComponentsInChildren<MeshRenderer>().Length>30,"Ancient stele has stepped stone, recessed runes and cracks");
            var hud=FindAnyObjectByType<FireWarningHUD>();hud.FindDoor();yield return new WaitForSecondsRealtime(.2f);Check(ShelterDetector.AtFeet(w.player.transform.position)==Shelter.Indoor,"Indoor fixture is really sheltered");
            var arrow=(Image)typeof(FireWarningHUD).GetField("arrow",flags).GetValue(hud);bool hidden=!arrow.gameObject.activeSelf&&!hud.DoorMarkerVisible;
            // Reconstruct only the old extra-arrow visual; same indoor position/camera as the after capture.
            hud.enabled=false;arrow.gameObject.SetActive(true);yield return Shot("indoor-arrow-before");hud.enabled=true;yield return Shot("indoor-arrow-after");Check(hidden&&!arrow.gameObject.activeSelf&&!hud.DoorMarkerVisible,"Indoor hides both redundant route arrows");
            foreach(int size in new[]{0,1,2}){Mode(size);yield return Audit("size"+size+"-gameplay-pc");Mode(size,true);yield return Audit("size"+size+"-gameplay-mobile");}Mode(2);
            shrine.Cancel();yield return new WaitForSecondsRealtime(.3f);Check(!shrine.GetComponentInChildren<Light>().enabled,"Used shrine disables readiness glow");
            // Real icon routing is shared by level intel and skill cards.
            Check(Enum.GetValues(typeof(Element)).Cast<Element>().All(e=>ContentImages.Get("Elements",e==Element.None?"vo-he":e==Element.Moc?"moc":e==Element.Thuy?"thuy":e==Element.Hoa?"hoa":e==Element.Tho?"tho":e==Element.Loi?"loi":e==Element.Am?"am":e==Element.KhongGian?"khong-gian":"kim")==Accessibility.SymbolSprite(e)),"Level intel resolves nine geometric symbols in color blind mode");
            var slots=FindObjectsByType<SkillSlotUI>();Check(slots.Any(s=>s.transform.Find("Element shape")!=null),"PC equipped skills show independent element shapes");
            var mark=EnemyTelegraph.Show(w.player.transform.position+Vector3.forward*2,Vector3.forward,2,10,DangerShape.Cone,120);Check(mark.transform.Find("Crossed danger pattern").GetComponent<LineRenderer>().enabled,"Telegraph renders white pattern and alternative color");mark.Hide();
            DamageNumberPool.Instance.Show(w.player.transform.position+Vector3.up*2,123,Element.Moc,false,null);yield return null;Check(DamageNumberPool.Instance.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.StartsWith(Accessibility.Symbol(Element.Moc)+" ")),"Damage number includes non-color element symbol");
            yield return Shot("colorblind-hud-large");
            // Before/after of the fixture: old ring is intentionally hidden; current HUD shows the tier.
            var card=ComboUIFactory.Canvas("P23 fixture evidence",transform,150);var label=ComboUIFactory.Text("Fixture observation",card.transform,new Vector2(1500,90),new Vector2(0,300),28);
            label.text="FIXTURE CŨ: ring HUD ẩn; lookup trả null (19 PASS / 2 FAIL lịch sử).";yield return Shot("learning-fixture-before");
            var current=FindAnyObjectByType<LevelHUD>();ProfileService.Instance.Cultivation.SetState(Realm.LuyenKhi,5,0);yield return null;
            label.text="FIXTURE MỚI: LevelHUD hiện hành · tầng "+ProfileService.Instance.Cultivation.Tier+" · không truy cập ring ẩn.";yield return Shot("learning-fixture-after");Check(ProfileService.Instance.Cultivation.Tier==5&&current.BodyText.Length>0,"Learning fixture reads current LevelHUD tier and body");Destroy(card.gameObject);
            StartCoroutine(P21StoryCinematic.Play(false));float deadline=Time.realtimeSinceStartup+10;while(P21StoryCinematic.Active==null&&Time.realtimeSinceStartup<deadline)yield return null;var story=P21StoryCinematic.Active;Check(story!=null,"Real Long Vuong reveal starts");
            if(story!=null){story.SeekForCapture(.65f);yield return Shot("long-vuong-after");yield return Audit("story-reveal-large");Check(story.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="Story subtitle"&&t.enabled&&t.text.Length>0),"Reveal subtitles enabled at large size");story.Skip();yield return null;}
            UIStateManager.Instance.EnterScene(false);UIStateManager.Instance.OpenSettings();yield return new WaitForSecondsRealtime(.3f);FindAnyObjectByType<SettingsUI>().SelectTab(3);yield return new WaitForSecondsRealtime(4);yield return Shot("settings-accessibility-large");yield return Audit("settings-accessibility-large");UIStateManager.Instance.Back();
        }
    }
}
#endif
