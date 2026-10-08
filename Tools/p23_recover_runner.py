"""Recover completed suite results after a collector-only exception, without re-running them."""
from pathlib import Path
import json,shutil
root=Path('Artifacts/V2/P23-regression');p=root/'Summary.json'
diag=Path('task/p23/diagnostics/console-collector');diag.mkdir(parents=True,exist_ok=True);shutil.copy2(p,diag/'Summary-before-recovery.json')
rows=json.loads(p.read_text(encoding='utf-8'))
for row in rows:
 if row.get('exception')!="'str' object has no attribute 'get'":continue
 folder=root/'runs'/row['name']
 if row['name']=='MobileControls':
  result=json.loads((folder/'result.json').read_text(encoding='utf-8-sig'));assert not result['failed'];row['status']='PASS'
 elif row['name']=='MobileChase':
  text=(folder/'result.txt').read_text(encoding='utf-8-sig');assert '=False' not in text and text.count('=True')==5;row['status']='PASS';row['passed']=5;row['failed']=0
 else:continue
 row['collectorException']=row.pop('exception');row['consoleErrors']=0;row['consoleWarnings']=5
 row['collectorRecovery']='Original raw had 5 identical NavMesh prewarm warnings. Detailed follow-up console on fresh SampleScene identifies all five as Warning in SoulSummonRuntime.cs:25; see task/p23/diagnostics/console-collector/typed-warning.json. Functional suite did not run twice.'
p.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8');print('Recovered',len(rows),'completed results')
