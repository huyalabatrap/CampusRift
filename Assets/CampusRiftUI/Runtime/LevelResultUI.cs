using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Levels;

namespace CampusRift.UI
{
    // Victory card content: time, monsters defeated and a star row (P05-T07). The star rules arrive with P12; for now
    // only the "completed" star is lit. Buttons: replay, next level, back to the menu (the hall arrives with P09).
    public sealed class LevelResultUI : MonoBehaviour
    {
        public TMP_Text Eyebrow, Title, Stats;
        public Image[] Stars = new Image[3];
        public GameObject NextButton;
        public Color StarOn = new Color(1f, 0.82f, 0.32f), StarOff = new Color(0.25f, 0.28f, 0.36f);

        void OnEnable() { Refresh(); }
        Coroutine counting;
        string baseText;
        int linhShown, linhTarget; float tuViTarget;
        public int LinhThachShown => linhShown;

        public void Refresh()
        {
            var director = LevelDirector.Instance;
            if (director == null || director.Level == null) return;
            var result = director.Result; var level = director.Level;
            bool vn = LevelHUD.Vietnamese;
            string time = FormatTime(result.seconds);
            if (Eyebrow != null) Eyebrow.text = vn ? "KHE NỨT ĐÃ ĐƯỢC DẸP YÊN" : "THE RIFT IS QUIET";
            if (Title != null) Title.text = vn ? $"HOÀN THÀNH MÀN {level.index}" : $"LEVEL {level.index} COMPLETE";
            if(Title!=null&&level.runMode!=EndgameMode.Normal)Title.text=level.LocalizedName(vn);
            bool timeStar = (result.starMask&2)!=0;
            var special = level.stars != null && level.stars.Length > 2 ? level.stars[2] : default(StarCondition);
            string specialText = vn ? special.descriptionVN : special.descriptionEN;
            var lines = new System.Collections.Generic.List<string>
            {
                level.LocalizedName(vn),
                vn ? $"Thời gian {time}  (mốc {FormatTime(level.parTimeSeconds)})   ·   Hạ {result.kills}/{result.total} quái" : $"Time {time}  (target {FormatTime(level.parTimeSeconds)})   ·   Defeated {result.kills}/{result.total}",
                (result.won ? "[x] " : "[ ] ") + (vn ? "Hoàn thành màn" : "Complete the level"),
                (timeStar ? "[x] " : "[ ] ") + (vn ? $"Hoàn thành trong {FormatTime(level.parTimeSeconds)}" : $"Finish within {FormatTime(level.parTimeSeconds)}"),
                ((result.starMask&4)!=0?"[x] ":"[ ] ") + specialText,
            };
            var levels = Progression.LevelProgressService.Current;
            linhTarget = result.won && levels != null ? levels.LastLinhThach : 0;
            if(level.runMode!=EndgameMode.Normal)linhTarget=result.won?Progression.EndgameService.LastReward:0;
            tuViTarget = result.won && director.ClearAward != null ? director.ClearAward.tuVi : 0;
            int due = LearningService_DueCount();
            if (due > 0) lines.Add((vn ? "Bài nên ôn: " : "Reviews due: ") + due);
            baseText = string.Join("\n", lines);
            for (int i = 0; i < Stars.Length; i++)
                if (Stars[i] != null)
                {
                    bool on = (result.starMask&(1<<i))!=0;
                    var art = ContentImages.Star(on);
                    if (art != null) { Stars[i].sprite = art; Stars[i].preserveAspect = true; Stars[i].color = Color.white; }
                    else Stars[i].color = on ? StarOn : StarOff;
                }
            if (NextButton != null) {NextButton.SetActive(level.runMode==EndgameMode.Tower||level.index < LevelSession.LastIndex);var label=NextButton.GetComponentInChildren<TMP_Text>();if(label!=null)label.text=level.runMode==EndgameMode.Tower?(vn?"TẦNG TIẾP":"NEXT FLOOR"):(vn?"MÀN TIẾP":"NEXT LEVEL");}
            if (Stats != null) { Stats.enableAutoSizing = true; Stats.fontSizeMax = 22; Stats.fontSizeMin = 11; }
            if (counting != null) StopCoroutine(counting);
            linhShown = 0;
            if (Stats != null) { Stats.text = baseText; if (linhTarget > 0 || tuViTarget > 0) counting = StartCoroutine(CountUp()); }
        }

        static int LearningService_DueCount() => Learning.LearningService.Instance != null ? Learning.LearningService.Instance.Engine.DueReviewCount : 0;

        // The rewards count up over a second; the numbers are already final in the profile, this is only the presentation.
        System.Collections.IEnumerator CountUp()
        {
            bool vn = LevelHUD.Vietnamese; float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime; float k = Mathf.Clamp01(t / 1f);
                linhShown = Mathf.RoundToInt(linhTarget * k);
                Stats.text = baseText + "\n+" + linhShown + " Linh Thạch" + (tuViTarget > 0 ? "   +" + (tuViTarget * k).ToString("0.#") + " Tu Vi" : "");
                yield return null;
            }
            linhShown = linhTarget;
            Stats.text = baseText + "\n+" + linhTarget + " Linh Thạch" + (tuViTarget > 0 ? "   +" + tuViTarget.ToString("0.#") + " Tu Vi" : "");
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        public void Replay() { GameSceneManager.Instance.RetryLevel(); }
        public void Next() { GameSceneManager.Instance.NextLevel(); }
        public void Menu() { GameSceneManager.Instance.LoadMainMenu(); }   // lands in the Hub
    }
}
