using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CampusRift.UI
{
    public sealed partial class SettingsUI : MonoBehaviour
    {
        public TMP_Dropdown Resolution, Quality;
        public TMP_Dropdown Language;
        public Toggle Fullscreen,VSync,InvertY;
        public Toggle ComicEffects;
        public Toggle ReduceSkillFlashes, ReduceCameraShake, LocalTelemetryToggle;
        TMP_Text comfortNote; Button deleteLocalData;
        public Toggle SubtitlesToggle, SlowReadingToggle;
        public TMP_Dropdown TextSizeDropdown, PaletteDropdown;
        public Slider[] Volumes;
        public TMP_Text[] VolumeValues;
        public Slider MouseSensitivity,CameraSensitivity,FieldOfView;
        public TMP_Text MouseValue,CameraValue,FovValue,Feedback;
        public Slider SkyBrightness;
        public TMP_Text SkyValue;
        public GameObject PCOptions,MobileOptions;
        public Slider TouchSensitivity,MobileScale,MobileOpacity;
        public TMP_Text TouchValue,ScaleValue,OpacityValue,PCModeLabel,MobileModeLabel;
        public Toggle AutoSprint,MobileHaptics,BoostToggle;
        public GameObject[] Tabs;
        public Image[] TabLines;
        public CampusRiftUITheme Theme;
        List<Vector2Int> resolutions=new List<Vector2Int>();
        public GameSettings Draft {get;private set;}
        void Awake()
        {
            BuildComicToggle();
            BuildReducedFlashToggle();
            BuildDeveloperSettings();
            BuildBoostToggle();
            // Slider.UpdateVisuals stretches horizontal handles across their parent height.
            // A positive Y sizeDelta then adds height beyond the track instead of setting it.
            foreach(var slider in GetComponentsInChildren<Slider>(true))
                if(slider.handleRect!=null&&(slider.direction==Slider.Direction.LeftToRight||slider.direction==Slider.Direction.RightToLeft))
                {
                    var size=slider.handleRect.sizeDelta;size.y=0;slider.handleRect.sizeDelta=size;
                }
            foreach(var d in new[]{Language,Resolution,Quality})
                if(d!=null&&d.captionText!=null){var r=d.captionText.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(20,16);r.offsetMax=new Vector2(-44,-9);d.captionText.textWrappingMode=TextWrappingModes.NoWrap;}
            foreach(var slider in Volumes)slider.onValueChanged.AddListener(delegate{UpdateLabels();});
            MouseSensitivity.onValueChanged.AddListener(delegate{UpdateLabels();});CameraSensitivity.onValueChanged.AddListener(delegate{UpdateLabels();});FieldOfView.onValueChanged.AddListener(delegate{UpdateLabels();});
            if(TouchSensitivity!=null){TouchSensitivity.onValueChanged.AddListener(delegate{UpdateLabels();});MobileScale.onValueChanged.AddListener(delegate{UpdateLabels();});MobileOpacity.onValueChanged.AddListener(delegate{UpdateLabels();});}
            if(SkyBrightness!=null)SkyBrightness.onValueChanged.AddListener(PreviewSky);
            if(Language!=null)Language.onValueChanged.AddListener(PreviewLanguage);
        }
        void BuildComicToggle()
        {
            if (ComicEffects != null || VSync == null) return;
            ComicEffects = Instantiate(VSync, VSync.transform.parent);
            ComicEffects.name = "Comic Effects";
            var rect = (RectTransform)ComicEffects.transform;
            rect.anchoredPosition += new Vector2(0,-154);
            var label = ComicEffects.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.text = LevelHUD.Vietnamese ? "VIỀN MỰC NHẸ (PC)" : "SUBTLE COMIC INK (PC)"; ComicTheme.Text(label); }
            ComicEffects.onValueChanged.RemoveAllListeners();
            ComicEffects.onValueChanged.AddListener(value => { if(Draft != null) { Draft.ComicEffects = value; ComicRendering.PreviewEnabled = value; } });
        }
        void BuildBoostToggle()
        {
            if(AutoSprint==null)return;
            if(BoostToggle==null)
            {
                BoostToggle=Instantiate(AutoSprint,AutoSprint.transform.parent);
                BoostToggle.name="Boost toggle";BoostToggle.onValueChanged.RemoveAllListeners();
            }
            // Compact two-column toggle rows below the three mobile sliders.
            foreach(var t in new[]{AutoSprint,BoostToggle,MobileHaptics})
            {
                var r=(RectTransform)t.transform;r.sizeDelta=new Vector2(500,60);
                var text=t.GetComponentInChildren<TMP_Text>(true);
                text.rectTransform.sizeDelta=new Vector2(420,60);text.enableAutoSizing=true;text.fontSizeMin=17;text.fontSizeMax=22;ComicTheme.Text(text);
                var mark=t.targetGraphic.rectTransform;mark.anchorMin=mark.anchorMax=new Vector2(1,.5f);mark.pivot=Vector2.one*.5f;mark.anchoredPosition=new Vector2(-26,0);
            }
            ((RectTransform)AutoSprint.transform).anchoredPosition=new Vector2(0,-195);
            ((RectTransform)BoostToggle.transform).anchoredPosition=new Vector2(520,-195);
            ((RectTransform)MobileHaptics.transform).anchoredPosition=new Vector2(0,-260);
            RefreshBoostText();
        }
        void RefreshBoostText()
        {
            if(BoostToggle==null)return;
            bool vi=Draft!=null?Draft.Language==Localization.GameLanguage.Vietnamese:LevelHUD.Vietnamese;
            AutoSprint.GetComponentInChildren<TMP_Text>(true).text=vi?"Tự Boost khi kéo hết cần":"AUTO BOOST AT JOYSTICK EDGE";
            BoostToggle.GetComponentInChildren<TMP_Text>(true).text=vi?"Boost kiểu bật/tắt":"TOGGLE BOOST";
        }
        void BuildReducedFlashToggle()
        {
            if(ReduceSkillFlashes!=null||InvertY==null||Tabs.Length<3)return;
            var kit=UiKit.Create();if(kit==null)return;
            var parent=Tabs[2].transform.parent;
            var comfort=kit.Rect(parent,"Comfort and local data",48,255,1024,415);
            var newTabs=Tabs.ToList();newTabs.Add(comfort.gameObject);Tabs=newTabs.ToArray();
            for(int i=0;i<3;i++)
            {
                var button=TabLines[i].GetComponentInParent<Button>();if(button==null)continue;
                var r=(RectTransform)button.transform;r.anchoredPosition=new Vector2(48+i*256,-163);r.sizeDelta=new Vector2(246,68);
                foreach(var t in button.GetComponentsInChildren<TMP_Text>()){var tr=(RectTransform)t.transform;tr.sizeDelta=new Vector2(220,68);t.enableAutoSizing=true;t.fontSizeMin=16;}
            }
            var fourth=kit.Button(parent,LevelHUD.Vietnamese?"TIỆN NGHI & DỮ LIỆU":"COMFORT & DATA",816,163,256,68,()=>SelectTab(3),true,false,20);
            var lines=TabLines.ToList();lines.Add(fourth.GetComponent<Image>());TabLines=lines.ToArray();
            Toggle Clone(string name,float y)
            {
                var t=Instantiate(InvertY,comfort);t.name=name;t.onValueChanged.RemoveAllListeners();
                var r=(RectTransform)t.transform;r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(1024,60);
                var label=t.GetComponentInChildren<TMP_Text>(true);label.fontSize=23;label.enableAutoSizing=true;label.fontSizeMin=17;((RectTransform)label.transform).sizeDelta=new Vector2(850,60);ComicTheme.Text(label);
                return t;
            }
            void PlaceMark(Toggle t)
            {foreach(Transform child in t.transform)if(child.GetComponent<Graphic>()!=null&&child.GetComponent<TMP_Text>()==null){var r=child as RectTransform;if(r==null)continue;r.anchorMin=r.anchorMax=new Vector2(1,.5f);r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(-25,0);r.sizeDelta=Vector2.one*40;}}
            ReduceCameraShake=Clone("Reduce camera shake",0);ReduceSkillFlashes=Clone("Reduce flashes",76);
            SubtitlesToggle=Clone("Important sound captions",152);SlowReadingToggle=Clone("Slow reading",228);
            foreach(var t in new[]{ReduceCameraShake,ReduceSkillFlashes,SubtitlesToggle,SlowReadingToggle})
            {((RectTransform)t.transform).sizeDelta=new Vector2(500,68);var l=t.GetComponentInChildren<TMP_Text>(true);l.rectTransform.sizeDelta=new Vector2(414,64);l.textWrappingMode=TextWrappingModes.Normal;l.fontSizeMax=22;l.fontSizeMin=19;PlaceMark(t);}
            TMP_Dropdown Drop(string name,float y)
            {var d=Instantiate(Language,comfort);d.name=name;d.onValueChanged.RemoveAllListeners();var r=(RectTransform)d.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(540,-y);r.sizeDelta=new Vector2(470,62);var label=d.captionText;if(label!=null){var lr=label.rectTransform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(22,16);lr.offsetMax=new Vector2(-48,-9);label.textWrappingMode=TextWrappingModes.NoWrap;label.fontSizeMax=22;label.fontSizeMin=17;}return d;}
            TextSizeDropdown=Drop("Text size",36);PaletteDropdown=Drop("Accessible palette",146);
            kit.Text(comfort,LevelHUD.Vietnamese?"CỠ CHỮ":"TEXT SIZE",540,0,470,35,20);
            kit.Text(comfort,LevelHUD.Vietnamese?"BẢNG MÀU & KÝ HIỆU":"PALETTE & SYMBOLS",540,110,470,35,20);
            LocalTelemetryToggle=Clone("Record local telemetry",224);var telemetryRect=(RectTransform)LocalTelemetryToggle.transform;telemetryRect.anchoredPosition=new Vector2(540,-224);telemetryRect.sizeDelta=new Vector2(470,68);var telemetryLabel=LocalTelemetryToggle.GetComponentInChildren<TMP_Text>(true);telemetryLabel.rectTransform.sizeDelta=new Vector2(384,64);telemetryLabel.textWrappingMode=TextWrappingModes.Normal;telemetryLabel.fontSizeMax=21;telemetryLabel.fontSizeMin=17;
            PlaceMark(LocalTelemetryToggle);
            comfortNote=kit.Text(comfort,"",0,315,500,95,17,UiKit.Muted);
            deleteLocalData=kit.Button(comfort,LevelHUD.Vietnamese?"XÓA DỮ LIỆU CỤC BỘ":"DELETE LOCAL DATA",540,336,470,68,()=>{
                bool ok=Progression.LocalTelemetry.Instance!=null&&Progression.LocalTelemetry.Instance.DeleteLocalFiles();
                Feedback.text=ok?(LevelHUD.Vietnamese?"Đã xóa dữ liệu chơi cục bộ.":"Local play data deleted."):(Progression.LocalTelemetry.Instance?.LastError??"No telemetry service.");
            },true,false,23);
            comfort.gameObject.SetActive(false);
        }
        void RefreshComfortText()
        {
            if(Draft==null||comfortNote==null)return;
                bool vn=Draft.Language==Localization.GameLanguage.Vietnamese;
                ReduceSkillFlashes.GetComponentInChildren<TMP_Text>(true).text=vn?"GIẢM NHẤP NHÁY":"REDUCE FLASHES";
                ReduceCameraShake.GetComponentInChildren<TMP_Text>(true).text=vn?"GIẢM RUNG CAMERA":"REDUCE CAMERA SHAKE";
                LocalTelemetryToggle.GetComponentInChildren<TMP_Text>(true).text=vn?"GHI DỮ LIỆU CHƠI CỤC BỘ (ĐỒNG Ý)":"RECORD LOCAL PLAY DATA (OPT IN)";
                SubtitlesToggle.GetComponentInChildren<TMP_Text>(true).text=vn?"PHỤ ĐỀ ÂM BÁO QUAN TRỌNG":"IMPORTANT SOUND CAPTIONS";
                SlowReadingToggle.GetComponentInChildren<TMP_Text>(true).text=vn?"THỜI GIAN LUYỆN TẬP ×1,5":"PRACTICE TIME ×1.5";
                comfortNote.text=vn?"Đọc chậm: giờ luyện tập ×1,5; bia không giới hạn giờ. Thi Đột Phá giữ giờ gốc. Telemetry mặc định tắt, chỉ lưu trên máy.":"Slow reading: practice time ×1.5; stele has no deadline. Exam time unchanged. Telemetry: off by default, local only.";
                TextSizeDropdown.ClearOptions();TextSizeDropdown.AddOptions(new List<string>(vn?new[]{"Chuẩn · 100%","Lớn · 115%","Rất lớn · 130%"}:new[]{"Normal · 100%","Large · 115%","Extra large · 130%"}));TextSizeDropdown.SetValueWithoutNotify(Draft.TextSize);
                PaletteDropdown.ClearOptions();PaletteDropdown.AddOptions(new List<string>(vn?new[]{"Mặc định","Mù màu · màu + hình"}:new[]{"Default","Color blind · color + shape"}));PaletteDropdown.SetValueWithoutNotify((int)Draft.AccessibleColors);
            TabLines[3].GetComponentInParent<Button>(true).GetComponentInChildren<TMP_Text>(true).text=vn?"TIỆN NGHI & DỮ LIỆU":"COMFORT & DATA";
            deleteLocalData.GetComponentInChildren<TMP_Text>(true).text=vn?"XÓA DỮ LIỆU CỤC BỘ":"DELETE LOCAL DATA";
        }
        void PreviewLanguage(int value)
        {if(Draft==null)return;Draft.Language=value==0?Localization.GameLanguage.English:Localization.GameLanguage.Vietnamese;Localization.LocalizationService.Instance?.Preview(Draft.Language);RefreshComfortText();RefreshBoostText();}
        void OnEnable() { if(UIStateManager.Instance!=null)UIStateManager.Instance.Changed+=StateChanged;BeginEditing(); }
        void StateChanged(UIState state)
        {if(state!=UIState.Settings){SettingsManager.Instance?.RestoreSkyPreview();Localization.LocalizationService.Instance?.Restore();}}
        void PreviewSky(float value)
        {if(Draft==null)return;Draft.SkyBrightness=value;SettingsManager.Instance?.PreviewSky(value);UpdateLabels();}
        public void BeginEditing()
        {
            if(SettingsManager.Instance==null)return;
            Draft=SettingsManager.Instance.Current.Copy();
            RefreshDeveloperSettings();
            if(BoostToggle!=null)BoostToggle.SetIsOnWithoutNotify(Draft.BoostToggle);
            RefreshBoostText();
            if(ComicEffects!=null)ComicEffects.SetIsOnWithoutNotify(Draft.ComicEffects);
            if(ReduceSkillFlashes!=null)
            {
                ReduceSkillFlashes.SetIsOnWithoutNotify(Draft.ReduceSkillFlashes);ReduceCameraShake.SetIsOnWithoutNotify(Draft.ReduceCameraShake);LocalTelemetryToggle.SetIsOnWithoutNotify(Draft.LocalTelemetryEnabled);
                SubtitlesToggle.SetIsOnWithoutNotify(Draft.Subtitles);SlowReadingToggle.SetIsOnWithoutNotify(Draft.SlowReading);
                RefreshComfortText();
            }
            if(ComicEffects!=null)ComicEffects.GetComponentInChildren<TMP_Text>(true).text=Draft.Language==Localization.GameLanguage.Vietnamese?"VIỀN MỰC NHẸ (PC)":"SUBTLE COMIC INK (PC)";
            ComicRendering.PreviewEnabled = null;
            if(Language!=null)Language.SetValueWithoutNotify(Draft.Language==Localization.GameLanguage.English?0:1);
            resolutions=Screen.resolutions.Select(r=>new Vector2Int(r.width,r.height)).Distinct().ToList();
            var selected=new Vector2Int(Draft.ResolutionWidth,Draft.ResolutionHeight);
            if(!resolutions.Contains(selected))resolutions.Add(selected);
            resolutions=resolutions.OrderBy(r=>r.x).ThenBy(r=>r.y).ToList();
            Resolution.ClearOptions();Resolution.AddOptions(resolutions.Select(r=>$"{r.x} × {r.y}").ToList());Resolution.SetValueWithoutNotify(resolutions.IndexOf(selected));
            Quality.ClearOptions();Quality.AddOptions(QualitySettings.names.ToList());Quality.SetValueWithoutNotify(Draft.Quality);
            Fullscreen.SetIsOnWithoutNotify(Draft.Fullscreen);VSync.SetIsOnWithoutNotify(Draft.VSync);InvertY.SetIsOnWithoutNotify(Draft.InvertY);
            float[] v={Draft.MasterVolume,Draft.MusicVolume,Draft.SFXVolume,Draft.MonsterVolume,Draft.UIVolume};
            for(int i=0;i<Volumes.Length;i++)Volumes[i].SetValueWithoutNotify(v[i]);
            MouseSensitivity.SetValueWithoutNotify(Draft.MouseSensitivity);CameraSensitivity.SetValueWithoutNotify(Draft.CameraSensitivity);FieldOfView.SetValueWithoutNotify(Draft.FieldOfView);
            if(SkyBrightness!=null)SkyBrightness.SetValueWithoutNotify(Draft.SkyBrightness);
            SettingsManager.Instance.RestoreSkyPreview();
            Feedback.text="";UpdateLabels();SelectTab(0);
            if(TouchSensitivity!=null){TouchSensitivity.SetValueWithoutNotify(Draft.TouchSensitivity);MobileScale.SetValueWithoutNotify(Draft.MobileControlScale);MobileOpacity.SetValueWithoutNotify(Draft.MobileOpacity);AutoSprint.SetIsOnWithoutNotify(Draft.JoystickAutoSprint);MobileHaptics.SetIsOnWithoutNotify(Draft.MobileHaptics);RefreshMode();UpdateLabels();}
        }
        public void ChoosePC(){if(Draft==null)return;Draft.ControlMode=Controls.ControlMode.PC;RefreshMode();}
        public void ChooseMobile(){if(Draft==null)return;Draft.ControlMode=Controls.ControlMode.Mobile;RefreshMode();}
        void RefreshMode()
        {
            bool mobile=Draft.ControlMode==Controls.ControlMode.Mobile;PCOptions.SetActive(!mobile);MobileOptions.SetActive(mobile);
            PCModeLabel.text="PC";MobileModeLabel.text=LevelHUD.Vietnamese?"CẢM ỨNG":"MOBILE";
            PCModeLabel.color=mobile?Theme.TextSecondary:Theme.SecondaryAccent;MobileModeLabel.color=mobile?Theme.SecondaryAccent:Theme.TextSecondary;
        }
        public void SelectVideo(){SelectTab(0);}public void SelectAudio(){SelectTab(1);}public void SelectGameplay(){SelectTab(2);}
        public void SelectTab(int tab)
        {
            for(int i=0;i<Tabs.Length;i++){Tabs[i].SetActive(i==tab);TabLines[i].color=i==tab?Theme.PrimaryAccent:Theme.DisabledColor;}
            var button=TabLines[tab].GetComponentInParent<Button>();
            if(button!=null && EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        void UpdateLabels()
        {
            for(int i=0;i<Volumes.Length;i++)VolumeValues[i].text=$"{Mathf.RoundToInt(Volumes[i].value*100)}%";
            MouseValue.text=$"{MouseSensitivity.value:0.00}×";CameraValue.text=$"{CameraSensitivity.value:0.00}×";FovValue.text=$"{FieldOfView.value:0}°";
            if(TouchSensitivity!=null){TouchValue.text=$"{TouchSensitivity.value:0.00}×";ScaleValue.text=$"{MobileScale.value:0.00}×";OpacityValue.text=$"{MobileOpacity.value*100:0}%";}
            if(SkyBrightness!=null && SkyValue!=null)SkyValue.text=SkyBrightness.value<=.01f?"NIGHT":SkyBrightness.value>=.99f?"DAY":$"{SkyBrightness.value*100:0}%";
        }
        public void Apply()
        {
            if(Language!=null)Draft.Language=Language.value==0?Localization.GameLanguage.English:Localization.GameLanguage.Vietnamese;
            Draft.MasterVolume=Volumes[0].value;Draft.MusicVolume=Volumes[1].value;Draft.SFXVolume=Volumes[2].value;Draft.MonsterVolume=Volumes[3].value;Draft.UIVolume=Volumes[4].value;
            Draft.MouseSensitivity=MouseSensitivity.value;Draft.CameraSensitivity=CameraSensitivity.value;Draft.FieldOfView=FieldOfView.value;Draft.InvertY=InvertY.isOn;
            Draft.Fullscreen=Fullscreen.isOn;Draft.VSync=VSync.isOn;Draft.Quality=Quality.value;
            if(ComicEffects!=null)Draft.ComicEffects=ComicEffects.isOn;
            if(BoostToggle!=null)Draft.BoostToggle=BoostToggle.isOn;
            if(ReduceSkillFlashes!=null){Draft.ReduceSkillFlashes=ReduceSkillFlashes.isOn;Draft.ReduceCameraShake=ReduceCameraShake.isOn;Draft.LocalTelemetryEnabled=LocalTelemetryToggle.isOn;Draft.TelemetryConsentAsked=true;}
            Draft.Subtitles=SubtitlesToggle.isOn;Draft.SlowReading=SlowReadingToggle.isOn;Draft.TextSize=TextSizeDropdown.value;Draft.AccessibleColors=(AccessiblePalette)PaletteDropdown.value;
            ComicRendering.PreviewEnabled = null;
            if(SkyBrightness!=null)Draft.SkyBrightness=SkyBrightness.value;
            if(TouchSensitivity!=null){Draft.TouchSensitivity=TouchSensitivity.value;Draft.MobileControlScale=MobileScale.value;Draft.MobileOpacity=MobileOpacity.value;Draft.JoystickAutoSprint=AutoSprint.isOn;Draft.MobileHaptics=MobileHaptics.isOn;}
            var res=resolutions[Resolution.value];Draft.ResolutionWidth=res.x;Draft.ResolutionHeight=res.y;
            SettingsManager.Instance.Apply(Draft);Feedback.text="SETTINGS SAVED";
        }
        public void Cancel(){SettingsManager.Instance?.RestoreSkyPreview();UIStateManager.Instance.Back();}
        void OnDisable()
        {
            CancelDevConfirmation();
            ComicRendering.PreviewEnabled = null;
            if(UIStateManager.Instance!=null)UIStateManager.Instance.Changed-=StateChanged;
            SettingsManager.Instance?.RestoreSkyPreview();
            Localization.LocalizationService.Instance?.Restore();
            if(Language!=null)Language.Hide();
            if(Resolution!=null)Resolution.Hide();if(Quality!=null)Quality.Hide();Draft=null;
        }
    }
}
