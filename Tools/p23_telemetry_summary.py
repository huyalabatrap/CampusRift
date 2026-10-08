"""Aggregate explicitly supplied local telemetry. Never treats QA rows as player evidence."""
import argparse,collections,csv,json,statistics,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('--out',type=Path,default=Path('Artifacts/V2/P23-telemetry'));a=p.parse_args()
if not a.folder.is_dir():p.error('Input folder does not exist; specify the supplied telemetry folder.')
a.out.mkdir(parents=True,exist_ok=True);groups=collections.defaultdict(list);answers=collections.defaultdict(list);bad=[];seen=set();duplicates=0
for file in sorted(a.folder.rglob('*.jsonl')):
 for number,line in enumerate(file.read_text(encoding='utf-8-sig').splitlines(),1):
  if not line.strip():continue
  try:
   r=json.loads(line)
   if r.get('schema')!=1 or r.get('kind') not in ('run','quiz'):raise ValueError('unsupported schema/kind')
   key=json.dumps(r,sort_keys=True)
   if key in seen:duplicates+=1;continue
   seen.add(key)
   if r['kind']=='run':
    mode=r.get('mode') or 'Normal';level=int(r['level']);floor=int(r.get('towerFloor') or 0)
    if mode not in ('Normal','Tower','Nightmare') or not 1<=level<=10 or (mode=='Tower' and floor<1):raise ValueError('mode/level/floor')
    duration=float(r.get('seconds') or 0)
    if not math.isfinite(duration) or not 0<=duration<86400:raise ValueError('duration')
    for counter in ('deaths','fireHits'):
     if not isinstance(r.get(counter,0),int) or isinstance(r.get(counter,0),bool) or r.get(counter,0)<0:raise ValueError(counter+' must be a non-negative integer')
    groups[mode,floor if mode=='Tower' else level].append(r)
   else:
    rows=r.get('answers',[])
    if not isinstance(rows,list) or any(not isinstance(row,dict) or not isinstance(row.get('id'),str) or not isinstance(row.get('correct'),bool) for row in rows):raise ValueError('answer id/boolean correct')
    for row in rows:
     answers[row['id']].append(row['correct'])
  except (ValueError,TypeError,KeyError,json.JSONDecodeError) as e:bad.append({'file':str(file),'line':number,'reason':str(e)})
with (a.out/'runs.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.writer(f);w.writerow(['mode','level_or_floor','samples','wins','win_percent','median_seconds','mean_seconds','deaths','fire_hits'])
 for (mode,level),rows in sorted(groups.items()):
  wins=sum(r.get('outcome')=='won' for r in rows);secs=[float(r.get('seconds') or 0) for r in rows]
  w.writerow([mode,level,len(rows),wins,round(100*wins/len(rows),1),round(statistics.median(secs),2),round(statistics.mean(secs),2),sum(r.get('deaths',0) for r in rows),sum(r.get('fireHits',0) for r in rows)])
with (a.out/'questions.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.writer(f);w.writerow(['question_id','answers','correct','accuracy_percent'])
 for q,rows in sorted(answers.items()):w.writerow([q,len(rows),sum(rows),round(100*sum(rows)/len(rows),1)])
manifest={'input':str(a.folder.resolve()),'runSamples':sum(map(len,groups.values())),'answerSamples':sum(map(len,answers.values())),'duplicatesIgnored':duplicates,'malformed':bad,'normalMissing':[i for i in range(1,11) if ('Normal',i) not in groups],'nightmareMissing':[i for i in range(1,11) if ('Nightmare',i) not in groups],'towerFloors':sorted(level for mode,level in groups if mode=='Tower'),'limits':['mode absent in legacy telemetry is grouped Normal; historical P22 modes cannot be inferred','kills/hits-per-enemy and boss combat time require observation/Profiler; not in schema1','sample provenance requires human confirmation; no auto certification']}
(a.out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(manifest,ensure_ascii=True))
