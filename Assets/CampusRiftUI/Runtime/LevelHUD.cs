using UnityEngine;
using CampusRift.Levels;
using CampusRift.Localization;

namespace CampusRift.UI
{
    // Writes the level state into the existing objective card: monsters left, wave, rest countdown (P05-T06).
    // Also removes the old breakthrough ring from the HUD and owns the Tầm Yêu markers.
    public sealed class LevelHUD : MonoBehaviour
    {
        public string BodyText { get; private set; } = "";
        public EnemyRevealMarker Reveal { get; private set; }
        GameplayHUD hud; ObjectiveUI objective; float nextRefresh;

        public static bool Vietnamese => LocalizationService.Instance == null || LocalizationService.Instance.Language == GameLanguage.Vietnamese;

        // Called by LevelDirector when a level begins. Safe to call again on retry.
        public static LevelHUD Attach()
        {
            var hud = FindAnyObjectByType<GameplayHUD>(FindObjectsInactive.Include);
            if (hud == null) return null;
            var component = hud.GetComponent<LevelHUD>();
            if (component == null) component = hud.gameObject.AddComponent<LevelHUD>();
            component.Bind(hud);
            return component;
        }

        void Bind(GameplayHUD source)
        {
            hud = source; objective = source.Objective;
            if (source.Breakthrough != null) source.Breakthrough.gameObject.SetActive(false);
            if (Reveal == null) Reveal = EnemyRevealMarker.Create(source.transform);
            Refresh();
        }

        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.15f; Refresh();
        }

        void Refresh()
        {
            var director = LevelDirector.Instance;
            if (director == null || director.Level == null || objective == null || objective.Body == null) return;
            bool vn = Vietnamese; var level = director.Level;
            string title = vn ? "MỤC TIÊU" : "OBJECTIVE";
            string body;
            switch (director.State)
            {
                case LevelDirector.Phase.Intro:
                    body = vn ? $"Màn {level.index}: {level.LocalizedName(true)}\nKhe Nứt sắp mở…" : $"Level {level.index}: {level.LocalizedName(false)}\nThe rift is about to open…"; break;
                case LevelDirector.Phase.Rest:
                    int s = Mathf.CeilToInt(director.RestRemaining);
                    body = vn ? $"Đợt {director.WaveIndex + 1}/{director.WaveCount} đã xong\nĐợt tiếp theo sau {s} giây" : $"Wave {director.WaveIndex + 1}/{director.WaveCount} cleared\nNext wave in {s} s"; break;
                case LevelDirector.Phase.Won:
                    body = vn ? "Hoàn thành màn!" : "Level complete!"; title = vn ? "HOÀN THÀNH" : "COMPLETE"; break;
                case LevelDirector.Phase.Lost:
                    body = vn ? "Thất bại" : "Defeated"; break;
                default:
                    body = vn ? $"Quái còn lại: {director.Remaining}/{director.TotalPlanned}\nĐợt {Mathf.Clamp(director.WaveIndex + 1, 1, director.WaveCount)}/{director.WaveCount}"
                              : $"Monsters left: {director.Remaining}/{director.TotalPlanned}\nWave {Mathf.Clamp(director.WaveIndex + 1, 1, director.WaveCount)}/{director.WaveCount}"; break;
            }
            if(director.AwaitingSkySword){title=vn?"THIÊN KIẾM":"HEAVEN SWORD";body=vn?"Đợt đã sạch · Kiếm Ý đầy\nRa ngoài trời và triệu hồi Thiên Kiếm":"Wave cleared · Sword Intent full\nGo outdoors and summon the Heaven Sword";}
            if(level.runMode==EndgameMode.Tower)title=(vn?"THÁP · TẦNG ":"TOWER · FLOOR ")+level.towerFloor;
            else if(level.runMode==EndgameMode.Nightmare)title=(vn?"ÁC MỘNG · MÀN ":"NIGHTMARE · LEVEL ")+level.index;
            BodyText = body;
            // The meter and beast phase cover this objective on mobile, freeing the ultimate's space.
            bool swordHud=Controls.CampusInput.Mobile&&SkyBeast.HeavenSwordUltimate.Instance!=null&&SkyBeast.HeavenSwordUltimate.Instance.Visible;
            objective.gameObject.SetActive(!swordHud);
            if (objective.Body.text != body) objective.Body.text = body;
            if (objective.Status != null)
            {
                if (objective.Status.text != title) objective.Status.text = title;
                objective.Status.color = director.State == LevelDirector.Phase.Won ? objective.Theme.SuccessColor : objective.Theme.TextSecondary;
            }
        }
    }
}
