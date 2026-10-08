from pathlib import Path
p=Path('Assets/CampusRiftUI/Runtime/SettingsUI.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('public Toggle ReduceSkillFlashes;', 'public Toggle ReduceSkillFlashes, ReduceCameraShake, LocalTelemetryToggle;\n        TMP_Text comfortNote;')
a=s.index('        void BuildReducedFlashToggle()');b=s.index('        void PreviewLanguage',a)
s=s[:a]+'''        void BuildReducedFlashToggle()
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
                var label=t.GetComponentInChildren<TMP_Text>();label.fontSize=23;label.enableAutoSizing=true;label.fontSizeMin=17;((RectTransform)label.transform).sizeDelta=new Vector2(850,60);ComicTheme.Text(label);
                return t;
            }
            ReduceCameraShake=Clone("Reduce camera shake",0);ReduceSkillFlashes=Clone("Reduce flashes",78);LocalTelemetryToggle=Clone("Record local telemetry",156);
            comfortNote=kit.Text(comfort,"",0,225,1000,104,20,UiKit.Muted);
            kit.Button(comfort,LevelHUD.Vietnamese?"XÓA DỮ LIỆU CỤC BỘ":"DELETE LOCAL DATA",0,340,490,68,()=>{
                bool ok=Progression.LocalTelemetry.Instance!=null&&Progression.LocalTelemetry.Instance.DeleteLocalFiles();
                Feedback.text=ok?(LevelHUD.Vietnamese?"Đã xóa dữ liệu chơi cục bộ.":"Local play data deleted."):(Progression.LocalTelemetry.Instance?.LastError??"No telemetry service.");
            },true,false,23);
            comfort.gameObject.SetActive(false);
        }
''' + s[b:]
s=s.replace('if(ReduceSkillFlashes!=null){ReduceSkillFlashes.SetIsOnWithoutNotify(Draft.ReduceSkillFlashes);ReduceSkillFlashes.GetComponentInChildren<TMP_Text>().text=Draft.Language==Localization.GameLanguage.Vietnamese?"GIẢM NHẤP NHÁY KỸ NĂNG":"REDUCE SKILL FLASHES";}', '''if(ReduceSkillFlashes!=null)
            {
                bool vn=Draft.Language==Localization.GameLanguage.Vietnamese;
                ReduceSkillFlashes.SetIsOnWithoutNotify(Draft.ReduceSkillFlashes);ReduceCameraShake.SetIsOnWithoutNotify(Draft.ReduceCameraShake);LocalTelemetryToggle.SetIsOnWithoutNotify(Draft.LocalTelemetryEnabled);
                ReduceSkillFlashes.GetComponentInChildren<TMP_Text>().text=vn?"GIẢM NHẤP NHÁY":"REDUCE FLASHES";
                ReduceCameraShake.GetComponentInChildren<TMP_Text>().text=vn?"GIẢM RUNG CAMERA":"REDUCE CAMERA SHAKE";
                LocalTelemetryToggle.GetComponentInChildren<TMP_Text>().text=vn?"GHI DỮ LIỆU CHƠI CỤC BỘ (ĐỒNG Ý)":"RECORD LOCAL PLAY DATA (OPT IN)";
                comfortNote.text=vn?"Mặc định tắt. Chỉ lưu thống kê trên máy, không gửi mạng và không có thông tin cá nhân. Có thể tắt hoặc xóa bất cứ lúc nào.":"Off by default. Statistics stay on this device, without network transfer or personal information. Disable or delete at any time.";
            }''')
s=s.replace('if(ReduceSkillFlashes!=null)Draft.ReduceSkillFlashes=ReduceSkillFlashes.isOn;', '''if(ReduceSkillFlashes!=null){Draft.ReduceSkillFlashes=ReduceSkillFlashes.isOn;Draft.ReduceCameraShake=ReduceCameraShake.isOn;Draft.LocalTelemetryEnabled=LocalTelemetryToggle.isOn;Draft.TelemetryConsentAsked=true;}''')
p.write_text(s,encoding='utf-8')
