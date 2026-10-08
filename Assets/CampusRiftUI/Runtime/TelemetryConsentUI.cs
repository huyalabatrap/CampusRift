using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CampusRift.Progression;
namespace CampusRift.UI
{
    // Asked on the first menu visit. Closing/back has the same effect as declining.
    public sealed class TelemetryConsentUI : MonoBehaviour
    {
        RectTransform card;
        public bool Visible => card != null && card.gameObject.activeSelf;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; Create(); }
        static void Loaded(Scene scene, LoadSceneMode mode) { Create(); }
        static void Create() { if (FindAnyObjectByType<TelemetryConsentUI>() == null) new GameObject("Telemetry consent").AddComponent<TelemetryConsentUI>(); }
        void Update()
        {
            var settings = SettingsManager.Instance; var ui = UIStateManager.Instance;
            Shader.SetGlobalFloat("_CampusReduceFlashes", settings?.Current.ReduceSkillFlashes == true ? 1 : 0);
            if (settings == null || ui == null) return;
#if UNITY_EDITOR
            if (TutorialDirector.Suppress) { if (card != null) card.gameObject.SetActive(false); return; }
#endif
            bool show = !settings.Current.TelemetryConsentAsked && (ui.State == UIState.Menu || ui.State == UIState.Hub) && !(ProfileService.Instance?.Transient ?? false);
            if (show && card == null) Build();
            if (card != null) card.gameObject.SetActive(show);
            if(show&&UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true)Choose(false);
        }
        public void Build()
        {
            if (card != null) return;
            var kit = UiKit.Create(); if (kit == null) return;
            var go = new GameObject("Consent canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); go.transform.SetParent(transform,false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
            var fit = kit.Fit(go.transform,"Consent fit");
            var shade = kit.Image(fit,"Shade",0,0,1920,1080,new Color(0,0,0,.65f),null,true);
            card = kit.Panel(fit,"Local data consent",490,270,940,520);
            bool vn = LevelHUD.Vietnamese;
            kit.Text(card,vn?"GHI DỮ LIỆU CHƠI?":"RECORD PLAY DATA?",30,24,880,66,40,UiKit.Gold);
            kit.Text(card,vn?"Bạn có muốn lưu thống kê màn chơi và tỉ lệ trả lời đúng để hỗ trợ cân bằng game?\n\nMặc định tắt. Chỉ lưu trên máy này, không gửi qua mạng và không lưu tên hay thông tin cá nhân. Bạn có thể tắt hoặc xóa file trong Cài đặt → Tiện nghi & dữ liệu.":"Save level statistics and question accuracy to help improve the game?\n\nOff by default. Files stay on this device; nothing is sent online and no name or personal information is stored. Disable or delete them in Settings → Comfort & data.",30,110,880,250,27);
            kit.Button(card,vn?"KHÔNG GHI":"KEEP OFF",30,402,420,78,()=>Choose(false));
            kit.Button(card,vn?"ĐỒNG Ý, LƯU CỤC BỘ":"AGREE, SAVE LOCALLY",490,402,420,78,()=>Choose(true),true,true,24);
            shade.transform.SetParent(card,false); shade.transform.SetAsFirstSibling(); shade.rectTransform.anchoredPosition=new Vector2(-490,270);
        }
        public void Choose(bool enabled)
        {
            var s = SettingsManager.Instance.Current.Copy(); s.LocalTelemetryEnabled=enabled; s.TelemetryConsentAsked=true; SettingsManager.Instance.Apply(s); if(card!=null)card.gameObject.SetActive(false);
        }
    }
}
