"""Harvest already-launched harness after orchestrator stopped; never launches a test."""
import json,pathlib,shutil,sys,time
import unity_mcp as m
name,report,done=sys.argv[1:4];out=pathlib.Path('Artifacts/V2/P17-regression');p=pathlib.Path(report);marker=pathlib.Path(done)
summary=json.loads((out/'Summary.json').read_text(encoding='utf-8'))
assert name not in {x['name'] for x in summary},'Already harvested'
assert marker.exists() and marker.stat().st_mtime>time.time()-1800,'Missing fresh completion marker'
data=json.loads(p.read_text(encoding='utf-8-sig'));row=dict(name=name,status='FAIL' if data.get('failed') else 'PASS',summary=marker.read_text(encoding='utf-8-sig'),passed=data.get('passed'),failed=data.get('failed',[]),report=str(out/(name+'.json')),seconds=None,note='Harness launched once by initial orchestrator; harvested without relaunch')
shutil.copy2(p,out/(name+'.json'));shutil.copy2(marker,out/(name+'-DONE.txt'))
m.initialize();console=m.call('read_console',{'action':'get','types':['error','warning'],'count':100,'include_stacktrace':True});(out/(name+'-console.json')).write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
summary.append(row);(out/'Summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8');print(row['summary'])
with pathlib.Path('task/p17/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- T08 '+name+': '+row['status']+'; thu raw từ harness đã launch, không chạy lần hai.\n')
