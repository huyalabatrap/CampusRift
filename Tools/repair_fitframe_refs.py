from pathlib import Path
import re
meta=Path('Assets/CampusRiftUI/Runtime/FitFrame.cs.meta').read_text()
guid=re.search(r'^guid: (\w+)',meta,re.M).group(1)
for path in list(Path('Assets/Scenes').glob('*.unity'))+list(Path('Assets/CampusRiftUI/Prefabs').glob('*.prefab')):
    source=path.read_text(encoding='utf-8-sig')
    docs=re.split(r'(?=^--- !u!)',source,flags=re.M)
    ids=[];keep=[]
    for doc in docs:
        if doc.startswith('--- !u!115 ') and re.search(r'^  m_ClassName: FitFrame$',doc,re.M):
            ids.append(re.search(r'^--- !u!115 &(\d+)',doc).group(1))
        else: keep.append(doc)
    if not ids: continue
    source=''.join(keep)
    for ident in ids:
        source=source.replace('m_Script: {fileID: '+ident+'}', 'm_Script: {fileID: 11500000, guid: '+guid+', type: 3}')
    path.write_text(source,encoding='utf-8')
    print('Repaired FitFrame references:',path)
