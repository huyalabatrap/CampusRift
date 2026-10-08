using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Progression;

namespace CampusRift.UI
{
    public sealed partial class SettingsUI
    {
        public Toggle DevModeToggle { get; private set; }
        public Toggle DevInvincibleToggle { get; private set; }
        public Toggle DevNoCooldownToggle { get; private set; }
        public Button DeveloperTab { get; private set; }
        public Button VersionButton { get; private set; }
        public GameObject DevConfirmation { get; private set; }
        public int VersionTaps { get; private set; }
        int developerIndex;
        bool DeveloperAvailable => DevMode.DeveloperVisible(Application.isEditor && !SimulateRelease, Debug.isDebugBuild && !SimulateRelease, VersionTaps);
        bool SimulateRelease {
            get {
#if UNITY_EDITOR
                return releasePolicyForValidation;
#else
                return false;
#endif
            }
        }
#if UNITY_EDITOR
        bool releasePolicyForValidation;
        public void UseReleasePolicyForValidation(bool value) { releasePolicyForValidation = value; VersionTaps = 0; RefreshDeveloperSettings(); }
#endif
        public const string DevWarning = "Dev Mode mở khóa mọi thứ để thử nghiệm. Tiến trình trong lúc bật sẽ không được lưu. Tắt để trở về hồ sơ thật.";

        void BuildDeveloperSettings()
        {
            var kit = UiKit.Create(); if (kit == null || Tabs.Length == 0) return;
            var parent = Tabs[0].transform.parent;
            developerIndex = Tabs.Length;
            var page = kit.Rect(parent, "Developer settings", 48, 255, 1024, 415);
            var pages = new List<GameObject>(Tabs); pages.Add(page.gameObject); Tabs = pages.ToArray();
            DeveloperTab = kit.Button(parent, "NHÀ PHÁT TRIỂN", 48, 163, 200, 68, () => SelectTab(developerIndex), true, false, 19);
            var lines = new List<Image>(TabLines); lines.Add(DeveloperTab.GetComponent<Image>()); TabLines = lines.ToArray();
            kit.Text(page, "NHÀ PHÁT TRIỂN", 0, 0, 1024, 50, 30, UiKit.Gold);
            Toggle Row(string name, float y)
            {
                var t = Instantiate(InvertY, page); t.name = name; t.onValueChanged.RemoveAllListeners();
                var r = (RectTransform)t.transform; r.anchoredPosition = new Vector2(0, -y); r.sizeDelta = new Vector2(1024, 60);
                var label = t.GetComponentInChildren<TMP_Text>(true); label.text = name; label.rectTransform.sizeDelta = new Vector2(840, 60); ComicTheme.Text(label);
                t.targetGraphic.rectTransform.anchorMin = t.targetGraphic.rectTransform.anchorMax = new Vector2(1, .5f);
                t.targetGraphic.rectTransform.anchoredPosition = new Vector2(-36, 0);
                return t;
            }
            DevModeToggle = Row("Dev Mode · Mở khóa tất cả / tài nguyên vô hạn", 64);
            DevInvincibleToggle = Row("Bất tử", 148);
            DevNoCooldownToggle = Row("Bỏ hồi chiêu", 224);
            kit.Text(page, "Tiến trình thử nghiệm không được lưu. Tắt để trở về hồ sơ thật.", 0, 320, 1010, 75, 23, UiKit.Muted);
            DevModeToggle.onValueChanged.AddListener(RequestDevMode);
            DevInvincibleToggle.onValueChanged.AddListener(DevMode.SetInvincible);
            DevNoCooldownToggle.onValueChanged.AddListener(DevMode.SetNoCooldown);
            VersionButton = kit.Button(parent, "Phiên bản " + Application.version, 750, 695, 320, 34, TapVersion, true, false, 17);
            VersionButton.name = "Version reveal";
            page.gameObject.SetActive(false);
            DevMode.Changed += RefreshDeveloperSettings;
            RefreshDeveloperSettings();
        }
        public void TapVersion() { VersionTaps = Mathf.Min(7, VersionTaps + 1); RefreshDeveloperSettings(); }
        void RefreshDeveloperSettings()
        {
            if (DeveloperTab == null) return;
            bool visible = DeveloperAvailable;
            DeveloperTab.gameObject.SetActive(visible);
            int count = visible ? Tabs.Length : Tabs.Length - 1;
            float width = 1024f / count;
            for (int i = 0; i < count; i++)
            {
                var button = TabLines[i].GetComponentInParent<Button>(true);
                if (button == null) continue;
                var r = (RectTransform)button.transform; r.anchoredPosition = new Vector2(48 + i * width, -163); r.sizeDelta = new Vector2(width - 10, 68);
                var label = button.GetComponentInChildren<TMP_Text>(true); label.rectTransform.sizeDelta = new Vector2(width - 26, 68); label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = 19; label.fontSize = 19; label.textWrappingMode = TextWrappingModes.Normal;
            }
            DevModeToggle.SetIsOnWithoutNotify(DevMode.Active);
            DevInvincibleToggle.SetIsOnWithoutNotify(DevMode.Invincible);
            DevNoCooldownToggle.SetIsOnWithoutNotify(DevMode.NoCooldown);
            DevInvincibleToggle.gameObject.SetActive(DevMode.Active);
            DevNoCooldownToggle.gameObject.SetActive(DevMode.Active);
            DevModeToggle.interactable = !DevMode.Changing;
        }
        public void RequestDevMode(bool enabled)
        {
            if (DevConfirmation != null || enabled == DevMode.Active) { RefreshDeveloperSettings(); return; }
            if (!enabled && !DevMode.InRun) { DevMode.SetActive(false); return; }
            var kit = UiKit.Create(); var parent = Tabs[0].transform.parent;
            var shade = kit.Rect(parent, "Dev Mode confirmation", 0, 0, 1120, 850); DevConfirmation = shade.gameObject;
            var image = shade.gameObject.AddComponent<Image>(); image.color = new Color(0, 0, 0, .92f); image.raycastTarget = true;
            kit.Panel(shade, "Comic confirmation", 72, 220, 976, 410);
            kit.Text(shade, enabled ? "DEV MODE" : "TẮT DEV MODE", 110, 250, 900, 58, 36, UiKit.Gold);
            kit.Text(shade, enabled ? DevWarning : "Tắt Dev Mode sẽ đưa bạn về Sảnh", 110, 326, 900, 150, 27);
            kit.Button(shade, "HỦY", 112, 520, 390, 72, CancelDevConfirmation);
            kit.Button(shade, "XÁC NHẬN", 620, 520, 390, 72, () => ConfirmDevMode(enabled), true, true);
        }
        public void CancelDevConfirmation() { if (DevConfirmation != null) Destroy(DevConfirmation); DevConfirmation = null; RefreshDeveloperSettings(); }
        public void ConfirmDevMode(bool enabled) { CancelDevConfirmation(); DevMode.SetActive(enabled); RefreshDeveloperSettings(); }
        void OnDestroy() { DevMode.Changed -= RefreshDeveloperSettings; }
    }
}
