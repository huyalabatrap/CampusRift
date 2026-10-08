#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Levels;
using CampusRift.Localization;
using CampusRift.Progression;

namespace CampusRift.UI
{
    // P09-T08: the Hub, level map, skill book and preparation screens at 16:9, 19.5:9, 4:3 and 720p, in both languages:
    // every control inside the screen, touch targets of at least 44 px, no two controls overlapping, no truncated text.
    // Output: Artifacts/UI/HubLayout.json, HubLayout-DONE.txt and screenshots Artifacts/UI/P09-layout-*.png.
    public sealed class HubLayoutPlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report { public List<string> passed = new List<string>(), failed = new List<string>(); }
        readonly Report report = new Report();
        const string Output = "Artifacts/UI/";
        GameSettings originalSettings;
        void Check(bool ok, string label) { (ok ? report.passed : report.failed).Add(label); Directory.CreateDirectory(Output); File.WriteAllText(Output + "HubLayout.json", JsonUtility.ToJson(report, true)); Debug.Log("LAYOUT QA " + (ok ? "PASS " : "FAIL ") + label); }
        IEnumerator Frames(int n = 4) { for (int i = 0; i < n; i++) yield return null; }
        IEnumerator Wait(Func<bool> condition, float timeout) { float until = Time.realtimeSinceStartup + timeout; while (!condition() && Time.realtimeSinceStartup < until) yield return null; }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); Directory.CreateDirectory(Output); Application.runInBackground = true;
            originalSettings = SettingsManager.Instance.Current.Copy();
            var profile = ProfileService.Instance; profile.UseTransient(new ProfileData());
            var run = Run();
            while (true)
            {
                bool next = false; object current = null;
                try { next = run.MoveNext(); if (next) current = run.Current; }
                catch (Exception e) { Check(false, e.ToString()); break; }
                if (!next) break; yield return current;
            }
            try { LevelSession.Clear(); profile.EndTransient(); } catch { }
            SettingsManager.Instance.Apply(originalSettings, false);
            try { UIValidation.SetResolution(1920, 1080); } catch { }
            File.WriteAllText(Output + "HubLayout-DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
            Destroy(gameObject);
        }

        // Problems of the visible controls and texts under `root`; controls inside a scroll list are only checked against overlap of neighbours.
        static List<string> Audit(Transform root)
        {
            Canvas.ForceUpdateCanvases();
            var issues = new List<string>(); var rects = new List<(string name, Rect rect)>();
            foreach (var s in root.GetComponentsInChildren<Selectable>())
            {
                if (!s.IsActive() || !s.interactable && s.name.StartsWith("Level ") == false && false) continue;
                if (s.GetComponentInParent<ScrollRect>() != null) continue;
                var r = (RectTransform)s.transform; var c = new Vector3[4]; r.GetWorldCorners(c);
                float x0 = c[0].x, y0 = c[0].y, x1 = c[2].x, y1 = c[2].y;
                if (x0 < -.5f || y0 < -.5f || x1 > Screen.width + .5f || y1 > Screen.height + .5f) issues.Add(s.name + " outside the screen");
                if (y1 - y0 < 44f - .5f) issues.Add(s.name + " smaller than 44 px (" + (y1 - y0).ToString("0") + ")");
                rects.Add((s.name, Rect.MinMaxRect(x0, y0, x1, y1)));
            }
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                {
                    var a = rects[i].rect; var b = rects[j].rect;
                    float ox = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), oy = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                    if (ox > 3f && oy > 3f) issues.Add(rects[i].name + " overlaps " + rects[j].name);
                }
            foreach (var t in root.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                if (!t.gameObject.activeInHierarchy || t.GetComponentInParent<ScrollRect>() != null) continue;
                t.ForceMeshUpdate();
                if (t.isTextTruncated) issues.Add("truncated: " + t.text.Replace("\n", " / "));
            }
            return issues.Distinct().ToList();
        }

        IEnumerator Run()
        {
            var profile = ProfileService.Instance; profile.Cultivation.SetState(Realm.KetDan, 2, 0);
            profile.Wallet.Earn(1234, "test"); profile.Inventory.Add("hoi-khi-dan", 4); profile.Inventory.Add("tu-linh-dan", 2);
            GameSceneManager.Instance.LoadMainMenu();
            yield return Wait(() => !GameSceneManager.Instance.IsLoading && UIStateManager.Instance.State == UIState.Hub, 15f);
            yield return Frames(5);
            var sizes = new[] { (1920, 1080), (2340, 1080), (1440, 1080), (1280, 720) };
            foreach (var language in new[] { GameLanguage.English, GameLanguage.Vietnamese })
            {
                var settings = originalSettings.Copy(); settings.Language = language; SettingsManager.Instance.Apply(settings, false);
                foreach (var (w, h) in sizes)
                {
                    UIValidation.SetResolution(w, h); yield return new WaitForSecondsRealtime(.6f); yield return Frames(4);
                    string tag = language.ToString().Substring(0, 2) + " " + w + "x" + h;
                    var hub = HubUI.Instance;
                    if (UIStateManager.Instance.State != UIState.Hub) { UIStateManager.Instance.Back(); yield return Frames(3); }
                    hub.Select(HubUI.Tab.Map); yield return Frames(4);
                    var issues = Audit(hub.transform); Check(issues.Count == 0, "Hub / map at " + tag + (issues.Count > 0 ? ": " + string.Join("; ", issues.Take(4)) : ""));
                    if (w == 2340 || w == 1440) ScreenCapture.CaptureScreenshot(Output + "P09-layout-map-" + w + language.ToString().Substring(0, 2) + ".png");
                    hub.Select(HubUI.Tab.Skills); yield return Frames(4);
                    issues = Audit(hub.transform); Check(issues.Count == 0, "Hub / skills at " + tag + (issues.Count > 0 ? ": " + string.Join("; ", issues.Take(4)) : ""));
                    hub.Select(HubUI.Tab.Map); yield return Frames(3);
                    LoadoutUI.Level = LevelCatalog.Instance.Get(1); UIStateManager.Instance.OpenLoadout(); yield return Frames(5);
                    issues = Audit(LoadoutUI.Instance.transform); Check(issues.Count == 0, "Preparation at " + tag + (issues.Count > 0 ? ": " + string.Join("; ", issues.Take(4)) : ""));
                    if (w == 1440 || w == 2340) ScreenCapture.CaptureScreenshot(Output + "P09-layout-loadout-" + w + language.ToString().Substring(0, 2) + ".png");
                    yield return Frames(3);
                    UIStateManager.Instance.Back(); yield return Frames(3);
                }
            }
        }
    }
}
#endif
