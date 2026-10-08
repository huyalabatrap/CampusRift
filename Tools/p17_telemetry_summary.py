"""Local-only telemetry aggregation. Standard library; no network or personal identifiers."""
import argparse,csv,json,pathlib,collections
p=argparse.ArgumentParser();p.add_argument('folder',type=pathlib.Path);p.add_argument('--out',type=pathlib.Path,default=pathlib.Path('Artifacts/V2/Telemetry-Summary.csv'));a=p.parse_args()
levels=collections.defaultdict(list);counts=collections.Counter();quiz=collections.defaultdict(list);bad=0
for file in sorted(a.folder.rglob('*.jsonl')):
    for line in file.read_text(encoding='utf-8-sig').splitlines():
        if not line.strip():continue
        try:
            r=json.loads(line)
            if r.get('schema')!=1 or r.get('kind') not in ('run','quiz'):raise ValueError('unsupported row')
            if r['kind']=='run':
                levels[int(r['level'])].append(r)
                for source,label in (('skills','skill'),('items','item'),('reactions','reaction')):
                    for value in r.get(source,[]):counts[label,value['id']]+=int(value['count'])
                counts['outcome',r.get('outcome','unknown')]+=1
                if r.get('deathCause'):counts['death_cause',r['deathCause']]+=r.get('deaths',0)
                counts['star_mask',str(r.get('stars',0))]+=1
            else:
                for answer in r.get('answers',[]):quiz[answer['id']].append(bool(answer['correct']))
        except (ValueError,TypeError,KeyError,json.JSONDecodeError):bad+=1
a.out.parent.mkdir(parents=True,exist_ok=True)
with a.out.open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.writer(f);w.writerow(('kind','id','samples','successes','mean_seconds','deaths','fire_hits'))
    for level,rows in sorted(levels.items()):w.writerow(('level',level,len(rows),sum(r.get('outcome')=='won' for r in rows),round(sum(r.get('seconds',0) for r in rows)/len(rows),2),sum(r.get('deaths',0) for r in rows),sum(r.get('fireHits',0) for r in rows)))
    for q,answers in sorted(quiz.items()):w.writerow(('question',q,len(answers),sum(answers),'','',''))
    for (kind,identity),count in sorted(counts.items()):w.writerow((kind,identity,count,'','','',''))
    w.writerow(('malformed','rows',bad,'','','',''))
print(str(a.out))
