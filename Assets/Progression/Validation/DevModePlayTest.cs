#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Learning;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.UI;

namespace CampusRift.Progression
{
    public sealed class DevModePlayTest : MonoBehaviour
    {
        const string Root = "task/devmode/";
        [Serializable] public sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(); public string beforeHash, afterHash, userBeforeHash, userAfterHash; }
        Report report = new Report();
        public bool ResumeAfterHub;
        public bool CaptureBadgeOnly;
        string ReportName => CaptureBadgeOnly ? "BadgeCapture" : "DevModePlayTest";
        GameSettings settings;
        ProfileService profile;
        bool qaInstalled, originalDev, originalInvincible, originalCooldown, oldTutorial;
        int[] prefs;
        string[] keys = { DevMode.ActiveKey, DevMode.InvincibleKey, DevMode.CooldownKey };
        int width, height;
        LevelDefinition fixture;
        UIStateManager State => UIStateManager.Instance;
        void Check(bool ok, string text)
        { (ok ? report.passed : report.failed).Add(text); SaveReport(); Debug.Log("DEVMODE QA " + (ok ? "PASS " : "FAIL ") + text); }
        void SaveReport() { Directory.CreateDirectory(Root); File.WriteAllText(Root + ReportName + ".json", JsonUtility.ToJson(report, true)); }
        static string Hash(string path)
        { if (!File.Exists(path)) return "missing"; using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        IEnumerator Frames(int n=4) { for (int i=0;i<n;i++) yield return null; }
        IEnumerator Wait(Func<bool> condition, float seconds=30)
        { float until=Time.realtimeSinceStartup+seconds; while (!condition() && Time.realtimeSinceStartup<until) yield return null; if(!condition()) throw new TimeoutException("DevMode fixture timeout"); }
        IEnumerator Shots(string name)
        {
            foreach (var size in new[]{new Vector2Int(2400,1080),new Vector2Int(1600,720)})
            {
                UIValidation.SetResolution(size.x,size.y); yield return new WaitForSecondsRealtime(.35f); yield return Frames(5);
                Check(Screen.width==size.x && Screen.height==size.y, name+" render size "+size);
                ScreenCapture.CaptureScreenshot(Root+"screens/"+name+"-"+size.x+"x"+size.y+".png"); yield return Frames(4);
            }
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); Application.runInBackground=true; Directory.CreateDirectory(Root+"screens");
            settings=SettingsManager.Instance.Current.Copy(); width=Screen.width; height=Screen.height;
            if (ResumeAfterHub) { report = JsonUtility.FromJson<Report>(File.ReadAllText(Root+"runs/initial/DevModePlayTest.json")); report.failed.Clear(); }
            profile=ProfileService.Ensure(); originalDev=DevMode.Active; originalInvincible=DevMode.Invincible; originalCooldown=DevMode.NoCooldown;
            prefs=keys.Select(k=>PlayerPrefs.GetInt(k,-1)).ToArray(); oldTutorial=TutorialDirector.Suppress; TutorialDirector.Suppress=true;
            report.userBeforeHash=Hash(ProfileService.SavePath); SaveReport();
            var run=CaptureBadgeOnly ? CaptureBadges() : Run();
            while(true)
            {
                bool more=false; object current=null;
                try {more=run.MoveNext();if(more)current=run.Current;}
                catch(Exception e){Check(false,e.ToString());break;}
                if(!more)break;yield return current;
            }
            if(DevMode.Active){DevMode.SetActive(false);yield return Wait(()=>!DevMode.Active && !DevMode.Changing);}
            if(qaInstalled)profile.PopTransient();
            if(originalDev)DevMode.SetActive(true);
            DevMode.SetInvincible(originalInvincible);DevMode.SetNoCooldown(originalCooldown);
            for(int i=0;i<keys.Length;i++){if(prefs[i]<0)PlayerPrefs.DeleteKey(keys[i]);else PlayerPrefs.SetInt(keys[i],prefs[i]);}PlayerPrefs.Save();
            SettingsManager.Instance.Apply(settings,false); UIValidation.SetResolution(width,height); TutorialDirector.Suppress=oldTutorial;
            LevelSession.Clear();LevelSession.Loadout=null;if(fixture!=null)Destroy(fixture);
            report.userAfterHash=Hash(ProfileService.SavePath); Check(report.userBeforeHash==report.userAfterHash,"User save bytes unchanged across QA");
            File.WriteAllText(Root+ReportName+"-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed"); Destroy(gameObject);
        }
        // Capture final corner placement without repeating the gameplay acceptance suite.
        IEnumerator CaptureBadges()
        {
            GameSceneManager.Instance.LoadMainMenu();yield return Wait(()=>!GameSceneManager.Instance.IsLoading&&State.State==UIState.Hub);yield return Frames(8);
            var draft=settings.Copy();draft.Language=Localization.GameLanguage.Vietnamese;draft.ControlMode=ControlMode.Mobile;SettingsManager.Instance.Apply(draft,false);
            DevMode.SetActive(true);DevMode.SetInvincible(true);yield return Frames(5);
            HubUI.Instance.Select(HubUI.Tab.Map);yield return Shots("hub-map");
            HubUI.Instance.Select(HubUI.Tab.Skills);yield return Shots("hub-skills");
            GameSceneManager.Instance.StartLevel(5,new[]{"kim-chung-trao","phat-no-hoa-lien","hu-khong-ket-gioi","dai-thu-an"});
            yield return Wait(()=>!GameSceneManager.Instance.IsLoading&&LevelDirector.Instance!=null&&LevelDirector.Instance.State!=LevelDirector.Phase.Idle);yield return Frames(8);yield return Shots("hud");
        }
        IEnumerator Run()
        {
            GameSceneManager.Instance.LoadMainMenu(); yield return Wait(()=>!GameSceneManager.Instance.IsLoading && State.State==UIState.Hub);yield return Frames(8);
            if(DevMode.Active)DevMode.SetActive(false);
            DevMode.SetInvincible(false);DevMode.SetNoCooldown(false);
            var qa=new ProfileData();qa.wallet.linhThach=137;
            var qaStore=new JsonProfileStore(Path.GetFullPath(Root+"qa-profile.json"));qaStore.Save(qa);
            profile.PushStoredProfileForValidation(qaStore);qaInstalled=true;
            profile.NotifyChanged();yield return Frames(6);
            report.beforeHash=Hash(Root+"qa-profile.json");string memoryBefore=JsonUtility.ToJson(profile.Data);int saveCount=profile.SaveCount;
            var lockedSkill=SkillCatalog.Instance.Find("vo-hon-chan-than");
            Check(!profile.Skills.IsUnlocked(lockedSkill)&&!profile.Levels.IsUnlocked(2,out _)&&profile.Wallet.Balance==137,"Low-realm QA: skill/level2 locked, 137 stones");
            var language=settings.Copy();language.Language=Localization.GameLanguage.Vietnamese;language.ControlMode=ControlMode.Mobile;language.LocalTelemetryEnabled=true;language.TelemetryConsentAsked=true;SettingsManager.Instance.Apply(language,false);
            var lockedItem=ItemCatalog.Instance.Item("ti-hoa-chau");
            SettingsUI controls;
            if (!ResumeAfterHub) {
            State.OpenSettings();yield return new WaitForSecondsRealtime(.5f);
            controls=FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);
            Check(controls!=null&&controls.DeveloperTab.gameObject.activeSelf,"Editor exposes developer section");
            controls.DeveloperTab.onClick.Invoke();yield return Frames();yield return Shots("settings");
            controls.RequestDevMode(true);yield return Frames();
            Check(!DevMode.Active&&controls.DevConfirmation!=null,"Enable requires concrete warning confirmation");yield return Shots("confirmation");
            controls.ConfirmDevMode(true);yield return Frames();
            Check(DevMode.Active&&profile.Transient&&PlayerPrefs.GetInt(DevMode.ActiveKey)==1,"Dev enable pushes transient and persists separate preference");
            Check(!DevMode.Invincible&&!DevMode.NoCooldown,"Invincibility and cooldown bypass default off");
            Check(SkillCatalog.Instance.skills.Count(s=>s!=null&&profile.Skills.IsUnlocked(s)&&profile.Skills.GetRank(s.id)==5)==21,"All 21 skills open at rank5; four-slot contract");
            Check(SkillLoadout.SlotCount==4&&Enumerable.Range(1,10).All(i=>profile.Levels.IsUnlocked(i,out _)),"Four skill slots retained; levels1-10 open");
            Check(EndgameService.TowerOpen(profile.Data)&&Enumerable.Range(1,10).All(i=>EndgameService.NightmareOpen(profile.Data,i)),"Tower and all Nightmare entries open");
            Check(ItemCatalog.Instance.artifacts.All(a=>profile.Artifacts.Level(a.id)==a.MaxLevel)&&PlayerCostume.Catalog.All(c=>PlayerCostume.Open(profile.Data,c)),"All artifacts max and costumes open");
            var shop=new ShopService(profile);int before=profile.Data.wallet.linhThach;
            Check(shop.Buy(lockedItem,1)==PurchaseResult.Ok&&shop.Price(lockedItem,1)==0&&profile.Data.wallet.linhThach==before,"Realm-locked fire ward purchased free");
            Check(profile.Wallet.BalanceText=="∞"&&profile.Inventory.CountText(lockedItem.id)=="∞"&&profile.Cultivation.TuViText=="∞","Infinite display strings for stones, items and TuVi");
            var engine=LearningService.Instance.Engine;var course=engine.Catalog.courses.Last();var lesson=course.lessons.Last();
            Check(engine.Available(course,lesson)&&engine.CanTakeExam(course,out _),"Prerequisite and breakthrough gates bypassed with questions retained");
            var exam=engine.StartExam(course,()=>Time.unscaledTime);while(!exam.Session.Complete)exam.Session.Answer(exam.Session.Questions[exam.Session.Answered].Data.correctOptionIds);
            int telemetryRows=LocalTelemetry.Instance.WrittenRows;var outcome=engine.SubmitExam(exam);LocalTelemetry.Quiz(outcome.result);
            engine.Daily.Correct(20);engine.Daily.Lesson();
            Check(outcome.passed&&outcome.linhThach==0&&!outcome.breakthrough&&LocalTelemetry.Instance.WrittenRows==telemetryRows&&profile.Data.learning.studyDaily.correct==0,"Exam works; no reward, daily count or telemetry even opt-in");
            controls.Cancel();yield return Frames(10);HubUI.Instance.Select(HubUI.Tab.Map);yield return Shots("hub-map");
            Check(HubUI.Instance.ContentRect.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Level ")).Count(b=>b.interactable)==10&&DevModeBadge.Instance.Visible,"Hub map opens all ten cards and shows DEV MODE");
            HubUI.Instance.Select(HubUI.Tab.Skills);yield return Shots("hub-skills");
            } else {
                State.OpenSettings(); yield return Frames(8); controls=FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);
                controls.DeveloperTab.onClick.Invoke(); yield return Shots("settings");
                controls.RequestDevMode(true); yield return Shots("confirmation"); controls.ConfirmDevMode(true); controls.Cancel(); yield return Frames(8);
            }
            profile.Inventory.ClearCarry();profile.Inventory.SetCarry(lockedItem,1);
            DevMode.SetInvincible(true);
            GameSceneManager.Instance.StartLevel(5,new[]{"kim-chung-trao","phat-no-hoa-lien","hu-khong-ket-gioi","dai-thu-an"});
            yield return Wait(()=>!GameSceneManager.Instance.IsLoading&&LevelDirector.Instance!=null&&LevelDirector.Instance.State!=LevelDirector.Phase.Idle);yield return Frames(8);
            var director=LevelDirector.Instance;var player=FindAnyObjectByType<CampusExplorer>();var health=player.GetComponent<PlayerMonsterHealth>();var spirit=player.GetComponent<SpiritPower>();var items=player.GetComponent<PlayerItems>();
            Check(director.Level.index==5&&DevModeBadge.Instance.Visible,"Locked level5 entered; HUD badge visible");yield return Shots("hud");
            int count=items.Bag.Slots[0].remaining;int owned=profile.Inventory.Count(lockedItem.id);
            Check(items.Use(0)==ItemUseResult.Used&&items.Bag.Slots[0].remaining==count&&profile.Inventory.Count(lockedItem.id)==owned,"Using item keeps bag and inventory counts");
            var bell=player.GetComponent<GoldenBellRuntime>();float ll=spirit.Current;bool cast=bell.CastAt(player.transform.position);
            Check(cast&&Mathf.Approximately(spirit.Current,ll)&&bell.CooldownRemaining>0,"Actual skill cast spends no spirit and starts normal cooldown");
            DevMode.SetNoCooldown(true);Check(bell.CooldownRemaining==0,"Optional cooldown bypass applies to an existing cooldown");DevMode.SetNoCooldown(false);Check(bell.CooldownRemaining>0,"Turning bypass off retains original cooldown deadline");
            health.Revive(1,0);float hp=health.CurrentHealth;DevMode.SetInvincible(true);health.ApplyDamage(DamageInfo.Create(1e6f,Element.None,DamageSource.Environment,player.transform.position,Vector3.down));
            Check(health.CurrentHealth==hp,"Optional invincibility blocks lethal environment damage");
            var input=player.GetComponent<CampusInput>();float energy=player.Energy;float end=Time.realtimeSinceStartup+10;int sprintFrames=0;
            while(Time.realtimeSinceStartup<end){input.TouchMove=Vector2.up;input.TouchSprint=true;if(player.IsSprinting)sprintFrames++;yield return null;}
            input.ResetAll();Check(sprintFrames>5&&Mathf.Approximately(energy,player.Energy)&&!player.BoostExhausted,"Actual mobile Boost10s keeps energy full");
            fixture=Instantiate(director.Level);fixture.hideFlags=HideFlags.DontSave;fixture.restSeconds=.1f;
            director.introSeconds=.1f;director.spawnInterval=.02f;director.portalLead=.1f;director.winDelay=.1f;director.Begin(fixture);
            float deadline=Time.realtimeSinceStartup+60;
            while(director.State!=LevelDirector.Phase.Won&&Time.realtimeSinceStartup<deadline)
            {
                foreach(var e in director.Alive.ToArray())if(e!=null&&e.Alive)e.Vitality.ApplyDamage(DamageInfo.Create(1e7f,Element.None,DamageSource.Melee,e.transform.position,Vector3.down));
                yield return null;
            }
            Check(director.State==LevelDirector.Phase.Won&&director.Kills==director.TotalPlanned&&director.WinsRaised==1,"Level5 real waves and boss clear via existing lethal-damage fixture");
            profile.Flush();Check(Hash(Root+"qa-profile.json")==report.beforeHash&&profile.SaveCount==saveCount&&profile.Levels.Entry(5)==null&&profile.Data.wallet.linhThach==137,"Win/Flush produce no stars, stones, attempts or save writes");
            State.EnterScene(true);State.Pause();State.OpenSettings();yield return Frames();controls=FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);
            controls.RequestDevMode(false);yield return Frames();Check(controls.DevConfirmation!=null&&controls.DevConfirmation.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="Tắt Dev Mode sẽ đưa bạn về Sảnh"),"Disable during run warns of return to Hub");
            controls.ConfirmDevMode(false);yield return Wait(()=>!DevMode.Active&&!DevMode.Changing&&State.State==UIState.Hub);yield return Frames(8);
            report.afterHash=Hash(Root+"qa-profile.json");
            Check(report.afterHash==report.beforeHash&&JsonUtility.ToJson(profile.Data)==memoryBefore,"QA disk hash and full in-memory real profile restored exactly");
            Check(!profile.Skills.IsUnlocked(lockedSkill)&&!profile.Levels.IsUnlocked(2,out _)&&profile.Wallet.Balance==137&&!DevModeBadge.Instance.Visible,"Disable restores locked skill/level, 137 stones and hides badge");
            State.OpenSettings();yield return Frames(6);controls=FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);controls.UseReleasePolicyForValidation(true);
            Check(!controls.DeveloperTab.gameObject.activeSelf,"Release policy hides actual developer tab");
            for(int i=0;i<6;i++)controls.VersionButton.onClick.Invoke();Check(!controls.DeveloperTab.gameObject.activeSelf,"Six version taps keep developer tab hidden");
            controls.VersionButton.onClick.Invoke();Check(controls.DeveloperTab.gameObject.activeSelf,"Seventh version tap reveals actual developer tab");controls.UseReleasePolicyForValidation(false);controls.Cancel();
        }
    }
}
#endif
