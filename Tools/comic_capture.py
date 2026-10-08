"""Run review captures sequentially and abort when Unity reports an error."""
import json,sys,time
from pathlib import Path
import unity_mcp as m
m.initialize()
log=[]
for name in sys.argv[1:]:
    r=m.call('execute_code',{'action':'execute','code':Path('task/ui-comic/'+name+'.cs').read_text(encoding='utf-8-sig')})
    data=r.get('result',{}).get('structuredContent')
    if data is None:
        data=json.loads(r['result']['content'][0]['text'])
    if 'result' in data and isinstance(data['result'],dict): data=data['result']
    log.append({'script':name,'response':data})
    print(name+': '+str(data.get('success'))+' '+str(data.get('message','')),flush=True)
    if not data.get('success'): break
    time.sleep(.4)
folder=Path('task/ui-comic/screens/round1');folder.mkdir(parents=True,exist_ok=True)
(folder/('capture-log-'+sys.argv[1]+'.json')).write_text(json.dumps(log,indent=2,ensure_ascii=False),encoding='utf-8')
if not log[-1]['response'].get('success'): sys.exit(1)
