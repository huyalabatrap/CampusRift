"""Read complete supplied reports and retain concise metadata for gameplay integration."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
rows = []
for family in ('GameReadyModels', 'GameReadyDragons'):
    for path in sorted((root / 'Assets' / family).glob('*/report.json')):
        raw = json.loads(path.read_text(encoding='utf-8-sig'))
        row = {k: v for k, v in raw.items() if k != 'clips'}
        row['clips'] = [{k: v for k, v in c.items() if k != 'quality'} for c in raw['clips']]
        row['quality_samples'] = sum(len(c.get('quality', [])) for c in raw['clips'])
        row['source'] = str(path.relative_to(root))
        rows.append(row)
out = root / 'Artifacts' / 'P12'
out.mkdir(parents=True, exist_ok=True)
(out / 'SourceMetadata.json').write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding='utf-8')
for r in rows:
    print(r['id'], r['name'], r.get('height_m'), [(c['name'], c.get('seconds'), c['loop']) for c in r['clips']])
