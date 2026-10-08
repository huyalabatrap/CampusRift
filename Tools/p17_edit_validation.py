import pathlib,json,shutil,re
import unity_mcp as m
m.initialize();out=pathlib.Path('Artifacts/V2/P17-regression');summary_path=out/'Summary.json';rows=json.loads(summary_path.read_text(encoding='utf-8'))
bad=[r for r in rows if r['name'] in ('Cultivation','LevelData') and r['status']=='ERROR'];(out/'Runner-wrong-mode.json').write_text(json.dumps(bad,ensure_ascii=False,indent=2),encoding='utf-8')
assert len(bad)==2
rows=[r for r in rows if r not in bad]
for name,method,path in [('Cultivation','CultivationValidation.Validate()','Artifacts/Progression/Cultivation.txt'),('LevelData','LevelValidation.Validate()','Artifacts/Levels/LevelData.txt')]:
 raw=m.call('execute_code',{'action':'execute','code':'return '+method+';'})
 (out/(name+'-Edit-tool.json')).write_text(json.dumps(raw,ensure_ascii=False,indent=2),encoding='utf-8')
 r=raw['result']['structuredContent'];assert r['success'],r
 text=r['data']['result'];counts=re.search(r'(\d+) passed.*?(\d+) failed',text)
 assert counts,text
 p,f=map(int,counts.groups());shutil.copy2(path,out/(name+'.txt'));rows.append(dict(name=name,status='FAIL' if f else 'PASS',summary=text,passed=p,failed=f,report=str(out/(name+'.txt')),note='First actual assertions in Edit Mode; prior guard rejected before assertions'))
 print(name,text)
summary_path.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
with pathlib.Path('task/p17/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- Cultivation/LevelData gọi đúng Edit Mode lần assertion đầu; wrong-mode rejection trước assertions giữ Runner-wrong-mode.json.\n')
