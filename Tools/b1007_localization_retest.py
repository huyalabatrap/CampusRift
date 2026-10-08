from b1007_retest import *
focused('Localization','CampusRift.Localization.LocalizationPlayTest','t.CurriculumOnly=true',
        'Artifacts/Localization/PlayMode.json','Artifacts/Localization/DONE.txt',80)
stop()
for source in (OUT/'runs/Localization/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/Localization/historical'))
