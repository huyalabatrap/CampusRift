from pathlib import Path
import json,re
out=Path('task/batch-1007/regression')
for folder in (out/'runs').glob('ShabanHunt-*'):
    p=folder/'row.json'
    if not p.exists():continue
    r=json.loads(p.read_text(encoding='utf-8'))
    result=(folder/'DONE.txt').read_text(encoding='utf-8-sig')
    match=re.search(r'RESULT .*reacquiredAt=(-?[\d.]+).*',result)
    if match and not r.get('consoleErrors'):
        if r.get('collectorCorrection'):continue
        (folder/'row-before-collector-correction.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')
        r['collectorCorrection']='Read RESULT from full DONE.txt; 5000-character display summary omitted it.'
        r['resultLine']=match[0];r['status']='PASS' if float(match[1])>=0 else 'FAIL'
        p.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')
        with (out.parent/'PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- Job7 collector: '+r['name']+' → '+r['status']+' từ RESULT đầy đủ; summary5000 ký tự trước đó bị cắt. Không chạy lại suite.\n')
        print(r['name'],r['status'],match[0])
