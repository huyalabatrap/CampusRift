from pathlib import Path
import hashlib,json
p=Path('Artifacts/Skills/fix3/PreEdit.json');before=json.loads(p.read_text(encoding='utf-8'));root=Path(before['backup'])
result=[]
paths=list(before['data'])+['Assets/Combat/Runtime/ReactionResolver.cs','Assets/Combat/Runtime/GenerationChainTracker.cs','Assets/Combat/Runtime/StatusEffectHost.cs']
for name in paths:
    source=Path(name);original=root/source
    if not source.exists() or not original.exists():raise RuntimeError(name)
    a=hashlib.sha256(original.read_bytes()).hexdigest();b=hashlib.sha256(source.read_bytes()).hexdigest()
    result.append(dict(file=source.as_posix(),before=a,after=b,unchanged=a==b))
output=dict(passed=all(r['unchanged'] for r in result),files=result)
Path('Artifacts/Skills/fix3/GameplayDataAudit.json').write_text(json.dumps(output,indent=2),encoding='utf-8')
print(str(len(result))+' unchanged: '+str(output['passed']))
assert output['passed']
