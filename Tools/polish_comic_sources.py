from pathlib import Path
for name in ['HubPages.cs','LoadoutUI.cs','HubUI.cs']:
    path=Path('Assets/CampusRiftUI/Runtime')/name
    source=path.read_text(encoding='utf-8-sig')
    source=source.replace('fontStyle = FontStyles.Bold;', 'fontStyle = FontStyles.Bold | FontStyles.Italic | FontStyles.UpperCase;')
    path.write_text(source,encoding='utf-8')
