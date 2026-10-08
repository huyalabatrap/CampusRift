from pathlib import Path
p=Path('Assets/CampusRiftUI/Runtime/SettingsUI.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('TMP_Text comfortNote;','TMP_Text comfortNote; Button deleteLocalData;')
s=s.replace('kit.Button(comfort,LevelHUD.Vietnamese?', 'deleteLocalData=kit.Button(comfort,LevelHUD.Vietnamese?')
s=s.replace('Localization.LocalizationService.Instance?.Preview(Draft.Language);}', 'Localization.LocalizationService.Instance?.Preview(Draft.Language);RefreshComfortText();}')
start=s.index('                bool vn=Draft.Language==Localization.GameLanguage.Vietnamese;')
end=s.index('\n            }',start)
block=s[start:end]
toggleline='                ReduceSkillFlashes.SetIsOnWithoutNotify(Draft.ReduceSkillFlashes);ReduceCameraShake.SetIsOnWithoutNotify(Draft.ReduceCameraShake);LocalTelemetryToggle.SetIsOnWithoutNotify(Draft.LocalTelemetryEnabled);'
assert toggleline in block
s=s[:start]+toggleline+'\n                RefreshComfortText();'+s[end:]
method='''        void RefreshComfortText()
        {
            if(Draft==null||comfortNote==null)return;
'''+block.replace(toggleline+'\n','')+'''
            TabLines[3].GetComponentInParent<Button>().GetComponentInChildren<TMP_Text>().text=vn?"TIỆN NGHI & DỮ LIỆU":"COMFORT & DATA";
            deleteLocalData.GetComponentInChildren<TMP_Text>().text=vn?"XÓA DỮ LIỆU CỤC BỘ":"DELETE LOCAL DATA";
        }
'''
s=s.replace('        void PreviewLanguage(int value)',method+'        void PreviewLanguage(int value)')
p.write_text(s,encoding='utf-8');print('Comfort labels update with language preview.')
