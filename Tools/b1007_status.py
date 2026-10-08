from pathlib import Path
import json,collections
out=Path('task/batch-1007/regression')
rows=[json.loads(p.read_text(encoding='utf-8')) for p in (out/'runs').glob('*/row.json')]
print('Completed:',len(rows),'initial',dict(collections.Counter(r['status'] for r in rows)))
final={r['name']:r['status'] for r in rows}
for p in (out/'retests').glob('*/row.json'):
    r=json.loads(p.read_text(encoding='utf-8'));final[r['name']]=r['status']
print('After focused retests:',dict(collections.Counter(final.values())))
for p in (out/'runs').glob('*/invoked.json'):
    if not (p.parent/'row.json').exists():print('RUNNING:',p.parent.name)
for r in rows:
    if final[r['name']]!='PASS':
        detail=r.get('failures',r.get('error',r.get('summary')))
        print(r['name'],final[r['name']],str(detail)[:650])
