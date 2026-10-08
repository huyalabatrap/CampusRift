from b1007_retest import *
focused('SkyBeastPresencePlayTest','CampusRift.Validation.SkyBeastPresencePlayTest','t.PrimaryOnly=true',
        'Artifacts/P12/tests/SkyBeastPresencePlayTest.json','Artifacts/P12/tests/SkyBeastPresencePlayTest-DONE.txt',80)
stop()
for source in (OUT/'runs/SkyBeastPresencePlayTest/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/SkyBeastPresencePlayTest/historical'))
