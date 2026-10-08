"""Wait for an already launched harness; harvest it and resume unfinished cases only."""
import pathlib,subprocess,sys,time
p=pathlib.Path('Artifacts/Skills/SkillSet1-Pool-DONE.txt')
deadline=time.time()+650
while time.time()<deadline:
    if p.exists() and p.stat().st_mtime>1780720000 and p.stat().st_mtime>time.time()-800:
        break
    time.sleep(1)
else:
    raise RuntimeError('Already launched SkillSet1Pool did not complete; do not relaunch')
subprocess.run([sys.executable,'Tools/p17_harvest.py','SkillSet1Pool','Artifacts/Skills/SkillSet1-Pool.json',str(p)],check=True)
subprocess.run([sys.executable,'Tools/p17_regressions.py'],check=True)
