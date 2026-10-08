from b1007_retest import *
row=json.loads((OUT/'runs/SkillSet1/row.json').read_text(encoding='utf-8'))
labels='new string[]{'+','.join(json.dumps(s) for s in row['failures'])+'}'
focused('SkillSet1','CampusRift.Skills.SkillSet1PlayTest','t.smokeOnly=false;t.LegacyFailedOnly=true;t.FailureLabels='+labels,
        'Artifacts/Skills/SkillSet1.json','Artifacts/Skills/SkillSet1-DONE.txt',160)
stop()
for source in (OUT/'runs/SkillSet1/historical').rglob('*'):
    if source.is_file():shutil.copy2(source,ROOT/source.relative_to(OUT/'runs/SkillSet1/historical'))
