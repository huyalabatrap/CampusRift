from b1007_retest import *
focused('P23Gameplay','CampusRift.UI.P23GameplaySmoke','t.LargeMobileOnly=true',
        'task/p23/gameplay.json','task/p23/gameplay-DONE.txt',90)
stop()
for source in (OUT/'runs/P23Gameplay/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/P23Gameplay/historical'))
