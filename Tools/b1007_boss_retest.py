from b1007_retest import *
focused('ShabanBossPlayTest','CampusRift.Validation.ShabanBossPlayTest','t.FailedItemsOnly=true',
        'Artifacts/P12/tests/ShabanBossPlayTest.json','Artifacts/P12/tests/ShabanBossPlayTest-DONE.txt',80)
stop()
for source in (OUT/'runs/ShabanBossPlayTest/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/ShabanBossPlayTest/historical'))
