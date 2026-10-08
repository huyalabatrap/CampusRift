from pathlib import Path
path=Path('Assets/CampusRiftUI/Runtime/UiKit.cs')
source=path.read_text(encoding='utf-8-sig')
start=source.index('    // Scales its 1920')
fit=source[start:source.rfind('\n}')]
Path('Assets/CampusRiftUI/Runtime/FitFrame.cs').write_text('using UnityEngine;\n\nnamespace CampusRift.UI\n{\n'+fit+'\n}\n',encoding='utf-8')
path.write_text(source[:start]+'}\n',encoding='utf-8')
