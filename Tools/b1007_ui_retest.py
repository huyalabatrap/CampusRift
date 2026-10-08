from b1007_retest import *
focused('UIPlayAcceptance','UIPlayValidation','t.SkipCaptureMatrix=true;t.FailedItemsOnly=true',
        'Artifacts/UI/UI_TEST_REPORT.json','Artifacts/UI/UI_TEST_REPORT.json',240)
stop()
for source in (OUT/'runs/UIPlayAcceptance/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/UIPlayAcceptance/historical'))
