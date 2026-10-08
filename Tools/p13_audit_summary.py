import json,collections
from pathlib import Path
d=json.loads(Path('Artifacts/SkyBeast/ShelterAudit.json').read_text())
print('Kinds',collections.Counter((m['kind'],m['actual']) for m in d['mismatches']))
groups=collections.defaultdict(list)
for m in d['mismatches']:groups[(m['building'],m['floor'])].append(m)
for key,values in groups.items():
    bounds=[(round(min(v['position'][a] for v in values),1),round(max(v['position'][a] for v in values),1)) for a in ['x','y','z']]
    print(key,len(values),bounds,[v['room'] for v in values[:2]])
