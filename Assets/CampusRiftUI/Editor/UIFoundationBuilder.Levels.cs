using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// V2 (P05-T07): turns the old "You defeated Shaban" victory card into the level result card.
public static partial class UIFoundationBuilder
{
    [MenuItem("Campus Rift/V2/Build Level Result Card")]
    public static string BuildLevelResult()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        Prepare();
        var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); string originalPath = original.path;
        if (original.isDirty) EditorSceneManager.SaveScene(original);
        var scene = EditorSceneManager.OpenScene(GamePath);
        var ui = Object.FindAnyObjectByType<UIManager>();
        string log;
        if (ui.Victory == null) log = "no Victory panel";
        else
        {
            var panel = ui.Victory; var card = panel.transform.Find("Victory Card");
            var result = panel.GetComponent<LevelResultUI>();
            if (result != null)
            {
                foreach (var name in new[] { "Star 1", "Star 2", "Star 3", "REPLAY", "NEXT LEVEL", "MAIN MENU" }) { var old = card.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject); }
                Object.DestroyImmediate(result); result = null; log = "rebuilt";
            }
            {
                result = panel.gameObject.AddComponent<LevelResultUI>();
                foreach (var name in new[] { "PLAY AGAIN", "MAIN MENU" }) { var old = card.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject); }
                result.Eyebrow = card.Find("Eyebrow").GetComponent<TMP_Text>();
                result.Title = card.Find("Title").GetComponent<TMP_Text>();
                result.Stats = card.Find("Description").GetComponent<TMP_Text>();
                result.Title.enableAutoSizing = true; result.Title.fontSizeMin = 36; result.Title.fontSizeMax = 72;
                var statsRect = result.Stats.rectTransform; statsRect.anchoredPosition = new Vector2(40, -232); statsRect.sizeDelta = new Vector2(770, 90);
                result.Stats.fontSize = 24; result.Stats.enableAutoSizing = true; result.Stats.fontSizeMin = 16; result.Stats.fontSizeMax = 24;
                result.Stats.alignment = TextAlignmentOptions.Center;
                for (int i = 0; i < 3; i++)
                {
                    var star = Image(card, "Star " + (i + 1), 425 - 15 + (i - 1) * 64, 322, 30, 30, result.StarOff);
                    star.rectTransform.pivot = new Vector2(0.5f, 0.5f); star.rectTransform.anchoredPosition += new Vector2(15, -15);
                    star.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                    result.Stars[i] = star;
                }
                var replay = Button(card, "REPLAY", "REPLAY", 45, 375, 250, 64, result.Replay);
                var next = Button(card, "NEXT LEVEL", "NEXT LEVEL", 300, 375, 250, 64, result.Next, true);
                Button(card, "MAIN MENU", "MAIN MENU", 555, 375, 250, 64, result.Menu);
                result.NextButton = next.gameObject; panel.FirstSelection = next;
                log = "built";
            }
            EditorUtility.SetDirty(panel); EditorUtility.SetDirty(ui);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if (!string.IsNullOrEmpty(originalPath)) EditorSceneManager.OpenScene(originalPath);
        return log;
    }
}
