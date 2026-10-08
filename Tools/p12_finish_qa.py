"""Sequential Editor QA, with independent runners and durable stdout for takeover."""
import subprocess
import sys
from pathlib import Path

jobs = [
    ['Tools/p12_run_tests.py', 'P12BossBalancePlayTest'],
    ['Tools/p12_regressions.py', 'MinionCombat', 'ReactionPlayTest', 'SkillSet1',
     'SkillSet1Edges', 'GiantHandSeal', 'PhantomDecoy', 'VoidWall', 'VoidWallQuickCast',
     'SkillLoadout', 'DamagePipeline', 'PlayerCombat', 'LightningFlash', 'HubFlow', 'HubLayout'],
    ['Tools/p12_navigation_regressions.py'],
    ['Tools/p12_run_tests.py', 'P12BalancePlayTest'],
]
root = Path('Artifacts/P12')
root.mkdir(parents=True, exist_ok=True)
with (root/'Finish-QA.log').open('a', encoding='utf-8', buffering=1) as log:
    for args in jobs:
        label = ' '.join(args)
        log.write('START ' + label + '\n')
        log.flush()
        print('QUEUE START ' + label, flush=True)
        result = subprocess.run([sys.executable, *args], stdout=log, stderr=subprocess.STDOUT)
        log.write('END exit=' + str(result.returncode) + '\n')
        print('QUEUE END exit=' + str(result.returncode), flush=True)
        if result.returncode:
            raise SystemExit(result.returncode)
    log.write('Runners finished; inspect all raw verdicts. This is not an aggregate PASS.\n')
