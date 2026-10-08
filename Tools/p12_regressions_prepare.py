from pathlib import Path
p=Path('Tools/p11_run_tests.py').read_text(encoding='utf-8')
p=p.replace("Artifacts/Reactions/regressions","Artifacts/P12/regressions").replace('P11 regression','P12 regression')
p=p.replace("('ReactionPlayTest'", "('MinionCombat','CampusRift.Enemies.MinionCombatPlayTest','Artifacts/Enemies/MinionCombat.json','Artifacts/Enemies/MinionCombat-DONE.txt',400),\n('ShabanCombat','ShabanCombatPlayTest','Artifacts/Combat/Validation.json','Artifacts/Combat/DONE.txt',350),\n('ShabanPressure','ShabanPressurePlayTest','Artifacts/ShabanPressure/Validation.json','Artifacts/ShabanPressure/DONE.txt',400),\n('ShabanBehavior','ShabanBehaviorPlayTest','Assets/MonsterShaban/Validation/BehaviorPlayMode.json','Temp/shaban-behavior-progress.txt',450),\n('ReactionPlayTest'")
p=p.replace("if path.exists() and path.stat().st_mtime>=started:break", "if path.exists() and path.stat().st_mtime>=started and (name!='ShabanBehavior' or path.read_text().startswith('DONE')):break")
Path('Tools/p12_regressions.py').write_text(p,encoding='utf-8')
