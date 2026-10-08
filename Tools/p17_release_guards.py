from pathlib import Path
import re,json
changed=[]
for p in Path('Assets').rglob('*.cs'):
    s=p.read_text(encoding='utf-8-sig');new=re.sub(r'#if UNITY_EDITOR \|\| (P\d+_BENCH)',r'#if UNITY_EDITOR || (DEVELOPMENT_BUILD && \1)',s)
    if new!=s:p.write_text(new,encoding='utf-8');changed.append(str(p))
Path('task/p17/release-guard-edits.json').write_text(json.dumps(changed,indent=2),encoding='utf-8')
print('Benchmark guards require DEVELOPMENT_BUILD:',len(changed))
