#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CampusRift.UI;
using UnityEngine;

namespace CampusRift.Levels
{
    // Runs the existing Play Mode harnesses one after another, each in a freshly loaded gameplay scene,
    // and collects their reports. Used for the V2 baseline (P00-T02) and later full regressions (P17, P23).
    public sealed class V2RegressionRunner : MonoBehaviour
    {
        [Serializable] sealed class SuiteResult { public string name, status, summary, report; public float seconds; }
        [Serializable] sealed class Summary { public string started, finished; public List<SuiteResult> suites = new List<SuiteResult>(); }

        sealed class Suite
        {
            public string Name; public Type Harness; public string Done; public string DoneContains; public string Report;
            public Action<Component> Configure; public bool OwnSceneFlow; public Action Launch; public float Timeout = 300;
        }

        public string outputFolder = "Artifacts/V2/Baseline/";
        public string only = ""; // comma-separated suite names; empty runs everything
        readonly Summary summary = new Summary();
        GameSettings originalSettings;

        public static V2RegressionRunner Begin(string folder, string only = "")
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var go = new GameObject("V2 regression runner (Editor only)"); DontDestroyOnLoad(go);
            var runner = go.AddComponent<V2RegressionRunner>(); runner.outputFolder = folder; runner.only = only ?? "";
            return runner;
        }

