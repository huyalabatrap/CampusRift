from pathlib import Path
p=Path('Assets/CampusRiftUI/Runtime/SettingsUI.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('GetComponentInChildren<TMP_Text>()','GetComponentInChildren<TMP_Text>(true)');p.write_text(s,encoding='utf-8')
