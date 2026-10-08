"""Run Editor-dependent QA sequentially; each suite retains its own raw artifacts."""
import subprocess
import sys
from pathlib import Path

jobs = [
    ['Tools/p12_regressions.py'],
    ['Tools/p12_navigation_regressions.py'],
]
for args in jobs:
    print('QUEUE START ' + ' '.join(args), flush=True)
    result = subprocess.run([sys.executable, *args])
    print('QUEUE END ' + args[0] + ' exit=' + str(result.returncode), flush=True)
    if result.returncode:
        raise SystemExit(result.returncode)
Path('Artifacts/P12/QA-queue-DONE.txt').write_text('Finished runners; inspect every result, not an aggregate PASS.', encoding='utf-8')
