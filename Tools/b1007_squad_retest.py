from b1007_retest import *
focused('SquadIntegrationSmoke','CampusRift.Validation.SquadIntegrationSmoke','t.AdaptationOnly=true',
        'Artifacts/P12/tests/SquadIntegrationSmoke.json','Artifacts/P12/tests/SquadIntegrationSmoke-DONE.txt',95)
stop()
for source in (OUT/'runs/SquadIntegrationSmoke/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/SquadIntegrationSmoke/historical'))
