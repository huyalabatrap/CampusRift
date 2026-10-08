using System.Text;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Levels;
using CampusRift.Progression;

namespace CampusRift.UI
{
    // Content of the realm ("Cảnh Giới") screen (P06-T07): realm and tier, Tu Vi bar, stats and what opens next.
    // The text is built here from CultivationService so the numbers cannot drift from the rules; LearningUI draws it.
    public sealed class CultivationSummary
    {
        public string title, tiers, bar, stats, nextUnlock, breakthroughHint;
        public float fraction;
        public bool canBreakthrough, bottleneck;
    }

    public static class CultivationUI
    {
        public static CultivationSummary Describe(CultivationService c, bool vn)
        {
            var table = c.Table; var s = new CultivationSummary();
            s.title = table.RealmName(c.Realm, vn) + " - " + (vn ? "Tầng " : "Tier ") + c.Tier + "/" + CultivationTable.TiersPerRealm;
            var dots = new StringBuilder();
            for (int i = 1; i <= CultivationTable.TiersPerRealm; i++) dots.Append(i <= c.Tier ? "● " : "○ ");
            s.tiers = dots.ToString().TrimEnd();
            s.fraction = c.Fraction; s.bottleneck = c.IsBottleneck; s.canBreakthrough = c.CanAttemptBreakthrough;
            s.bar = (vn ? "Tu Vi " : "Cultivation ") + c.TuViText + " / " + c.TuViPerTier;
            s.stats = (vn ? "Máu " : "Health ") + Mathf.RoundToInt(c.MaxHealth) + "   " + (vn ? "Công " : "Attack ") + Mathf.RoundToInt(c.Attack) + "   "
                + (vn ? "Linh Lực " : "Spirit ") + Mathf.RoundToInt(c.MaxSpirit) + "\n" + (vn ? "Phòng thủ " : "Defense ") + Mathf.RoundToInt(c.Defense * 100) + "%   "
                + (vn ? "Tốc chạy " : "Run speed ") + "+" + (c.RunBonus * 100).ToString("0.#") + "%";
            s.nextUnlock = NextUnlock(c, vn);
            s.breakthroughHint = c.IsLastRealm
                ? (vn ? "Đã đến cảnh giới cuối. Tầng cao hơn đến từ ôn tập, luyện tập và chơi màn." : "Final realm. Higher tiers come from review, practice and levels.")
                : c.CanAttemptBreakthrough ? (vn ? "Bình cảnh - hãy Thi Đột Phá để lên " + table.RealmName((Realm)(c.RealmIndex + 1), true) : "Bottleneck - take the Breakthrough Exam to reach " + table.RealmName((Realm)(c.RealmIndex + 1), false))
                : (vn ? "Đầy Tu Vi tầng 5 để mở Thi Đột Phá." : "Fill tier 5 to unlock the Breakthrough Exam.");
            return s;
        }

        // The first level still closed by cultivation, and the skills of the next realm.
        static string NextUnlock(CultivationService c, bool vn)
        {
            var text = new StringBuilder();
            var catalog = LevelCatalog.Instance;
            if (catalog != null)
                foreach (var level in catalog.levels)
                {
                    if (level == null || c.AtLeast(level.requiredRealm, level.requiredTier)) continue;
                    string need = c.Table.TierName((Realm)Mathf.Clamp(level.requiredRealm, 0, c.Table.RealmCount - 1), level.requiredTier, vn);
                    text.Append(vn ? $"Màn {level.index}: {level.LocalizedName(true)} - cần {need}" : $"Level {level.index}: {level.LocalizedName(false)} - needs {need}");
                    break;
                }
            if (!c.IsLastRealm)
            {
                var next = c.Table.Row((Realm)(c.RealmIndex + 1));
                if (text.Length > 0) text.Append('\n');
                text.Append(vn ? $"Kỹ năng ở {next.nameVN}: {next.skillsVN}" : $"Skills at {next.nameEN}: {next.skillsEN}");
            }
            return text.Length > 0 ? text.ToString() : (vn ? "Đã mở hết." : "Everything is open.");
        }

        // Two stacked images make a progress bar that fits any layout width.
        public static RectTransform CreateBar(Transform parent, float fraction, Color back, Color fill, float height = 28)
        {
            var go = new GameObject("Tu Vi Bar", typeof(RectTransform), typeof(LayoutElement), typeof(Image)); go.layer = 5;
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var bg = go.GetComponent<Image>(); bg.color = back; bg.raycastTarget = false;
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fillGo.layer = 5;
            var fillRect = fillGo.GetComponent<RectTransform>(); fillRect.SetParent(rect, false);
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1); fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var image = fillGo.GetComponent<Image>(); image.color = fill; image.raycastTarget = false;
            return rect;
        }
    }
}
