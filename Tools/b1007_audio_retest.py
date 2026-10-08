from b1007_retest import *
focused('P12AudioEndingPlayTest','CampusRift.Validation.P12AudioEndingPlayTest','t.CompletionOnly=true',
        'Artifacts/P12/tests/P12AudioEndingPlayTest.json','Artifacts/P12/tests/P12AudioEndingPlayTest-DONE.txt',80)
stop()
for source in (OUT/'runs/P12AudioEndingPlayTest/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/P12AudioEndingPlayTest/historical'))