        List<Suite> Suites()
        {
            var list = new List<Suite>
            {
                new Suite { Name = "MobileControls", Harness = typeof(Controls.MobileControlPlayTest), Done = "Artifacts/MobileControls/DONE.txt", Report = "Artifacts/MobileControls/Validation.json" },
                new Suite { Name = "MobileChase", Harness = typeof(Controls.MobileChasePlayTest), Done = "Artifacts/MobileControls/Chase.txt", DoneContains = "Twenty", Report = "Artifacts/MobileControls/Chase.txt" },
                new Suite { Name = "BoostEnergy", Harness = typeof(Controls.BoostEnergyPlayTest), Done = "Artifacts/BoostEnergy/DONE.txt", Report = "Artifacts/BoostEnergy/Validation.json" },
                new Suite { Name = "GiantHand", Harness = typeof(Skills.GiantHandPlayTest), Done = "Artifacts/GiantHandSeal/DONE.txt", Report = "Artifacts/GiantHandSeal/Validation.json", Timeout = 420 },
                new Suite { Name = "VoidWall", Harness = typeof(Skills.VoidWallPlayTest), Done = "Artifacts/VoidWall/DONE.txt", Report = "Artifacts/VoidWall/Validation.json" },
                new Suite { Name = "VoidWallQuickCast", Harness = typeof(Skills.VoidWallQuickCastPlayTest), Done = "Artifacts/VoidWall/QuickCast-DONE.txt", Report = "Artifacts/VoidWall/QuickCast.json" },
                new Suite { Name = "PhantomDecoy", Harness = typeof(Skills.PhantomDecoyWorldPlayTest), Done = "Artifacts/PhantomDecoy/WorldValidation-DONE.txt", Report = "Artifacts/PhantomDecoy/WorldValidation.json" },
                new Suite { Name = "ShabanCombat", Harness = typeof(ShabanCombatPlayTest), Done = "Artifacts/Combat/DONE.txt", Report = "Artifacts/Combat/Validation.json" },
                new Suite { Name = "ShabanPressure", Harness = typeof(ShabanPressurePlayTest), Done = "Artifacts/ShabanPressure/DONE.txt", Report = "Artifacts/ShabanPressure/Validation.json" },
                new Suite { Name = "ShabanBehavior", Harness = typeof(ShabanBehaviorPlayTest), Done = "Temp/shaban-behavior-progress.txt", DoneContains = "DONE", Report = "Temp/shaban-behavior-progress.txt" },
                new Suite { Name = "ShabanNavigation", Harness = typeof(ShabanNavigationPlayTest), Done = "Temp/shaban-nav-progress.txt", DoneContains = "DONE", Report = "Temp/shaban-nav-progress.txt", Timeout = 600 },
                new Suite { Name = "ShabanTraversal", Harness = typeof(ShabanTraversalPlayTest), Done = "Temp/shaban-traversal-progress.txt", DoneContains = "DONE", Report = "Temp/shaban-traversal-progress.txt", Timeout = 900 },
            };
            foreach (var scenario in new[] { "elevator", "elevator-return", "stairs", "room", "sprint" })
            {
                string file = "Temp/shaban-scenario-" + scenario + ".txt", name = scenario;
                list.Add(new Suite
                {
                    Name = "ShabanHunt-" + scenario, Harness = typeof(ShabanHuntScenarioTest), Done = file, DoneContains = "DONE", Report = file,
                    Configure = c => { var t = (ShabanHuntScenarioTest)c; t.scenario = name; t.output = file; }
                });
            }
            // V2 harnesses (added phase by phase).
            list.Add(new Suite { Name = "DamagePipeline", Harness = typeof(Combat.DamagePipelinePlayTest), Done = "Artifacts/Combat/Pipeline-DONE.txt", Report = "Artifacts/Combat/Pipeline.json" });
            list.Add(new Suite { Name = "MinionCombat", Harness = typeof(Enemies.MinionCombatPlayTest), Done = "Artifacts/Enemies/MinionCombat-DONE.txt", Report = "Artifacts/Enemies/MinionCombat.json", Timeout = 420 });
            list.Add(new Suite { Name = "LevelFlow", Harness = typeof(LevelFlowPlayTest), Done = "Artifacts/Levels/LevelFlow-DONE.txt", Report = "Artifacts/Levels/LevelFlow.json", Timeout = 360 });
            list.Add(new Suite { Name = "Learning", Harness = typeof(Learning.LearningPlayTest), Done = "Artifacts/Learning/PlayMode-DONE.txt", Report = "Artifacts/Learning/PlayMode.json", Timeout = 240 });
            list.Add(new Suite { Name = "BreakthroughExam", Harness = typeof(Learning.BreakthroughExamPlayTest), Done = "Artifacts/Learning/Breakthrough-DONE.txt", Report = "Artifacts/Learning/Breakthrough.json", Timeout = 120 });
            list.Add(new Suite { Name = "Economy", Harness = typeof(Progression.EconomyPlayTest), Done = "Artifacts/Economy/Economy-DONE.txt", Report = "Artifacts/Economy/Economy.json", Timeout = 180 });
            list.Add(new Suite { Name = "HubFlow", Harness = typeof(UI.HubFlowPlayTest), Done = "Artifacts/UI/HubFlow-DONE.txt", Report = "Artifacts/UI/HubFlow.json", Timeout = 300 });
            list.Add(new Suite { Name = "HubLayout", Harness = typeof(UI.HubLayoutPlayTest), Done = "Artifacts/UI/HubLayout-DONE.txt", Report = "Artifacts/UI/HubLayout.json", Timeout = 300 });
            list.Add(new Suite { Name = "PlayerCombat", Harness = typeof(Combat.PlayerCombatPlayTest), Done = "Artifacts/Combat/PlayerCombat-DONE.txt", Report = "Artifacts/Combat/PlayerCombat.json", Timeout = 240 });
            list.Add(new Suite { Name = "SkillLoadout", Harness = typeof(Skills.SkillLoadoutPlayTest), Done = "Artifacts/Skills/Loadout-DONE.txt", Report = "Artifacts/Skills/Loadout.json" });
            list.Add(new Suite { Name = "SkyVictory", Harness = typeof(SkyVictoryPlayTest), Done = "Artifacts/SkyVictory/DONE.txt", Report = "Artifacts/SkyVictory/Validation.json" });
            return list;
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            Directory.CreateDirectory(outputFolder);
            summary.started = DateTime.Now.ToString("s");
            originalSettings = SettingsManager.Instance.Current.Copy();
            var filter = new HashSet<string>(only.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            foreach (var suite in Suites())
            {
                if (filter.Count > 0 && !filter.Contains(suite.Name)) continue;
                yield return Run(suite);
                Write();
            }
            SettingsManager.Instance.Apply(originalSettings.Copy(), false);
            if (GameSceneManager.Instance != null) GameSceneManager.Instance.LoadMainMenu();
            summary.finished = DateTime.Now.ToString("s"); Write();
            File.WriteAllText(outputFolder + "DONE.txt", summary.suites.Count + " suites finished at " + summary.finished);
            Destroy(gameObject);
        }

        IEnumerator Run(Suite suite)
        {
            var result = new SuiteResult { name = suite.Name };
            summary.suites.Add(result);
            File.WriteAllText(outputFolder + "PROGRESS.txt", "Running " + suite.Name);
            float begin = Time.realtimeSinceStartup;
            var stamp = File.Exists(suite.Done) ? File.GetLastWriteTimeUtc(suite.Done) : DateTime.MinValue;
            if (suite.Done.StartsWith("Temp/") && File.Exists(suite.Done)) File.Delete(suite.Done);

            // Every suite starts from a freshly loaded gameplay scene with the user's settings, so an earlier
            // harness (e.g. MobileChase switching to touch controls) cannot leak state into the next one.
            SettingsManager.Instance.Apply(originalSettings.Copy(), false);
            // Input System disables devices when the unfocused editor loses focus; harnesses that drive
            // Keyboard.current (e.g. VoidWallQuickCast) then silently receive nothing.
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
                if (!device.enabled) UnityEngine.InputSystem.InputSystem.EnableDevice(device);
            GameSceneManager.Instance.StartSandbox();
            float until = Time.realtimeSinceStartup + 90;
            while ((GameSceneManager.Instance.IsLoading || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity")
                   && Time.realtimeSinceStartup < until) yield return null;
            UIStateManager.Instance.EnterScene(true);
            yield return null; yield return null;

            var host = new GameObject("V2 regression / " + suite.Name);
            var component = host.AddComponent(suite.Harness);
            suite.Configure?.Invoke(component);

            until = Time.realtimeSinceStartup + suite.Timeout;
            while (!Finished(suite, stamp) && Time.realtimeSinceStartup < until) yield return null;
            result.seconds = Mathf.Round(Time.realtimeSinceStartup - begin);
            if (!Finished(suite, stamp)) { result.status = "TIMEOUT"; result.summary = "No completion marker after " + suite.Timeout + " s"; }
            else
            {
                result.summary = Read(suite.Done, 600);
                result.status = result.summary.Contains(" 0 failed") ? "PASS" : result.summary.Contains("failed") ? "FAIL" : "DONE";
            }
            if (File.Exists(suite.Report))
            {
                result.report = outputFolder + suite.Name + Path.GetExtension(suite.Report);
                File.Copy(suite.Report, result.report, true);
            }
            if (host != null) Destroy(host);
            yield return null;
        }

        static bool Finished(Suite suite, DateTime stamp)
        {
            if (!File.Exists(suite.Done) || File.GetLastWriteTimeUtc(suite.Done) == stamp) return false;
            return string.IsNullOrEmpty(suite.DoneContains) || Read(suite.Done, 1000000).Contains(suite.DoneContains);
        }

        static string Read(string path, int max)
        {
            try { var text = File.ReadAllText(path); return text.Length > max ? text.Substring(0, max) : text; }
            catch (IOException) { return ""; }
        }

        void Write() => File.WriteAllText(outputFolder + "Summary.json", JsonUtility.ToJson(summary, true));
    }
}
#endif
