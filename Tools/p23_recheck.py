"""Preserve failed raw evidence before one explicit, justified related-suite recheck."""
from pathlib import Path
import json,shutil,sys
out=Path('Artifacts/V2/P23-regression');summary_path=out/'Summary.json'
summary=json.loads(summary_path.read_text(encoding='utf-8'));names=set(sys.argv[1:])
for row in summary:
 if row['name'] not in names:continue
 if row['status'] not in ('FAIL','ERROR','TIMEOUT'):raise RuntimeError('Refuse rechecking completed PASS '+row['name'])
 dst=Path('task/p23/diagnostics/recheck-before')/row['name']
 if dst.exists():
  attempt=2
  while (dst.parent/(row['name']+'-'+str(attempt))).exists():attempt+=1
  dst=dst.parent/(row['name']+'-'+str(attempt))
 shutil.copytree(out/'runs'/row['name'],dst)
 (dst/'original-summary-row.json').write_text(json.dumps(row,ensure_ascii=False,indent=2),encoding='utf-8')
summary_path.write_text(json.dumps([r for r in summary if r['name'] not in names],ensure_ascii=False,indent=2),encoding='utf-8')
print('Original failing rows/raw retained:',', '.join(names))
