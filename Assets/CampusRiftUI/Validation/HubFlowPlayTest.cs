#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Localization;
using CampusRift.Levels;
using CampusRift.Progression;
using CampusRift.Skills;

namespace CampusRift.UI
{
    // P09 flow: title menu → Hub (5 tabs) → level map → preparation (skills, items) → level → result → Hub, plus the new pause menu and
    // the rule that the Library cannot be opened during a level. Starts in the gameplay sandbox, then changes scenes itself.
    // Output: Artifacts/UI/HubFlow.json, HubFlow-DONE.txt and screenshots Artifacts/UI/P09-*.png.
    public sealed class HubFlowPlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(); }
        readonly Report report = new Report();
        const string Output = "Artifacts/UI/";
        GameSettings originalSettings;
        void Check(bool ok, string label) { (ok ? report.passed : report.failed).Add(label); Directory.CreateDirectory(Output); File.WriteAllText(Output + "HubFlow.json", JsonUtility.ToJson(report, true)); Debug.Log("HUB QA " + (ok ? "PASS " : "FAIL ") + label); }
        UIStateManager State => UIStateManager.Instance;
        IEnumerator Frames(int n = 3) { for (int i = 0; i < n; i++) yield return null; }
        IEnumerator Wait(Func<bool> condition, float timeout) { float until = Time.realtimeSinceStartup + timeout; while (!condition() && Time.realtimeSinceStartup < until) yield return null; }
        void Shot(string name) { ScreenCapture.CaptureScreenshot(Output + name + ".png"); }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); Directory.CreateDirectory(Output); Application.runInBackground = true;
            originalSettings = SettingsManager.Instance.Current.Copy();
            var english = originalSettings.Copy(); english.Language = GameLanguage.English; SettingsManager.Instance.Apply(english, false);
            var profile = ProfileService.Instance; profile.UseTransient(new ProfileData());
            var run = Run();
            while (true)
            {
                bool next = false; object current = null;
                try { next = run.MoveNext(); if (next) current = run.Current; }
                catch (Exception e) { Check(false, e.ToString()); break; }
                if (!next) break; yield return current;
            }
            try { LevelSession.Clear(); LevelSession.Loadout = null; profile.EndTransient(); } catch { }
            SettingsManager.Instance.Apply(originalSettings, false);
            File.WriteAllText(Output + "HubFlow-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
            Destroy(gameObject);
        }

        IEnumerator Run()
        {
            var profile = ProfileService.Instance; var cult = profile.Cultivation;
            profile.Wallet.Earn(200, "test"); profile.Inventory.Add("hoi-khi-dan", 3);
            cult.SetState(Realm.LuyenKhi, 1, 0);

            // ---- the Hub after a level ----
            GameSceneManager.Instance.LoadMainMenu();
            yield return Wait(() => !GameSceneManager.Instance.IsLoading && State.State == UIState.Hub, 15f);
            yield return Frames(5);
            var hub = HubUI.Instance;
            Check(State.State == UIState.Hub && hub != null && hub.Visible, "Returning to the menu scene from a level lands in the Hub");
            var buttons = hub.transform.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name.StartsWith("Button ")).ToList();
            Check(buttons.Count >= 6, "The Hub shows five tabs and the way back");
            Check(hub.transform.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("Luyện Khí") || t.text.Contains("Qi Refining")), "The header names the realm");
            Check(hub.transform.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text == "200"), "The header shows the Linh Thạch balance");

            // ---- map ----
            hub.Select(HubUI.Tab.Map); yield return Frames(3);
            var cards = hub.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name.StartsWith("Level ")).ToList();
            Check(cards.Count == 10, "The map has ten level cards");
            Check(cards[0].interactable && cards.Skip(1).All(c => !c.interactable), "Only level 1 is open on a new profile; the others are locked");
            Check(hub.ContentRect.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("Luyện Khí 3") || t.text.Contains("Qi Refining 3")), "A locked level says what it needs");
            Shot("P09-hub-map"); yield return Frames(3);

            // ---- other tabs ----
            hub.Select(HubUI.Tab.Skills); yield return Frames(3);
            Check(hub.Current == HubUI.Tab.Skills && hub.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().Count(b => b.name.StartsWith("Skill ")) >= 3, "The Skills tab lists the skills");
            Shot("P09-hub-skills"); yield return Frames(3);
            hub.Select(HubUI.Tab.Library); yield return Frames(3);
            var chapters = hub.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name.StartsWith("Chapter ")).ToList();
            Check(hub.Current == HubUI.Tab.Library && chapters.Count == 6, "The Library tab shows the six chapters as cards");
            Shot("P09-hub-library"); yield return Frames(3);
            chapters[0].onClick.Invoke(); yield return new WaitForSecondsRealtime(.6f);
            var learning = FindAnyObjectByType<Learning.LearningUI>(FindObjectsInactive.Include);
            Check(State.State == UIState.Course && learning != null && learning.Screen == "Lessons", "A chapter card opens its lessons in the study panel");
            Shot("P09-study-lessons"); yield return Frames(3);
            State.Back(); yield return new WaitForSecondsRealtime(.6f);
            Check(State.State == UIState.Hub, "Esc from the study panel returns to the Hub");
            hub.Select(HubUI.Tab.Shop); yield return Frames(3);
            Check(hub.Current == HubUI.Tab.Shop && hub.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().Count(b => b.name.StartsWith("Category ")) == 4, "The Alchemy tab shows four categories");
            var exchange = hub.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.name == "Button EXCHANGE");
            long owned = profile.Inventory.Count("hoi-khi-dan"); long balance = profile.Wallet.Balance;
            exchange.onClick.Invoke(); yield return Frames(3);
            Check(profile.Inventory.Count("hoi-khi-dan") == owned + 1 && profile.Wallet.Balance == balance - 25, "EXCHANGE buys the selected item for its price");
            profile.Inventory.Add("hoi-khi-dan", 0);
            Shot("P09-hub-shop"); yield return Frames(3);
            hub.Select(HubUI.Tab.Realm); yield return Frames(3);
            Check(hub.Current == HubUI.Tab.Realm && hub.ContentRect.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("Luyện Khí") || t.text.Contains("Qi Refining")), "The Realm tab shows the realm page");
            Shot("P09-hub-realm"); yield return Frames(3);
            hub.Select(HubUI.Tab.Map); yield return Frames(3);

            // ---- preparation ----
            hub.Select(HubUI.Tab.Map); yield return Frames(3);
            cards = hub.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name.StartsWith("Level ")).ToList();
            cards[0].onClick.Invoke(); yield return Frames(4);
            var loadout = LoadoutUI.Instance;
            Check(State.State == UIState.Loadout && loadout != null && loadout.Visible && LoadoutUI.Level.index == 1, "Choosing level 1 opens the preparation screen");
            Check(loadout.Slots.Count(s => !string.IsNullOrEmpty(s)) == 3 && loadout.Slots[3] == "dai-thu-an", "It starts with the three starter skills in their slots");
            var catalog = SkillCatalog.Instance;
            loadout.SetSlot(2, "dai-thu-an"); Check(loadout.Slots[3] == "" && loadout.Slots[2] == "dai-thu-an", "Equipping a skill that sits elsewhere moves it");
            loadout.UseSuggested(); Check(string.Join(",", loadout.Slots) == string.Join(",", LoadoutUI.SuggestedIds(1)), "The suggested set of level 1 is applied");
            loadout.SavePreset(0); loadout.SetSlot(0, ""); loadout.LoadPreset(0);
            Check(loadout.HasPreset(0) && loadout.Slots[0] == LoadoutUI.SuggestedIds(1)[0], "A saved set can be loaded back");
            Check(profile.Data.loadouts.Count >= 1 && profile.Data.loadouts[0].ids.Count == 4, "Saved sets live in the profile");
            loadout.Apply(new[] { "dai-thu-an", "phat-no-hoa-lien", "", "" });
            Check(loadout.Slots[0] == "dai-thu-an" && loadout.Slots[1] == "", "A skill that is locked or missing is refused");
            loadout.Apply(new[] { "hu-khong-ket-gioi", "anh-phan-than", "", "dai-thu-an" });
            // items: tap the card twice to carry 2
            var itemCards = loadout.transform.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name == "Item hoi-khi-dan").ToList();
            Check(itemCards.Count == 1, "The owned item is offered");
            itemCards[0].onClick.Invoke(); yield return Frames(2);
            itemCards = loadout.transform.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name == "Item hoi-khi-dan").ToList(); itemCards[0].onClick.Invoke(); yield return Frames(2);
            Check(profile.Inventory.CarryCount("hoi-khi-dan") == 2, "Tapping an item raises how many are carried");
            Shot("P09-loadout"); yield return Frames(3);

            // ---- into the level ----
            long linhBefore = profile.Wallet.Balance; int expectedLinh = 0;
            loadout.StartLevel();
            yield return Wait(() => GameSceneManager.Instance.IsLoading, 3f);
            yield return Wait(() => !GameSceneManager.Instance.IsLoading && LevelDirector.Instance != null && LevelDirector.Instance.State != LevelDirector.Phase.Idle, 25f);
            yield return new WaitForSeconds(1f);
            var director = LevelDirector.Instance; var player = FindAnyObjectByType<CampusExplorer>();
            Check(State.State == UIState.Gameplay && director != null && director.Level.index == 1, "START loads level 1 in gameplay");
            var skillLoadout = player.GetComponent<SkillLoadout>();
            Check(skillLoadout.Get(0)?.Id == "hu-khong-ket-gioi" && skillLoadout.Get(1)?.Id == "anh-phan-than" && skillLoadout.Get(3)?.Id == "dai-thu-an" && skillLoadout.Get(2) == null, "The chosen skills are in the level's slots");
            var items = player.GetComponent<PlayerItems>();
            Check(items != null && items.Bag.Slots.Count == 1 && items.Bag.Slots[0].item.id == "hoi-khi-dan" && items.Bag.Slots[0].remaining == 2, "The carried items are in the level's bag");
            Check(FindAnyObjectByType<ItemBarUI>(FindObjectsInactive.Include) != null, "The HUD shows the item bar");
            State.OpenCourse(); yield return Frames(2);
            Check(State.State == UIState.Gameplay, "The Library cannot be opened during a level");

            // ---- pause menu ----
            State.Pause(); yield return new WaitForSecondsRealtime(.5f);
            var pause = FindAnyObjectByType<UIManager>().PauseMenu;
            var adapter = pause.GetComponent<PauseMenuAdapter>();
            var pauseButtons = pause.GetComponentsInChildren<RiftButton>(true);
            Check(adapter != null && pauseButtons.Where(b => b.gameObject.activeSelf).Select(b => b.name).OrderBy(x => x).SequenceEqual(new[] { "MAIN MENU", "RESUME", "SETTINGS" }), "The pause menu has Continue, Settings and Leave only");
            Shot("P09-pause"); yield return Frames(3);
            adapter.LeaveButton.onClick.Invoke(); yield return Frames(2);
            Check(adapter.Armed && !GameSceneManager.Instance.IsLoading, "Leaving needs a second click");
            State.Resume(); yield return new WaitForSecondsRealtime(.8f);
            Check(!adapter.Armed && State.State == UIState.Gameplay, "Resuming cancels the leave request");

            // ---- win and result ----
            var dirEnemies = director;
            int guard = 0;
            var hp = player.GetComponent<Monsters.PlayerMonsterHealth>(); hp.Heal(hp.maxHealth);
            while (director.State != LevelDirector.Phase.Won && guard++ < 120)
            {
                foreach (var e in new List<EnemyInstance>(director.Alive)) if (e != null && e.Alive) e.Vitality.ApplyDamage(DamageInfo.Create(1e7f, Element.None, DamageSource.Melee, e.transform.position + Vector3.up, Vector3.forward));
                hp.Heal(hp.maxHealth); yield return new WaitForSeconds(0.25f);
            }
            yield return Wait(() => State.State == UIState.Victory, 6f);
            yield return new WaitForSecondsRealtime(1.4f);
            Check(State.State == UIState.Victory, "Clearing every wave shows the result screen");
            var result = FindAnyObjectByType<LevelResultUI>(FindObjectsInactive.Include);
            string resultText = result.Stats.text;
            Check(resultText.Contains("Linh Thạch") && result.LinhThachShown == 60 + (director.Level.parTimeSeconds >= director.Result.seconds ? 20 : 0), "The result screen counts up the Linh Thạch reward (" + result.LinhThachShown + ")");
            Check(resultText.Contains("Complete the level") && resultText.Contains("target"), "It lists the star conditions and the target time");
            Check(profile.Wallet.Balance == linhBefore + 60 + (director.Level.parTimeSeconds >= director.Result.seconds ? 20 : 0), "The reward was added once (" + (profile.Wallet.Balance - linhBefore) + ")");
            Check(items.Bag.Slots[0].remaining == 2 && profile.Inventory.Count("hoi-khi-dan") == 4, "Unused items stay in the inventory after the level");
            Shot("P09-result"); yield return Frames(3);
            result.Refresh(); yield return new WaitForSecondsRealtime(1.3f);
            Check(profile.Wallet.Balance == linhBefore + 60 + (director.Level.parTimeSeconds >= director.Result.seconds ? 20 : 0), "Refreshing the result does not pay again");
            result.Menu();
            yield return Wait(() => GameSceneManager.Instance.IsLoading, 3f);
            yield return Wait(() => !GameSceneManager.Instance.IsLoading && State.State == UIState.Hub, 15f);
            yield return Frames(4);
            Check(State.State == UIState.Hub && HubUI.Instance.Visible, "\"Back to Hub\" returns to the Hub");
            HubUI.Instance.Select(HubUI.Tab.Map); yield return Frames(3);
            var cards2 = HubUI.Instance.ContentRect.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name.StartsWith("Level ")).ToList();
            Check(cards2[0].interactable, "Level 1 is still open after clearing it");
            Check(HubUI.Instance.ContentRect.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("BEST")), "The map shows the best time");
            Shot("P09-hub-after"); yield return Frames(3);

            // ---- title menu ----
            State.Back(); yield return new WaitForSecondsRealtime(.4f);
            Check(State.State == UIState.Menu, "Esc in the Hub returns to the title menu");
            var title = FindAnyObjectByType<UIManager>().MainMenu;
            var titleButtons = title.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b => b.gameObject.activeSelf).Select(b => b.name).OrderBy(x => x).ToArray();
            Check(titleButtons.SequenceEqual(new[] { "CREDITS", "PLAY", "QUIT", "SETTINGS" }), "The title menu keeps Play, Settings, Credits and Quit");
            Shot("P09-title"); yield return Frames(3);
        }
    }
}
#endif
