from b1007_retest import *
focused('P17Smoke','CampusRift.Validation.P17Smoke','t.FailedItemsOnly=true',
        'task/p17/smoke.json','task/p17/smoke-DONE.txt',80)
stop()
for source in (OUT/'runs/P17Smoke/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/P17Smoke/historical'))
