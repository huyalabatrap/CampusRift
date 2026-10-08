#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Controls;
using CampusRift.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CampusRift.Skills
{
    // P02: the four-slot loadout drives PC keys, the PC skill bar and the mobile buttons.
    // Writes Artifacts/Skills/Loadout.json + Loadout-DONE.txt.
    public sealed class SkillLoadoutPlayTest : MonoBehaviour
    {
        [Serializable] sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(); }
        const string Output = "Artifacts/Skills/";
        readonly Report report = new Report();
        InputSettings originalInput, testInput;
        GameSettings originalSettings;
        bool running;

        void Update() { if (running && UIStateManager.Instance != null && UIStateManager.Instance.State != UIState.Gameplay) UIStateManager.Instance.EnterScene(true); }
        void Check(bool ok, string label)
        {
            (ok ? report.passed : report.failed).Add(label);
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "Loadout.json", JsonUtility.ToJson(report, true));
            Debug.Log("LOADOUT QA " + (ok ? "PASS " : "FAIL ") + label);
        }
        static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
        static IEnumerator Frames(int n = 2) { for (int i = 0; i < n; i++) yield return null; }
        void Mode(ControlMode mode) { var s = SettingsManager.Instance.Current.Copy(); s.ControlMode = mode; SettingsManager.Instance.Apply(s, false); }

        IEnumerator Start()
        {
            running = true; Application.runInBackground = true;
            originalSettings = SettingsManager.Instance.Current.Copy();
            originalInput = InputSystem.settings; testInput = Instantiate(originalInput); InputSystem.settings = testInput;
            testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            foreach (var d in InputSystem.devices) if (!d.enabled) InputSystem.EnableDevice(d);
            yield return null;
            var run = Run();
            while (true)
            {
                bool next; object current = null;
                try { next = run.MoveNext(); if (next) current = run.Current; }
                catch (Exception e) { Check(false, "Exception: " + e); break; }
                if (!next) break; yield return current;
            }
            Keys();
            SettingsManager.Instance.Apply(originalSettings, false);
            InputSystem.settings = originalInput; Destroy(testInput);
            running = false;
            File.WriteAllText(Output + "Loadout-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
        }

        IEnumerator Run()
        {
            Mode(ControlMode.PC);
            var player = FindAnyObjectByType<CampusExplorer>();
            var brain = FindAnyObjectByType<Monsters.MonsterBrain>(); if (brain != null) brain.gameObject.SetActive(false);
            var loadout = player.GetComponent<SkillLoadout>(); var input = player.GetComponent<CampusInput>();
            var wall = player.GetComponent<VoidWallSkill>(); var phantom = player.GetComponent<PhantomDecoySkill>();
            var bar = FindAnyObjectByType<SkillBarUI>(); var mobile = FindAnyObjectByType<MobileControlsHUD>();
            player.ReturnToSpawn(); yield return Frames(3);

            // 1. Default loadout and key labels.
            Check(loadout.Get(0) is VoidWallRuntime && loadout.Get(1) is PhantomRuntime && loadout.Get(2) == null && loadout.Get(3) is GiantHandRuntime,
                "Default loadout: Q Void Wall, E Phantom, R empty, F Giant Hand");
            Check(input.KeyLabel(CampusAction.Wall) == "Q" && input.KeyLabel(CampusAction.Phantom) == "E" && input.KeyLabel(CampusAction.Hand) == "F" &&
                  input.KeyLabel(CampusAction.Interact) == "G", "Key labels follow the loadout; interact moved to G");
            Check(bar.Slots.Length == SkillLoadout.SlotCount && bar.Slots.All(s => s != null && s.gameObject.activeSelf), "PC skill bar shows four slots");
            float x0 = ((RectTransform)bar.Slots[0].transform).anchoredPosition.x, x3 = ((RectTransform)bar.Slots[3].transform).anchoredPosition.x;
            Check(x3 > x0 && bar.Slots[3].KeyLabel.text.StartsWith("F"), "Slots are laid out left to right in Q, E, R, F order");

            // 2. PC keys reach the slotted skill.
            Keys(Key.E); yield return Frames();
            bool phantomAimed = phantom.IsPreviewing; Keys(); yield return Frames();
            phantom.CancelPreview(); yield return Frames();
            Check(phantomAimed, "E opens the Phantom preview (Phantom sits in slot 2)");
            int charges = wall.Charges;
            Keys(Key.Q); yield return Frames(); Keys(); yield return Frames(3);
            Check(wall.Charges == charges - 1, "Q tap still deploys Void Wall from slot 1");

            // 3. Moving a skill moves its key, bar slot and mobile button.
            Check(loadout.Equip(2, "anh-phan-than"), "Equip moves Phantom to slot 3");
            yield return Frames();
            Check(loadout.Get(1) == null && loadout.Get(2) is PhantomRuntime && input.KeyLabel(CampusAction.Phantom) == "R", "Phantom now answers to R and slot 2 is empty");
            Keys(Key.E); yield return Frames(); bool eIgnored = !phantom.IsPreviewing; Keys(); yield return Frames();
            Keys(Key.R); yield return Frames(); bool rAims = phantom.IsPreviewing; Keys(); yield return Frames();
            phantom.CancelPreview(); yield return Frames();
            Check(eIgnored && rAims, "E no longer triggers Phantom; R does");
            var phantomHud = FindAnyObjectByType<PhantomDecoyHUD>();
            Check(bar.Slots[2] == phantomHud.slot && bar.Slots[2].KeyLabel.text.StartsWith("R"), "Phantom's authored slot moved to position 3 and shows R");
            Check(bar.Slots[1] != phantomHud.slot && bar.Slots[1].gameObject.activeSelf && bar.Slots[1].KeyLabel.text == "E", "Position 2 shows an empty E slot");

            // 4. Unequipped skills ignore input.
            loadout.Equip(0, ""); yield return Frames();
            charges = wall.Charges;
            Keys(Key.Q); yield return Frames(); Keys(); yield return Frames(3);
            Check(wall.Charges == charges && !wall.IsPreviewing && input.KeyLabel(CampusAction.Wall) == "", "Unequipped Void Wall ignores Q and has no key");
            Check(!FindAnyObjectByType<VoidWallHUD>().slot.gameObject.activeSelf, "Unequipped skill's slot is hidden");

            // 5. Mobile buttons follow the loadout.
            Mode(ControlMode.Mobile); yield return Frames(3);
            Check(mobile.RoleFor(CampusAction.Phantom) == TouchRole.Skill3 && mobile.RoleFor(CampusAction.Hand) == TouchRole.Skill4, "Mobile: Phantom on button 3, Hand on button 4");
            var face = mobile.Zones.First(z => z.role == TouchRole.Skill3).GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => t.text == Localization.LocalizationService.T("PHANTOM") || t.text == "PHANTOM");
            Check(face, "Mobile button 3 is labelled with the Phantom");
            bool aim = input.BeginAim(CampusAction.Skill3);
            Check(aim && phantom.IsPreviewing && input.Aiming == CampusAction.Phantom, "Touching button 3 aims the Phantom (aiming keeps its identity)");
            input.EndAim(true); yield return Frames();
            Check(!phantom.IsPreviewing && !input.Aiming.HasValue, "Cancelling the touch cancels the preview");
            Check(!input.BeginAim(CampusAction.Skill1), "An empty slot cannot be aimed");
            bool queued = true; try { input.Press(CampusAction.LockOn); input.Pressed(CampusAction.LockOn); } catch (Exception) { queued = false; }
            Check(queued, "Queued-press array covers every action (no fixed size)");
            Mode(ControlMode.PC); yield return Frames();

            // 6. Restore the default loadout.
            loadout.Equip(0, "hu-khong-ket-gioi"); loadout.Equip(1, "anh-phan-than"); loadout.Equip(2, ""); loadout.Equip(3, "dai-thu-an");
            yield return Frames();
            Check(loadout.Get(0) is VoidWallRuntime && loadout.Get(1) is PhantomRuntime && loadout.Get(2) == null && input.KeyLabel(CampusAction.Wall) == "Q",
                "Default loadout restored");
            if (brain != null) brain.gameObject.SetActive(true);
        }
    }
}
#endif
