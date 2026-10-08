from b1007_retest import *
focused('P17Related','CampusRift.Validation.P17RelatedSmoke','t.ContinueMusicOnly=true',
        'task/p17/related-smoke.json','task/p17/related-smoke-DONE.txt',80)
stop()
for source in (OUT/'runs/P17Related/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/P17Related/historical'))
