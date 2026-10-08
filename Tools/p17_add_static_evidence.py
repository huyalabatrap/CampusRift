import json,pathlib
out=pathlib.Path('Artifacts/V2/P17-regression');p=out/'Summary.json';rows=json.loads(p.read_text(encoding='utf-8'))
evidence={
 'SkillDefinitions':('PASS','SKILL DEFINITIONS PASS: 10 assets'),
 'EnemyData':('FAIL','ENEMY DATA FAIL: 12 archetypes; shaban-boss5: prefab lacks EnemyInstance/MinionBrain/MinionMotor; shaban-boss7: prefab lacks EnemyInstance/MinionBrain/MinionMotor; shaban-elite: prefab lacks EnemyInstance/MinionBrain/MinionMotor; tieu-yeu: duplicate id'),
 'LearningContent':('PASS','6 chapters / 21 lessons / 440 questions / 0 errors / 0 warnings; ReleaseContentGate.Check passed')
}
for name,(status,text) in evidence.items():
 assert name not in {r['name'] for r in rows};report=out/(name+'.txt');report.write_text(text+'\n',encoding='utf-8');rows.append(dict(name=name,status=status,summary=text,report=str(report),note='Captured from execute_code tool response; not re-invoked'))
p.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
