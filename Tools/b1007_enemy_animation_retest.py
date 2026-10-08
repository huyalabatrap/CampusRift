from b1007_retest import *
focused('EnemyAnimationPlayTest','CampusRift.Validation.EnemyAnimationPlayTest','t.FailedItemsOnly=true',
        'Artifacts/P12/tests/EnemyAnimationPlayTest.json','Artifacts/P12/tests/EnemyAnimationPlayTest-DONE.txt',90)
stop()
for source in (OUT/'runs/EnemyAnimationPlayTest/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/EnemyAnimationPlayTest/historical'))
