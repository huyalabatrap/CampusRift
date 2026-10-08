from pathlib import Path
import json
p=Path('Artifacts/SkyBeast/ShelterAudit.json');d=json.loads(p.read_text())
for m in d['mismatches']:
    m['reason']='Original Yard grid node lies under actual roof/building footprint. Keep Indoor for physical shelter; do not override it to match the graph label.' if m['kind']=='Outdoor' else 'Ground-floor observation at an open porch/edge. Partial shelter is physically correct; strict indoor expectation retained as a mismatch.'
d['acceptedReasonsDoNotChangePassCount']=True
p.write_text(json.dumps(d,indent=2,ensure_ascii=False),encoding='utf-8')
print(d['accuracy'],d['failed'],'mismatches explained; counts unchanged')
